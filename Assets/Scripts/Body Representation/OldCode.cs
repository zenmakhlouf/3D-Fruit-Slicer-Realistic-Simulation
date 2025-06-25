// using UnityEngine;

// public class Particle
// {
//     public Vector3 position;
//     public Vector3 prevPosition;
//     public float mass = 1f;
//     public bool isFixed = false;

//     public Particle(Vector3 pos, float mass, bool isFixed = false)
//     {
//         this.position = pos;
//         this.prevPosition = pos;
//         this.mass = mass;
//         this.isFixed = isFixed;
//     }

//     public void Integrate(float deltaTime, Vector3 gravity, float damping = 0.98f)
//     {
//         if (isFixed) return;

//         Vector3 velocity = (position - prevPosition) * damping;
//         prevPosition = position;
//         position += velocity + gravity * (deltaTime * deltaTime);
//     }
// }

// using UnityEngine;

// public class DistanceConstraint
// {
//     public Particle p1, p2;
//     public float restLength;
//     public float stiffness = 1.0f;

//     public DistanceConstraint(Particle a, Particle b, float stiffness = 1.0f)
//     {
//         p1 = a;
//         p2 = b;
//         restLength = Vector3.Distance(p1.position, p2.position);
//         this.stiffness = stiffness;
//     }

//     public void Solve()
//     {
//         Vector3 delta = p2.position - p1.position;
//         float dist = delta.magnitude;

//         if (dist < 1e-6f) return;

//         float diff = (dist - restLength) / dist;

//         float w1 = p1.isFixed ? 0f : 1f / p1.mass;
//         float w2 = p2.isFixed ? 0f : 1f / p2.mass;
//         float wSum = w1 + w2;
//         if (wSum == 0f) return;

//         Vector3 correction = delta * stiffness * diff;
//         if (!p1.isFixed)
//             p1.position += correction * (w1 / wSum);
//         if (!p2.isFixed)
//             p2.position -= correction * (w2 / wSum);
//     }
// }

// using System.Collections.Generic;
// using UnityEngine;

// public class Body : MonoBehaviour
// {
//     public List<Particle> particles = new List<Particle>();
//     public List<DistanceConstraint> constraints = new List<DistanceConstraint>();

//     public Vector3 gravity = new Vector3(0, -9.81f, 0);
//     public float stepSize = 0.02f;
//     public int solverIterations = 5;

//     void Update()
//     {
//         if (particles.Count < 8)
//             Debug.Log("Body particles still not populated: " + particles.Count);
//         Simulate(Time.deltaTime);

//     }

//     void Simulate(float dt)
//     {
//         foreach (var p in particles)
//         {
//             p.Integrate(dt, gravity);
//             if (p.position.y < 0.0f)
//             {
//                 p.position.y = 0.0f;
//                 float velocityY = p.position.y - p.prevPosition.y;
//                 p.prevPosition.y = p.position.y + velocityY * 0.5f;
//             }
//         }


//         for (int i = 0; i < solverIterations; i++)
//             foreach (var c in constraints)
//                 c.Solve();
//     }
//     void OnDrawGizmos()
//     {
//         if (particles == null) return;

//         Gizmos.color = Color.yellow;
//         foreach (var p in particles)
//             Gizmos.DrawSphere(p.position, 0.05f);

//         Gizmos.color = Color.cyan;
//         foreach (var c in constraints)
//             Gizmos.DrawLine(c.p1.position, c.p2.position);
//     }
// }

// using UnityEngine;

// public class CubeBody : MonoBehaviour
// {
//     public float size = 1f;
//     public float mass = 1f;
//     public float stiffness = 1f;

//     void Start()
//     {
//         Body body = GetComponent<Body>();
//         if (body == null)
//         {
//             Debug.LogError("No Body component found on this object!");
//             return;
//         }

//         Vector3[] offsets = new Vector3[]
//         {
//             new Vector3(-1,-1,-1), new Vector3(1,-1,-1),
//             new Vector3(1,-1,1), new Vector3(-1,-1,1),
//             new Vector3(-1,1,-1), new Vector3(1,1,-1),
//             new Vector3(1,1,1), new Vector3(-1,1,1)
//         };

//         Particle[] ps = new Particle[8];

//         for (int i = 0; i < 8; i++)
//         {
//             Vector3 worldPos = transform.position + offsets[i] * size * 0.5f;
//             ps[i] = new Particle(worldPos, mass);
//             body.particles.Add(ps[i]);
//         }

//         void Link(int a, int b)
//         {
//             body.constraints.Add(new DistanceConstraint(ps[a], ps[b], stiffness));
//         }

//         int[,] edges = new int[,]
//         {
//             {0,1},{1,2},{2,3},{3,0},
//             {4,5},{5,6},{6,7},{7,4},
//             {0,4},{1,5},{2,6},{3,7}
//         };

