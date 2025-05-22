using UnityEngine;

public class DistanceConstraint
{
    public Particle p1, p2;
    public float restLength;
    public float stiffness = 1.0f;

    public DistanceConstraint(Particle a, Particle b, float stiffness = 1.0f)
    {
        p1 = a;
        p2 = b;
        restLength = Vector3.Distance(p1.position, p2.position);
        this.stiffness = stiffness;
    }

    public void Solve()
    {
        
        Vector3 delta = p2.position - p1.position;
        float dist = delta.magnitude;
        
        float diff = (dist - restLength) / dist;

        if (dist < 1e-6f) return;

        if (!p1.isFixed)
            p1.position += delta * 0.5f * stiffness * diff;
        if (!p2.isFixed)
            p2.position -= delta * 0.5f * stiffness * diff;
    }
}