using UnityEngine;

/// <summary>
/// Represents a single point mass in the physics simulation.
/// </summary>
public class Particle
{
    public Vector3 position;
    public Vector3 prevPosition;
    public float mass = 1f;
    public bool isFixed = false;
    public Body body; // Reference to owning body (added for collision handling)
    public float collisionRadius = 0.1f; // Default collision radius

    public Particle(Vector3 initialPosition, float mass, bool isFixed = false)
    {
        this.position = initialPosition;
        this.prevPosition = initialPosition;
        this.mass = mass;
        this.isFixed = isFixed;
    }

    public void Integrate(float deltaTime, Vector3 gravity, float damping = 0.98f)
    {
        if (isFixed) return;
        Vector3 velocityEstimate = (position - prevPosition);
        prevPosition = position;
        position += velocityEstimate * damping + gravity * (deltaTime * deltaTime);
    }
}
