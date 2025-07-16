using UnityEngine;
using System.Collections.Generic;

public class BasketCollector : MonoBehaviour
{
    public float detectionRadius = 1.5f;
    public float collectionInterval = 0.1f;
    public MeshCollider basketCollider;

    private readonly HashSet<Body> collectedBodies = new();

    private void Start()
    {
        if (basketCollider == null)
            basketCollider = GetComponent<MeshCollider>();

        if (basketCollider == null)
        {
            Debug.LogWarning("❌ BasketCollector: basketCollider is not assigned!");
            enabled = false;
            return;
        }

        InvokeRepeating(nameof(CheckBasketCollisions), 0f, collectionInterval);
    }

    private void CheckBasketCollisions()
    {
        var allBodies = FindObjectsOfType<Body>();

        foreach (var body in allBodies)
        {
            if (collectedBodies.Contains(body) || body.particles == null) continue;

            foreach (var p in body.particles)
            {
                Vector3 closestPoint = basketCollider.ClosestPoint(p.position);
                float distSqr = (p.position - closestPoint).sqrMagnitude;
                float minDist = p.collisionRadius * p.collisionRadius;

                if (distSqr <= minDist)
                {
                    Vector3 normal = (p.position - closestPoint).normalized;
                    Vector3 correctedPos = closestPoint + normal * minDist;

                    // تعديل الموضع لتجنب الاختراق
                    Vector3 correction = correctedPos - p.position;
                    p.position += correction; 
                    p.prevPosition = p.position - correction * 0.5f;

                    collectedBodies.Add(body);
                    ScoreManager.Instance?.IncreaseScore(1);
                    Debug.Log($"✅ جمعنا: {body.name}");
                    break; // نخرج من الجسيمات بعد أول تصادم
                }
            }
        }
    }

    public void ClearCollected()
    {
        collectedBodies.Clear();
    }
}
