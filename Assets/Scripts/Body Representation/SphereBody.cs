using UnityEngine;
using System.Collections.Generic;

public class SphereBody : MonoBehaviour
{
    [Header("Sphere Parameters")]
    [Range(0.1f, 5f)]
    public float radius = 0.75f;                // Radius of the sphere

    [Header("Particle Generation")]
    public SphereGenerationMethod generationMethod = SphereGenerationMethod.FibonacciSphere_Surface;
    [Range(3, 15)]
    public int resolution = 5;                  // Controls particle density
    public bool addCenterParticleForFibonacci = true; // Add a central particle for Fibonacci method

    [Header("Physics Properties")]
    [Range(0.01f, 10f)]
    public float totalMass = 1f;                // Total mass distributed across particles
    [Range(0.01f, 1.0f)]
    public float surfaceStiffness = 0.5f;       // Stiffness for surface springs
    [Range(0.01f, 1.0f)]
    public float volumeStiffness = 0.8f;        // Stiffness for volume springs (e.g., to center)

    [Header("Spring Connectivity")]
    [Range(1.1f, 3.0f)]
    public float connectionRadiusMultiplier = 2.0f; // Multiplier for spring connection distance

    private Body body;
    public int centerParticlePhysicsIndex = -1; // Index of the center particle, if added

    public enum SphereGenerationMethod
    {
        VolumetricGrid,
        FibonacciSphere_Surface
    }

    void Start()
    {
        body = GetComponent<Body>();
        GenerateSphere();
    }

    [ContextMenu("Re-Generate Sphere")]
    public void RegenerateSphere()
    {
        if (!Application.isPlaying)
        {
            // Debug.LogWarning("Can only regenerate sphere in Play Mode.");
            return;
        }
        if (body == null) body = GetComponent<Body>();
        GenerateSphere();
    }

    void GenerateSphere()
    {
        if (body == null) return;

        // Clear existing particles and constraints
        body.particles.Clear();
        body.constraints.Clear();
        centerParticlePhysicsIndex = -1;

        List<Particle> localParticleList = new List<Particle>();

        // Generate particles based on the selected method
        switch (generationMethod)
        {
            case SphereGenerationMethod.VolumetricGrid:
                GenerateVolumetricGridParticles(localParticleList);
                break;
            case SphereGenerationMethod.FibonacciSphere_Surface:
                int numPoints = 10 + (resolution * resolution * resolution / 2);
                if (resolution <= 3) numPoints = 10 + resolution * 5;
                else if (resolution <= 5) numPoints = 20 + resolution * 8;
                else numPoints = 40 + resolution * 10;
                GenerateFibonacciSphereParticles(localParticleList, numPoints);
                break;
        }

        if (localParticleList.Count == 0)
        {
            // Debug.LogError($"SphereBody '{gameObject.name}' generated no particles.", this);
            return;
        }

        // Distribute total mass across particles
        float massPerParticle = totalMass / localParticleList.Count;
        foreach (var p in localParticleList)
        {
            p.mass = massPerParticle;
            p.body = this.GetComponent<Body>();             // Set body reference
            p.collisionRadius = radius / resolution;       // Set collision radius
            body.particles.Add(p);                         // Add to body
        }

        // Add an optional center particle for Fibonacci sphere
        if (generationMethod == SphereGenerationMethod.FibonacciSphere_Surface &&
            addCenterParticleForFibonacci && body.particles.Count > 0)
        {
            Vector3 centerWorldPos = transform.TransformPoint(Vector3.zero);
            Particle centerP = new Particle(centerWorldPos, massPerParticle * 2f, false);
            centerP.body = this.GetComponent<Body>();       // Set body reference
            centerP.collisionRadius = radius / resolution;  // Set collision radius
            body.particles.Add(centerP);
            centerParticlePhysicsIndex = body.particles.Count - 1;

            // Connect center particle to all surface particles
            for (int i = 0; i < centerParticlePhysicsIndex; i++)
            {
                body.constraints.Add(new DistanceConstraint(
                    body.particles[i],
                    centerP,
                    volumeStiffness));
            }
        }

        // Connect surface particles with springs
        ConnectSurfaceParticles();

        // Debug.Log($"SphereBody '{gameObject.name}': {body.particles.Count} particles " +
                //   $"(CenterIdx: {centerParticlePhysicsIndex}), {body.constraints.Count} constraints.");
    }

    void ConnectSurfaceParticles()
    {
        if (body.particles.Count <= 1) return;

        // Determine the number of surface particles (exclude center if present)
        int numSurfaceParticles = (centerParticlePhysicsIndex != -1)
            ? centerParticlePhysicsIndex
            : body.particles.Count;
        if (numSurfaceParticles <= 1) return;

        // Estimate average spacing and set connection distance
        float estimatedSpacing = Mathf.Sqrt((4 * Mathf.PI * radius * radius) / numSurfaceParticles) * 0.8f;
        float connectionDistance = estimatedSpacing * connectionRadiusMultiplier;

        // Add springs between nearby surface particles
        for (int i = 0; i < numSurfaceParticles; i++)
        {
            for (int j = i + 1; j < numSurfaceParticles; j++)
            {
                if (Vector3.Distance(body.particles[i].position, body.particles[j].position) < connectionDistance)
                {
                    body.constraints.Add(new DistanceConstraint(
                        body.particles[i],
                        body.particles[j],
                        surfaceStiffness));
                }
            }
        }
    }

    private void GenerateVolumetricGridParticles(List<Particle> particleList)
    {
        float step = radius / (float)resolution;
        for (int i = -resolution; i <= resolution; i++)
        {
            for (int j = -resolution; j <= resolution; j++)
            {
                for (int k = -resolution; k <= resolution; k++)
                {
                    Vector3 offset = new Vector3(i, j, k) * step;
                    if (offset.magnitude <= radius * 1.05f) // Slightly larger to ensure full coverage
                    {
                        Vector3 worldPos = transform.TransformPoint(offset);
                        Particle p = new Particle(worldPos, 1f);
                        p.body = this.GetComponent<Body>();         // Set body reference
                        p.collisionRadius = radius / resolution;    // Set collision radius
                        particleList.Add(p);
                    }
                }
            }
        }
    }

    private void GenerateFibonacciSphereParticles(List<Particle> particleList, int numPoints)
    {
        if (numPoints <= 0) numPoints = 20;
        float goldenAngle = Mathf.PI * (3f - Mathf.Sqrt(5f));

        for (int i = 0; i < numPoints; i++)
        {
            float y = 1 - (i / (float)(numPoints - 1)) * 2;        // Distribute along y-axis
            float rAtY = Mathf.Sqrt(1 - y * y);                     // Radius at this y-level
            float theta = goldenAngle * i;                          // Angle using golden ratio

            Vector3 localPos = new Vector3(
                Mathf.Cos(theta) * rAtY,
                y,
                Mathf.Sin(theta) * rAtY) * radius;
            Vector3 worldPos = transform.TransformPoint(localPos);
            Particle p = new Particle(worldPos, 1f);
            p.body = this.GetComponent<Body>();         // Set body reference
            p.collisionRadius = radius / resolution;    // Set collision radius
            particleList.Add(p);
        }
    }
}