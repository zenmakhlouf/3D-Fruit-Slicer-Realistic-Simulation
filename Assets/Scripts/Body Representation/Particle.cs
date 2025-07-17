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
    public Color color = Color.red;

    // Cached values for performance
    private float invMass = 1f;
    private bool invMassDirty = true;

    public Particle(Vector3 initialPosition, float mass, bool isFixed = false)
    {
        this.position = initialPosition;
        this.prevPosition = initialPosition;
        this.mass = mass;
        this.isFixed = isFixed;
        UpdateInvMass();
    }

    public void Integrate(float deltaTime, Vector3 gravity, float damping = 0.98f)
    {
        if (isFixed) return;

        // Optimized Verlet integration with damping
        Vector3 velocity = (position - prevPosition) * damping;
        prevPosition = position;
        position += velocity + gravity * (deltaTime * deltaTime);
    }

    // Property for inverse mass with caching
    public float InvMass
    {
        get
        {
            if (invMassDirty)
            {
                invMass = isFixed ? 0f : 1f / mass;
                invMassDirty = false;
            }
            return invMass;
        }
    }

    // Update inverse mass when mass changes
    public void SetMass(float newMass)
    {
        mass = newMass;
        invMassDirty = true;
    }

    public void SetFixed(bool fixedState)
    {
        isFixed = fixedState;
        invMassDirty = true;
    }

    private void UpdateInvMass()
    {
        invMass = isFixed ? 0f : 1f / mass;
        invMassDirty = false;
    }
}
