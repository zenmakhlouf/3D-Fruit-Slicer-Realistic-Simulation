using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Body))]
public class HammerBody : MonoBehaviour
{
    [Header("Hammer Generation")]
    public float handleLength = 2f;
    public float handleRadius = 0.1f;
    public float headRadius = 0.3f;
    public float headHeight = 0.4f;
    public int handleResolution = 6;
    public int headResolution = 8;
    public float totalMass = 3f;

    [Header("Hammer Physics")]
    public float surfaceStiffness = 0.9f;
    public float volumeStiffness = 0.7f;
    public float connectionRadiusMultiplier = 2.0f;

    [Header("Mouse Control")]
    public float mouseSensitivity = 8f;
    public float maxDistanceFromCamera = 15f;
    public float minDistanceFromCamera = 3f;
    public LayerMask groundLayer = 1;
    public float dragDamping = 0.95f;

    [Header("Dynamic Collision Response")]
    public float pushVelocityThreshold = 2f;      // Below this: push objects
    public float deformVelocityThreshold = 5f;    // Below this: deform objects
    public float crushVelocityThreshold = 8f;     // Above this: crush objects
    public float pushForce = 3f;
    public float deformForce = 8f;
    public float crushForce = 15f;
    public float impactRadius = 0.5f;

    [Header("Visual Effects")]
    public GameObject impactEffectPrefab;
    public AudioClip impactSound;
    public AudioClip crushSound;
    public float impactEffectLifetime = 2f;

    [Header("Debug")]
    public bool showVelocityDebug = true;
    public bool showImpactDebug = true;

    private Body body;
    private Camera mainCamera;
    private bool isDragging = false;
    private Vector3 dragOffset;
    private Plane dragPlane;
    private Vector3 previousPosition;
    private Vector3 currentVelocity;
    private float currentVelocityMagnitude;

    // Impact tracking
    private Dictionary<Body, float> lastImpactTimes = new Dictionary<Body, float>();
    private float impactCooldown = 0.1f;

    // Audio
    private AudioSource audioSource;

    void Start()
    {
        body = GetComponent<Body>();
        mainCamera = Camera.main;

        if (body == null)
        {
            Debug.LogError("❌ HammerBody: No Body component found!");
            enabled = false;
            return;
        }

        GenerateHammer();
        SetupMouseControl();
        SetupAudio();

        previousPosition = transform.position;
    }

    void Update()
    {
        HandleMouseInput();
        UpdateVelocity();

        if (showVelocityDebug)
        {
            Debug.Log($"🔨 Hammer Velocity: {currentVelocityMagnitude:F2} m/s");
        }
    }

    void GenerateHammer()
    {
        // Clear existing particles and constraints
        body.particles.Clear();
        body.constraints.Clear();

        List<Particle> hammerParticles = new List<Particle>();

        // Generate handle
        GenerateHandle(hammerParticles);

        // Generate head
        GenerateHead(hammerParticles);

        // Distribute mass
        float massPerParticle = totalMass / hammerParticles.Count;
        foreach (var particle in hammerParticles)
        {
            particle.SetMass(massPerParticle);
            particle.body = body;
            particle.collisionRadius = Mathf.Min(handleRadius, headRadius) / Mathf.Min(handleResolution, headResolution);
            body.particles.Add(particle);
        }

        // Create constraints to maintain hammer shape
        CreateHammerConstraints();

        Debug.Log($"🔨 Generated hammer with {body.particles.Count} particles and {body.constraints.Count} constraints");
    }

    void GenerateHandle(List<Particle> particles)
    {
        int segments = handleResolution;
        float segmentLength = handleLength / segments;

        for (int i = 0; i <= segments; i++)
        {
            float height = i * segmentLength;

            // Create ring of particles at this height
            int particlesInRing = Mathf.Max(4, segments);
            for (int j = 0; j < particlesInRing; j++)
            {
                float angle = (j / (float)particlesInRing) * 2f * Mathf.PI;
                Vector3 localPos = new Vector3(
                    Mathf.Cos(angle) * handleRadius,
                    height,
                    Mathf.Sin(angle) * handleRadius
                );
                Vector3 worldPos = transform.TransformPoint(localPos);
                particles.Add(new Particle(worldPos, 1f));
            }
        }
    }

