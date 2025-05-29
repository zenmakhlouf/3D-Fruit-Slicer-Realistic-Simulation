// using System.Collections.Generic;
// using UnityEngine;

// ///<summary>
// /// Detects adn resolves collisions between all soft bodies in the scene
// /// Attach this to a central manager object
// /// </summary>
// public class SoftBodyCollisionManager : MonoBehaviour
// {
//     private Body[] bodies;

//     [System.Obsolete]
//     void Start()
//     {
//         bodies = FindObjectsOfType<Body>();
//     }

//     void Update()
//     {
//         for (int i = 0; i < bodies.Length; i++)
//         {
//             for (int j = i + 1; j < bodies.Length; j++)
//             {
//                 Body a = bodies[i];
//                 Body b = bodies[j];

//                 if (!AABBsOverlap(a, b)) continue;

//                 ResolveSoftBodyCollision(a, b);
//             }
//         }
//     }
//     ///<summary>
//     /// Simple AABB overlap test between two bodies.
//     /// </summary>
//     bool AABBsOverlap(Body a, Body b)
//     {
//         Bounds boundsA = ComputeAABB(a);
//         Bounds boundsB = ComputeAABB(b);
//         return boundsA.Intersects(boundsB);
//     }
//     Bounds ComputeAABB(Body body)
//     {
//         if (body.particles.Count == 0) return new Bounds(body.transform.position, Vector3.zero);
//         Vector3 min = body.particles[0].position;
//         Vector3 max = body.particles[0].position;

//         foreach (var p in body.particles)
//         {
//             min = Vector3.Min(min, p.position);
//             max = Vector3.Max(max, p.position);
//         }
//         return new Bounds((min + max) * 0.5f, max - min);
//     }
//     ///<summary>
//     /// Naive particle-particle collision resolution.
//     /// Later this will be optimized
//     /// </summary>
//     void ResolveSoftBodyCollision(Body a, Body b)
//     {
//         float collisionRadius = 1f;

//         foreach (var pa in a.particles)
//         {
//             foreach (var pb in b.particles)
//             {
//                 Vector3 delta = pb.position - pa.position;
//                 float dist = delta.magnitude;
//                 float target = collisionRadius * 2f;
//                 if (dist < target && dist > 0.0001f)
//                 {
//                     Vector3 correction = (delta.normalized) * (target - dist) * 0.5f;

//                     if (!pa.isFixed) pa.position -= correction;
//                     if (!pb.isFixed) pb.position += correction;
//                 }
//             }
//         }
//     }
// }