using UnityEngine;

public class MeshDeformer : MonoBehaviour
{
    Mesh mesh;
    Vector3[] originalVertices;
    Vector3[] displacedVertices;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        originalVertices = mesh.vertices;
        displacedVertices = mesh.vertices;
    }
    public void ApplyDeformation(Vector3 worldPoint, float force)
    {
        System.Array.Copy(originalVertices, displacedVertices, originalVertices.Length);
        for (int i = 0; i < displacedVertices.Length; i++)
        {
            Vector3 worldVertex = transform.TransformPoint(displacedVertices[i]);
            float distance = Vector3.Distance(worldPoint, worldVertex);
            if (distance < 0.2f)
            {
                displacedVertices[i] += (displacedVertices[i].normalized) * force * 0.003f;
            }
        }
        mesh.vertices = displacedVertices;
        mesh.RecalculateNormals();

        CancelInvoke("RestoreShape");
        Invoke("RestoreShape", 0.2f);
    }
    void RestoreShape()
    {
        mesh.vertices = originalVertices;
        mesh.RecalculateNormals();
    }


}
