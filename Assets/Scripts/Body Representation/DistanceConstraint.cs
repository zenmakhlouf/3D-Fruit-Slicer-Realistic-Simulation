using UnityEngine;

/// <summary>
/// Represents a spring-like constraint that tries to maintain a fixed distance between two particles.
/// </summary>
public class DistanceConstraint
{
    public Particle p1, p2;
    public float restLength;    // The target distance for the spring
    public float stiffness = 1.0f;  // How strongly the constraint corrects errors (0-1 for PBD)

    public DistanceConstraint(Particle particle1, Particle particle2, float stiffness = 1.0f)
    {
        this.p1 = particle1;
        this.p2 = particle2;
        this.stiffness = stiffness;
        this.restLength = Vector3.Distance(particle1.position, particle2.position);
    }

    /// <summary>
    /// Solves the constraint by adjusting particle positions to meet the restLength.
    /// </summary>
    public void Solve()
    {
        Vector3 delta = p2.position - p1.position;
        float currentDistance = delta.magnitude;

        if (currentDistance < 0.0001f) return; // Avoid division by zero if particles are coincident

        float error = currentDistance - restLength;
        if (Mathf.Abs(error) < 0.0001f) return; // Already satisfied

        Vector3 correctionNormal = delta / currentDistance; // Direction of correction
        Vector3 totalCorrection = correctionNormal * error * stiffness;

        // Use cached inverse mass for better performance
        float invMass1 = p1.InvMass;
        float invMass2 = p2.InvMass;
        float totalInverseMass = invMass1 + invMass2;

        if (totalInverseMass == 0f) return; // Both particles are fixed or have infinite mass

        // Distribute correction based on inverse mass
        if (invMass1 > 0f)
        {
            p1.position += totalCorrection * (invMass1 / totalInverseMass);
        }
        if (invMass2 > 0f)
        {
            p2.position -= totalCorrection * (invMass2 / totalInverseMass);
        }
    }
}
