using UnityEngine;
using System.Collections.Generic;

public class CollisionManager : MonoBehaviour
{
    [Header("Collision Settings")]
    public float globalCollisionRadius = 0.15f;
    public float collisionStiffness = 0.8f;
    public float collisionDamping = 0.9f;
    public float separationForce = 10f;

    [Header("Performance")]
    public float gridCellSize = 0.5f; // Should be roughly 2x your collision radius
    public LayerMask collisionLayers = -1;

    private SpatialGrid spatialGrid;
    private List<Body> registeredBodies = new List<Body>();
    private int nextBodyId = 0;

    public static CollisionManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            spatialGrid = new SpatialGrid(gridCellSize);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterBody(Body body)
    {
        if (!registeredBodies.Contains(body))
        {
            registeredBodies.Add(body);
            int bodyId = nextBodyId++;

            // Assign body ID to all particles
            foreach (var particle in body.particles)
            {
                particle.bodyId = bodyId;
                particle.ownerBody = body;
                particle.radius = globalCollisionRadius;
            }
        }
    }

    public void UnregisterBody(Body body)
    {
        registeredBodies.Remove(body);
    }

    void FixedUpdate()
    {
        ProcessCollisions();
    }

    void ProcessCollisions()
    {
        // Clear and rebuild spatial grid
        spatialGrid.Clear();

        // Add all particles to spatial grid
        foreach (var body in registeredBodies)
        {
            if (body == null || body.particles == null) continue;

            foreach (var particle in body.particles)
            {
                spatialGrid.AddParticle(particle);
            }
        }

        // Process collisions
        foreach (var body in registeredBodies)
        {
            if (body == null || body.particles == null) continue;

            foreach (var particle in body.particles)
            {
                ProcessParticleCollisions(particle);
            }
        }
    }

    void ProcessParticleCollisions(Particle particle)
    {
        var nearbyParticles = spatialGrid.GetNearbyParticles(particle.position, particle.radius * 2f);

        foreach (var otherParticle in nearbyParticles)
        {
            // Skip self and particles from same body
            if (otherParticle == particle || otherParticle.bodyId == particle.bodyId)
                continue;

            ProcessParticlePairCollision(particle, otherParticle);
        }
    }

    void ProcessParticlePairCollision(Particle p1, Particle p2)
    {
        Vector3 delta = p2.position - p1.position;
        float distance = delta.magnitude;
        float minDistance = p1.radius + p2.radius;

        if (distance >= minDistance || distance < 0.0001f) return;

        // Calculate penetration and response
        float penetration = minDistance - distance;
        Vector3 normal = delta / distance;

        // Calculate relative velocity for damping
        Vector3 vel1 = p1.position - p1.prevPosition;
        Vector3 vel2 = p2.position - p2.prevPosition;
        Vector3 relativeVelocity = vel2 - vel1;
        float velocityAlongNormal = Vector3.Dot(relativeVelocity, normal);

        // Calculate collision impulse
        float impulse = penetration * separationForce;

        // Add velocity damping if particles are approaching
        if (velocityAlongNormal < 0)
        {
            impulse += -velocityAlongNormal * collisionDamping;
        }

        // Apply mass-weighted forces
        float totalMass = p1.mass + p2.mass;
        float massRatio1 = p2.mass / totalMass;
        float massRatio2 = p1.mass / totalMass;

        Vector3 force1 = -normal * impulse * massRatio1 * collisionStiffness;
        Vector3 force2 = normal * impulse * massRatio2 * collisionStiffness;

        // Apply forces (accumulated for this frame)
        if (!p1.isFixed)
        {
            p1.collisionForce += force1;
            p1.collisionCount++;
        }

        if (!p2.isFixed)
        {
            p2.collisionForce += force2;
            p2.collisionCount++;
        }
    }
}