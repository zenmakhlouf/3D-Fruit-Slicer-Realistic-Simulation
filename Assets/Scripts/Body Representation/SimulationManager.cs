using UnityEngine;
using System.Collections.Generic;

public enum GameMode
{
    Smash,   // تحطيم الفواكه
    Collect  // جمع الفواكه بالسلة
}

public class SimulationManager : MonoBehaviour
{
    public List<Body> bodies = new List<Body>();
    public float fixedTimeStep = 0.02f;
    public int solverIterations = 10;
    public float particleRadius = 0.1f;
    private float timeAccumulator = 0f;

    // Performance optimization settings
    [Header("Performance Settings")]
    public int maxParticlesForFullSimulation = 10000;
    public bool adaptiveSolverIterations = true;
    public float collisionDetectionRadius = 2.0f;
    public bool enableSpatialOptimization = true;

    // Spatial grid optimization
    private float gridCellSize;
    private Dictionary<Vector3Int, List<Particle>> spatialGrid;
    private List<Vector3Int> neighborCells = new List<Vector3Int>(27); // Pre-allocated
    private readonly Vector3Int[] neighborOffsets = new Vector3Int[27]; // Pre-calculated offsets

    // Object pooling for collision detection
    private struct CollisionPair
    {
        public Particle p1;
        public Particle p2;
        public float minDistanceSqr; // Using squared distance for performance
    }
    private List<CollisionPair> collisionPairs = new List<CollisionPair>();
    private HashSet<ulong> checkedPairs = new HashSet<ulong>(); // Using ulong for faster hashing

    // Pre-allocated collections to avoid GC
    private List<Particle> tempParticleList = new List<Particle>();
    private List<Vector3Int> tempCellList = new List<Vector3Int>();

    // Fields for dragging and launching
    private Body selectedBody = null;
    private Plane dragPlane;
    private Vector3 initialMouseWorldPos;
    private bool isDragging = false;
    private float selectionThreshold = 0.5f;
    private float launchFactor = 10.0f;

    public GameMode currentGameMode = GameMode.Collect;

    void Start()
    {
        string mode = PlayerPrefs.GetString("GameMode", "Collect");

        if (mode == "Smash")
            currentGameMode = GameMode.Smash;
        else
            currentGameMode = GameMode.Collect;

        Debug.Log("🚀 بدأنا اللعبة في وضع: " + currentGameMode);
    }

    void Awake()
    {
        // Optimize grid cell size based on collision radius
        gridCellSize = collisionDetectionRadius * 1.5f;
        spatialGrid = new Dictionary<Vector3Int, List<Particle>>();

        // Pre-calculate neighbor offsets
        int index = 0;
        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                for (int z = -1; z <= 1; z++)
                    neighborOffsets[index++] = new Vector3Int(x, y, z);
    }

    void Update()
    {
        timeAccumulator += Time.deltaTime;
        while (timeAccumulator >= fixedTimeStep)
        {
            SimulatePhysicsStep(fixedTimeStep);
            timeAccumulator -= fixedTimeStep;
        }

        // Start dragging on mouse click
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            selectedBody = GetSelectedBody(ray, selectionThreshold);
            if (selectedBody != null)
            {
                isDragging = true;
                // Calculate the body's center
                Vector3 bodyCenter = Vector3.zero;
                foreach (Particle p in selectedBody.particles)
                    bodyCenter += p.position;
                bodyCenter /= selectedBody.particles.Count;
                // Define drag plane parallel to camera view through body center
                dragPlane = new Plane(Camera.main.transform.forward, bodyCenter);
                // Get initial mouse position on the plane
                if (dragPlane.Raycast(ray, out float enter))
                    initialMouseWorldPos = ray.GetPoint(enter);
                else
                    initialMouseWorldPos = bodyCenter; // Fallback if raycast fails
            }
        }

