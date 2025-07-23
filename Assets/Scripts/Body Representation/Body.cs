using UnityEngine;
using System.Collections.Generic;


/// <summary>
/// Robust Body controller with proper constraint solving and stability features
/// </summary>
public class Body : MonoBehaviour
{
    [Header("Particles and Constraints")]
    public List<Particle> particles = new List<Particle>();
    public List<DistanceConstraint> constraints = new List<DistanceConstraint>();
    public List<ShapeMatchingConstraint> shapeMatchingConstraints = new List<ShapeMatchingConstraint>();

    [Header("Physics Settings")]
    public Vector3 gravity = new Vector3(0, -9.81f, 0);
    public float fixedTimeStep = 0.0167f;

    [Header("Solver Settings")]
    [Range(3, 20)]
    public int solverIterations = 8; // Increased for stability
    [Range(0.95f, 1.0f)]
    public float globalDamping = 0.999f; // Less aggressive damping

    [Header("Ground Collision")]
    public bool enableGroundCollision = true;
    public float groundY = 0f;
    public float groundRestitution = 0.3f; // Less bouncy
    public float groundFriction = 0.8f; // Add friction

    [Header("Simulation Control")]
    public bool runSimulation = true;
    [Range(1, 4)]
    public int subSteps = 2; // Sub-stepping for stability

    private float timeAccumulator = 0f;

    void Update()
    {
        if (!runSimulation) return;

        timeAccumulator += Time.deltaTime;

        while (timeAccumulator >= fixedTimeStep)
        {
            SimulatePhysicsStep(fixedTimeStep);
            timeAccumulator -= fixedTimeStep;
        }
    }

    private void SimulatePhysicsStep(float deltaTime)
    {
        float subDeltaTime = deltaTime / subSteps;

        for (int subStep = 0; subStep < subSteps; subStep++)
        {
            // 1. Integration
            foreach (var particle in particles)
            {
                particle.Integrate(subDeltaTime, gravity, globalDamping);
            }

            // 2. Constraint solving with multiple iterations
            for (int iter = 0; iter < solverIterations; iter++)
            {
                // Solve distance constraints
                foreach (var constraint in constraints)
                {
                    constraint.Solve();
                }

                // Solve shape matching constraints
                foreach (var shapeConstraint in shapeMatchingConstraints)
                {
                    shapeConstraint.Solve();
                }

                // Apply accumulated corrections
                foreach (var particle in particles)
                {
                    particle.FinalizeCorrections();
                }

                // Ground collision
                if (enableGroundCollision)
                {
                    foreach (var particle in particles)
                    {
                        ApplyGroundCollision(particle);
                    }
                }
            }
        }
    }

    public void ApplyGroundCollision(Particle particle)
    {
        if (particle.position.y <= groundY + particle.collisionRadius)
        {
            // Position correction
            particle.position.y = groundY + particle.collisionRadius;

            // Velocity correction using proper Verlet integration
            Vector3 velocity = particle.position - particle.prevPosition;
            float normalVelocity = velocity.y;

            if (normalVelocity < 0) // Moving towards ground
            {
                // Apply restitution to normal component
                velocity.y = -normalVelocity * groundRestitution;

                // Apply friction to tangential components
                velocity.x *= (1f - groundFriction);
                velocity.z *= (1f - groundFriction);

                // Update previous position to reflect new velocity
                particle.prevPosition = particle.position - velocity;
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || particles == null || particles.Count == 0)
            return;

        // Draw particles with different colors based on state
        foreach (var particle in particles)
        {
            Gizmos.color = particle.isFixed ? Color.red : particle.color;
            Gizmos.DrawSphere(particle.position, particle.collisionRadius);
        }

        // Draw distance constraints
        Gizmos.color = Color.cyan;
        foreach (var constraint in constraints)
        {
            if (constraint.p1 != null && constraint.p2 != null)
            {
                Gizmos.DrawLine(constraint.p1.position, constraint.p2.position);
            }
        }

        // Draw ground plane
        if (enableGroundCollision)
        {
            Gizmos.color = Color.green;
            Vector3 center = transform.position;
            center.y = groundY;
            Gizmos.DrawCube(center, new Vector3(10f, 0.1f, 10f));
        }
    }
}