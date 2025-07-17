using UnityEngine;
using System.Collections.Generic;

public class ShowcaseSetup : MonoBehaviour
{
    [Header("Prefab References")]
    public GameObject[] fruitPrefabs;
    public GameObject basketPrefab;
    public GameObject hammerPrefab;
    public GameObject knifePrefab;

    [Header("Spawn Settings")]
    public Vector3 spawnAreaSize = new Vector3(10f, 5f, 10f);
    public Vector3 spawnAreaCenter = new Vector3(0, 8f, 0);
    public float spawnDelay = 0.1f;

    [Header("Scene Objects")]
    public Transform fruitParent;
    public Transform toolsParent;
    public Camera mainCamera;

    private ShowcaseManager showcaseManager;
    private SimulationManager simulationManager;
    private ShowcaseParameters currentParameters;
    private List<GameObject> spawnedObjects = new List<GameObject>();

    void Start()
    {
        showcaseManager = FindObjectOfType<ShowcaseManager>();
        simulationManager = FindObjectOfType<SimulationManager>();

        if (showcaseManager == null)
        {
            Debug.LogError("❌ ShowcaseSetup: No ShowcaseManager found!");
            return;
        }

        // Load scenario parameters
        currentParameters = showcaseManager.LoadScenarioParameters();

        // Setup the scene
        SetupScene();

        // Start spawning
        StartCoroutine(SpawnObjects());
    }

    void SetupScene()
    {
        // Configure simulation manager
        if (simulationManager != null)
        {
            simulationManager.adaptiveSolverIterations = currentParameters.adaptiveSolver;
            simulationManager.solverIterations = currentParameters.solverIterations;
            simulationManager.fixedTimeStep = currentParameters.timeStep;
            simulationManager.enableSpatialOptimization = currentParameters.particleOptimization;
        }

        // Configure gravity for all bodies
        var allBodies = FindObjectsOfType<Body>();
        foreach (var body in allBodies)
        {
            body.gravity = currentParameters.gravity;
            body.solverIterations = currentParameters.solverIterations;
            body.fixedTimeStep = currentParameters.timeStep;
        }

        // Setup tools based on parameters
        SetupTools();

        // Setup camera
        SetupCamera();

        Debug.Log($"🎮 Setup complete for scenario: {PlayerPrefs.GetString("CurrentScenario", "Unknown")}");
    }

    void SetupTools()
    {
        // Setup basket
        if (currentParameters.basketEnabled && basketPrefab != null)
        {
            GameObject basket = Instantiate(basketPrefab, new Vector3(0, 2f, 0), Quaternion.identity, toolsParent);
            spawnedObjects.Add(basket);

            // Configure basket
            var basketBody = basket.GetComponent<BasketBody>();
            if (basketBody != null)
            {
                basketBody.collectionRadius = 1.5f;
                basketBody.collectionForce = 5f;
            }

            // Add to simulation
            var body = basket.GetComponent<Body>();
            if (body != null && simulationManager != null)
            {
                simulationManager.bodies.Add(body);
            }
        }

        // Setup hammer
        if (currentParameters.hammerEnabled && hammerPrefab != null)
        {
            GameObject hammer = Instantiate(hammerPrefab, new Vector3(3f, 2f, 0), Quaternion.identity, toolsParent);
            spawnedObjects.Add(hammer);

            // Configure hammer
            var hammerBody = hammer.GetComponent<HammerBody>();
            if (hammerBody != null && currentParameters.hammerSettings != null)
            {
                hammerBody.pushVelocityThreshold = currentParameters.hammerSettings.pushThreshold;
                hammerBody.deformVelocityThreshold = currentParameters.hammerSettings.deformThreshold;
                hammerBody.crushVelocityThreshold = currentParameters.hammerSettings.crushThreshold;
                hammerBody.pushForce = currentParameters.hammerSettings.pushForce;
                hammerBody.deformForce = currentParameters.hammerSettings.deformForce;
                hammerBody.crushForce = currentParameters.hammerSettings.crushForce;
            }

            // Add to simulation
            var body = hammer.GetComponent<Body>();
            if (body != null && simulationManager != null)
            {
                simulationManager.bodies.Add(body);
            }
        }

        // Setup knife
        if (currentParameters.knifeEnabled && knifePrefab != null)
        {
            GameObject knife = Instantiate(knifePrefab, new Vector3(-3f, 2f, 0), Quaternion.identity, toolsParent);
            spawnedObjects.Add(knife);

            // Configure knife
            var knifeComponent = knife.GetComponent<Knife>();
            if (knifeComponent != null)
            {
                knifeComponent.simManager = simulationManager;
                knifeComponent.cutImpulse = 0.1f;
                knifeComponent.cutDistanceThreshold = 0.3f;
            }
        }
    }

    void SetupCamera()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera != null)
        {
            // Position camera based on scenario
            if (currentParameters.performanceMode)
            {
                mainCamera.transform.position = new Vector3(0, 15f, -20f);
                mainCamera.transform.LookAt(Vector3.zero);
            }
            else
            {
                mainCamera.transform.position = new Vector3(0, 12f, -15f);
                mainCamera.transform.LookAt(Vector3.zero);
            }
        }
    }

    System.Collections.IEnumerator SpawnObjects()
    {
        int spawnedCount = 0;

        while (spawnedCount < currentParameters.fruitCount)
        {
            SpawnRandomFruit();
            spawnedCount++;

            yield return new WaitForSeconds(spawnDelay);
        }

        Debug.Log($"✅ Spawned {spawnedCount} fruits");
    }

    void SpawnRandomFruit()
    {
        if (fruitPrefabs == null || fruitPrefabs.Length == 0) return;

        // Select random fruit type
        GameObject selectedPrefab = fruitPrefabs[Random.Range(0, fruitPrefabs.Length)];

        // Random position within spawn area
        Vector3 randomPos = spawnAreaCenter + new Vector3(
            Random.Range(-spawnAreaSize.x * 0.5f, spawnAreaSize.x * 0.5f),
            Random.Range(-spawnAreaSize.y * 0.5f, spawnAreaSize.y * 0.5f),
            Random.Range(-spawnAreaSize.z * 0.5f, spawnAreaSize.z * 0.5f)
        );

        // Random rotation
        Quaternion randomRot = Quaternion.Euler(
            Random.Range(0f, 360f),
            Random.Range(0f, 360f),
            Random.Range(0f, 360f)
        );

        // Spawn the fruit
        GameObject fruit = Instantiate(selectedPrefab, randomPos, randomRot, fruitParent);
        spawnedObjects.Add(fruit);

        // Add to simulation
        var body = fruit.GetComponent<Body>();
        if (body != null && simulationManager != null)
        {
            simulationManager.bodies.Add(body);
        }
    }

    public void ClearScene()
    {
        foreach (var obj in spawnedObjects)
        {
            if (obj != null)
            {
                DestroyImmediate(obj);
            }
        }
        spawnedObjects.Clear();

        // Clear simulation manager bodies
        if (simulationManager != null)
        {
            simulationManager.bodies.Clear();
        }
    }

    public void RestartScenario()
    {
        ClearScene();
        SetupScene();
        StartCoroutine(SpawnObjects());
    }

    void OnDrawGizmos()
    {
        // Draw spawn area
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(spawnAreaCenter, spawnAreaSize);

        // Draw spawn center
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(spawnAreaCenter, 0.5f);
    }
}