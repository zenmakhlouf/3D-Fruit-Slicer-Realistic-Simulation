using System.Collections.Generic;
using UnityEngine;

public class Knife : MonoBehaviour
{
    public MeshCollider knifeCollider;
    public SimulationManager simManager;
    public float connectionRadius = 0.3f;
    public float stiffness = 0.5f;
    public float cutImpulse = 0.05f; 
    public float impulseDamping = 0.8f; 
    public float cutDistanceThreshold = 0.2f; 
    public float rigidbodyImpulseMultiplier = 10f; 
    public GameObject sparkPrefab; 
    public AudioClip cutSound;

    void Start()
{
    InvokeRepeating(nameof(CleanupBodies), 10f, 10f);
}


    void Update()
    {
        if (knifeCollider == null || simManager == null)
        {
            // Debug.LogWarning("KnifeCollider or SimulationManager is null!");
            return;
        }

        Dictionary<Vector3Int, List<Particle>> grid = new Dictionary<Vector3Int, List<Particle>>();
        float cellSize = knifeCollider.bounds.size.magnitude;

        // Build spatial grid
        foreach (Body fruitBody in simManager.bodies)
        {
            if (fruitBody == null || fruitBody.particles == null) continue;
            foreach (Particle p in fruitBody.particles)
            {
                if (p == null) continue;
                Vector3Int cell = GetGridCell(p.position, cellSize);
                if (!grid.TryGetValue(cell, out var cellList))
                {
                    cellList = new List<Particle>();
                    grid[cell] = cellList;
                }
                cellList.Add(p);
            }
        }

        // Check for nearby particles
        List<Particle> cutParticles = new List<Particle>();
        Vector3Int knifeCell = GetGridCell(knifeCollider.transform.position, cellSize);
        for (int x = -1; x <= 1; x++)
        for (int y = -1; y <= 1; y++)
        for (int z = -1; z <= 1; z++)
        {
            Vector3Int neighborCell = knifeCell + new Vector3Int(x, y, z);
            if (!grid.TryGetValue(neighborCell, out var particles)) continue;

            foreach (Particle p in particles)
            {
                Vector3 closestPoint = knifeCollider.ClosestPoint(p.position);
                float dist = Vector3.Distance(closestPoint, p.position);
                if (dist < p.collisionRadius)
                {
                    cutParticles.Add(p);
                }
            }
        }

        if (cutParticles.Count > 0)
        {
            HandleCut(cutParticles[0].body, cutParticles);
        }
    }

    void HandleCut(Body originalBody, List<Particle> cutParticles)
    {
        if (originalBody == null || originalBody.particles == null)
        {
            // Debug.LogWarning("Original body or its particles are null!");
            return;
        }

        if (originalBody.name.Contains("FruitPart"))
        {
            // Debug.Log("Skipping cut: already a FruitPart.");
            return;
        }


        // Create new body
        GameObject newObj = new GameObject("FruitPart");
        newObj.transform.position = originalBody.transform.position;
        Body newBody = newObj.AddComponent<Body>();

        // Copy Body settings
        newBody.gravity = originalBody.gravity;
        newBody.fixedTimeStep = originalBody.fixedTimeStep;
        newBody.solverIterations = originalBody.solverIterations;
        newBody.groundRestitution = originalBody.groundRestitution;
        newBody.enableGroundCollision = originalBody.enableGroundCollision;
        newBody.runSimulation = true;

        MeshFilter meshFilter = newObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = newObj.AddComponent<MeshRenderer>();
        MeshCollider meshCollider = newObj.AddComponent<MeshCollider>();

        // Add Rigidbody for physics fallback
        Rigidbody rb = newObj.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.isKinematic = true; // Default to kinematic, enable only if no particles
        rb.mass = 1f;
        rb.linearDamping = 0.1f;
        rb.angularDamping = 0.05f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Copy material and add cut surface material
        Material cutMaterial = new Material(Shader.Find("Standard"));
        cutMaterial.color = Color.red;
        meshRenderer.materials = new Material[] { originalBody.GetComponent<MeshRenderer>().material, cutMaterial };
         Vector3 knifePoint = knifeCollider.transform.position;

        // Add spark effect if prefab is assigned
        if (sparkPrefab != null)
        {
            GameObject sparkEffect = Instantiate(sparkPrefab, knifePoint, Quaternion.identity);
            Destroy(sparkEffect, 1f);
        }

        // Define cutting plane
        Vector3 knifeNormal = knifeCollider.transform.right;
        Plane cuttingPlane = new Plane(knifeNormal, knifePoint);

        // Distribute particles to ensure both bodies have at least 3 particles
        List<Particle> toTransfer = new List<Particle>();
        List<Particle> remaining = new List<Particle>();
        foreach (Particle p in originalBody.particles)
        {
            if (p == null) continue;
            float side = cuttingPlane.GetDistanceToPoint(p.position);
            if (side > 0)
                toTransfer.Add(p);
            else
                remaining.Add(p);
        }

        // Balance particles to ensure both bodies have at least 3 particles
        if (toTransfer.Count < 3 || remaining.Count < 3)
        {
            if (toTransfer.Count > remaining.Count && toTransfer.Count >= 3)
            {
                while (toTransfer.Count > 3 && remaining.Count < 3)
                {
                    Particle p = toTransfer[toTransfer.Count - 1];
                    toTransfer.RemoveAt(toTransfer.Count - 1);
                    remaining.Add(p);
                }
            }
            else if (remaining.Count > toTransfer.Count && remaining.Count >= 3)
            {
                while (remaining.Count > 3 && toTransfer.Count < 3)
                {
                    Particle p = remaining[remaining.Count - 1];
                    remaining.RemoveAt(remaining.Count - 1);
                    toTransfer.Add(p);
                }
            }
        }

        // Final check: both bodies must have at least 3 particles
        if (toTransfer.Count < 3 || remaining.Count < 3)
        {
            // Debug.LogWarning($"Cannot cut: Not enough particles (New: {toTransfer.Count}, Original: {remaining.Count})");
            Destroy(newObj);
            return;
        }

        // Apply impulse to particles near the cutting plane
        foreach (Particle p in originalBody.particles)
        {
            if (p == null) continue;
            float side = cuttingPlane.GetDistanceToPoint(p.position);
            if (Mathf.Abs(side) < cutDistanceThreshold)
            {
                Vector3 impulse = (toTransfer.Contains(p) ? knifeNormal : -knifeNormal) * cutImpulse * impulseDamping;
                p.prevPosition -= impulse * originalBody.fixedTimeStep;
            }
        }

        // Transfer particles
        originalBody.particles.Clear();
        originalBody.particles.AddRange(remaining);
        foreach (var p in toTransfer)
        {
            newBody.particles.Add(p);
            p.body = newBody;
            p.color = Color.red;
        }

        // Split constraints
        List<DistanceConstraint> keptConstraints = new List<DistanceConstraint>();
        foreach (var constraint in originalBody.constraints)
        {
            if (constraint == null || constraint.p1 == null || constraint.p2 == null) continue;
            bool aInNew = newBody.particles.Contains(constraint.p1);
            bool bInNew = newBody.particles.Contains(constraint.p2);

            if (aInNew && bInNew)
                newBody.constraints.Add(constraint);
            else if (!aInNew && !bInNew)
                keptConstraints.Add(constraint);
        }
        originalBody.constraints = keptConstraints;

        // Rebuild constraints
        RebuildConstraints(originalBody, connectionRadius, stiffness);
        RebuildConstraints(newBody, connectionRadius, stiffness);

        // // Add new body to SimulationManager
        simManager.bodies.Add(newBody);


        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null && cutSound != null)
        {
            audioSource.PlayOneShot(cutSound);
        }

