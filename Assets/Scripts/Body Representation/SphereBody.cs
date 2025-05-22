using UnityEngine;
using System.Collections.Generic;

public class SphereBody : MonoBehaviour
{

    public int resolution = 3;  // increase to add more particles
    public float radius = 1f;
    public float mass = 1f;
    public float stiffness = 1f;

    void Start()
    {


        Body body = GetComponent<Body>();
        if (body == null)
        {
            Debug.LogError("No Body component found!");
            return;
        }

        List<Particle> ps = new List<Particle>();

        for (int i = -resolution; i <= resolution; i++)
        {
            for (int j = -resolution; j <= resolution; j++)
            {
                for (int k = -resolution; k <= resolution; k++)
                {
                    Vector3 offset = new Vector3(i, j, k);

                    if (offset.magnitude <= resolution)
                    {
                        Vector3 localPos = offset.normalized * radius * 0.95f * (offset.magnitude / resolution);
                         Vector3 worldPos = transform.position + localPos;

                        var p = new Particle(worldPos, mass);
                        p.prevPosition = worldPos;
                        ps.Add(p);
                    }
                }
            }
        }
        foreach (var p in ps)
            body.particles.Add(p);

        // Connect nearby particles with springs
        float connectDist = (radius / resolution) * 1.5f;
        for (int i = 0; i < ps.Count; i++)
        {
            for (int j = i + 1; j < ps.Count; j++)
            {
                if (Vector3.Distance(ps[i].position, ps[j].position) < connectDist)
                {
                    body.constraints.Add(new DistanceConstraint(ps[i], ps[j], stiffness));
                }
            }
        }
        transform.localScale = Vector3.one * radius * 2f;

        Debug.Log($"SphereBody initialized particles: {ps.Count}");
    }

}