using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class FruitSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public List<GameObject> fruitPrefabs;
    public List<GameObject> bombPrefabs;

    [Header("Spawn Settings")]
    [HideInInspector] public float spawnInterval = 2f;
    public Vector2 spawnArea = new Vector2(3f, 3f);
    public float spawnHeight = 20f;
    public SimulationManager simManager;
    public float bombSpawnChance = 0.2f;
    public GameObject gameOverImage;

    [Header("Batch Settings")]
    private int fruitsPerBatch = 4;

    [Header("Pooling")]
    public float cleanupInterval = 30f;
    public float maxIdleTime = 60f;

    [Header("Mode Objects")]
    public GameObject hammerObject;
    public GameObject basketObject;

    private Dictionary<string, Queue<PooledObject>> pool = new();
    private List<GameObject> allPrefabs = new();

    private bool currentTypeIsBomb = false;
    private int sameTypeCount = 0;
    private const int maxSameTypeInRow = 2;


    private int fallenFruitCount = 0;
    public int maxFruitsBeforeStop = 4;
    private bool gameEnded = false;


    private Coroutine spawnRoutine;

    private struct BatchState
    {
        public Vector3 position;
        public int count;
        public bool active;
    }

    private BatchState currentBatch;
    private Camera mainCamera;

    private void Start()
    {
         gameOverImage.SetActive(false);
        PreparePools();
        InvokeRepeating(nameof(CleanupPool), cleanupInterval, cleanupInterval);
        UpdateGameModeObjects();
        mainCamera = Camera.main;
    }

    private bool spawnStarted = false;

//=============================================================>

private void Update()
{
    if (!spawnStarted && simManager != null && simManager.currentGameMode != GameMode.None)
    {
        spawnStarted = true;
        spawnInterval = 2f; // أو القيمة المناسبة
        spawnRoutine = StartCoroutine(SpawnLoop());
        UpdateGameModeObjects();
    }
}

//=============================================================>

    private void UpdateGameModeObjects()
    {
         bool isSmash = simManager.currentGameMode == GameMode.Smash;

        if (simManager == null) return;

        if (simManager.currentGameMode == GameMode.None)
        {
            // لا تظهر أي أدوات ولا تفعل شيء
            if (hammerObject != null)
                hammerObject.SetActive(false);

            if (basketObject != null)
                basketObject.SetActive(false);

            spawnInterval = 0f; 
            return;
        }


        if (hammerObject != null)
            hammerObject.SetActive(isSmash);

        if (basketObject != null)
            basketObject.SetActive(!isSmash);

        spawnInterval = isSmash ? 4f : 1f;
    }


//=============================================================>

    private void PreparePools()
    {
        if (simManager == null)
            Debug.LogWarning("SimulationManager is not assigned!");

        PreparePool(fruitPrefabs, 10);
        PreparePool(bombPrefabs, 5);

        allPrefabs.AddRange(fruitPrefabs);
        allPrefabs.AddRange(bombPrefabs);
    }

//=============================================================>

    private void PreparePool(List<GameObject> prefabList, int countPerPrefab)
    {
        foreach (var prefab in prefabList)
        {
            if (prefab == null) continue;

            if (!pool.TryGetValue(prefab.name, out var q))
            {
                q = new Queue<PooledObject>();
                pool[prefab.name] = q;
            }

            for (int i = 0; i < countPerPrefab; i++)
            {
                GameObject obj = Instantiate(prefab);
                obj.SetActive(false);
                q.Enqueue(new PooledObject(obj));
            }
        }
    }

