using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A robust mesh body generator that uses a Shape Matching constraint for superior stability.
/// This approach prevents the "crumbling" seen with simple distance constraints.
/// </summary>
[RequireComponent(typeof(Body), typeof(MeshFilter), typeof(MeshCollider))]
public class RobustMeshBody : MonoBehaviour
{
    [Header("Particle Generation")]
    [Tooltip("Controls the density of internal particles. Higher values mean more particles.")]
    [Range(2, 20)]
    public int resolution = 8;

    [Header("Physics Properties")]
    [Tooltip("The total mass of the entire soft body.")]
    public float totalMass = 1.0f;
    [Tooltip("The stiffness of the shape matching constraint. Controls how rigidly the body holds its shape.")]
    [Range(0.1f, 1.0f)]
    public float shapeStiffness = 0.8f;

    private Body body;
    private Mesh mesh;
    private MeshCollider meshCollider;

    void Awake()
    {
        body = GetComponent<Body>();
        var meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();

        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError($"{name}: RobustMeshBody requires a MeshFilter with an assigned mesh.", this);
            enabled = false;
            return;
        }
        mesh = meshFilter.sharedMesh;

        // IMPORTANT: The point-in-mesh check requires a MeshCollider.
        // We ensure it's convex for reliable raycasting from the inside.
        meshCollider.sharedMesh = mesh;
        meshCollider.convex = true;
    }

    void Start()
    {
        GenerateFromMesh();
    }

    [ContextMenu("Regenerate Robust Mesh Body")]
    public void GenerateFromMesh()
    {
        if (mesh == null) return;

        var particles = new List<Particle>();

        // --- Step 1: Generate Surface Particles from unique vertices ---
        var uniqueVertices = new List<Vector3>();
        var vertexIndexMap = new Dictionary<Vector3, int>();
        foreach (var vertex in mesh.vertices)
        {
            if (vertexIndexMap.TryAdd(vertex, uniqueVertices.Count))
            {
                uniqueVertices.Add(vertex);
            }

        }
        foreach (var localPos in uniqueVertices)
        {
            particles.Add(new Particle(transform.TransformPoint(localPos), 1f) { body = this.body });
        }

        // --- Step 2: Generate Internal Particles ---
        Bounds bounds = mesh.bounds;
        float step = bounds.size.magnitude / (resolution * 2f);
        for (float x = bounds.min.x; x < bounds.max.x; x += step)
            for (float y = bounds.min.y; y < bounds.max.y; y += step)
                for (float z = bounds.min.z; z < bounds.max.z; z += step)
                {
                    Vector3 localPoint = new Vector3(x, y, z);
                    if (IsPointInsideMesh(localPoint))
                    {
                        particles.Add(new Particle(transform.TransformPoint(localPoint), 1f) { body = this.body });
                    }
                }

        body.particles.Clear();
        body.particles.AddRange(particles);

        // --- Step 3: Create a single Shape Matching Constraint for the whole body ---
        body.constraints.Clear();
        body.shapeMatchingConstraints.Clear(); // Clear the new list
        if (body.particles.Count > 0)
        {
            body.shapeMatchingConstraints.Add(new ShapeMatchingConstraint(body.particles, shapeStiffness));
        }

        // --- Step 4: Normalize Mass ---
        if (body.particles.Count > 0)
        {
            float massPerParticle = totalMass / body.particles.Count;
            foreach (var p in body.particles)
            {
                p.mass = massPerParticle;
            }
        }
        Debug.Log($"{name}: Generated robust body with {body.particles.Count} particles and 1 Shape Matching constraint.");
    }

    /// <summary>
    /// Checks if a point is inside the mesh using the attached MeshCollider.
    /// This is more reliable than manual raycasting.
    /// </summary>
    private bool IsPointInsideMesh(Vector3 localPoint)
    {
        Vector3 worldPoint = transform.TransformPoint(localPoint);
        Vector3 direction = (meshCollider.bounds.center - worldPoint).normalized;
        float distance = Vector3.Distance(meshCollider.bounds.center, worldPoint);

        // Raycast from the point towards the outside. If it doesn't hit anything, it must be outside.
        if (!meshCollider.Raycast(new Ray(worldPoint, -direction), out _, distance * 2))
        {
            return true;
        }
        return false;
    }
}

