using UnityEngine;
using System.Collections.Generic;

public class SimulationManager : MonoBehaviour
{
    public List<Body> bodies = new List<Body>();
    public float fixedTimeStep = 0.02f;
    public int solverIterations = 10;
    public float particleRadius = 0.1f;
    private float timeAccumulator = 0f;

    private float gridCellSize;
    private Dictionary<Vector3Int, List<Particle>> spatialGrid;
    private struct CollisionPair
    {
        public Particle p1;
        public Particle p2;
        public float minDistance;
    }
    private List<CollisionPair> collisionPairs = new List<CollisionPair>();
    // Fields for dragging and launching
    private Body selectedBody = null;          // The currently selected body
    private Plane dragPlane;                   // Plane for drag calculation
    private Vector3 initialMouseWorldPos;      // Initial mouse position in world space
    private bool isDragging = false;           // Flag to track dragging state
    private float selectionThreshold = 0.5f;   // Max distance for selection (adjustable)
    private float launchFactor = 10.0f;        // Scaling factor for launch strength (adjustable)
    void Awake()
    {
        gridCellSize = 2.1f * particleRadius;
        spatialGrid = new Dictionary<Vector3Int, List<Particle>>();
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
        foreach (Body body in bodies)
        {
            foreach (Particle p in body.particles)
            {
                p.Integrate(deltaTime, body.gravity);
            }
        }

        spatialGrid.Clear();
        foreach (Body body in bodies)
        {
            foreach (Particle p in body.particles)
            {
                Vector3Int cell = GetGridCell(p.position);
                if (!spatialGrid.ContainsKey(cell))
                    spatialGrid[cell] = new List<Particle>();
                spatialGrid[cell].Add(p);
            }
        }

        collisionPairs.Clear();
        HashSet<(Particle, Particle)> checkedPairs = new HashSet<(Particle, Particle)>();
        foreach (Body body1 in bodies)
        {
            foreach (Particle p1 in body1.particles)
            {
                Vector3Int cell = GetGridCell(p1.position);
                List<Vector3Int> neighborCells = GetNeighborCells(cell);
                foreach (Vector3Int neighbor in neighborCells)
                {
                    if (spatialGrid.TryGetValue(neighbor, out List<Particle> cellParticles))
                    {
                        foreach (Particle p2 in cellParticles)
                        {
                            if (p1 == p2 || p1.body == p2.body) continue;
                            if (checkedPairs.Contains((p2, p1))) continue;
                            float minDistance = p1.collisionRadius + p2.collisionRadius;
                            float distance = Vector3.Distance(p1.position, p2.position);
                            if (distance < minDistance)
                            {
                                collisionPairs.Add(new CollisionPair { p1 = p1, p2 = p2, minDistance = minDistance });
                                checkedPairs.Add((p1, p2));
                            }
                        }
                    }
                }
            }
        }

        for (int i = 0; i < solverIterations; i++)
        {
            foreach (Body body in bodies)
            {
                foreach (DistanceConstraint constraint in body.constraints)
                {
                    constraint.Solve();
                }
            }

            foreach (CollisionPair pair in collisionPairs)
            {
                SolveCollision(pair, deltaTime);
            }

            foreach (Body body in bodies)
            {
                foreach (Particle p in body.particles)
                {
                    body.ApplyGroundCollision(p);
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

    List<Vector3Int> GetNeighborCells(Vector3Int cell)
    {
        List<Vector3Int> neighbors = new List<Vector3Int>();
        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                for (int z = -1; z <= 1; z++)
                    neighbors.Add(cell + new Vector3Int(x, y, z));
        return neighbors;
    }

    void SolveCollision(CollisionPair pair, float deltaTime)
    {
        Particle p1 = pair.p1;
        Particle p2 = pair.p2;
        float minDistance = pair.minDistance;

        Vector3 delta = p2.position - p1.position;
        float currentDistance = delta.magnitude;

        if (currentDistance >= minDistance || currentDistance < 0.0001f) return;

        float overlap = minDistance - currentDistance;
        Vector3 correctionNormal = delta / currentDistance;
        Vector3 totalCorrection = correctionNormal * overlap;

        float invMass1 = p1.isFixed ? 0f : 1f / p1.mass;
        float invMass2 = p2.isFixed ? 0f : 1f / p2.mass;
        float totalInverseMass = invMass1 + invMass2;

        if (totalInverseMass == 0f) return;

        if (!p1.isFixed)
            p1.position -= totalCorrection * (invMass1 / totalInverseMass);
        if (!p2.isFixed)
            p2.position += totalCorrection * (invMass2 / totalInverseMass);

        // Optional restitution (uncomment to enable)
        /*
        Vector3 vel1 = (p1.position - p1.prevPosition) / deltaTime;
        Vector3 vel2 = (p2.position - p2.prevPosition) / deltaTime;
        Vector3 relativeVel = millimetresvel1 - vel2;
        float normalVel = Vector3.Dot(relativeVel, correctionNormal);
        if (normalVel < 0)
        {
            float restitution = 0.5f;
            float reflectedVel = -normalVel * restitution;
            Vector3 velCorrection = correctionNormal * (reflectedVel - normalVel);
            if (!p1.isFixed) p1.prevPosition -= velCorrection * invMass1 / totalInverseMass * deltaTime;
            if (!p2.isFixed) p2.prevPosition += velCorrection * invMass2 / totalInverseMass * deltaTime;
        }
        */
    }
    private Body GetSelectedBody(Ray ray, float threshold)
    {
        Body closestBody = null;
        float minDist = float.MaxValue;
        foreach (Body body in bodies)
        {
            foreach (Particle p in body.particles)
            {
                // Find the closest point on the ray to the particle
                Vector3 pointOnRay = ray.origin + ray.direction * Vector3.Dot(p.position - ray.origin, ray.direction);
                float dist = Vector3.Distance(p.position, pointOnRay);
                if (dist < threshold && dist < minDist)
                {
                    minDist = dist;
                    closestBody = body;
                }
            }
        }
        return closestBody;
    }
}