//=============================================================>

    private IEnumerator SpawnLoop()
    {
        bool isSmash = simManager.currentGameMode == GameMode.Smash;

        while (true)
        {
            SpawnRandomObject();
            if(isSmash) SpawnRandomObject();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

//=============================================================>

    private void SpawnRandomObject()
    {
        if (simManager == null || simManager.currentGameMode != GameMode.Collect)
        {
            SpawnSingleObject(GenerateRandomPosition());
            return;
        }

        if (!currentBatch.active || currentBatch.count >= fruitsPerBatch)
        {
            currentBatch.position = GenerateRandomPosition();
            currentBatch.count = 0;
            currentBatch.active = true;
        }

        SpawnSingleObject(currentBatch.position);
        currentBatch.count++;

        if (currentBatch.count >= fruitsPerBatch)
            currentBatch.active = false;
    }

//=============================================================>

    private Vector3 GenerateRandomPosition()
    {
        Vector3 pos = mainCamera.transform.position + mainCamera.transform.forward * 25f;
        pos.x += Random.Range(-spawnArea.x / 2f, spawnArea.x / 2f);
        pos.z += Random.Range(-spawnArea.y / 2f, spawnArea.y / 2f);
        pos.y = spawnHeight;
        return pos;
    }

    //=============================================================>


    private void SpawnSingleObject(Vector3 position)
    {
        bool spawnBomb = DetermineNextType();
        List<GameObject> selectedList = spawnBomb ? bombPrefabs : fruitPrefabs;

        if (selectedList.Count == 0) return;

        GameObject prefabToSpawn = selectedList[Random.Range(0, selectedList.Count)];
        GameObject obj = GetFromPool(prefabToSpawn);
        if (obj == null) return;

        obj.SetActive(true);
        obj.transform.position = position;

        Body body = obj.GetComponent<Body>();
        if (body != null)
        {
            simManager?.bodies.Add(body);
            StartCoroutine(DisableAfterTime(obj, prefabToSpawn, body, 5f));
        }
    }

//=============================================================>

    private bool DetermineNextType()
    {
        float forceSwitchChance = 0.1f + sameTypeCount * 0.2f;
        bool result = currentTypeIsBomb;

        if (sameTypeCount >= maxSameTypeInRow || Random.value < forceSwitchChance)
        {
            result = !currentTypeIsBomb;
            sameTypeCount = 1;
        }
        else
        {
            sameTypeCount++;
        }

        currentTypeIsBomb = result;
        return result;
    }

//=============================================================>

    private GameObject GetFromPool(GameObject prefab)
    {
        if (prefab == null) return null;

        string name = prefab.name;
        if (!pool.TryGetValue(name, out var q))
        {
            q = new Queue<PooledObject>();
            pool[name] = q;
        }

        if (q.Count == 0)
        {
            for (int i = 0; i < Random.Range(2, 6); i++)
            {
                GameObject obj = Instantiate(prefab);
                obj.SetActive(false);
                q.Enqueue(new PooledObject(obj));
            }
        }

        PooledObject pooled = q.Dequeue();
        pooled.lastUsedTime = Time.time;
        return pooled.gameObject;
    }

//=============================================================>

    private void ReturnToPool(GameObject prefab, GameObject obj)
    {
        if (obj == null) return;
        obj.SetActive(false);

        string name = prefab.name;
        if (!pool.TryGetValue(name, out var q))
        {
            q = new Queue<PooledObject>();
            pool[name] = q;
        }

        q.Enqueue(new PooledObject(obj));
    }
//=============================================================>

private IEnumerator DisableAfterTime(GameObject obj, GameObject prefab, Body body, float time)
{
    yield return new WaitForSeconds(time);

    if (obj != null && body != null && simManager?.bodies != null)
        simManager.bodies.Remove(body);

    ReturnToPool(prefab, obj);

    // فقط عد الفواكه
        fallenFruitCount++;

        if (fallenFruitCount >= maxFruitsBeforeStop && !gameEnded)
        {
            EndGame();
        }
   }
//=============================================================>

    private void CleanupPool()
    {
        foreach (var key in new List<string>(pool.Keys))
        {
            var q = pool[key];
            int originalCount = q.Count;
            int cleaned = 0;

            for (int i = 0; i < originalCount; i++)
            {
                var pooled = q.Dequeue();
                if (Time.time - pooled.lastUsedTime > maxIdleTime)
                    cleaned++;
                else
                    q.Enqueue(pooled);
            }

            for (int i = 0; i < cleaned; i++)
            {
                GameObject prefab = GetRandomPrefab();
                if (prefab != null)
                {
                    GameObject obj = Instantiate(prefab);
                    obj.SetActive(false);
                    if (!pool.ContainsKey(prefab.name))
                        pool[prefab.name] = new Queue<PooledObject>();
                    pool[prefab.name].Enqueue(new PooledObject(obj));
                }
            }
        }
    }
//=============================================================>

    private GameObject GetRandomPrefab()
    {
        if (allPrefabs == null || allPrefabs.Count == 0) return null;
        return allPrefabs[Random.Range(0, allPrefabs.Count)];
    }

    private class PooledObject
    {
        public GameObject gameObject;
        public float lastUsedTime;

        public PooledObject(GameObject go)
        {
            gameObject = go;
            lastUsedTime = Time.time;
        }
    }

    private void EndGame()
    {
        gameEnded = true;

        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);

        Debug.Log("اللعبة انتهت! وصلت إلى الحد الأقصى من الفواكه.");

         if (gameOverImage != null)
            gameOverImage.SetActive(true);

        StartCoroutine(LoadMainMenuAfterDelay(3f));
    
        
    }

    private IEnumerator LoadMainMenuAfterDelay(float delay)
{
    yield return new WaitForSeconds(delay);
    SceneManager.LoadScene("MainMenu");
}

}