        ScoreManager scoreManager = FindObjectOfType<ScoreManager>();
        if (scoreManager != null)
        {
            scoreManager.IncreaseScore(1);
        }

    Debug.Log($"✅ Cut successful! Transferred {toTransfer.Count} particles. New body particles: {newBody.particles.Count}, Original body particles: {originalBody.particles.Count}");

// إزالة المرجع أولاً
    simManager.bodies.Remove(originalBody);

    // تدمير الأجسام بعد فترة للسماح للأصوات أو التأثيرات بالانتهاء
    Destroy(newObj, 2f);
    Destroy(originalBody.gameObject, 2f);


    }

    Vector3Int GetGridCell(Vector3 position, float cellSize)
    {
        return new Vector3Int(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.y / cellSize),
            Mathf.FloorToInt(position.z / cellSize)
        );
    }
    

    void RebuildConstraints(Body body, float connectionRadius, float stiffness)
    {
        if (body == null || body.particles == null) return;
        body.constraints.Clear();
        Dictionary<Vector3Int, List<Particle>> grid = new Dictionary<Vector3Int, List<Particle>>();
        float cellSize = connectionRadius;

        foreach (var p in body.particles)
        {
            if (p == null) continue;
            Vector3Int cell = GetGridCell(p.position, cellSize);
            if (!grid.TryGetValue(cell, out var cellList))
            {
                cellList = new List<Particle>();
                grid[cell] = cellList;
            }
            cellList.Add(p);
        }

        foreach (var p in body.particles)
        {
            if (p == null) continue;
            Vector3Int cell = GetGridCell(p.position, cellSize);
            int maxLinks = 6;

            for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            for (int z = -1; z <= 1; z++)
            {
                Vector3Int neighborCell = cell + new Vector3Int(x, y, z);
                if (!grid.TryGetValue(neighborCell, out var neighbors)) continue;

                foreach (var neighbor in neighbors)
                {
                    if (p == neighbor || neighbor == null) continue;

                    float dist = Vector3.SqrMagnitude(p.position - neighbor.position);
                    if (dist <= connectionRadius * connectionRadius)
                    {
                        if (p.GetHashCode() < neighbor.GetHashCode())
                        {
                            body.constraints.Add(new DistanceConstraint(p, neighbor, stiffness));
                        }
                    }
                }
            }
        }
    }

    void CleanupBodies()
{
    for (int i = simManager.bodies.Count - 1; i >= 0; i--)
    {
        Body body = simManager.bodies[i];
        if (body == null || body.gameObject == null)
        {
            simManager.bodies.RemoveAt(i);
            continue;
        }
        // يمكنك إضافة شروط إضافية للتدمير، مثلا إذا الجسم بعيد أو غير نشط لفترة طويلة
        if (!body.gameObject.activeInHierarchy)
        {
            Destroy(body.gameObject);
            simManager.bodies.RemoveAt(i);
        }
    }
}

}