using UnityEngine;
using System.Collections.Generic;

public class BasketCollector : MonoBehaviour
{
    [Header("Collection Settings")]
    public float detectionRadius = 1.5f;
    public float collectionInterval = 0.1f;
    public float collectionForce = 3f;
    public bool enableVisualEffects = true;

    [Header("References")]
    public BasketBody basketBody;
    public GameObject collectionEffectPrefab;
    public AudioClip collectionSound;

    private readonly HashSet<Body> collectedBodies = new();
    private AudioSource audioSource;
    private ParticleSystem collectionParticles;

    private void Start()
    {
        // Find BasketBody if not assigned
        if (basketBody == null)
            basketBody = FindObjectOfType<BasketBody>();

        if (basketBody == null)
        {
            Debug.LogWarning("❌ BasketCollector: No BasketBody found!");
            enabled = false;
            return;
        }

        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && collectionSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Setup particle effects
        if (enableVisualEffects)
        {
            SetupVisualEffects();
        }

        InvokeRepeating(nameof(CheckBasketCollisions), 0f, collectionInterval);
    }

    void SetupVisualEffects()
    {
        // Create particle system for collection effects
        GameObject effectObj = new GameObject("CollectionEffects");
        effectObj.transform.SetParent(transform);
        effectObj.transform.localPosition = Vector3.zero;

        collectionParticles = effectObj.AddComponent<ParticleSystem>();
        var main = collectionParticles.main;
        main.startLifetime = 1f;
        main.startSpeed = 2f;
        main.startSize = 0.1f;
        main.maxParticles = 50;

        var emission = collectionParticles.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 10)
        });

        var shape = collectionParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;
    }

    private void CheckBasketCollisions()
    {
        if (basketBody == null) return;

        var allBodies = FindObjectsOfType<Body>();

        foreach (var body in allBodies)
        {
            if (collectedBodies.Contains(body) || body.particles == null || body == basketBody.GetComponent<Body>())
                continue;

            bool shouldCollect = false;
            Vector3 closestPoint = Vector3.zero;
            float closestDistance = float.MaxValue;

            // Find the closest particle to the basket
            foreach (var p in body.particles)
            {
                float distance = Vector3.Distance(p.position, basketBody.transform.position);
                if (distance < detectionRadius && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPoint = p.position;
                    shouldCollect = true;
                }
            }

            if (shouldCollect)
            {
                CollectBody(body, closestPoint);
            }
        }
    }

    void CollectBody(Body body, Vector3 collectionPoint)
    {
        collectedBodies.Add(body);

        // Apply collection force to all particles in the body
        Vector3 basketCenter = basketBody.transform.position;
        foreach (var p in body.particles)
        {
            Vector3 direction = (basketCenter - p.position).normalized;
            Vector3 force = direction * collectionForce;
            p.position += force * collectionInterval;
        }

        // Visual effects
        if (enableVisualEffects && collectionParticles != null)
        {
            collectionParticles.transform.position = collectionPoint;
            collectionParticles.Play();
        }

        // Audio effects
        if (audioSource != null && collectionSound != null)
        {
            audioSource.PlayOneShot(collectionSound);
        }

        // Score
        ScoreManager.Instance?.IncreaseScore(1);
        Debug.Log($"✅ Collected: {body.name}");

        // Optional: Destroy the body after a delay
        StartCoroutine(DestroyBodyAfterDelay(body, 2f));
    }

    System.Collections.IEnumerator DestroyBodyAfterDelay(Body body, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (body != null && body.gameObject != null)
        {
            // Remove from simulation manager
            var simManager = FindObjectOfType<SimulationManager>();
            if (simManager != null && simManager.bodies.Contains(body))
            {
                simManager.bodies.Remove(body);
            }

            // Destroy the GameObject
            Destroy(body.gameObject);
        }
    }

    void OnDrawGizmos()
    {
        if (basketBody == null) return;

        // Draw detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(basketBody.transform.position, detectionRadius);

        // Draw collection area
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(basketBody.transform.position, detectionRadius * 0.8f);
    }
}
