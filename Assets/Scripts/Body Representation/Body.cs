using System.Collections.Generic;
using UnityEngine;

public class Body : MonoBehaviour
{
    public List<Particle> particles = new List<Particle>();
    public List<DistanceConstraint> constraints = new List<DistanceConstraint>();

    public Vector3 gravity = new Vector3(0, -9.81f, 0);
    public float stepSize = 0.02f;
    public int solverIterations = 5;

    void Update()
    {
        if (particles.Count < 8)
            Debug.Log("Body particles still not populated: " + particles.Count);
        Simulate(Time.deltaTime);
        
    }

    void Simulate(float dt)
    {
        foreach (var p in particles)
        {
            p.Integrate(dt, gravity);
            if (p.position.y < 0.0f)
            {
                p.position.y = 0.0f;
                p.prevPosition.y = 0.0f;
            }
        }


            for (int i = 0; i < solverIterations; i++)
                foreach (var c in constraints)
                    c.Solve();
    }
    void OnDrawGizmos()
    {
        if (particles == null) return;

        Gizmos.color = Color.yellow;
        foreach (var p in particles)
            Gizmos.DrawSphere(p.position, 0.05f);

        Gizmos.color = Color.cyan;
        foreach (var c in constraints)
            Gizmos.DrawLine(c.p1.position, c.p2.position);
    }
}