//         for (int i = 0; i < edges.GetLength(0); i++)
//             Link(edges[i, 0], edges[i, 1]);

//         Debug.Log("CubeBody initialized particles: " + body.particles.Count);
//     }
// }

// using UnityEngine;
// using System.Collections.Generic;

// public class SphereBody : MonoBehaviour
// {

//     public int resolution = 3;  // increase to add more particles
//     public float radius = 1f;
//     public float mass = 1f;
//     public float stiffness = 1f;

//     void Start()
//     {


//         Body body = GetComponent<Body>();
//         if (body == null)
//         {
//             Debug.LogError("No Body component found!");
//             return;
//         }

//         List<Particle> ps = new List<Particle>();

//         // for (int i = -resolution; i <= resolution; i++)
//         // {
//         //     for (int j = -resolution; j <= resolution; j++)
//         //     {
//         //         for (int k = -resolution; k <= resolution; k++)
//         //         {
//         //             Vector3 offset = new Vector3(i, j, k);

//         //             if (offset.magnitude <= resolution)
//         //             {
//         //                 // Vector3 localPos = offset.normalized * radius * 0.95f * (offset.magnitude / resolution);
//         //                 //  Vector3 worldPos = transform.position + localPos;
//         //                 Vector3 localPos = offset.normalized * radius;
//         //                 Vector3 worldPos = transform.position + localPos;
//         //                 var p = new Particle(worldPos, mass);
//         //                 p.prevPosition = worldPos;
//         //                 ps.Add(p);
//         //             }
//         //         }
//         //     }
//         // }
//         // for (int i = 0; i < resolution; i++)
//         // {
//         //     float phi = Mathf.Acos(1 - 2 * (i + 0.5f) / resolution);
//         //     float theta = Mathf.PI * (1 + Mathf.Sqrt(5)) * i;
//         //     Vector3 localPos = new Vector3(
//         //         Mathf.Cos(theta) * Mathf.Sin(phi),
//         //         Mathf.Sin(theta) * Mathf.Sin(phi),
//         //         Mathf.Cos(phi)
//         //     ) * radius;
//         //     Vector3 worldPos = transform.position + localPos;
//         //     var p = new Particle(worldPos, mass);
//         //     ps.Add(p);
//         // }
//         for (int i = -resolution; i <= resolution; i++)
//             for (int j = -resolution; j <= resolution; j++)
//                 for (int k = -resolution; k <= resolution; k++)
//                 {
//                     Vector3 offset = new Vector3(i, j, k);
//                     if (offset.magnitude <= resolution)
//                     {
//                         Vector3 localPos = offset / resolution * radius;
//                         Vector3 worldPos = transform.position + localPos;
//                         var p = new Particle(worldPos, mass);
//                         ps.Add(p);
//                     }
//                 }

//         foreach (var p in ps)
//             body.particles.Add(p);

//         // Connect nearby particles with springs
//         float connectDist = (radius / resolution) * 1.5f;
//         for (int i = 0; i < ps.Count; i++)
//         {
//             for (int j = i + 1; j < ps.Count; j++)
//             {
//                 if (Vector3.Distance(ps[i].position, ps[j].position) < connectDist)
//                 {
//                     body.constraints.Add(new DistanceConstraint(ps[i], ps[j], stiffness));
//                 }
//             }
//         }
//         transform.localScale = Vector3.one * radius * 2f;

//         Debug.Log($"SphereBody initialized particles: {ps.Count}");
//     }

// }

// // using System.Collections;
// // using System.Collections.Generic;
// // using UnityEngine;

// // [RequireComponent(typeof(MeshFilter))]
// // [RequireComponent(typeof(Body))]
// // public class BodyMeshRenderer : MonoBehaviour
// // {
// //     Mesh mesh;
// //     Vector3[] originalVerts;
// //     Body body;

// //     public List<int> particleVertexMap = new List<int>();  // Maps mesh vertex → particle index

// //     bool initialized = false;

// //     void Start()
// //     {
// //         StartCoroutine(DelayedInit());
// //         Debug.Log("Mesh bounds: " + mesh.bounds.size);
// //     }

// //     public IEnumerator DelayedInit()

// //     {
// //         body = GetComponent<Body>();
// //         mesh = GetComponent<MeshFilter>().mesh;
// //         originalVerts = mesh.vertices;

// //         // Wait until SphereBody populates the particles
// //         while (body == null || body.particles.Count < 8)
// //         {
// //             Debug.Log("Waiting for particles to be populated...");
// //             yield return null;
// //         }

// //         particleVertexMap = new List<int>();

// //         for (int i = 0; i < originalVerts.Length; i++)
// //         {
// //             Vector3 worldVert = transform.TransformPoint(originalVerts[i]);

// //             float minDist = float.MaxValue;
// //             int closestIndex = -1;

