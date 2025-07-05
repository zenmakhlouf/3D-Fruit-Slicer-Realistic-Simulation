using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// هذا السكربت يتيح محاكاة جسم ناعم أو مرن باستخدام الجسيمات والقيود.
/// يتعامل مع التكامل الفيزيائي، حلّ القيود، والتصادم مع الأرض.
/// </summary>
public class Body : MonoBehaviour
{
    // قائمة الجسيمات المرتبطة بهذا الجسم
    [Header("Particles and Constraints")]
    public List<Particle> particles = new List<Particle>();
    public List<DistanceConstraint> constraints = new List<DistanceConstraint>();

    // إعدادات الفيزياء الأساسية
    [Header("Physics Settings")]
    public Vector3 gravity = new Vector3(0, -9.81f, 0);   // قوة الجاذبية
    public float fixedTimeStep = 0.0167f;                   // خطوة الزمن الفيزيائية الثابتة (مثل FixedUpdate)
    public int solverIterations = 3;                     // عدد مرات حل القيود في كل إطار
    public float groundRestitution = 0.5f;                // معامل ارتداد الجسيمات عند اصطدامها بالأرض

    // إعدادات التصادم الأرضي
    [Header("Collision Settings")]
    public bool enableGroundCollision = true;             // هل نفعّل التصادم مع الأرض؟
    public float groundY = 0f;                            // مستوى الأرض Y

    // إعدادات التشغيل والتحكم
    [Header("Simulation")]
    public bool runSimulation = true;                     // هل نفعّل المحاكاة؟
    private float timeAccumulator = 0f;                   // لتجميع الوقت وتطبيق المحاكاة بخطوات ثابتة

    // دالة التحديث (تعادل FixedUpdate ولكن بطريقة يدوية)
    void Update()
    {
        if (!runSimulation) return;

        timeAccumulator += Time.deltaTime;

        while (timeAccumulator >= fixedTimeStep)
        {
            SimulatePhysicsStep(fixedTimeStep);
            timeAccumulator -= fixedTimeStep;
        }
    }

    /// <summary>
    /// خطوة فيزيائية واحدة: دمج الحركة ثم حل القيود
    /// </summary>
    private void SimulatePhysicsStep(float deltaTime)
    {
        // 1. دمج حركة الجسيمات (تطبيق الجاذبية وتحديث المواقع)
        for (int i = 0; i < particles.Count; i++)
        {
            particles[i].Integrate(deltaTime, gravity);
        }

        // 2. حلّ القيود بشكل متكرر لتحسين الدقة والثبات
        for (int iter = 0; iter < solverIterations; iter++)
        {
            for (int i = 0; i < constraints.Count; i++)
            {
                constraints[i].Solve();
            }

            // 3. تطبيق تصادم مع الأرض (إن وُجد)
            if (enableGroundCollision)
            {
                for (int i = 0; i < particles.Count; i++)
                {
                    ApplyGroundCollision(particles[i]);
                }
            }
        }
    }

    /// <summary>
    /// يحل تصادم الجسيم مع الأرض عن طريق تعديل موضعه وسرعته بشكل فيزيائي
    /// </summary>
    public void ApplyGroundCollision(Particle particle)
    {
        if (particle.position.y < groundY)
        {
            // نثبّت الجسيم على الأرض
            particle.position.y = groundY;

            // نحسب السرعة التقريبية في الاتجاه Y باستخدام المواقع الحالية والسابقة
            float velocityY = particle.position.y - particle.prevPosition.y;

            // نعدّل الموضع السابق لمحاكاة ارتداد (باستخدام Verlet Integration)
            particle.prevPosition.y = particle.position.y + velocityY * groundRestitution;
        }
    }

    /// <summary>
    /// رسم الجسيمات والقيود في المشهد لأغراض التصحيح (Debug)
    /// </summary>
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || particles == null || particles.Count == 0)
            return;

        // رسم الجسيمات ككرات صغيرة
        Gizmos.color = Color.yellow;
        for (int i = 0; i < particles.Count; i++)
        {
            Gizmos.DrawSphere(particles[i].position, 0.05f);
        }

        // رسم القيود كخطوط بين الجسيمات
        Gizmos.color = Color.cyan;
        for (int i = 0; i < constraints.Count; i++)
        {
            var c = constraints[i];
            if (c != null && c.p1 != null && c.p2 != null)
                Gizmos.DrawLine(c.p1.position, c.p2.position);
        }
    }
}