    void GenerateHead(List<Particle> particles)
    {
        int headSegments = headResolution;
        float segmentHeight = headHeight / headSegments;

        for (int i = 0; i <= headSegments; i++)
        {
            float height = handleLength + (i * segmentHeight);
            float radius = headRadius * (1f - (i / (float)headSegments) * 0.3f); // Tapered head

            int particlesInRing = Mathf.Max(6, headSegments);
            for (int j = 0; j < particlesInRing; j++)
            {
                float angle = (j / (float)particlesInRing) * 2f * Mathf.PI;
                Vector3 localPos = new Vector3(
                    Mathf.Cos(angle) * radius,
                    height,
                    Mathf.Sin(angle) * radius
                );
                Vector3 worldPos = transform.TransformPoint(localPos);
                particles.Add(new Particle(worldPos, 1f));
            }
        }

        // Add center particles for volume
        for (int i = 0; i < headSegments; i++)
        {
            float height = handleLength + (i * segmentHeight);
            Vector3 localPos = new Vector3(0, height, 0);
            Vector3 worldPos = transform.TransformPoint(localPos);
            particles.Add(new Particle(worldPos, 1f));
        }
    }

    void CreateHammerConstraints()
    {
        int particleCount = body.particles.Count;

        // Connect handle segments
        int handleParticlesPerRing = Mathf.Max(4, handleResolution);
        int handleRings = handleResolution + 1;

        for (int ring = 0; ring < handleRings - 1; ring++)
        {
            int currentRingStart = ring * handleParticlesPerRing;
            int nextRingStart = (ring + 1) * handleParticlesPerRing;

            // Connect particles within rings (circle)
            for (int i = 0; i < handleParticlesPerRing; i++)
            {
                int currentIndex = currentRingStart + i;
                int nextIndex = currentRingStart + ((i + 1) % handleParticlesPerRing);
                int diagIndex = currentRingStart + ((i + 2) % handleParticlesPerRing); // Diagonal for extra rigidity

                if (currentIndex < particleCount && nextIndex < particleCount)
                {
                    body.constraints.Add(new DistanceConstraint(
                        body.particles[currentIndex],
                        body.particles[nextIndex],
                        surfaceStiffness
                    ));
                }
                // Diagonal connection (skip if handleParticlesPerRing < 6)
                if (handleParticlesPerRing >= 6 && currentIndex < particleCount && diagIndex < particleCount)
                {
                    body.constraints.Add(new DistanceConstraint(
                        body.particles[currentIndex],
                        body.particles[diagIndex],
                        surfaceStiffness * 0.7f
                    ));
                }
            }

            // Connect rings vertically
            for (int i = 0; i < handleParticlesPerRing; i++)
            {
                int currentIndex = currentRingStart + i;
                int nextIndex = nextRingStart + i;
                int diagIndex = nextRingStart + ((i + 1) % handleParticlesPerRing); // Diagonal vertical

                if (currentIndex < particleCount && nextIndex < particleCount)
                {
                    body.constraints.Add(new DistanceConstraint(
                        body.particles[currentIndex],
                        body.particles[nextIndex],
                        surfaceStiffness
                    ));
                }
                // Diagonal vertical connection
                if (currentIndex < particleCount && diagIndex < particleCount)
                {
                    body.constraints.Add(new DistanceConstraint(
                        body.particles[currentIndex],
                        body.particles[diagIndex],
                        surfaceStiffness * 0.7f
                    ));
                }
            }
        }

        // Connect handle to head (already present)
        int lastHandleRing = handleRings - 1;
        int firstHeadRing = lastHandleRing;
        int headParticlesPerRing = Mathf.Max(6, headResolution);

        for (int i = 0; i < Mathf.Min(handleParticlesPerRing, headParticlesPerRing); i++)
        {
            int handleIndex = lastHandleRing * handleParticlesPerRing + i;
            int headIndex = firstHeadRing * headParticlesPerRing + i;

            if (handleIndex < particleCount && headIndex < particleCount)
            {
                body.constraints.Add(new DistanceConstraint(
                    body.particles[handleIndex],
                    body.particles[headIndex],
                    surfaceStiffness
                ));
            }
        }

        // Head: connect within and between rings, add diagonals
        int headRings = headResolution + 1;
        int headStart = handleParticlesPerRing * handleRings;
        for (int ring = 0; ring < headRings - 1; ring++)
        {
            int currentRingStart = headStart + ring * headParticlesPerRing;
            int nextRingStart = headStart + (ring + 1) * headParticlesPerRing;
            for (int i = 0; i < headParticlesPerRing; i++)
            {
                int currentIndex = currentRingStart + i;
                int nextIndex = currentRingStart + ((i + 1) % headParticlesPerRing);
                int diagIndex = currentRingStart + ((i + 2) % headParticlesPerRing);
                // Within ring
                if (currentIndex < particleCount && nextIndex < particleCount)
                {
                    body.constraints.Add(new DistanceConstraint(
                        body.particles[currentIndex],
                        body.particles[nextIndex],
                        surfaceStiffness
                    ));
                }
                // Diagonal within ring
                if (headParticlesPerRing >= 6 && currentIndex < particleCount && diagIndex < particleCount)
                {
                    body.constraints.Add(new DistanceConstraint(
                        body.particles[currentIndex],
                        body.particles[diagIndex],
                        surfaceStiffness * 0.7f
                    ));
                }
                // Between rings
                int verticalIndex = nextRingStart + i;
                int verticalDiag = nextRingStart + ((i + 1) % headParticlesPerRing);
                if (currentIndex < particleCount && verticalIndex < particleCount)
                {
                    body.constraints.Add(new DistanceConstraint(
                        body.particles[currentIndex],
                        body.particles[verticalIndex],
                        surfaceStiffness
                    ));
                }
                // Diagonal between rings
                if (currentIndex < particleCount && verticalDiag < particleCount)
                {
                    body.constraints.Add(new DistanceConstraint(
                        body.particles[currentIndex],
                        body.particles[verticalDiag],
                        surfaceStiffness * 0.7f
                    ));
                }
            }
        }
        // Add a few long-range cross-connections for extra rigidity (not n²)
        int crossStep = Mathf.Max(2, headParticlesPerRing / 3);
        for (int i = headStart; i < particleCount; i += crossStep)
        {
            for (int j = i + crossStep; j < Mathf.Min(i + 2 * crossStep, particleCount); j++)
            {
                body.constraints.Add(new DistanceConstraint(
                    body.particles[i],
                    body.particles[j],
                    surfaceStiffness * 0.5f
                ));
            }
        }
    }

