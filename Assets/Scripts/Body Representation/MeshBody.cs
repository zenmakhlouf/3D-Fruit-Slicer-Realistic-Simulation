using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Body), typeof(MeshFilter))]
public class MeshBody : MonoBehaviour
{
    [Header("Mesh Particle Generation")]
    public int resolution = 5; // Controls density of internal particles
    public float totalMass = 1f;
    public float stiffness = 1.0f;
    public float connectionRadius = 0.5f;
    public bool includeSurfaceVertices = true;
    public bool includeInternalParticles = true;
    public float internalDensity = 0.7f; // Probability for internal particle placement

    private Body body;
    private Mesh mesh;

    void Start()
    {
        body = GetComponent<Body>();
        var meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogError($"{name}: MeshFilter is missing! MeshBody requires a MeshFilter component.");
            enabled = false;
            return;
        }
        mesh = meshFilter.sharedMesh;
        if (mesh == null)
        {
            Debug.LogError($"{name}: MeshFilter has no mesh assigned! Please assign a mesh.");
            enabled = false;
            return;
        }
        GenerateFromMesh();
    }

    public void GenerateFromMesh()
    {
        if (mesh == null)
        {
            Debug.LogError($"{name}: Mesh is null in GenerateFromMesh!");
            return;
        }

        body.particles.Clear();
        body.constraints.Clear();
        HashSet<Vector3> added = new HashSet<Vector3>();

        // 1. Surface vertices
        if (includeSurfaceVertices)
        {
            foreach (Vector3 local in mesh.vertices)
            {
                Vector3 world = transform.TransformPoint(local);
                if (added.Add(world))
                {
                    body.particles.Add(new Particle(world, 1f) { body = body });
                }
            }
        }

        // 2. Internal particles (grid sampling inside mesh bounds)
        if (includeInternalParticles)
        {
            Bounds bounds = mesh.bounds;
            Vector3 min = bounds.min;
            Vector3 size = bounds.size;
            float step = Mathf.Min(size.x, size.y, size.z) / Mathf.Max(2, resolution);

            for (float x = min.x; x <= min.x + size.x; x += step)
            {
                for (float y = min.y; y <= min.y + size.y; y += step)
                {
                    for (float z = min.z; z <= min.z + size.z; z += step)
                    {
                        Vector3 localPoint = new Vector3(x, y, z);
                        Vector3 worldPoint = transform.TransformPoint(localPoint);
                        if (Random.value < internalDensity && IsPointInsideMesh(localPoint, mesh))
                        {
                            if (added.Add(worldPoint))
                                body.particles.Add(new Particle(worldPoint, 1f) { body = body });
                        }
                    }
                }
            }
        }

        // 3. Normalize mass
        float massPer = totalMass / Mathf.Max(1, body.particles.Count);
        foreach (var p in body.particles)
            p.mass = massPer;

        // 4. Create constraints using spatial proximity
        CreateConstraints();

        Debug.Log($"{name}: MeshBody generated {body.particles.Count} particles and {body.constraints.Count} constraints.");
    }

    // Simple point-in-mesh test using raycast (works for closed meshes)
    bool IsPointInsideMesh(Vector3 localPoint, Mesh mesh)
    {
        // Cast a ray in an arbitrary direction and count intersections
        int hitCount = 0;
        Vector3 dir = Vector3.right;
        Vector3 origin = localPoint + dir * 0.001f; // Offset to avoid edge cases
        Ray ray = new Ray(origin, dir);
        var triangles = mesh.triangles;
        var vertices = mesh.vertices;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v0 = vertices[triangles[i]];
            Vector3 v1 = vertices[triangles[i + 1]];
            Vector3 v2 = vertices[triangles[i + 2]];
            if (RayIntersectsTriangle(ray, v0, v1, v2, out float _))
                hitCount++;
        }
        return (hitCount % 2) == 1;
    }

    // Möller–Trumbore ray-triangle intersection
    bool RayIntersectsTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, out float t)
    {
        t = 0f;
        Vector3 edge1 = v1 - v0;
        Vector3 edge2 = v2 - v0;
        Vector3 h = Vector3.Cross(ray.direction, edge2);
        float a = Vector3.Dot(edge1, h);
        if (Mathf.Abs(a) < 1e-6f) return false;
        float f = 1.0f / a;
        Vector3 s = ray.origin - v0;
        float u = f * Vector3.Dot(s, h);
        if (u < 0.0f || u > 1.0f) return false;
        Vector3 q = Vector3.Cross(s, edge1);
        float v = f * Vector3.Dot(ray.direction, q);
        if (v < 0.0f || u + v > 1.0f) return false;
        t = f * Vector3.Dot(edge2, q);
        return t > 1e-6f;
    }

    void CreateConstraints()
    {
        // Connect each particle to nearby particles within connectionRadius
        int count = body.particles.Count;
        for (int i = 0; i < count; i++)
        {
            Particle p1 = body.particles[i];
            for (int j = i + 1; j < count; j++)
            {
                Particle p2 = body.particles[j];
                float dist = Vector3.Distance(p1.position, p2.position);
                if (dist < connectionRadius)
                {
                    body.constraints.Add(new DistanceConstraint(p1, p2, stiffness));
                }
            }
        }
    }

    // Optional: Regenerate mesh body at runtime
    [ContextMenu("Regenerate Mesh Body")]
    public void RegenerateMeshBody()
    {
        GenerateFromMesh();
    }
}
