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

    private float timeAccumulator = 0.0f; // Accumulates game time for fixed physics updates

   public void ApplyGroundCollision(Particle particle)
    {
        if (particle.position.y < 0.0f)
        {
            // Move particle to be exactly on the ground
            particle.position.y = 0.0f;

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
