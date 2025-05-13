using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    Mesh mesh;
    Color[] colors;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;

        Debug.Log("Vertices count: " + mesh.vertexCount);
        Debug.Log("Triangles count: " + mesh.triangles.Length / 3);
        Debug.Log("First vertex position: " + mesh.vertices[0]);

        colors = new Color[mesh.vertexCount];

        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            float height = vertices[i].y;
            colors[i] = Color.Lerp(Color.green, Color.black, Mathf.InverseLerp(-0.5f, 0.5f, height));
        }
        mesh.colors = colors;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < 515; i += 3)
        {
           // vertices[i] += 0.0001f * Mathf.Sin(Time.time) * Vector3.up;
           // mesh.vertices = vertices;
           // mesh.RecalculateNormals();
        }
        // for (int i = 0; i < vertices.Length; i++)
        // {
        //     float height = vertices[i].y;
        //     colors[i] = Color.Lerp(Color.blue, Color.red, Mathf.InverseLerp(-0.5f, 0.5f, height));
        // }
        // mesh.colors = colors;

    }
}
