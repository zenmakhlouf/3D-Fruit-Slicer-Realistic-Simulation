using UnityEngine;
using System.Collections.Generic;

public class FruitSpawner : MonoBehaviour
{
    public List<GameObject> fruitPrefabs;
    public List<GameObject> bombPrefabs;
    public float spawnInterval = 1f;
    public Vector2 spawnArea = new Vector2(3f, 3f);
    public float spawnHeight = 7f;
    public SimulationManager simManager;
    private int consecutiveCount = 0;
    private bool lastWasBomb = false;
    int maxSameTypeInRow = 2;
    bool currentTypeIsBomb = false;
    int sameTypeCount = 0;
    private List<GameObject> allPrefabs;

    [Range(0f, 1f)]
    public float bombSpawnChance = 0.2f;

    // استخدام اسم prefab كمفتاح بدل GameObject
    private Dictionary<string, Queue<PooledObject>> pool = new Dictionary<string, Queue<PooledObject>>();

    public float cleanupInterval = 30f; // فترة التنظيف (ثواني)
    public float maxIdleTime = 60f;     // حذف الكائنات غير المستخدمة أكثر من هذا الوقت (ثواني)



void Start()
{
    if (simManager == null)
    {
        Debug.LogWarning("SimulationManager is not assigned!");
    }

    PreparePool(fruitPrefabs, 10);
    PreparePool(bombPrefabs, 5);

    // ادمج جميع prefabs في قائمة واحدة
    allPrefabs = new List<GameObject>();
    allPrefabs.AddRange(fruitPrefabs);
    allPrefabs.AddRange(bombPrefabs);

    InvokeRepeating(nameof(SpawnRandomObject), 1f, spawnInterval);
    InvokeRepeating(nameof(CleanupPool), cleanupInterval, cleanupInterval);
}


//============================================================================================>

    void PreparePool(List<GameObject> prefabList, int countPerPrefab)
    {
        foreach (GameObject prefab in prefabList)
        {
            if (prefab == null)
            {
                Debug.LogWarning("Prefab list contains null reference!");
                continue;
            }

            string prefabName = prefab.name;

            if (!pool.ContainsKey(prefabName))
                pool[prefabName] = new Queue<PooledObject>();

            for (int i = 0; i < countPerPrefab; i++)
            {
                GameObject obj = Instantiate(prefab);
                obj.SetActive(false);
                pool[prefabName].Enqueue(new PooledObject(obj));
            }
        }
    }
    //============================================================================================>


GameObject GetFromPool(GameObject prefab)
{
    if (prefab == null)
    {
        Debug.LogError("GetFromPool called with null prefab!");
        return null;
    }

    string prefabName = prefab.name;

    if (!pool.ContainsKey(prefabName))
        pool[prefabName] = new Queue<PooledObject>();

    if (pool[prefabName].Count > 0)
    {
        PooledObject pooledObj = pool[prefabName].Dequeue();
        pooledObj.lastUsedTime = Time.time;
        return pooledObj.gameObject;
    }
    else
    {
        // نولد عدد عشوائي من الأجسام بين 2 و5
        int replenishCount = Random.Range(2, 6);

        for (int i = 0; i < replenishCount; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            pool[prefabName].Enqueue(new PooledObject(obj));
        }

        // نرجع أول كائن تمت إضافته
        PooledObject newPooledObj = pool[prefabName].Dequeue();
        newPooledObj.lastUsedTime = Time.time;
        return newPooledObj.gameObject;
    }
}

//============================================================================================>


