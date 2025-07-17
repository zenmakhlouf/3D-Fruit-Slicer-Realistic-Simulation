using UnityEngine;

public class GameControlPanel : MonoBehaviour
{
    private Body[] allBodies;
    private MeshBody[] allMeshBodies;
    private BodyMeshRenderer2[] allMeshRenderers;

    // Body properties
    private Vector3 gravity = new Vector3(0, -9.81f, 0);
    private float restitution = 0.5f;
    private int iterations = 3;

    // MeshBody properties
    private int resolution = 5;
    private float totalMass = 1f;
    private float stiffness = 1f;
    private float connectionRadius = 0.5f;
    private bool includeSurface = true;
    private bool includeInternal = true;

    // BodyMeshRenderer2 properties
    private float influenceRadius = 0.3f;
    private int maxInfluencers = 2;
    private float updateInterval = 0.01f;

    void Start()
    {
        allBodies = FindObjectsOfType<Body>();
        allMeshBodies = FindObjectsOfType<MeshBody>();
        allMeshRenderers = FindObjectsOfType<BodyMeshRenderer2>();

        if (allBodies.Length > 0)
        {
            gravity = allBodies[0].gravity;
            restitution = allBodies[0].groundRestitution;
            iterations = allBodies[0].solverIterations;
        }

        if (allMeshBodies.Length > 0)
        {
            var m = allMeshBodies[0];
            resolution = m.resolution;
            totalMass = m.totalMass;
            stiffness = m.stiffness;
            connectionRadius = m.connectionRadius;
            includeSurface = m.includeSurfaceVertices;
            includeInternal = m.includeInternalParticles;
        }

        if (allMeshRenderers.Length > 0)
        {
            var r = allMeshRenderers[0];
            influenceRadius = r.influenceRadius;
            maxInfluencers = r.maxInfluencers;
            updateInterval = r.updateInterval;
        }
    }

    void OnGUI()
    {
        GUI.Box(new Rect(10, 10, 280, 700), "Body & Mesh Settings");

        int y = 40;

        // Gravity
        GUI.Label(new Rect(20, y, 200, 20), "Gravity Y: " + gravity.y.ToString("F2"));
        gravity.y = GUI.HorizontalSlider(new Rect(20, y + 20, 200, 20), gravity.y, -20f, 0f);
        y += 50;

        // Restitution
        GUI.Label(new Rect(20, y, 200, 20), "Ground Restitution: " + restitution.ToString("F2"));
        restitution = GUI.HorizontalSlider(new Rect(20, y + 20, 200, 20), restitution, 0f, 1f);
        y += 50;

        // Iterations
        GUI.Label(new Rect(20, y, 200, 20), "Solver Iterations: " + iterations);
        iterations = (int)GUI.HorizontalSlider(new Rect(20, y + 20, 200, 20), iterations, 1, 10);
        y += 50;

        // MeshBody settings
        GUI.Label(new Rect(20, y, 200, 20), "Resolution: " + resolution);
        resolution = (int)GUI.HorizontalSlider(new Rect(20, y + 20, 200, 20), resolution, 3, 10);
        y += 50;

        GUI.Label(new Rect(20, y, 200, 20), "Total Mass: " + totalMass.ToString("F2"));
        totalMass = GUI.HorizontalSlider(new Rect(20, y + 20, 200, 20), totalMass, 0.1f, 10f);
        y += 50;

        GUI.Label(new Rect(20, y, 200, 20), "Stiffness: " + stiffness.ToString("F2"));
        stiffness = GUI.HorizontalSlider(new Rect(20, y + 20, 200, 20), stiffness, 0.1f, 2f);
        y += 50;

        GUI.Label(new Rect(20, y, 200, 20), "Connection Radius: " + connectionRadius.ToString("F2"));
        connectionRadius = GUI.HorizontalSlider(new Rect(20, y + 20, 200, 20), connectionRadius, 0.1f, 2f);
        y += 50;

        includeSurface = GUI.Toggle(new Rect(20, y, 250, 20), includeSurface, "Include Surface Vertices");
        y += 30;
        includeInternal = GUI.Toggle(new Rect(20, y, 250, 20), includeInternal, "Include Internal Particles");
        y += 40;

        // BodyMeshRenderer2 settings
        GUI.Label(new Rect(20, y, 200, 20), "Influence Radius: " + influenceRadius.ToString("F2"));
        influenceRadius = GUI.HorizontalSlider(new Rect(20, y + 20, 200, 20), influenceRadius, 0.05f, 1f);
        y += 50;

        GUI.Label(new Rect(20, y, 200, 20), "Max Influencers: " + maxInfluencers);
        maxInfluencers = (int)GUI.HorizontalSlider(new Rect(20, y + 20, 200, 20), maxInfluencers, 1, 10);
        y += 50;

        GUI.Label(new Rect(20, y, 200, 20), "Update Interval: " + updateInterval.ToString("F3"));
        updateInterval = GUI.HorizontalSlider(new Rect(20, y + 20, 200, 20), updateInterval, 0.001f, 0.1f);
        y += 50;

        if (GUI.Button(new Rect(70, y, 120, 30), "Apply on All"))
        {
            foreach (var body in allBodies)
            {
                body.gravity = gravity;
                body.groundRestitution = restitution;
                body.solverIterations = iterations;
            }

            foreach (var mesh in allMeshBodies)
            {
                mesh.resolution = resolution;
                mesh.totalMass = totalMass;
                mesh.stiffness = stiffness;
                mesh.connectionRadius = connectionRadius;
                mesh.includeSurfaceVertices = includeSurface;
                mesh.includeInternalParticles = includeInternal;
            }

            foreach (var rend in allMeshRenderers)
            {
                rend.influenceRadius = influenceRadius;
                rend.maxInfluencers = maxInfluencers;
                rend.updateInterval = updateInterval;
            }
        }
    }
}
