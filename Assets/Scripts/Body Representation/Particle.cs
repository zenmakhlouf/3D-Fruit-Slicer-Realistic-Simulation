using UnityEngine;

/// <summary>
/// Represents a single point mass in the physics simulation.
/// </summary>
public class Particle
{
    public Vector3 position;
    public Vector3 prevPosition; // Used for Verlet integration
    public float mass = 1f;
    public bool isFixed = false; // If true, particle does not move

    public Particle(Vector3 initialPosition, float mass, bool isFixed = false)
    {
        this.position = initialPosition;
        this.prevPosition = initialPosition; // Start at rest
        this.mass = mass;
        this.isFixed = isFixed;
    }

    /// <summary>
    /// Updates the particle's position based on Verlet integration.
    /// x_next = x_current + (x_current - x_previous) * damping + acceleration * dt^2
    /// </summary>
    public void Integrate(float deltaTime, Vector3 gravity, float damping = 0.98f)
    {
        if (isFixed) return;

        Vector3 velocityEstimate = (position - prevPosition);
        prevPosition = position;
        position += velocityEstimate * damping + gravity * (deltaTime * deltaTime);
    }
}
