using UnityEngine;

public class CubeBody : MonoBehaviour
{
    public float size = 1f;
    public float mass = 1f;
    public float stiffness = 1f;

    void Start()
    {
        Body body = GetComponent<Body>();
        if (body == null)
        {
            Debug.LogError("No Body component found on this object!");
            return;
        }

        Vector3[] offsets = new Vector3[]
        {
            new Vector3(-1,-1,-1), new Vector3(1,-1,-1),
            new Vector3(1,-1,1), new Vector3(-1,-1,1),
            new Vector3(-1,1,-1), new Vector3(1,1,-1),
            new Vector3(1,1,1), new Vector3(-1,1,1)
        };

        Particle[] ps = new Particle[8];

        for (int i = 0; i < 8; i++)
        {
            Vector3 worldPos = transform.position + offsets[i] * size * 0.5f;
            ps[i] = new Particle(worldPos, mass);
            body.particles.Add(ps[i]);
        }

        void Link(int a, int b)
        {
            body.constraints.Add(new DistanceConstraint(ps[a], ps[b], stiffness));
        }

        int[,] edges = new int[,]
        {
            {0,1},{1,2},{2,3},{3,0},
            {4,5},{5,6},{6,7},{7,4},
            {0,4},{1,5},{2,6},{3,7}
        };

        for (int i = 0; i < edges.GetLength(0); i++)
            Link(edges[i, 0], edges[i, 1]);

        Debug.Log("CubeBody initialized particles: " + body.particles.Count);
    }
}