using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The best-of-both-worlds mesh deformer.
/// Combines robust, feature-rich binding with high-performance, throttled updates.
/// Deforms a mesh to visually represent a soft body's particles, ideal for organic
/// shapes like fruits (apples, watermelons), spheres, and other soft objects.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(Body))]
public class DynamicBodyDeformer : MonoBehaviour
{
    // --- Components & Core Data ---
    private Mesh mesh;
    private Body body;
    private SphereBody sphereBodyComponent; // Cached for sphere-specific optimizations
    private Transform cachedTransform;

    // --- Mesh Data Buffers ---
    private Vector3[] originalMeshVertices; // Original, undeformed vertex positions in local space
    private Vector3[] deformedMeshVertices; // Buffer for calculated deformed vertex positions

    // --- Influence Mapping ---
    // For each mesh vertex, this stores a list of particles that influence it and their calculated weights.
    // This is the core of the binding system.
    private List<(int particleIndex, float weight)>[] vertexParticleInfluenceMap;
    private bool isInitialized = false;

    // --- Inspector Parameters ---
    [Header("1. Binding Configuration")]
    [Tooltip("How far to search for particles around each vertex. If a SphereBody is present, this is a multiplier of its radius. Otherwise, it's an absolute world-space distance.")]
    [Range(0.1f, 2.0f)]
    public float influenceRadiusFactor = 0.5f;

    [Tooltip("The maximum number of the absolute closest particles that can influence a single mesh vertex.")]
    [Range(1, 8)]
    public int maxInfluencersPerVertex = 4;

    [Header("2. Performance Settings")]
    [Tooltip("The time interval in seconds between mesh updates. Lower values are smoother but more performance-intensive. 0.02 = 50 FPS.")]
    [Range(0.01f, 0.1f)]
    public float updateInterval = 0.02f;

    [Tooltip("Recalculate mesh normals every update for correct lighting on deformed surfaces. Can be disabled for a significant performance boost if lighting artifacts are not noticeable.")]
    public bool recalculateNormals = true;

    // --- Internal State ---
    private float updateTimer = 0f;
    private const float UNITY_DEFAULT_SPHERE_LOCAL_RADIUS = 0.5f;

    void Start()
    {
        // --- Component Caching ---
        body = GetComponent<Body>();
        sphereBodyComponent = GetComponent<SphereBody>();
        cachedTransform = transform;

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter.mesh == null)
        {
            Debug.LogError($"DynamicBodyDeformer on {gameObject.name}: MeshFilter has no mesh assigned. Disabling component.", this);
            this.enabled = false;
            return;
        }

        // --- Mesh Initialization ---
        mesh = meshFilter.mesh;
        mesh.MarkDynamic(); // Optimization: Informs Unity that this mesh's geometry will be updated frequently.

        originalMeshVertices = mesh.vertices;
        deformedMeshVertices = new Vector3[originalMeshVertices.Length];

