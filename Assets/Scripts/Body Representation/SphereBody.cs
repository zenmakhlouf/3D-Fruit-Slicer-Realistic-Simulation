using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generates particles and constraints for a sphere-shaped soft body.
/// </summary>
[RequireComponent(typeof(Body))]
public class SphereBody : MonoBehaviour
{
    [Header("Sphere Parameters")]
    [Range(0.1f, 5f)]
    public float radius = 0.75f;

    [Header("Particle Generation")]
    public SphereGenerationMethod generationMethod = SphereGenerationMethod.FibonacciSphere_Surface;
    [Range(3, 15)]
    public int resolution = 5; // Influences particle count and distribution
    public bool addCenterParticleForFibonacci = true;

    [Header("Physics Properties")]
    [Range(0.01f, 10f)]
    public float totalMass = 1f;
    [Range(0.01f, 1.0f)]
    public float surfaceStiffness = 0.5f; // Stiffness for springs connecting surface particles
    [Range(0.01f, 1.0f)]
    public float volumeStiffness = 0.8f;  // Stiffness for springs connecting to a center particle

    [Header("Spring Connectivity")]
    [Range(1.1f, 3.0f)]
    public float connectionRadiusMultiplier = 2.0f; // Factor for determining spring connection distance

    private Body body;
    [System.NonSerialized] // Not saved, set at runtime
    public int centerParticlePhysicsIndex = -1; // Index of the center particle, if used. -1 if none.

    public enum SphereGenerationMethod
    {
        VolumetricGrid,         // Particles arranged in a grid, then culled to sphere shape
        FibonacciSphere_Surface // Particles evenly distributed on the sphere's surface
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
            Debug.LogWarning("Can only regenerate sphere in Play Mode.");
            return;
        }
        if (body == null) body = GetComponent<Body>();
        GenerateSphere();
    }

    void GenerateSphere()
    {
        if (body == null) return;

        body.particles.Clear();
        body.constraints.Clear();
        centerParticlePhysicsIndex = -1;

        List<Particle> localParticleList = new List<Particle>();

        switch (generationMethod)
        {
            case SphereGenerationMethod.VolumetricGrid:
                GenerateVolumetricGridParticles(localParticleList);
                break;
            case SphereGenerationMethod.FibonacciSphere_Surface:
                int numPoints = 10 + (resolution * resolution * resolution / 2); // Heuristic for point count
                if (resolution <= 3) numPoints = 10 + resolution * 5;
                else if (resolution <= 5) numPoints = 20 + resolution * 8;
                else numPoints = 40 + resolution * 10;
                GenerateFibonacciSphereParticles(localParticleList, numPoints);
                break;
        }

        if (localParticleList.Count == 0)
        {
            Debug.LogError($"SphereBody '{gameObject.name}' generated no particles.", this);
            return;
        }

        float massPerParticle = (localParticleList.Count > 0) ? totalMass / localParticleList.Count : totalMass;
        foreach (var p in localParticleList)
        {
            p.mass = massPerParticle;
            body.particles.Add(p);
        }

        if (generationMethod == SphereGenerationMethod.FibonacciSphere_Surface && addCenterParticleForFibonacci && body.particles.Count > 0)
        {
            Vector3 centerWorldPos = transform.TransformPoint(Vector3.zero);
            Particle centerP = new Particle(centerWorldPos, massPerParticle * 2f, radius * 0.5f, false); // Center particle can be heavier
            body.particles.Add(centerP);
            centerParticlePhysicsIndex = body.particles.Count - 1;

            for (int i = 0; i < centerParticlePhysicsIndex; i++)
            { // Connect surface particles to center
                body.constraints.Add(new DistanceConstraint(body.particles[i], centerP, volumeStiffness));
            }
        }

        ConnectSurfaceParticles();

        Debug.Log($"SphereBody '{gameObject.name}': {body.particles.Count} particles (CenterIdx: {centerParticlePhysicsIndex}), {body.constraints.Count} constraints.");
    }

    void ConnectSurfaceParticles()
    {
        if (body.particles.Count <= 1) return;

        int numSurfaceParticlesToConsider = (centerParticlePhysicsIndex != -1) ? centerParticlePhysicsIndex : body.particles.Count;
        if (numSurfaceParticlesToConsider <= 1) return;

        float estimatedSpacing = Mathf.Sqrt((4 * Mathf.PI * radius * radius) / numSurfaceParticlesToConsider) * 0.8f;
        float connectionDistance = estimatedSpacing * connectionRadiusMultiplier;

        for (int i = 0; i < numSurfaceParticlesToConsider; i++)
        {
            for (int j = i + 1; j < numSurfaceParticlesToConsider; j++)
            {
                if (Vector3.Distance(body.particles[i].position, body.particles[j].position) < connectionDistance)
                {
                    body.constraints.Add(new DistanceConstraint(body.particles[i], body.particles[j], surfaceStiffness));
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
                    if (offset.magnitude <= radius * 1.05f)
                    { // Include particles slightly outside for better surface
                        Vector3 worldPos = transform.TransformPoint(offset);
                        particleList.Add(new Particle(worldPos, 1f)); // Mass set later
                    }
                }
            }
        }
    }

    private void GenerateFibonacciSphereParticles(List<Particle> particleList, int numPoints)
    {
        if (numPoints <= 0) numPoints = 20; // Min points
        float goldenAngle = Mathf.PI * (3f - Mathf.Sqrt(5f));

        for (int i = 0; i < numPoints; i++)
        {
            float y = 1 - (i / (float)(numPoints - 1)) * 2;  // y from 1 down to -1
            float rAtY = Mathf.Sqrt(1 - y * y);              // Radius of circle at this y
            float theta = goldenAngle * i;

            Vector3 localPos = new Vector3(Mathf.Cos(theta) * rAtY, y, Mathf.Sin(theta) * rAtY) * radius;
            Vector3 worldPos = transform.TransformPoint(localPos);
            particleList.Add(new Particle(worldPos, 1f)); // Mass set later
        }
    }
}