    void ReturnToPool(GameObject prefab, GameObject obj)
    {
        if (obj == null) return;

        obj.SetActive(false);

        string prefabName = prefab.name;

        if (!pool.ContainsKey(prefabName))
            pool[prefabName] = new Queue<PooledObject>();

        pool[prefabName].Enqueue(new PooledObject(obj));
    }

//============================================================================================>

void SpawnSingleObject()
{
    float forceSwitchChance = 0.1f + sameTypeCount * 0.2f;

    bool spawnBomb = currentTypeIsBomb;

    if (sameTypeCount >= maxSameTypeInRow || Random.value < forceSwitchChance)
    {
        spawnBomb = !currentTypeIsBomb;
        sameTypeCount = 1;
    }
    else
    {
        sameTypeCount++;
    }

    currentTypeIsBomb = spawnBomb;

    List<GameObject> selectedList = spawnBomb ? bombPrefabs : fruitPrefabs;

    if (selectedList == null || selectedList.Count == 0)
        return;

    int index = Random.Range(0, selectedList.Count);
    GameObject prefabToSpawn = selectedList[index];

    if (prefabToSpawn == null)
        return;

    GameObject obj = GetFromPool(prefabToSpawn);
    if (obj == null)
        return;

    obj.SetActive(true);

    Vector3 spawnPosition = Camera.main.transform.position + Camera.main.transform.forward * 20f;
    spawnPosition.x += Random.Range(-spawnArea.x / 2f, spawnArea.x / 2f);
    spawnPosition.z += Random.Range(-spawnArea.y / 2f, spawnArea.y / 2f);
    spawnPosition.y = spawnHeight;
    obj.transform.position = spawnPosition;

    Body body = obj.GetComponent<Body>();
    if (body != null)
    {
        simManager?.bodies.Add(body);
        StartCoroutine(DisableAfterTime(obj, prefabToSpawn, body, 5f));
    }
}


void SpawnRandomObject()
{
    SpawnSingleObject();
    // SpawnSingleObject(); // توليد جسم ثاني

}


//============================================================================================>


    System.Collections.IEnumerator DisableAfterTime(GameObject obj, GameObject prefab, Body body, float time)
    {
        yield return new WaitForSeconds(time);

        if (obj == null) yield break;

        if (simManager?.bodies != null && body != null)
        {
            simManager.bodies.Remove(body);
        }

        ReturnToPool(prefab, obj);
    }

    //============================================================================================>


   
void CleanupPool()
{
    List<string> keys = new List<string>(pool.Keys);

    foreach (var prefabName in keys)
    {
        Queue<PooledObject> q = pool[prefabName];
        int initialCount = q.Count;
        int destroyedCount = 0;

        int count = initialCount;
        for (int i = 0; i < count; i++)
        {
            PooledObject pooledObj = q.Dequeue();
            if (Time.time - pooledObj.lastUsedTime > maxIdleTime)
            {
                Destroy(pooledObj.gameObject);
                destroyedCount++;
            }
            else
            {
                q.Enqueue(pooledObj);
            }
        }

        // تعويض الكائنات المدمرة باستخدام prefabs عشوائية من allPrefabs
        for (int i = 0; i < destroyedCount; i++)
        {
            GameObject randomPrefab = GetRandomPrefab();
            if (randomPrefab != null)
            {
                GameObject obj = Instantiate(randomPrefab);
                obj.SetActive(false);

                string newPrefabName = randomPrefab.name;

                if (!pool.ContainsKey(newPrefabName))
                    pool[newPrefabName] = new Queue<PooledObject>();

                pool[newPrefabName].Enqueue(new PooledObject(obj));
            }
        }
    }
}

GameObject GetRandomPrefab()
{
    if (allPrefabs == null || allPrefabs.Count == 0)
        return null;

    int index = UnityEngine.Random.Range(0, allPrefabs.Count);
    return allPrefabs[index];
}

//============================================================================================>


// دالة مساعدة لإيجاد prefab في القوائم fruitPrefabs و bombPrefabs حسب الاسم
GameObject FindPrefabByName(string prefabName)
{
    foreach (var prefab in fruitPrefabs)
    {
        if (prefab != null && prefab.name == prefabName)
            return prefab;
    }
    foreach (var prefab in bombPrefabs)
    {
        if (prefab != null && prefab.name == prefabName)
            return prefab;
    }
    return null;
}

//============================================================================================>


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
}
