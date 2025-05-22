using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(Body))]
public class BodyMeshRenderer : MonoBehaviour
{
    Mesh mesh;
    Vector3[] originalVerts;
    Body body;

    public List<int> particleVertexMap = new List<int>();  // Maps mesh vertex → particle index

    bool initialized = false;

    void Start()
    {
        StartCoroutine(DelayedInit());
        Debug.Log("Mesh bounds: " + mesh.bounds.size);
    }

    public IEnumerator DelayedInit()
    
    {
        body = GetComponent<Body>();
        mesh = GetComponent<MeshFilter>().mesh;
        originalVerts = mesh.vertices;

        // Wait until SphereBody populates the particles
        while (body == null || body.particles.Count < 8)
        {
            Debug.Log("Waiting for particles to be populated...");
            yield return null;
        }

        particleVertexMap = new List<int>();

        for (int i = 0; i < originalVerts.Length; i++)
        {
            Vector3 worldVert = transform.TransformPoint(originalVerts[i]);

            float minDist = float.MaxValue;
            int closestIndex = -1;

            for (int j = 0; j < body.particles.Count; j++)
            {
                float dist = Vector3.Distance(worldVert, body.particles[j].position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestIndex = j;
                }
            }

            particleVertexMap.Add(closestIndex);
        }

        Debug.Log("Auto vertex-to-particle mapping complete: " + particleVertexMap.Count + " vertices mapped");
        initialized = true;
    }
    [ContextMenu("Rebind Vertices")]
    public void RebindVertices()
    {
        StartCoroutine(DelayedInit());
    }
    void Update()
    {
        if (!initialized || particleVertexMap.Count != originalVerts.Length) return;

        Vector3[] newVerts = new Vector3[originalVerts.Length];

        for (int i = 0; i < originalVerts.Length; i++)
        {
            int particleIndex = particleVertexMap[i];
            if (particleIndex >= 0 && particleIndex < body.particles.Count)
            {
                Vector3 worldPos = body.particles[particleIndex].position;
                newVerts[i] = transform.InverseTransformPoint(worldPos);
            }
            else
            {
                newVerts[i] = originalVerts[i];
            }
        }
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"Vertex 0 → particle {particleVertexMap[0]} at {body.particles[particleVertexMap[0]].position}");
        }

        mesh.vertices = newVerts;
        mesh.RecalculateNormals();
        
    }
}