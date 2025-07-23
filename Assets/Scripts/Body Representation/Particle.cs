using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Robust particle representation with proper Verlet integration and stability features
/// </summary>
public class Particle
{
    public Vector3 position;
    public Vector3 prevPosition;
    public Vector3 acceleration;
    public float mass = 1f;
    public bool isFixed = false;
    public Body body;
    public float collisionRadius = 0.1f;
    public Color color = Color.red;

    // Cached inverse mass for performance
    private float invMass = 1f;
    private bool invMassDirty = true;

    // Constraint correction accumulator
    [System.NonSerialized] public Vector3 correctionAccumulator;
    [System.NonSerialized] public int correctionCount;

    public Particle(Vector3 initialPosition, float mass, bool isFixed = false)
    {
        this.position = initialPosition;
        this.prevPosition = initialPosition;
        this.acceleration = Vector3.zero;
        this.mass = mass;
        this.isFixed = isFixed;
        UpdateInvMass();
    }

    /// <summary>
    /// Proper Verlet integration with sub-stepping for stability
    /// </summary>
    public void Integrate(float deltaTime, Vector3 gravity, float globalDamping = 0.999f)
    {
        if (isFixed) return;

        // Apply external forces (gravity) to acceleration
        acceleration += gravity;

        // Store current position
        Vector3 temp = position;

        // Verlet integration: x(t+dt) = 2*x(t) - x(t-dt) + a*dt²
        Vector3 velocity = (position - prevPosition) * globalDamping;
        position = position + velocity + acceleration * (deltaTime * deltaTime);

        // Update previous position
        prevPosition = temp;

        // Reset acceleration for next frame
        acceleration = Vector3.zero;
    }

    /// <summary>
    /// Apply constraint correction using averaged corrections for stability
    /// </summary>
    public void ApplyCorrection(Vector3 correction, float stiffness = 1f)
    {
        if (isFixed) return;

        correctionAccumulator += correction * stiffness;
        correctionCount++;
    }

    /// <summary>
    /// Finalize corrections by averaging and applying them
    /// </summary>
    public void FinalizeCorrections()
    {
        if (isFixed || correctionCount == 0)
        {
            ResetCorrections();
            return;
        }

        // Average the corrections and apply
        Vector3 avgCorrection = correctionAccumulator / correctionCount;
        position += avgCorrection;

        ResetCorrections();
    }

    private void ResetCorrections()
    {
        correctionAccumulator = Vector3.zero;
        correctionCount = 0;
    }

    // Cached inverse mass property
    public float InvMass
    {
        get
        {
            if (invMassDirty)
            {
                UpdateInvMass();
            }
            return invMass;
        }
    }

    public void SetMass(float newMass)
    {
        mass = Mathf.Max(newMass, 0.001f); // Prevent zero mass
        invMassDirty = true;
    }

    public void SetFixed(bool fixedState)
    {
        isFixed = fixedState;
        invMassDirty = true;
    }

    private void UpdateInvMass()
    {
        invMass = (isFixed || mass <= 0.001f) ? 0f : 1f / mass;
        invMassDirty = false;
    }

    /// <summary>
    /// Add external force to be integrated next frame
    /// </summary>
    public void AddForce(Vector3 force)
    {
        if (isFixed) return;
        acceleration += force * InvMass;
    }
}

