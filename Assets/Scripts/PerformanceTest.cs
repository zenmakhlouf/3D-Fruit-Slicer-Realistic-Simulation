using UnityEngine;
using System.Collections;

public class PerformanceTest : MonoBehaviour
{
    [Header("Test Settings")]
    public GameObject bodyPrefab;
    public int targetParticleCount = 100000;
    public int spawnBatchSize = 1000;
    public float spawnInterval = 0.1f;
    public bool autoSpawn = false;

    [Header("Spawn Area")]
    public Vector3 spawnAreaSize = new Vector3(10f, 5f, 10f);
    public Vector3 spawnAreaCenter = Vector3.zero;

    [Header("Controls")]
    public KeyCode spawnKey = KeyCode.Space;
    public KeyCode clearKey = KeyCode.C;
    public KeyCode benchmarkKey = KeyCode.B;

    private SimulationManager simManager;
    private PerformanceMonitor perfMonitor;
    private int currentParticleCount = 0;
    private bool isSpawning = false;

    void Start()
    {
        simManager = FindObjectOfType<SimulationManager>();
        perfMonitor = FindObjectOfType<PerformanceMonitor>();

        if (simManager == null)
        {
            Debug.LogError("❌ PerformanceTest: No SimulationManager found!");
            enabled = false;
            return;
        }

        if (bodyPrefab == null)
        {
            Debug.LogWarning("⚠️ PerformanceTest: No body prefab assigned!");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(spawnKey) && !isSpawning)
        {
            StartCoroutine(SpawnBodies());
        }

        if (Input.GetKeyDown(clearKey))
        {
            ClearAllBodies();
        }

        if (Input.GetKeyDown(benchmarkKey))
        {
            StartCoroutine(RunBenchmark());
        }

        if (autoSpawn && !isSpawning && currentParticleCount < targetParticleCount)
        {
            StartCoroutine(SpawnBodies());
        }
    }

    IEnumerator SpawnBodies()
    {
        isSpawning = true;

        while (currentParticleCount < targetParticleCount && bodyPrefab != null)
        {
            int particlesToSpawn = Mathf.Min(spawnBatchSize, targetParticleCount - currentParticleCount);

            for (int i = 0; i < particlesToSpawn; i++)
            {
                SpawnBody();
                currentParticleCount += GetParticleCount(bodyPrefab);

                if (currentParticleCount >= targetParticleCount)
                    break;
            }

            Debug.Log($"🚀 Spawned batch: {currentParticleCount:N0}/{targetParticleCount:N0} particles");

            yield return new WaitForSeconds(spawnInterval);
        }

        isSpawning = false;
        Debug.Log($"✅ Spawning complete: {currentParticleCount:N0} particles");
    }

    void SpawnBody()
    {
        Vector3 randomPos = spawnAreaCenter + new Vector3(
            Random.Range(-spawnAreaSize.x * 0.5f, spawnAreaSize.x * 0.5f),
            Random.Range(-spawnAreaSize.y * 0.5f, spawnAreaSize.y * 0.5f),
            Random.Range(-spawnAreaSize.z * 0.5f, spawnAreaSize.z * 0.5f)
        );

        GameObject newBody = Instantiate(bodyPrefab, randomPos, Random.rotation);

        // Add to simulation manager
        Body bodyComponent = newBody.GetComponent<Body>();
        if (bodyComponent != null && simManager.bodies != null)
        {
            simManager.bodies.Add(bodyComponent);
        }
    }

    int GetParticleCount(GameObject prefab)
    {
        Body body = prefab.GetComponent<Body>();
        return body != null ? body.particles.Count : 0;
    }

    void ClearAllBodies()
    {
        if (simManager == null) return;

        // Destroy all body GameObjects
        foreach (Body body in simManager.bodies.ToArray())
        {
            if (body != null && body.gameObject != null)
            {
                DestroyImmediate(body.gameObject);
            }
        }

        simManager.bodies.Clear();
        currentParticleCount = 0;

        Debug.Log("🗑️ Cleared all bodies");
    }

    IEnumerator RunBenchmark()
    {
        Debug.Log("🧪 Starting performance benchmark...");

        // Clear existing bodies
        ClearAllBodies();

        // Spawn test bodies
        yield return StartCoroutine(SpawnBodies());

        // Wait for simulation to stabilize
        yield return new WaitForSeconds(2f);

        // Measure performance for 10 seconds
        float startTime = Time.time;
        int frameCount = 0;
        float totalFPS = 0f;

        while (Time.time - startTime < 10f)
        {
            frameCount++;
            if (perfMonitor != null)
            {
                totalFPS += perfMonitor.GetCurrentFPS();
            }
            yield return null;
        }

        float avgFPS = totalFPS / frameCount;
        float avgParticles = perfMonitor != null ? perfMonitor.GetTotalParticles() : currentParticleCount;

        Debug.Log($"📊 Benchmark Results:");
        Debug.Log($"   Average FPS: {avgFPS:F1}");
        Debug.Log($"   Total Particles: {avgParticles:N0}");
        Debug.Log($"   Particles per FPS: {avgParticles / avgFPS:F0}");

        if (avgFPS < 30f)
        {
            Debug.LogWarning("⚠️ Performance is below 30 FPS - consider reducing particle count");
        }
        else if (avgFPS > 60f)
        {
            Debug.Log("✅ Performance is good - you can add more particles");
        }
    }

    void OnDrawGizmos()
    {
        // Draw spawn area
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(spawnAreaCenter, spawnAreaSize);

        // Draw current particle count
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(spawnAreaCenter, 0.5f);
    }

    void OnGUI()
    {
        if (!Application.isPlaying) return;

        GUILayout.BeginArea(new Rect(10, 220, 300, 150));
        GUILayout.BeginVertical("box");

        GUILayout.Label($"Performance Test Controls", GUI.skin.box);
        GUILayout.Label($"Current Particles: {currentParticleCount:N0}");
        GUILayout.Label($"Target Particles: {targetParticleCount:N0}");
        GUILayout.Label($"Spawn Progress: {(float)currentParticleCount / targetParticleCount * 100f:F1}%");

        GUILayout.Space(5);

        if (GUILayout.Button($"Spawn Bodies ({spawnKey})"))
        {
            if (!isSpawning)
                StartCoroutine(SpawnBodies());
        }

        if (GUILayout.Button($"Clear All ({clearKey})"))
        {
            ClearAllBodies();
        }

        if (GUILayout.Button($"Run Benchmark ({benchmarkKey})"))
        {
            StartCoroutine(RunBenchmark());
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}