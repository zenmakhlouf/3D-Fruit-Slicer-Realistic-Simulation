
using UnityEngine;
using System.Collections.Generic;


/// <summary>
/// Robust distance constraint with proper mass handling and relaxation
/// </summary>
public class DistanceConstraint
{
    public Particle p1, p2;
    public float restLength;
    public float stiffness;
    private float restLengthSqr;

    public DistanceConstraint(Particle particle1, Particle particle2, float stiffness = 1f)
    {
        p1 = particle1;
        p2 = particle2;
        this.stiffness = Mathf.Clamp01(stiffness);

        // Calculate rest length from initial positions
        restLength = Vector3.Distance(p1.position, p2.position);
        restLengthSqr = restLength * restLength;
    }

    public DistanceConstraint(Particle particle1, Particle particle2, float restLength, float stiffness = 1f)
    {
        p1 = particle1;
        p2 = particle2;
        this.restLength = restLength;
        this.stiffness = Mathf.Clamp01(stiffness);
        restLengthSqr = restLength * restLength;
    }

    /// <summary>
    /// Solve constraint using mass-weighted corrections for stability
    /// </summary>
    public void Solve()
    {
        Vector3 delta = p2.position - p1.position;
        float currentLength = delta.magnitude;

        if (currentLength < 0.001f) return; // Avoid division by zero

        float difference = currentLength - restLength;

        // Early exit if constraint is already satisfied
        if (Mathf.Abs(difference) < 0.001f) return;

        Vector3 direction = delta / currentLength;
        float totalMass = p1.InvMass + p2.InvMass;

        if (totalMass < 0.001f) return; // Both particles fixed

        // Calculate mass-weighted corrections
        float correctionMagnitude = difference * stiffness * 0.5f;
        Vector3 correction = direction * correctionMagnitude;

        float mass1Ratio = p1.InvMass / totalMass;
        float mass2Ratio = p2.InvMass / totalMass;

        // Apply corrections
        p1.ApplyCorrection(correction * mass1Ratio);
        p2.ApplyCorrection(-correction * mass2Ratio);
    }
}