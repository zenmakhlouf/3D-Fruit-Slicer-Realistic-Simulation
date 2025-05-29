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

    // Collision properties
    public float radius = 0.1f;
    public Body ownerBody; // Reference to the body this particle belongs to
    public int bodyId = -1; // Unique ID for the body (for fast collision filtering)

    // Collision response data
    public Vector3 collisionForce = Vector3.zero;
    public int collisionCount = 0;

    public Particle(Vector3 initialPosition, float mass, float radius = 0.1f, bool isFixed = false)
    {
        this.position = initialPosition;
        this.prevPosition = initialPosition;
        this.mass = mass;
        this.radius = radius;
        this.isFixed = isFixed;
    }

    public void Integrate(float deltaTime, Vector3 gravity, float damping = 0.98f)
    {
        if (isFixed) return;

        Vector3 velocityEstimate = (position - prevPosition);
        prevPosition = position;

        // Apply collision forces before position update
        Vector3 acceleration = gravity + (collisionForce / mass);
        position += velocityEstimate * damping + acceleration * (deltaTime * deltaTime);

        // Clear collision data for next frame
        collisionForce = Vector3.zero;
        collisionCount = 0;
    }
}