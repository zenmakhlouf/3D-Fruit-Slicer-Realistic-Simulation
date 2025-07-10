using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

[RequireComponent(typeof(Body), typeof(MeshFilter), typeof(MeshCollider))]
public class MeshBody : MonoBehaviour
{
    public int resolution = 5;
    public float totalMass = 1f;
    public float stiffness = 1.0f;
    public float connectionRadius = 0.5f; 
    public bool includeSurfaceVertices = true;
    public bool includeInternalParticles = true;

    private Body body;
    private Mesh mesh;

    private static readonly Vector3Int[] neighborOffsets = {
        new Vector3Int(-1, -1, -1), new Vector3Int(-1, -1, 0), new Vector3Int(-1, -1, 1),
        new Vector3Int(-1, 0, -1),  new Vector3Int(-1, 0, 0),  new Vector3Int(-1, 0, 1),
        new Vector3Int(-1, 1, -1),  new Vector3Int(-1, 1, 0),  new Vector3Int(-1, 1, 1),
        new Vector3Int(0, -1, -1),  new Vector3Int(0, -1, 0),  new Vector3Int(0, -1, 1),
        new Vector3Int(0, 0, -1),   new Vector3Int(0, 0, 0),   new Vector3Int(0, 0, 1),
        new Vector3Int(0, 1, -1),   new Vector3Int(0, 1, 0),   new Vector3Int(0, 1, 1),
        new Vector3Int(1, -1, -1),  new Vector3Int(1, -1, 0),  new Vector3Int(1, -1, 1),
        new Vector3Int(1, 0, -1),   new Vector3Int(1, 0, 0),   new Vector3Int(1, 0, 1),
        new Vector3Int(1, 1, -1),   new Vector3Int(1, 1, 0),   new Vector3Int(1, 1, 1)
    };

    void Start()
    {
        body = GetComponent<Body>();
        mesh = GetComponent<MeshFilter>().sharedMesh;
        GenerateFromMesh();
        Bounds bounds = GetComponent<MeshFilter>().sharedMesh.bounds;

        float size = bounds.size.magnitude;
        //ضرب الطول القطري في 1.5 لزيادة كثافة الجزيئات مع زيادة حجم الجسم.
        resolution = Mathf.Clamp((int)(size * 1.5f), 3, 10);
        MeshCollider meshCollider = GetComponent<MeshCollider>();
    }

    void GenerateFromMesh()
    {
        body.particles.Clear();
        body.constraints.Clear();
        HashSet<Vector3> added = new HashSet<Vector3>();

        // 1. Surface vertices وزيع جزيئات عند رؤوس سطح الميش (Vertices) فقط.
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

        // 2. Internal particles تأكد من أن الجسيم سيتم وضعه بحسب الكثافة
        if (includeInternalParticles)
        {
            Bounds bounds = mesh.bounds;
            Vector3 min = bounds.min;
            Vector3 size = bounds.size;
            float density = 0.7f;   
            float step = Mathf.Min(size.x, size.y, size.z) / resolution;

            for (float x = min.x; x <= min.x + size.x; x += step)
            {
                for (float y = min.y; y <= min.y + size.y; y += step)
                {
                    for (float z = min.z; z <= min.z + size.z; z += step)
                    {
                        Vector3 localPoint = new Vector3(x, y, z);
                        Vector3 worldPoint = transform.TransformPoint(localPoint);
                        if (Random.value < density && (IsNearMeshSurface(worldPoint, step * 0.5f) || FastPointInsideMesh(worldPoint)))
                        {
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

        // 4. Create constraints using spatial grid
        float cellSize = connectionRadius;
        Dictionary<Vector3Int, List<Particle>> grid = new Dictionary<Vector3Int, List<Particle>>();

        // Insert particles into grid
        grid.Clear();
        foreach (var p in body.particles)
        {
            Vector3Int cell = GetGridCell(p.position, cellSize);
            if (!grid.TryGetValue(cell, out var cellList))
            {
                cellList = new List<Particle>(10);
                grid[cell] = cellList;
            }
            cellList.Add(p);
        }

    
       // Keep track of added constraint pairs to avoid duplicates
        HashSet<(int, int)> addedConstraints = new HashSet<(int, int)>();
        object lockObj = new object(); // For thread-safe access to shared structures

        // Parallelized loop over particles هذا الجزء يسرّع الحسابات بفضل المعالجة المتوازية (multi-threaded)، كل جسيم p يتم معالجته بشكل مستقل.
            Parallel.ForEach(body.particles, p =>
            {
                Vector3Int cell = GetGridCell(p.position, cellSize);
                int maxLinks = Mathf.Clamp(6, 3, 10);
                int count = 0;

                foreach (var offset in neighborOffsets)
                {
                    Vector3Int neighborCell = cell + offset;
                    if (!grid.TryGetValue(neighborCell, out var neighbors)) continue;

                    foreach (var neighbor in neighbors)
                    {
                        if (p == neighbor) continue;

                        float dist = Vector3.SqrMagnitude(p.position - neighbor.position);
                        if (dist <= connectionRadius * connectionRadius)
                        {
                            int id1 = p.GetHashCode();
                            int id2 = neighbor.GetHashCode();
                            if (id1 > id2) (id1, id2) = (id2, id1); // Ensure consistent ordering

                            // Lock to avoid race conditions
                            lock (lockObj)
                            {
                                if (addedConstraints.Add((id1, id2)))
                                {
                                    body.constraints.Add(new DistanceConstraint(p, neighbor, stiffness));
                                    count++;
                                    if (count >= maxLinks) break;
                                }
                            }
                        }
                    }

                    if (count >= maxLinks) break;
                }
            });

            // Log results
            // Debug.Log($"[MeshBody] Particles: {body.particles.Count}, Constraints: {body.constraints.Count}");
        }


   //يُطلق شعاع (Raycast) من نقطة على بُعد 100 وحدة إلى يسار النقطة (point - dir * 100f) باتجاه اليمين (dir) لمسافة 200 وحدة.
    bool FastPointInsideMesh(Vector3 point)
    {
        Collider col = GetComponent<Collider>();
        Vector3 dir = Vector3.right;
        if (Physics.Raycast(point - dir * 100f, dir, out var hit, 200f))
        {
            return hit.collider == col;
        }
        return false;
    }


    bool IsNearMeshSurface(Vector3 point, float threshold)
    {
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (meshCollider == null) return false;

        Vector3 closest = meshCollider.ClosestPoint(point);
        return Vector3.Distance(point, closest) <= threshold;
    }

    Vector3Int GetGridCell(Vector3 position, float cellSize)
    {
        return new Vector3Int(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.y / cellSize),
            Mathf.FloorToInt(position.z / cellSize)
        );
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (body.particles.Count < 1000)
        {
            foreach (var p in body.particles)
            {
                Gizmos.DrawSphere(p.position, 0.02f);
                Gizmos.color = p.color;
            }
        }
    }
}
