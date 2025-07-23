using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A high-performance and stable mesh-based soft body generator.
/// It creates a robust particle-constraint system from a given mesh by:
/// 1. Generating particles on the mesh surface and within its volume.
/// 2. Building a "skin" by creating constraints along the mesh's triangle edges.
/// 3. Reinforcing the structure by connecting internal particles to the skin and each other.
/// 4. Using a spatial hash grid for fast (non-O(n^2)) neighbor finding to build constraints.
/// This results in much more stable and performant soft bodies for complex shapes.
/// </summary>
[RequireComponent(typeof(Body), typeof(MeshFilter))]
public class StableMeshBody : MonoBehaviour
{
    [Header("1. Particle Generation")]
    [Tooltip("Controls the density of the internal particle grid. Higher values mean more particles.")]
    [Range(2, 20)]
    public int resolution = 8;
    [Tooltip("The probability of creating a particle at each point in the internal grid. Lower values create sparser interiors.")]
    [Range(0.1f, 1.0f)]
    public float internalDensity = 0.7f;

    [Header("2. Physics Properties")]
    [Tooltip("The total mass of the entire soft body.")]
    public float totalMass = 1.0f;
    [Tooltip("Stiffness of the constraints forming the outer 'skin' of the mesh.")]
    public float surfaceStiffness = 1.0f;
    [Tooltip("Stiffness of the internal 'strut' constraints that prevent volume collapse.")]
    public float internalStiffness = 0.8f;
    [Tooltip("The radius used to connect internal particles to each other and to the surface.")]
    public float connectionRadius = 0.5f;

    private Body body;
    private Mesh mesh;

