using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
public class CollisionManager : MonoBehaviour
{
    public List<SimpleCollider> colliders;


    // Update is called once per frame
    void Update()
    {
        colliders.RemoveAll(item => item == null);

        for (int i = 0; i < colliders.Count; i++)
        {
            for (int j = i + 1; j < colliders.Count; j++)
            {
                SimpleCollider a = colliders[i];
                SimpleCollider b = colliders[j];

                Vector3 delta = b.transform.position - a.transform.position;
                float distance = delta.magnitude;
                float minDistance = a.radius + b.radius;

                if (distance < minDistance)
                {
                    // Enforce separation (move apart by overlap amount /2 each)
                    Vector3 correction = delta.normalized * (minDistance - distance) * 0.5f;
                    a.transform.position -= correction;
                    b.transform.position += correction;

                    ResolveCollision(a, b);
                }
            }
        }
    }
    void ResolveCollision(SimpleCollider a, SimpleCollider b)
    {
        CustomPhysics physA = a.GetComponent<CustomPhysics>();
        CustomPhysics physB = b.GetComponent<CustomPhysics>();

        Vector3 normal = (b.transform.position - a.transform.position).normalized;

        float velAlongNormalA = Vector3.Dot(physA.velocity, normal);
        float velAlongNormalB = Vector3.Dot(physB.velocity, normal);

        float restitution = 0.9f;

        float impulse = (-(1 + restitution) * (velAlongNormalA - velAlongNormalB)) / 2;

        Vector3 impulseVec = impulse * normal;

        physA.velocity += impulseVec;
        physB.velocity -= impulseVec;

        a.GetComponent<MeshDeformer>().ApplyDeformation(a.transform.position + normal * a.radius, 0.2f);
        b.GetComponent<MeshDeformer>().ApplyDeformation(b.transform.position - normal * b.radius, 0.2f);
    }
}
