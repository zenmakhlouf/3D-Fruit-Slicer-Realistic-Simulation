using UnityEngine;
using System.Collections.Generic;

public class PerformanceMonitor : MonoBehaviour
{
    [Header("Performance Display")]
    public bool showPerformanceStats = true;
    public KeyCode toggleKey = KeyCode.F3;

    [Header("Monitoring")]
    public float updateInterval = 0.5f;

    private float fps;
    private float deltaTime = 0.0f;
    private int frameCount = 0;
    private float timeAccumulator = 0f;

    // Performance metrics
    private int totalParticles = 0;
    private int totalBodies = 0;
    private int totalConstraints = 0;
    private int collisionPairs = 0;
    private float simulationTime = 0f;

    // Historical data for averaging
    private Queue<float> fpsHistory = new Queue<float>();
    private Queue<float> simTimeHistory = new Queue<float>();
    private const int HISTORY_SIZE = 60; // 30 seconds at 0.5s intervals

    private SimulationManager simManager;
    private GUIStyle style;

    void Start()
    {
        simManager = FindObjectOfType<SimulationManager>();

        // Create GUI style for performance display
        style = new GUIStyle();
        style.fontSize = 14;
        style.normal.textColor = Color.white;
        style.fontStyle = FontStyle.Bold;
    }

    void Update()
    {
        frameCount++;
        deltaTime += Time.unscaledDeltaTime;
        timeAccumulator += Time.unscaledDeltaTime;

        // Update FPS every updateInterval
        if (timeAccumulator >= updateInterval)
        {
            fps = frameCount / deltaTime;
            fpsHistory.Enqueue(fps);
            if (fpsHistory.Count > HISTORY_SIZE)
                fpsHistory.Dequeue();

            // Update simulation metrics
            UpdateSimulationMetrics();

            frameCount = 0;
            deltaTime = 0f;
            timeAccumulator = 0f;
        }

        // Toggle display
        if (Input.GetKeyDown(toggleKey))
        {
            showPerformanceStats = !showPerformanceStats;
        }
    }

    void UpdateSimulationMetrics()
    {
        if (simManager == null) return;

        totalBodies = simManager.bodies.Count;
        totalParticles = 0;
        totalConstraints = 0;

        foreach (var body in simManager.bodies)
        {
            if (body != null)
            {
                totalParticles += body.particles.Count;
                totalConstraints += body.constraints.Count;
            }
        }

        // Get collision pairs count (this would need to be exposed in SimulationManager)
        // For now, we'll estimate based on particle count
        collisionPairs = totalParticles * totalParticles / 100; // Rough estimate
    }

    void OnGUI()
    {
        if (!showPerformanceStats) return;

        float avgFps = 0f;
        if (fpsHistory.Count > 0)
        {
            foreach (float f in fpsHistory)
                avgFps += f;
            avgFps /= fpsHistory.Count;
        }

        float yPos = 10f;
        float lineHeight = 20f;

        // Background
        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.Box(new Rect(10, 10, 300, 200), "");
        GUI.color = Color.white;

        // Performance stats
        GUI.Label(new Rect(20, yPos, 280, lineHeight), $"FPS: {fps:F1} (Avg: {avgFps:F1})", style);
        yPos += lineHeight;

        GUI.Label(new Rect(20, yPos, 280, lineHeight), $"Bodies: {totalBodies}", style);
        yPos += lineHeight;

        GUI.Label(new Rect(20, yPos, 280, lineHeight), $"Particles: {totalParticles:N0}", style);
        yPos += lineHeight;

        GUI.Label(new Rect(20, yPos, 280, lineHeight), $"Constraints: {totalConstraints:N0}", style);
        yPos += lineHeight;

        GUI.Label(new Rect(20, yPos, 280, lineHeight), $"Collision Pairs: ~{collisionPairs:N0}", style);
        yPos += lineHeight;

        // Performance warnings
        if (fps < 30f)
        {
            GUI.color = Color.red;
            GUI.Label(new Rect(20, yPos, 280, lineHeight), "⚠️ LOW FPS - Consider reducing particle count", style);
            yPos += lineHeight;
        }
        else if (fps < 60f)
        {
            GUI.color = Color.yellow;
            GUI.Label(new Rect(20, yPos, 280, lineHeight), "⚠️ Moderate performance - Monitor particle count", style);
            yPos += lineHeight;
        }

        GUI.color = Color.white;

        // Performance tips
        yPos += lineHeight;
        GUI.Label(new Rect(20, yPos, 280, lineHeight), $"Press {toggleKey} to toggle display", style);
        yPos += lineHeight;

        if (totalParticles > 100000)
        {
            GUI.color = Color.cyan;
            GUI.Label(new Rect(20, yPos, 280, lineHeight), "💡 High particle count - Using optimized algorithms", style);
        }
    }

    // Public methods for external access
    public float GetCurrentFPS() => fps;
    public float GetAverageFPS() => fpsHistory.Count > 0 ? fpsHistory.Peek() : 0f;
    public int GetTotalParticles() => totalParticles;
    public int GetTotalBodies() => totalBodies;
}