        // Launch the body on mouse release
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (dragPlane.Raycast(ray, out float enter))
            {
                Vector3 currentMouseWorldPos = ray.GetPoint(enter);
                Vector3 dragVector = currentMouseWorldPos - initialMouseWorldPos;
                Vector3 velocity = launchFactor * dragVector;
                // Apply velocity to all particles
                foreach (Particle p in selectedBody.particles)
                {
                    p.prevPosition = p.position - velocity * fixedTimeStep;
                }
            }
            isDragging = false;
            selectedBody = null;
        }
    }

    void SimulatePhysicsStep(float deltaTime)
    {
        int totalParticles = 0;
        foreach (Body body in bodies)
            totalParticles += body.particles.Count;

        // Adaptive solver iterations based on particle count
        int currentSolverIterations = adaptiveSolverIterations ?
            Mathf.Max(1, solverIterations - (totalParticles / 10000)) : solverIterations;

        // 1. Integrate particle positions
        foreach (Body body in bodies)
        {
            foreach (Particle p in body.particles)
            {
                p.Integrate(deltaTime, body.gravity);
            }
        }

        // 2. Build spatial grid (only if optimization is enabled and we have many particles)
        if (enableSpatialOptimization && totalParticles > 1000)
        {
            BuildSpatialGrid();

            // 3. Detect collisions using spatial grid
            DetectCollisionsOptimized();
        }
        else
        {
            // Fallback to simple collision detection for small systems
            DetectCollisionsSimple();
        }

        // 4. Solve constraints and collisions
        for (int i = 0; i < currentSolverIterations; i++)
        {
            // Solve constraints in batches for better cache performance
            SolveConstraintsBatch();

            // Solve collisions
            foreach (CollisionPair pair in collisionPairs)
            {
                SolveCollision(pair, deltaTime);
            }

            // Apply ground collision
            foreach (Body body in bodies)
            {
                foreach (Particle p in body.particles)
                {
                    body.ApplyGroundCollision(p);
                }
            }
        }
    }

    void BuildSpatialGrid()
    {
        spatialGrid.Clear();

        foreach (Body body in bodies)
        {
            foreach (Particle p in body.particles)
            {
                Vector3Int cell = GetGridCell(p.position);
                if (!spatialGrid.TryGetValue(cell, out List<Particle> cellParticles))
                {
                    cellParticles = new List<Particle>();
                    spatialGrid[cell] = cellParticles;
                }
                cellParticles.Add(p);
            }
        }
    }

    void DetectCollisionsOptimized()
    {
        collisionPairs.Clear();
        checkedPairs.Clear();

        foreach (Body body1 in bodies)
        {
            foreach (Particle p1 in body1.particles)
            {
                Vector3Int cell = GetGridCell(p1.position);
                GetNeighborCellsOptimized(cell, neighborCells);

                foreach (Vector3Int neighbor in neighborCells)
                {
                    if (spatialGrid.TryGetValue(neighbor, out List<Particle> cellParticles))
                    {
                        foreach (Particle p2 in cellParticles)
                        {
                            if (p1 == p2 || p1.body == p2.body) continue;

                            // Use ulong hash for faster pair checking
                            ulong pairHash = GetPairHash(p1, p2);
                            if (checkedPairs.Contains(pairHash)) continue;

                            float minDistanceSqr = (p1.collisionRadius + p2.collisionRadius) * (p1.collisionRadius + p2.collisionRadius);
                            float distanceSqr = (p1.position - p2.position).sqrMagnitude;

                            if (distanceSqr < minDistanceSqr)
                            {
                                collisionPairs.Add(new CollisionPair
                                {
                                    p1 = p1,
                                    p2 = p2,
                                    minDistanceSqr = minDistanceSqr
                                });
                                checkedPairs.Add(pairHash);
                            }
                        }
                    }
                }
            }
        }
    }

    void DetectCollisionsSimple()
    {
        collisionPairs.Clear();
        checkedPairs.Clear();

        foreach (Body body1 in bodies)
        {
            foreach (Particle p1 in body1.particles)
            {
                foreach (Body body2 in bodies)
                {
                    if (body1 == body2) continue;

                    foreach (Particle p2 in body2.particles)
                    {
                        ulong pairHash = GetPairHash(p1, p2);
                        if (checkedPairs.Contains(pairHash)) continue;

                        float minDistanceSqr = (p1.collisionRadius + p2.collisionRadius) * (p1.collisionRadius + p2.collisionRadius);
                        float distanceSqr = (p1.position - p2.position).sqrMagnitude;

                        if (distanceSqr < minDistanceSqr)
                        {
                            collisionPairs.Add(new CollisionPair
                            {
                                p1 = p1,
                                p2 = p2,
                                minDistanceSqr = minDistanceSqr
                            });
                            checkedPairs.Add(pairHash);
                        }
                    }
                }
            }
        }
    }

    void SolveConstraintsBatch()
    {
        // Process constraints in batches for better cache performance
        const int batchSize = 64;

        foreach (Body body in bodies)
        {
            int constraintCount = body.constraints.Count;
            for (int i = 0; i < constraintCount; i += batchSize)
            {
                int endIndex = Mathf.Min(i + batchSize, constraintCount);
                for (int j = i; j < endIndex; j++)
                {
                    body.constraints[j].Solve();
                }
            }
        }
    }

    Vector3Int GetGridCell(Vector3 position)
    {
        return new Vector3Int(
            Mathf.FloorToInt(position.x / gridCellSize),
            Mathf.FloorToInt(position.y / gridCellSize),
            Mathf.FloorToInt(position.z / gridCellSize)
        );
    }

    void GetNeighborCellsOptimized(Vector3Int cell, List<Vector3Int> neighbors)
    {
        neighbors.Clear();
        for (int i = 0; i < neighborOffsets.Length; i++)
        {
            neighbors.Add(cell + neighborOffsets[i]);
        }
    }

    ulong GetPairHash(Particle p1, Particle p2)
    {
        // Create a unique hash for particle pairs, ensuring p1 < p2 for consistency
        if (p1.GetHashCode() < p2.GetHashCode())
            return ((ulong)p1.GetHashCode() << 32) | (ulong)p2.GetHashCode();
        else
            return ((ulong)p2.GetHashCode() << 32) | (ulong)p1.GetHashCode();
    }

    void SolveCollision(CollisionPair pair, float deltaTime)
    {
        Particle p1 = pair.p1;
        Particle p2 = pair.p2;
        float minDistanceSqr = pair.minDistanceSqr;

        Vector3 delta = p2.position - p1.position;
        float currentDistanceSqr = delta.sqrMagnitude;

        if (currentDistanceSqr >= minDistanceSqr || currentDistanceSqr < 0.0001f) return;

        float currentDistance = Mathf.Sqrt(currentDistanceSqr);
        float minDistance = Mathf.Sqrt(minDistanceSqr);
        float overlap = minDistance - currentDistance;
        Vector3 correctionNormal = delta / currentDistance;
        Vector3 totalCorrection = correctionNormal * overlap;

        // Check for hammer collision handling
        HandleHammerCollision(p1, p2, correctionNormal, currentDistance);

        // Use cached inverse mass for better performance
        float invMass1 = p1.InvMass;
        float invMass2 = p2.InvMass;
        float totalInverseMass = invMass1 + invMass2;

        if (totalInverseMass == 0f) return;

        if (invMass1 > 0f)
            p1.position -= totalCorrection * (invMass1 / totalInverseMass);
        if (invMass2 > 0f)
            p2.position += totalCorrection * (invMass2 / totalInverseMass);
    }

    void HandleHammerCollision(Particle p1, Particle p2, Vector3 collisionNormal, float distance)
    {
        // Check if either particle belongs to a hammer
        HammerBody hammer1 = p1.body?.GetComponent<HammerBody>();
        HammerBody hammer2 = p2.body?.GetComponent<HammerBody>();

        if (hammer1 != null && p2.body != null)
        {
            // Hammer1 hit p2's body
            Vector3 collisionPoint = p2.position;
            hammer1.HandleCollision(p2.body, collisionPoint, collisionNormal);
        }
        else if (hammer2 != null && p1.body != null)
        {
            // Hammer2 hit p1's body
            Vector3 collisionPoint = p1.position;
            hammer2.HandleCollision(p1.body, collisionPoint, -collisionNormal);
        }
    }

    private Body GetSelectedBody(Ray ray, float threshold)
    {
        Body closestBody = null;
        float minDistSqr = float.MaxValue;
        float thresholdSqr = threshold * threshold;

        foreach (Body body in bodies)
        {
            foreach (Particle p in body.particles)
            {
                // Find the closest point on the ray to the particle
                Vector3 pointOnRay = ray.origin + ray.direction * Vector3.Dot(p.position - ray.origin, ray.direction);
                float distSqr = (p.position - pointOnRay).sqrMagnitude;
                if (distSqr < thresholdSqr && distSqr < minDistSqr)
                {
                    minDistSqr = distSqr;
                    closestBody = body;
                }
            }
        }
        return closestBody;
    }
}
