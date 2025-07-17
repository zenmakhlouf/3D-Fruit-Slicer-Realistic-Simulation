using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Body))]
public class BasketBody : MonoBehaviour
{
    [Header("Basket Generation")]
    public float basketRadius = 1.5f;
    public float basketHeight = 0.8f;
    public int resolution = 8;
    public float totalMass = 2f;

    [Header("Basket Physics")]
    public float surfaceStiffness = 0.8f;
    public float volumeStiffness = 0.6f;
    public float connectionRadiusMultiplier = 2.0f;

    [Header("Mouse Control")]
    public float mouseSensitivity = 5f;
    public float maxDistanceFromCamera = 10f;
    public float minDistanceFromCamera = 2f;
    public LayerMask groundLayer = 1;

    [Header("Collection")]
    public float collectionRadius = 1.2f;
    public float collectionForce = 5f;
    public bool enableCollection = true;

    private Body body;
    private Camera mainCamera;
    private bool isDragging = false;
    private Vector3 dragOffset;
    private Plane dragPlane;

    // Collection tracking
    private HashSet<Body> collectedBodies = new HashSet<Body>();
    private List<Particle> collectionParticles = new List<Particle>();

    void Start()
    {
        body = GetComponent<Body>();
        mainCamera = Camera.main;

        if (body == null)
        {
            Debug.LogError("❌ BasketBody: No Body component found!");
            enabled = false;
            return;
        }

        GenerateBasket();
        SetupMouseControl();
    }

    void Update()
    {
        HandleMouseInput();

        if (enableCollection)
        {
            UpdateCollection();
        }
    }

    void GenerateBasket()
    {
        // Clear existing particles and constraints
        body.particles.Clear();
        body.constraints.Clear();

        List<Particle> basketParticles = new List<Particle>();

        // Generate basket shape (cylinder with open top)
        GenerateBasketShape(basketParticles);

        // Distribute mass
        float massPerParticle = totalMass / basketParticles.Count;
        foreach (var particle in basketParticles)
        {
            particle.SetMass(massPerParticle);
            particle.body = body;
            particle.collisionRadius = basketRadius / resolution;
            body.particles.Add(particle);
        }

        // Create constraints to maintain basket shape
        CreateBasketConstraints();

        Debug.Log($"🧺 Generated basket with {body.particles.Count} particles and {body.constraints.Count} constraints");
    }

    void GenerateBasketShape(List<Particle> particles)
    {
        // Bottom ring
        int bottomRingCount = resolution * 2;
        for (int i = 0; i < bottomRingCount; i++)
        {
            float angle = (i / (float)bottomRingCount) * 2f * Mathf.PI;
            Vector3 localPos = new Vector3(
                Mathf.Cos(angle) * basketRadius,
                0,
                Mathf.Sin(angle) * basketRadius
            );
            Vector3 worldPos = transform.TransformPoint(localPos);
            particles.Add(new Particle(worldPos, 1f));
        }

        // Side rings
        int sideRingCount = resolution;
        for (int ring = 1; ring <= 3; ring++)
        {
            float height = (ring / 3f) * basketHeight;
            for (int i = 0; i < sideRingCount; i++)
            {
                float angle = (i / (float)sideRingCount) * 2f * Mathf.PI;
                Vector3 localPos = new Vector3(
                    Mathf.Cos(angle) * basketRadius,
                    height,
                    Mathf.Sin(angle) * basketRadius
                );
                Vector3 worldPos = transform.TransformPoint(localPos);
                particles.Add(new Particle(worldPos, 1f));
            }
        }

        // Top ring (slightly smaller for funnel effect)
        float topRadius = basketRadius * 0.8f;
        for (int i = 0; i < sideRingCount; i++)
        {
            float angle = (i / (float)sideRingCount) * 2f * Mathf.PI;
            Vector3 localPos = new Vector3(
                Mathf.Cos(angle) * topRadius,
                basketHeight,
                Mathf.Sin(angle) * topRadius
            );
            Vector3 worldPos = transform.TransformPoint(localPos);
            particles.Add(new Particle(worldPos, 1f));
        }

        // Add some internal particles for volume
        for (int i = 0; i < resolution; i++)
        {
            Vector3 localPos = new Vector3(
                Random.Range(-basketRadius * 0.5f, basketRadius * 0.5f),
                Random.Range(0.2f, basketHeight * 0.8f),
                Random.Range(-basketRadius * 0.5f, basketRadius * 0.5f)
            );
            Vector3 worldPos = transform.TransformPoint(localPos);
            particles.Add(new Particle(worldPos, 1f));
        }
    }

