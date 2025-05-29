using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages a collection of particles and constraints to simulate a physical object.
/// Runs the physics simulation loop.
/// </summary>
public class Body : MonoBehaviour
{
    public List<Particle> particles = new List<Particle>();
    public List<DistanceConstraint> constraints = new List<DistanceConstraint>();

    [Header("Simulation Settings")]
    public Vector3 gravity = new Vector3(0, -9.81f, 0);
    public float fixedTimeStep = 0.02f; // Duration of each physics step
    public int solverIterations = 10;   // Number of times constraints are solved per step
    public float groundRestitution = 0.5f; // Bounciness of the ground (0-1)

    [Header("Initial Conditions")]
    public Vector3 initialVelocity = Vector3.zero;
    public Vector3 initialAcceleration = Vector3.zero;
    public Vector3 initialForce = Vector3.zero;

    private float timeAccumulator = 0.0f; // Accumulates game time for fixed physics updates
    private Vector3 currentForce = Vector3.zero;

    void Start()
    {
        // Register with collision manager
        if (CollisionManager.Instance != null)
        {
            CollisionManager.Instance.RegisterBody(this);
        }

        // Apply initial conditions
        ApplyForce(initialForce);
        SetVelocity(initialVelocity);
        SetAcceleration(initialAcceleration);
    }

    void OnDestroy()
    {
        // Unregister from collision manager
        if (CollisionManager.Instance != null)
        {
            CollisionManager.Instance.UnregisterBody(this);
        }
    }

    void Update()
    {
        // Use a fixed time step for stable and consistent physics simulation
        timeAccumulator += Time.deltaTime;
        while (timeAccumulator >= fixedTimeStep)
        {
            SimulatePhysicsStep(fixedTimeStep);
            timeAccumulator -= fixedTimeStep;
        }
    }

    void SimulatePhysicsStep(float deltaTime)
    {
        // 1. Integrate particle positions (apply forces, update velocity and position)
        foreach (Particle p in particles)
        {
            // Calculate total force for this particle
            Vector3 totalForce = gravity + currentForce;
            p.Integrate(deltaTime, totalForce);
        }

        // 2. Solve all constraints iteratively to enforce shape and connections
        for (int i = 0; i < solverIterations; i++)
        {
            foreach (DistanceConstraint constraint in constraints)
            {
                constraint.Solve();
            }
            // Apply ground collision as a hard constraint within the solver loop
            foreach (Particle p in particles)
            {
                ApplyGroundCollision(p);
            }
        }
    }

    /// <summary>
    /// Applies a force to all particles in the body
    /// </summary>
    public void ApplyForce(Vector3 force)
    {
        currentForce = force;
    }

    /// <summary>
    /// Adds a force to the current force being applied
    /// </summary>
    public void AddForce(Vector3 force)
    {
        currentForce += force;
    }

    /// <summary>
    /// Sets the velocity of all particles in the body
    /// </summary>
    public void SetVelocity(Vector3 velocity)
    {
        foreach (Particle p in particles)
        {
            p.prevPosition = p.position - velocity * fixedTimeStep;
        }
    }

    /// <summary>
    /// Sets the acceleration of all particles in the body
    /// </summary>
    public void SetAcceleration(Vector3 acceleration)
    {
        foreach (Particle p in particles)
        {
            // For Verlet integration, we need to adjust the previous position
            // to achieve the desired acceleration
            Vector3 currentVelocity = (p.position - p.prevPosition) / fixedTimeStep;
            p.prevPosition = p.position - (currentVelocity + acceleration * fixedTimeStep) * fixedTimeStep;
        }
    }

    void ApplyGroundCollision(Particle particle)
    {
        if (particle.position.y < 0.0f)
        {
            // Move particle to be exactly on the ground
            particle.position.y = 0.0f;

            // Adjust previous position to simulate a bounce with Verlet integration.
            // This effectively reflects the particle's vertical velocity component.
            float velocityYEstimate = particle.position.y - particle.prevPosition.y; // Uses new particle.position.y (0)
            particle.prevPosition.y = particle.position.y + velocityYEstimate * groundRestitution;
        }
    }

    void OnDrawGizmos()
    {
        if (particles == null || !Application.isPlaying) return; // Only draw if particles exist and in play mode

        // Draw particles
        Gizmos.color = Color.yellow;
        foreach (Particle p in particles)
        {
            if (p != null) Gizmos.DrawSphere(p.position, 0.05f);
        }

        // Draw constraints
        Gizmos.color = Color.cyan;
        foreach (DistanceConstraint c in constraints)
        {
            if (c != null && c.p1 != null && c.p2 != null)
                Gizmos.DrawLine(c.p1.position, c.p2.position);
        }
    }
}