    void SetupMouseControl()
    {
        dragPlane = new Plane(Vector3.up, transform.position);
    }

    void SetupAudio()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (impactSound != null || crushSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (IsMouseOverHammer(ray))
            {
                isDragging = true;
                if (dragPlane.Raycast(ray, out float enter))
                {
                    dragOffset = transform.position - ray.GetPoint(enter);
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (dragPlane.Raycast(ray, out float enter))
            {
                Vector3 targetPosition = ray.GetPoint(enter) + dragOffset;

                // Constrain to camera distance
                Vector3 cameraPos = mainCamera.transform.position;
                Vector3 direction = (targetPosition - cameraPos).normalized;
                float distance = Mathf.Clamp(Vector3.Distance(cameraPos, targetPosition), minDistanceFromCamera, maxDistanceFromCamera);
                targetPosition = cameraPos + direction * distance;

                // Ground collision
                if (Physics.Raycast(targetPosition + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f, groundLayer))
                {
                    targetPosition.y = Mathf.Max(targetPosition.y, hit.point.y + 0.5f);
                }

                // Apply movement to all particles with damping
                Vector3 movement = (targetPosition - transform.position) * dragDamping;
                foreach (var particle in body.particles)
                {
                    particle.position += movement;
                    particle.prevPosition += movement;
                }

                transform.position = targetPosition;
            }
        }
    }

    void UpdateVelocity()
    {
        currentVelocity = (transform.position - previousPosition) / Time.deltaTime;
        currentVelocityMagnitude = currentVelocity.magnitude;
        previousPosition = transform.position;
    }

    bool IsMouseOverHammer(Ray ray)
    {
        float distance = Vector3.Distance(ray.origin, transform.position);
        if (distance > maxDistanceFromCamera) return false;

        Vector3 closestPoint = ray.origin + ray.direction * Vector3.Dot(transform.position - ray.origin, ray.direction);
        float distToHammer = Vector3.Distance(closestPoint, transform.position);

        return distToHammer < headRadius * 2f;
    }

    // Called by SimulationManager during collision detection
    public void HandleCollision(Body otherBody, Vector3 collisionPoint, Vector3 collisionNormal)
    {
        if (otherBody == body) return;

        // Check cooldown to prevent multiple impacts
        if (lastImpactTimes.ContainsKey(otherBody))
        {
            if (Time.time - lastImpactTimes[otherBody] < impactCooldown)
                return;
        }
        lastImpactTimes[otherBody] = Time.time;

        // Determine collision response based on velocity
        CollisionResponse response = DetermineCollisionResponse(currentVelocityMagnitude);

        // Apply the appropriate response
        ApplyCollisionResponse(otherBody, collisionPoint, collisionNormal, response);

        // Visual and audio effects
        CreateImpactEffects(collisionPoint, response);

        if (showImpactDebug)
        {
            Debug.Log($"🔨 Impact: {response} at {currentVelocityMagnitude:F2} m/s");
        }
    }

    CollisionResponse DetermineCollisionResponse(float velocity)
    {
        if (velocity >= crushVelocityThreshold)
            return CollisionResponse.Crush;
        else if (velocity >= deformVelocityThreshold)
            return CollisionResponse.Deform;
        else if (velocity >= pushVelocityThreshold)
            return CollisionResponse.Push;
        else
            return CollisionResponse.None;
    }

    void ApplyCollisionResponse(Body otherBody, Vector3 collisionPoint, Vector3 collisionNormal, CollisionResponse response)
    {
        switch (response)
        {
            case CollisionResponse.Push:
                ApplyPushForce(otherBody, collisionPoint, collisionNormal);
                break;

            case CollisionResponse.Deform:
                ApplyDeformForce(otherBody, collisionPoint, collisionNormal);
                break;

            case CollisionResponse.Crush:
                ApplyCrushForce(otherBody, collisionPoint, collisionNormal);
                break;
        }
    }

    void ApplyPushForce(Body otherBody, Vector3 collisionPoint, Vector3 collisionNormal)
    {
        foreach (var particle in otherBody.particles)
        {
            float distance = Vector3.Distance(particle.position, collisionPoint);
            if (distance < impactRadius)
            {
                float forceMultiplier = 1f - (distance / impactRadius);
                Vector3 force = collisionNormal * pushForce * forceMultiplier;
                particle.position += force * Time.deltaTime;
            }
        }
    }

    void ApplyDeformForce(Body otherBody, Vector3 collisionPoint, Vector3 collisionNormal)
    {
        // Temporarily reduce constraint stiffness to allow deformation
        foreach (var constraint in otherBody.constraints)
        {
            constraint.stiffness *= 0.3f; // Reduce stiffness temporarily
        }

        // Apply stronger force
        foreach (var particle in otherBody.particles)
        {
            float distance = Vector3.Distance(particle.position, collisionPoint);
            if (distance < impactRadius)
            {
                float forceMultiplier = 1f - (distance / impactRadius);
                Vector3 force = collisionNormal * deformForce * forceMultiplier;
                particle.position += force * Time.deltaTime;
            }
        }

        // Restore stiffness after a short delay
        StartCoroutine(RestoreConstraintStiffness(otherBody, 0.5f));
    }

    void ApplyCrushForce(Body otherBody, Vector3 collisionPoint, Vector3 collisionNormal)
    {
        // Break constraints and apply crushing force
        List<DistanceConstraint> constraintsToRemove = new List<DistanceConstraint>();

        foreach (var constraint in otherBody.constraints)
        {
            float distance = Vector3.Distance(constraint.p1.position, collisionPoint);
            if (distance < impactRadius * 1.5f)
            {
                constraintsToRemove.Add(constraint);
            }
        }

        // Remove broken constraints
        foreach (var constraint in constraintsToRemove)
        {
            otherBody.constraints.Remove(constraint);
        }

        // Apply crushing force
        foreach (var particle in otherBody.particles)
        {
            float distance = Vector3.Distance(particle.position, collisionPoint);
            if (distance < impactRadius)
            {
                float forceMultiplier = 1f - (distance / impactRadius);
                Vector3 force = collisionNormal * crushForce * forceMultiplier;
                particle.position += force * Time.deltaTime;
            }
        }

        // Optional: Split the body into smaller pieces
        if (constraintsToRemove.Count > otherBody.constraints.Count * 0.3f)
        {
            SplitBody(otherBody);
        }
    }

    System.Collections.IEnumerator RestoreConstraintStiffness(Body otherBody, float delay)
    {
        yield return new WaitForSeconds(delay);

        foreach (var constraint in otherBody.constraints)
        {
            constraint.stiffness = Mathf.Min(constraint.stiffness * 2f, 1f);
        }
    }

    void SplitBody(Body otherBody)
    {
        // Create separate bodies from disconnected particle groups
        // This is a simplified version - you could implement more sophisticated splitting
        Debug.Log($"💥 Crushed {otherBody.name} - body split!");
    }

    void CreateImpactEffects(Vector3 collisionPoint, CollisionResponse response)
    {
        // Visual effects
        if (impactEffectPrefab != null)
        {
            GameObject effect = Instantiate(impactEffectPrefab, collisionPoint, Quaternion.identity);
            Destroy(effect, impactEffectLifetime);
        }

        // Audio effects
        if (audioSource != null)
        {
            AudioClip clipToPlay = response == CollisionResponse.Crush ? crushSound : impactSound;
            if (clipToPlay != null)
            {
                audioSource.PlayOneShot(clipToPlay, currentVelocityMagnitude / crushVelocityThreshold);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Draw hammer bounds
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, headRadius);

        // Draw velocity vector
        if (showVelocityDebug && currentVelocity.magnitude > 0.1f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, currentVelocity.normalized * 2f);
        }

        // Draw impact radius
        if (showImpactDebug)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, impactRadius);
        }
    }
}

public enum CollisionResponse
{
    None,
    Push,
    Deform,
    Crush
}