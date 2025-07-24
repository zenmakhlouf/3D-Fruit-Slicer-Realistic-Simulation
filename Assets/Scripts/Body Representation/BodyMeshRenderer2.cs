using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// كل نقطة في الميش تتحرك نتيجة حركة الجسيمات القريبة منها.
/// كل نقطة تصبح مرتبطة بعدة جسيمات قريبة مع تأثير موزون.
/// ليقوم بتحديث شكل المجسم بشكل ناعم أثناء المحاكاة.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(Body))]
public class BodyMeshRenderer2 : MonoBehaviour
{
    private Mesh mesh;  
    private Vector3[] originalVertices;         // النسخة الأصلية من نقاط الشبكة
    private Vector3[] deformedVertices;         // النسخة المعدّلة بناءً على الجسيمات
    private Body body;
    private Transform cachedTransform;

    // كل نقطة في الشبكة تتأثر بجسيمات قريبة منها
    private struct Influence
    {
        public int particleIndex;
        public float weight;
    }

    private List<Influence>[] vertexInfluences; // جدول يربط كل نقطة بجسيمات مؤثرة عليها
    private bool isInitialized = false;

    [Header("Mesh Binding Parameters")]
    public float influenceRadius = 0.3f;        // نصف قطر التأثير
    public int maxInfluencers = 2;              // الحد الأقصى للجسيمات المؤثرة على كل نقطة
    public float updateInterval = 0.01f;        // الزمن بين تحديثات الشكل (مثلاً 0.02 = 50FPS)

        //=============================================================>


    private float timer = 0f;

    // تعريف هيكل المفتاح الخاص بشبكة الفضاء
    private struct GridKey
    {
        public int x, y, z;
        public GridKey(int x, int y, int z)
        {
            this.x = x; this.y = y; this.z = z;
        }

        public override bool Equals(object obj)
        {
            return obj is GridKey key && x == key.x && y == key.y && z == key.z;
        }

        public override int GetHashCode()
        {
            // أعداد أولية لتقليل تصادمات الـ hash
            return x * 73856093 ^ y * 19349663 ^ z * 83492791;
        }
    }

    //=============================================================>

    void Start()
    {
        body = GetComponent<Body>();
        cachedTransform = transform;

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter.sharedMesh == null)
        {
            // Debug.LogError("لا يوجد Mesh مرفق بـ MeshFilter.");
            enabled = false;
            return;
        }

        mesh = meshFilter.mesh;
        mesh.MarkDynamic(); // لتحسين الأداء عند تعديل النقاط بشكل متكرر

        originalVertices = mesh.vertices;
        deformedVertices = new Vector3[originalVertices.Length];

        StartCoroutine(InitializeBindingWhenReady());
    }

    //=============================================================>

    IEnumerator InitializeBindingWhenReady()
    {
        while (body.particles == null || body.particles.Count == 0)
            yield return null;

        vertexInfluences = new List<Influence>[originalVertices.Length];

        // إنشاء شبكة الفضاء وتعبئتها بالجسيمات
        Dictionary<GridKey, List<int>> spatialGrid = new();
        float cellSize = influenceRadius;

        for (int i = 0; i < body.particles.Count; i++)
        {
            Vector3 pos = body.particles[i].position;
            GridKey key = GetGridKey(pos, cellSize);

            if (!spatialGrid.TryGetValue(key, out var list))
            {
                list = new List<int>();
                spatialGrid[key] = list;
            }
            list.Add(i);
        }

        for (int i = 0; i < originalVertices.Length; i++)
        {
            vertexInfluences[i] = new List<Influence>();

            Vector3 worldVertex = cachedTransform.TransformPoint(originalVertices[i]);
            List<(int index, float dist)> nearbyParticles = new();

            GridKey vertexKey = GetGridKey(worldVertex, cellSize);

            // فحص الخلايا المحيطة (27 خلية)
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            for (int dz = -1; dz <= 1; dz++)
            {
                GridKey neighborKey = new GridKey(
                    vertexKey.x + dx,
                    vertexKey.y + dy,
                    vertexKey.z + dz
                );

                if (spatialGrid.TryGetValue(neighborKey, out var particleIndices))
                {
                    foreach (int j in particleIndices)
                    {
                        float dist = Vector3.Distance(worldVertex, body.particles[j].position);
                        if (dist <= influenceRadius)
                            nearbyParticles.Add((j, dist));
                    }
                }
            }

            // ترتيب الجسيمات حسب المسافة الأقرب
            nearbyParticles.Sort((a, b) => a.dist.CompareTo(b.dist));

            float totalWeight = 0f;
            int count = Mathf.Min(maxInfluencers, nearbyParticles.Count);

            for (int k = 0; k < count; k++)
            {
                float rawWeight = 1f / (nearbyParticles[k].dist + 0.0001f);
                totalWeight += rawWeight;

                vertexInfluences[i].Add(new Influence
                {
                    particleIndex = nearbyParticles[k].index,
                    weight = rawWeight
                });
            }

            // تطبيع الأوزان لتكون مجموعها 1
            for (int k = 0; k < vertexInfluences[i].Count; k++)
            {
                var inf = vertexInfluences[i][k];
                inf.weight /= totalWeight;
                vertexInfluences[i][k] = inf;
            }
        }

        isInitialized = true;
    }

    //=============================================================>

    private GridKey GetGridKey(Vector3 position, float cellSize)
    {
        return new GridKey(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.y / cellSize),
            Mathf.FloorToInt(position.z / cellSize)
        );
    }

 //=============================================================>

    void LateUpdate()
    {
        if (!isInitialized) return;

        timer += Time.deltaTime;
        if (timer < updateInterval) return;
        timer = 0f;

        for (int i = 0; i < originalVertices.Length; i++)
        {
            var influences = vertexInfluences[i];
            if (influences == null || influences.Count == 0)
            {
                deformedVertices[i] = originalVertices[i];
                continue;
            }

            Vector3 blendedPos = Vector3.zero;
            foreach (var inf in influences)
            {
                if (inf.particleIndex < body.particles.Count)
                    blendedPos += body.particles[inf.particleIndex].position * inf.weight;
            }

            deformedVertices[i] = cachedTransform.InverseTransformPoint(blendedPos);
        }

        mesh.vertices = deformedVertices;
        mesh.RecalculateBounds();
    }
}
