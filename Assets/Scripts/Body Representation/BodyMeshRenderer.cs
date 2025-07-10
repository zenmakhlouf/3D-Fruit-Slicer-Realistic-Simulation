using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Deforms a MeshFilter's mesh to visually represent the soft body's particles.
/// Uses weighted blending of nearby particles to move mesh vertices.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(Body))]
public class BodyMeshRenderer : MonoBehaviour
{
    private Mesh mesh;
    private Vector3[] originalMeshVertices; // Original local-space vertex positions
    private Vector3[] deformedMeshVertices; // Buffer for new vertex positions

    private Body body;
    private SphereBody sphereBodyComponent; // Cached for sphere-specific logic

    // For each mesh vertex, stores a list of (particleIndex, weight) for blending
    private List<(int particleIndex, float weight)>[] vertexParticleInfluenceMap;
    private bool isInitialized = false;

    [Header("Mesh Binding Parameters")]
    [Tooltip("How far to look for particles, as a factor of the SphereBody's radius (if present), or as an absolute distance otherwise.")]
    public float influenceRadiusFactor = 0.33f;
    [Tooltip("Max number of closest particles (within influence radius) to bind each vertex to.")]
    public int numClosestParticlesToBind = 4;

    // Standard Unity sphere primitive has a local radius of 0.5
    private const float UNITY_DEFAULT_SPHERE_LOCAL_RADIUS = 0.5f;

    void Start()
    {
        body = GetComponent<Body>();
        sphereBodyComponent = body.GetComponent<SphereBody>();

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter.mesh == null)
        {
            // Debug.LogError($"BodyMeshRenderer on {gameObject.name}: MeshFilter has no mesh assigned.", this);
            this.enabled = false;
            return;
        }
        mesh = meshFilter.mesh;
        originalMeshVertices = mesh.vertices;
        deformedMeshVertices = new Vector3[originalMeshVertices.Length];

        AutoAdjustScaleForSphere();
        StartCoroutine(DelayedInitializeBinding());
    }

    void AutoAdjustScaleForSphere()
    {
        if (sphereBodyComponent != null)
        {
            float desiredScale = sphereBodyComponent.radius / UNITY_DEFAULT_SPHERE_LOCAL_RADIUS;
            transform.localScale = new Vector3(desiredScale, desiredScale, desiredScale);
            // Debug.Log($"BodyMeshRenderer on {gameObject.name}: Auto-scaled to match SphereBody radius {sphereBodyComponent.radius}. Ensure initial GameObject scale was (1,1,1).");
        }
    }

    [ContextMenu("Rebind Mesh Vertices")]
    public void TriggerRebindVertices()
    {
        if (!Application.isPlaying)
        {
            // Debug.LogWarning("Rebind can only be triggered in Play Mode.", this);
            return;
        }
        isInitialized = false;
        AutoAdjustScaleForSphere(); // Re-apply scale in case radius changed
        StartCoroutine(DelayedInitializeBinding());
    }

    IEnumerator DelayedInitializeBinding()
    {
        while (body == null || body.particles == null || body.particles.Count == 0)
        {
            // Debug.Log($"BodyMeshRenderer on {gameObject.name}: Waiting for Body particles...");
            yield return null;
        }
        // Debug.Log($"BodyMeshRenderer on {gameObject.name}: Body particles ready ({body.particles.Count}). Binding mesh vertices...");

        vertexParticleInfluenceMap = new List<(int, float)>[originalMeshVertices.Length];

        float actualWorldInfluenceRadius = influenceRadiusFactor;
        if (sphereBodyComponent != null)
        {
            actualWorldInfluenceRadius = influenceRadiusFactor * sphereBodyComponent.radius;
        }

        for (int i = 0; i < originalMeshVertices.Length; i++)
        {
            vertexParticleInfluenceMap[i] = new List<(int, float)>();
            Vector3 worldVertexPos = transform.TransformPoint(originalMeshVertices[i]);

            var potentialInfluencers = new List<(int pIndex, float dist)>();
            for (int j = 0; j < body.particles.Count; j++)
            {
                if (sphereBodyComponent != null && j == sphereBodyComponent.centerParticlePhysicsIndex)
                {
                    continue; // Skip center particle for surface mesh binding
                }
                potentialInfluencers.Add((j, Vector3.Distance(worldVertexPos, body.particles[j].position)));
            }

            potentialInfluencers.Sort((a, b) => a.dist.CompareTo(b.dist));

            var validInfluences = new List<(int pIndex, float rawWeight)>();
            float totalRawWeight = 0f;

            for (int k = 0; k < Mathf.Min(numClosestParticlesToBind, potentialInfluencers.Count); k++)
            {
                var infl = potentialInfluencers[k];
                if (infl.dist < actualWorldInfluenceRadius)
                {
                    float weight = 1.0f / (infl.dist + 0.0001f); // Inverse distance weighting
                    validInfluences.Add((infl.pIndex, weight));
                    totalRawWeight += weight;
                }
                else
                {
                    break; // Sorted by distance, no need to check further
                }
            }

            if (totalRawWeight > 0f)
            { // Normalize weights
                foreach (var influence in validInfluences)
                {
                    vertexParticleInfluenceMap[i].Add((influence.pIndex, influence.rawWeight / totalRawWeight));
                }
            }
            else if (potentialInfluencers.Count > 0)
            { // Fallback: bind to the single closest (non-center) particle
                vertexParticleInfluenceMap[i].Add((potentialInfluencers[0].pIndex, 1.0f));
            }
            // If still no influencers, vertexParticleInfluenceMap[i] will be empty.
        }
        // Debug.Log($"BodyMeshRenderer on {gameObject.name}: Vertex binding complete. Effective world influence radius: {actualWorldInfluenceRadius:F2}");
        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized || vertexParticleInfluenceMap == null) return;

        for (int i = 0; i < originalMeshVertices.Length; i++)
        {
            if (vertexParticleInfluenceMap[i] == null || vertexParticleInfluenceMap[i].Count == 0)
            {
                deformedMeshVertices[i] = originalMeshVertices[i]; // Keep original if no influences
                continue;
            }

            Vector3 blendedWorldPos = Vector3.zero;
            foreach (var (particleIdx, weight) in vertexParticleInfluenceMap[i])
            {
                if (particleIdx >= 0 && particleIdx < body.particles.Count)
                { // Safety check
                    blendedWorldPos += body.particles[particleIdx].position * weight;
                }
            }
            deformedMeshVertices[i] = transform.InverseTransformPoint(blendedWorldPos);
        }

        mesh.vertices = deformedMeshVertices;
        mesh.RecalculateNormals(); // Necessary for correct lighting on deformed mesh
        mesh.RecalculateBounds();
    }
}