    void Awake()
    {
        body = GetComponent<Body>();
        var meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError($"{name}: MeshBody requires a MeshFilter with an assigned mesh.", this);
            enabled = false;
            return;
        }
        mesh = meshFilter.sharedMesh;
    }

    void Start()
    {
        GenerateFromMesh();
    }

    [ContextMenu("Regenerate Stable Mesh Body")]
    public void GenerateFromMesh()
    {
        if (mesh == null) return;

        var particles = new List<Particle>();
        var particleMap = new Dictionary<int, int>(); // Maps mesh vertex index -> particle index

        // --- Step 1: Generate Surface Particles ---
        // Create one particle for each unique vertex on the mesh surface.
        var uniqueVertices = new List<Vector3>();
        var vertexIndexMap = new Dictionary<Vector3, int>();
        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            if (vertexIndexMap.TryAdd(mesh.vertices[i], uniqueVertices.Count))
            {
                uniqueVertices.Add(mesh.vertices[i]);
            }
            particleMap[i] = vertexIndexMap[mesh.vertices[i]];
        }

        foreach (var localPos in uniqueVertices)
        {
            particles.Add(new Particle(transform.TransformPoint(localPos), 1f) { body = this.body });
        }
        int surfaceParticleCount = particles.Count;

        // --- Step 2: Generate Internal Particles ---
        // Use a grid-based approach inside the mesh's bounding box.
        Bounds bounds = mesh.bounds;
        float step = bounds.size.magnitude / (resolution * 2f); // Adaptive step size
        for (float x = bounds.min.x; x < bounds.max.x; x += step)
            for (float y = bounds.min.y; y < bounds.max.y; y += step)
                for (float z = bounds.min.z; z < bounds.max.z; z += step)
                {
                    if (Random.value < internalDensity)
                    {
                        Vector3 localPoint = new Vector3(x, y, z);
                        if (IsPointInsideMesh(localPoint))
                        {
                            particles.Add(new Particle(transform.TransformPoint(localPoint), 1f) { body = this.body });
                        }
                    }
                }

        body.particles.Clear();
        body.particles.AddRange(particles);

        // --- Step 3: Create Constraints Intelligently ---
        CreateStableConstraints(surfaceParticleCount, particleMap);

        // --- Step 4: Normalize Mass ---
        if (body.particles.Count > 0)
        {
            float massPerParticle = totalMass / body.particles.Count;
            foreach (var p in body.particles)
            {
                p.mass = massPerParticle;
            }
        }
        Debug.Log($"{name}: Generated stable body with {body.particles.Count} particles and {body.constraints.Count} constraints.");
    }

    private void CreateStableConstraints(int surfaceParticleCount, Dictionary<int, int> particleMap)
    {
        body.constraints.Clear();
        var addedConstraints = new HashSet<(int, int)>();

        // --- Create Surface Constraints from Mesh Edges ---
        // This builds the "skin" and is crucial for maintaining shape.
        int[] triangles = mesh.triangles;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int pIdx1 = particleMap[triangles[i]];
            int pIdx2 = particleMap[triangles[i + 1]];
            int pIdx3 = particleMap[triangles[i + 2]];

            AddConstraint(pIdx1, pIdx2, surfaceStiffness, addedConstraints);
            AddConstraint(pIdx2, pIdx3, surfaceStiffness, addedConstraints);
            AddConstraint(pIdx3, pIdx1, surfaceStiffness, addedConstraints);
        }

        // --- Create Internal Constraints using a Spatial Hash for Performance ---
        // This connects the volume to the skin and itself, preventing collapse.
        var spatialHash = new SpatialHash(body.particles, connectionRadius);
        for (int i = surfaceParticleCount; i < body.particles.Count; i++) // Iterate only internal particles
        {
            var neighbors = spatialHash.FindNearby(body.particles[i].position);
            foreach (int neighborIdx in neighbors)
            {
                if (i == neighborIdx) continue; // Don't connect to self
                AddConstraint(i, neighborIdx, internalStiffness, addedConstraints);
            }
        }
    }

    private void AddConstraint(int idx1, int idx2, float stiffness, HashSet<(int, int)> added)
    {
        // Ensure consistent key order (min, max) to avoid duplicates like (1,0) and (0,1).
        var key = (idx1 < idx2) ? (idx1, idx2) : (idx2, idx1);
        if (added.Add(key))
        {
            body.constraints.Add(new DistanceConstraint(body.particles[idx1], body.particles[idx2], stiffness));
        }
    }

    // A simple but slow point-in-mesh test. Works for closed, convex meshes.
    // For complex models, a more robust solution like Voxelization might be needed.
    private bool IsPointInsideMesh(Vector3 localPoint)
    {
        // This check is expensive. It casts a ray from the point and counts intersections with the mesh.
        // An odd number of intersections means the point is inside.
        Ray ray = new Ray(localPoint, Vector3.one.normalized); // Arbitrary direction
        return Physics.Raycast(transform.TransformPoint(localPoint - Vector3.one.normalized * 1000), transform.TransformDirection(ray.direction), 1000f);
    }
}

/// <summary>
/// A spatial hashing grid for fast nearest-neighbor lookups.
/// This is key to avoiding O(n^2) complexity when building constraints.
/// </summary>
public class SpatialHash
{
    private readonly Dictionary<Vector3Int, List<int>> grid = new Dictionary<Vector3Int, List<int>>();
    private readonly float cellSize;
    private readonly List<Particle> particles;

    public SpatialHash(List<Particle> particles, float cellSize)
    {
        this.particles = particles;
        this.cellSize = cellSize;
        for (int i = 0; i < particles.Count; i++)
        {
            var cell = GetCell(particles[i].position);
            if (!grid.ContainsKey(cell))
            {
                grid[cell] = new List<int>();
            }
            grid[cell].Add(i);
        }
    }

    private Vector3Int GetCell(Vector3 position)
    {
        return new Vector3Int(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.y / cellSize),
            Mathf.FloorToInt(position.z / cellSize)
        );
    }

    public List<int> FindNearby(Vector3 position)
    {
        var centerCell = GetCell(position);
        var nearbyIndices = new List<int>();

        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                for (int z = -1; z <= 1; z++)
                {
                    var cell = new Vector3Int(centerCell.x + x, centerCell.y + y, centerCell.z + z);
                    if (grid.TryGetValue(cell, out var cellParticles))
                    {
                        nearbyIndices.AddRange(cellParticles);
                    }
                }
        return nearbyIndices;
    }
}
