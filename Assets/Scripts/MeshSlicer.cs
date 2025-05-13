using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MeshSlicer : MonoBehaviour
{
    public void Slice()
    {
        Plane cuttingPlane = new Plane(transform.right, transform.position);
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        List<Vector3> aboveVerts = new List<Vector3>();
        List<int> aboveTris = new List<int>();

        List<Vector3> belowVerts = new List<Vector3>();
        List<int> belowTris = new List<int>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int i0 = triangles[i];
            int i1 = triangles[i + 1];
            int i2 = triangles[i + 2];

            Vector3 v0 = transform.TransformPoint(vertices[i0]);
            Vector3 v1 = transform.TransformPoint(vertices[i1]);
            Vector3 v2 = transform.TransformPoint(vertices[i2]);

            float d0 = cuttingPlane.GetDistanceToPoint(v0);
            float d1 = cuttingPlane.GetDistanceToPoint(v1);
            float d2 = cuttingPlane.GetDistanceToPoint(v2);

            if (d0 >= 0 && d1 >= 0 && d2 >= 0)
            {
                AddTriangle(aboveVerts, aboveTris, v0, v1, v2);
            }
            else if (d0 <= 0 && d1 <= 0 && d2 <= 0)
            {
                AddTriangle(belowVerts, belowTris, v0, v1, v2);
            }

        }
        CreateMeshObject("AbovePart", aboveVerts, aboveTris);
        CreateMeshObject("BelowPart", belowVerts, belowTris);

        Destroy(gameObject);
    }
    void AddTriangle(List<Vector3> verts, List<int> tris, Vector3 v0, Vector3 v1, Vector3 v2)
    {
        int idx = verts.Count;
        verts.Add(transform.InverseTransformPoint(v0));
        verts.Add(transform.InverseTransformPoint(v1));
        verts.Add(transform.InverseTransformPoint(v2));
        tris.Add(idx);
        tris.Add(idx + 1);
        tris.Add(idx + 2);
    }
    void CreateMeshObject(string name, List<Vector3> verts, List<int> tris)
    {
        if (verts.Count == 0) return;

        GameObject go = new GameObject(name);
        go.transform.position = transform.position;
        go.transform.rotation = transform.rotation;
        go.transform.localScale = transform.localScale;

        Mesh mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();

        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.mesh = mesh;
        go.AddComponent<MeshRenderer>().material = GetComponent<MeshRenderer>().material;

        var phys = go.AddComponent<CustomPhysics>();
        phys.velocity = GetComponent<CustomPhysics>().velocity;

        var collider = go.AddComponent<SimpleCollider>();
        collider.radius = GetComponent<SimpleCollider>().radius;

        go.AddComponent<MeshDeformer>();
        go.AddComponent<MeshSlicer>();
        go.AddComponent<NewMonoBehaviourScript>();

    }
    void Start()
    {
        // Slice();
    }
    void Update()
    {
        // Press "C" key to slice
        if (Input.GetKeyDown(KeyCode.C))
        {
            Slice();
        }
    }
}