// //             for (int j = 0; j < body.particles.Count; j++)
// //             {
// //                 float dist = Vector3.Distance(worldVert, body.particles[j].position);
// //                 if (dist < minDist)
// //                 {
// //                     minDist = dist;
// //                     closestIndex = j;
// //                 }
// //             }

// //             particleVertexMap.Add(closestIndex);
// //         }

// //         Debug.Log("Auto vertex-to-particle mapping complete: " + particleVertexMap.Count + " vertices mapped");
// //         initialized = true;
// //     }
// //     [ContextMenu("Rebind Vertices")]
// //     public void RebindVertices()
// //     {
// //         StartCoroutine(DelayedInit());
// //     }
// //     void Update()
// //     {
// //         if (!initialized || particleVertexMap.Count != originalVerts.Length) return;

// //         Vector3[] newVerts = new Vector3[originalVerts.Length];

// //         for (int i = 0; i < originalVerts.Length; i++)
// //         {
// //             int particleIndex = particleVertexMap[i];
// //             if (particleIndex >= 0 && particleIndex < body.particles.Count)
// //             {
// //                 Vector3 worldPos = body.particles[particleIndex].position;
// //                 newVerts[i] = transform.InverseTransformPoint(worldPos);
// //             }
// //             else
// //             {
// //                 newVerts[i] = originalVerts[i];
// //             }
// //         }
// //         if (Time.frameCount % 60 == 0)
// //         {
// //             Debug.Log($"Vertex 0 → particle {particleVertexMap[0]} at {body.particles[particleVertexMap[0]].position}");
// //         }

// //         mesh.vertices = newVerts;
// //         mesh.RecalculateNormals();

// //     }
// // }


// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using System.Linq;

// [RequireComponent(typeof(MeshFilter))]
// [RequireComponent(typeof(Body))]
// public class BodyMeshRenderer : MonoBehaviour
// {
//     Mesh mesh;
//     Vector3[] originalVerts;
//     Body body;

//     public List<(int, float)>[] vertexToParticles; // For each vertex: list of (particleIndex, weight)

//     bool initialized = false;

//     float someThreshold;

//     void Start()
//     {
//         StartCoroutine(DelayedInit());
//         Debug.Log("Mesh bounds: " + mesh.bounds.size);
//     }

//     public IEnumerator DelayedInit()
//     {
//         body = GetComponent<Body>();
//         mesh = GetComponent<MeshFilter>().mesh;
//         originalVerts = mesh.vertices;

//         // Wait until SphereBody populates the particles
//         while (body == null || body.particles.Count < 8)
//         {
//             Debug.Log("Waiting for particles to be populated...");
//             yield return null;
//         }

//         float someThreshold = 0.2f; // Default fallback
//         var sphere = GetComponent<SphereBody>();
//         if (sphere != null)
//         {
//             someThreshold = (sphere.radius / sphere.resolution) * 1.5f;
//         }

//         vertexToParticles = new List<(int, float)>[originalVerts.Length];

//         for (int i = 0; i < originalVerts.Length; i++)
//         {
//             Vector3 worldVert = transform.TransformPoint(originalVerts[i]);
//             var nearest = new List<(int, float)>();
//             for (int j = 0; j < body.particles.Count; j++)
//             {
//                 float dist = Vector3.Distance(worldVert, body.particles[j].position);
//                 if (dist < someThreshold)
//                     nearest.Add((j, 1f / (dist + 1e-4f)));
//             }
//             // Normalize weights
//             float sum = nearest.Sum(x => x.Item2);
//             for (int k = 0; k < nearest.Count; k++)
//                 nearest[k] = (nearest[k].Item1, nearest[k].Item2 / sum);
//             vertexToParticles[i] = nearest;
//         }

//         Debug.Log("Auto vertex-to-particle mapping complete: " + vertexToParticles.Length + " vertices mapped");
//         initialized = true;
//     }
//     [ContextMenu("Rebind Vertices")]
//     public void RebindVertices()
//     {
//         StartCoroutine(DelayedInit());
//     }
//     void Update()
//     {
//         if (!initialized || vertexToParticles.Length != originalVerts.Length) return;

//         Vector3[] newVerts = new Vector3[originalVerts.Length];

//         for (int i = 0; i < originalVerts.Length; i++)
//         {
//             Vector3 blended = Vector3.zero;
//             foreach (var (idx, w) in vertexToParticles[i])
//                 blended += body.particles[idx].position * w;
//             newVerts[i] = transform.InverseTransformPoint(blended);
//         }
//         if (Time.frameCount % 60 == 0)
//         {
// //            Debug.Log($"Vertex 0 → particle {vertexToParticles[0][0].Item1} at {body.particles[vertexToParticles[0][0].Item1].position}");
//         }

//         mesh.vertices = newVerts;
//         mesh.RecalculateNormals();

//     }
// }