        // --- Start the Binding Process ---
        AutoAdjustScaleForSphere();
        StartCoroutine(InitializeBinding());
    }

    /// <summary>
    /// Runs in LateUpdate to ensure all physics calculations for the frame are complete before deforming the mesh.
    /// This prevents visual stuttering and ensures the visual representation matches the physics state.
    /// </summary>
    void LateUpdate()
    {
        if (!isInitialized) return;

        // --- Throttling Logic ---
        // Accumulate time and only run the expensive deformation logic when the interval has passed.
        updateTimer += Time.deltaTime;
        if (updateTimer < updateInterval)
        {
            return;
        }
        updateTimer = 0f; // Reset timer

        // --- Deformation Logic ---
        for (int i = 0; i < originalMeshVertices.Length; i++)
        {
            var influences = vertexParticleInfluenceMap[i];
            if (influences == null || influences.Count == 0)
            {
                // If a vertex has no influencers, keep its original position.
                deformedMeshVertices[i] = originalMeshVertices[i];
                continue;
            }

            Vector3 blendedWorldPosition = Vector3.zero;
            // Calculate the new world position of the vertex by blending the positions of its influencing particles.
            foreach (var (particleIdx, weight) in influences)
            {
                // Safety check to ensure the particle index is still valid.
                if (particleIdx >= 0 && particleIdx < body.particles.Count)
                {
                    blendedWorldPosition += body.particles[particleIdx].position * weight;
                }
            }
            // Convert the calculated world position back to the object's local space to apply to the mesh.
            deformedMeshVertices[i] = cachedTransform.InverseTransformPoint(blendedWorldPosition);
        }

        // --- Apply to Mesh ---
        mesh.vertices = deformedMeshVertices;

        if (recalculateNormals)
        {
            mesh.RecalculateNormals(); // Expensive, but needed for correct lighting.
        }
        mesh.RecalculateBounds(); // Necessary for the renderer to know the mesh's new size.
    }

    /// <summary>
    /// Waits until the Body's particles are initialized, then calculates the influence map for each vertex.
    /// This is the most performance-intensive part of the script and only runs once at the start.
    /// </summary>
    private IEnumerator InitializeBinding()
    {
        // Wait until the physics body has been properly initialized by the simulation.
        while (body == null || body.particles == null || body.particles.Count == 0)
        {
            yield return null;
        }

        vertexParticleInfluenceMap = new List<(int, float)>[originalMeshVertices.Length];

        // Determine the actual search radius in world units.
        float actualWorldInfluenceRadius = (sphereBodyComponent != null)
            ? influenceRadiusFactor * sphereBodyComponent.radius
            : influenceRadiusFactor;

        for (int i = 0; i < originalMeshVertices.Length; i++)
        {
            vertexParticleInfluenceMap[i] = new List<(int, float)>();
            Vector3 worldVertexPos = cachedTransform.TransformPoint(originalMeshVertices[i]);

            var potentialInfluencers = new List<(int pIndex, float dist)>();
            for (int j = 0; j < body.particles.Count; j++)
            {
                // Optimization: For spheres, ignore the central particle as it doesn't represent the surface.
                if (sphereBodyComponent != null && j == sphereBodyComponent.centerParticlePhysicsIndex)
                {
                    continue;
                }
                potentialInfluencers.Add((j, Vector3.Distance(worldVertexPos, body.particles[j].position)));
            }

            // Sort all potential influencers by distance to find the closest ones.
            potentialInfluencers.Sort((a, b) => a.dist.CompareTo(b.dist));

            var validInfluences = new List<(int pIndex, float rawWeight)>();
            float totalRawWeight = 0f;

            // Limit the number of influencers to the user-defined maximum.
            int influencersToConsider = Mathf.Min(maxInfluencersPerVertex, potentialInfluencers.Count);
            for (int k = 0; k < influencersToConsider; k++)
            {
                var infl = potentialInfluencers[k];
                // Only consider particles within the influence radius.
                if (infl.dist < actualWorldInfluenceRadius)
                {
                    // Use inverse distance for weighting: closer particles have more influence.
                    float weight = 1.0f / (infl.dist + 0.0001f); // Add small epsilon to avoid division by zero.
                    validInfluences.Add((infl.pIndex, weight));
                    totalRawWeight += weight;
                }
                else
                {
                    // Since the list is sorted by distance, we can stop searching once a particle is too far away.
                    break;
                }
            }

            // --- Normalize Weights ---
            if (totalRawWeight > 0f)
            {
                // Normalize the weights so they sum to 1. This ensures a stable, weighted average.
                foreach (var influence in validInfluences)
                {
                    vertexParticleInfluenceMap[i].Add((influence.pIndex, influence.rawWeight / totalRawWeight));
                }
            }
            // --- Fallback Mechanism ---
            else if (potentialInfluencers.Count > 0)
            {
                // If no particles were found within the radius, bind the vertex fully to the single closest particle.
                // This is a robust fallback that prevents parts of the mesh from being left behind.
                vertexParticleInfluenceMap[i].Add((potentialInfluencers[0].pIndex, 1.0f));
            }
        }
        isInitialized = true;
    }

    /// <summary>
    /// If a SphereBody component is present, this helper function automatically scales the
    /// GameObject to match the sphere's radius. Assumes the mesh is a default Unity sphere.
    /// </summary>
    private void AutoAdjustScaleForSphere()
    {
        if (sphereBodyComponent != null)
        {
            float desiredScale = sphereBodyComponent.radius / UNITY_DEFAULT_SPHERE_LOCAL_RADIUS;
            cachedTransform.localScale = new Vector3(desiredScale, desiredScale, desiredScale);
        }
    }

    /// <summary>
    /// Allows you to re-run the binding process from the Inspector while in Play Mode.
    /// Useful for tweaking parameters live without restarting the scene.
    /// </summary>
    [ContextMenu("Rebind Mesh Vertices (Play Mode Only)")]
    public void TriggerRebindVertices()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Rebind can only be triggered in Play Mode.", this);
            return;
        }
        isInitialized = false;
        AutoAdjustScaleForSphere(); // Re-apply scale in case radius changed
        StartCoroutine(InitializeBinding());
    }
}