    void CreateBasketConstraints()
    {
        int particleCount = body.particles.Count;

        // Connect particles within rings
        for (int ring = 0; ring < 5; ring++) // 5 rings total
        {
            int particlesPerRing = ring == 0 ? resolution * 2 : resolution;
            int startIndex = GetRingStartIndex(ring);

            for (int i = 0; i < particlesPerRing; i++)
            {
                int currentIndex = startIndex + i;
                int nextIndex = startIndex + ((i + 1) % particlesPerRing);
                int diagIndex = startIndex + ((i + 2) % particlesPerRing); // Diagonal for extra rigidity

                if (currentIndex < particleCount && nextIndex < particleCount)
                {
                    body.constraints.Add(new DistanceConstraint(
                        body.particles[currentIndex],
                        body.particles[nextIndex],
                        surfaceStiffness
                    ));
                }
                // Diagonal connection (skip if particlesPerRing < 6)
                if (particlesPerRing >= 6 && currentIndex < particleCount && diagIndex < particleCount)
                {
                    body.constraints.Add(new DistanceConstraint(
                        body.particles[currentIndex],
                        body.particles[diagIndex],
                        surfaceStiffness * 0.7f
                    ));
                }
            }
        }

        // Connect rings vertically
        for (int ring = 0; ring < 4; ring++)
        {
            int currentRingCount = ring == 0 ? resolution * 2 : resolution;
            int nextRingCount = resolution;
            int currentStart = GetRingStartIndex(ring);
            int nextStart = GetRingStartIndex(ring + 1);

            for (int i = 0; i < Mathf.Min(currentRingCount, nextRingCount); i++)
            {
                int currentIndex = currentStart + i;
                int nextIndex = nextStart + i;
                int diagIndex = nextStart + ((i + 1) % nextRingCount); // Diagonal vertical

                if (currentIndex < particleCount && nextIndex < particleCount)
                {
                    body.constraints.Add(new DistanceConstraint(
                        body.particles[currentIndex],
                        body.particles[nextIndex],
                        surfaceStiffness
                    ));
                }
                // Diagonal vertical connection
                if (currentIndex < particleCount && diagIndex < particleCount)
                {
                    body.constraints.Add(new DistanceConstraint(
                        body.particles[currentIndex],
                        body.particles[diagIndex],
                        surfaceStiffness * 0.7f
                    ));
                }
            }
        }

        // Add some cross-connections for stability (long-range, not n²)
        int crossStep = Mathf.Max(2, resolution / 3);
        for (int i = 0; i < particleCount; i += crossStep)
        {
            for (int j = i + crossStep; j < Mathf.Min(i + 2 * crossStep, particleCount); j++)
            {
                body.constraints.Add(new DistanceConstraint(
                    body.particles[i],
                    body.particles[j],
                    volumeStiffness * 0.5f
                ));
            }
        }
    }

    int GetRingStartIndex(int ring)
    {
        if (ring == 0) return 0; // Bottom ring
        if (ring == 1) return resolution * 2; // First side ring
        if (ring == 2) return resolution * 3; // Second side ring
        if (ring == 3) return resolution * 4; // Third side ring
        if (ring == 4) return resolution * 5; // Top ring
        return resolution * 6; // Internal particles
    }

    void SetupMouseControl()
    {
        // Create drag plane
        dragPlane = new Plane(Vector3.up, transform.position);
    }

    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (IsMouseOverBasket(ray))
            {
                isDragging = true;
                if (dragPlane.Raycast(ray, out float enter))
                {
                    dragOffset = transform.position - ray.GetPoint(enter);
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (dragPlane.Raycast(ray, out float enter))
            {
                Vector3 targetPosition = ray.GetPoint(enter) + dragOffset;

                // Constrain to camera distance
                Vector3 cameraPos = mainCamera.transform.position;
                Vector3 direction = (targetPosition - cameraPos).normalized;
                float distance = Mathf.Clamp(Vector3.Distance(cameraPos, targetPosition), minDistanceFromCamera, maxDistanceFromCamera);
                targetPosition = cameraPos + direction * distance;

                // Ground collision
                if (Physics.Raycast(targetPosition + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f, groundLayer))
                {
                    targetPosition.y = Mathf.Max(targetPosition.y, hit.point.y + 0.5f);
                }

                // Apply movement to all particles
                Vector3 movement = targetPosition - transform.position;
                foreach (var particle in body.particles)
                {
                    particle.position += movement;
                    particle.prevPosition += movement;
                }

                transform.position = targetPosition;
            }
        }
    }

    bool IsMouseOverBasket(Ray ray)
    {
        // Simple sphere check for basket selection
        float distance = Vector3.Distance(ray.origin, transform.position);
        if (distance > maxDistanceFromCamera) return false;

        Vector3 closestPoint = ray.origin + ray.direction * Vector3.Dot(transform.position - ray.origin, ray.direction);
        float distToBasket = Vector3.Distance(closestPoint, transform.position);

        return distToBasket < basketRadius * 1.5f;
    }

    void UpdateCollection()
    {
        // Find nearby fruit bodies
        var allBodies = FindObjectsOfType<Body>();

        foreach (var fruitBody in allBodies)
        {
            if (fruitBody == body || collectedBodies.Contains(fruitBody)) continue;

            bool shouldCollect = false;

            // Check if any particle is within collection radius
            foreach (var fruitParticle in fruitBody.particles)
            {
                float distance = Vector3.Distance(fruitParticle.position, transform.position);
                if (distance < collectionRadius)
                {
                    shouldCollect = true;

                    // Apply collection force
                    Vector3 direction = (transform.position - fruitParticle.position).normalized;
                    Vector3 force = direction * collectionForce;

                    // Apply force to all particles in the fruit
                    foreach (var p in fruitBody.particles)
                    {
                        p.position += force * Time.deltaTime;
                    }
                    break;
                }
            }

            if (shouldCollect)
            {
                collectedBodies.Add(fruitBody);
                ScoreManager.Instance?.IncreaseScore(1);
                Debug.Log($"🧺 Collected fruit: {fruitBody.name}");
            }
        }
    }

    void OnDrawGizmos()
    {
        // Draw basket bounds
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, basketRadius);

        // Draw collection radius
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, collectionRadius);

        // Draw drag area
        if (isDragging)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, basketRadius * 1.5f);
        }
    }
}