using UnityEngine;

public class CubeBody : MonoBehaviour
{
    [Header("Cube Properties")]
    public float size = 1f;                     // Side length of the cube
    [Range(0.01f, 100f)]
    public float massPerParticle = 0.125f;      // Mass assigned to each particle
    [Range(0.01f, 1.0f)]
    public float springStiffness = 1f;          // Stiffness of springs connecting particles

    [Header("Structural Rigidity")]
    public bool addFaceDiagonals = true;        // Add diagonal springs across faces
    public bool addInternalDiagonals = false;   // Add diagonal springs inside the cube

    void Start()
    {
        Body body = GetComponent<Body>();
        GenerateCube(body);
    }

    void GenerateCube(Body bodyComponent)
    {
        // Clear existing particles and constraints
        bodyComponent.particles.Clear();
        bodyComponent.constraints.Clear();

        // Define local positions of the 8 corners (unit cube scaled by size/2)
        Vector3[] cornerOffsets = new Vector3[]
        {
            new Vector3(-1, -1, -1), new Vector3(1, -1, -1), new Vector3(1, -1, 1), new Vector3(-1, -1, 1),
            new Vector3(-1, 1, -1),  new Vector3(1, 1, -1),  new Vector3(1, 1, 1),  new Vector3(-1, 1, 1)
        };

        // Array to store the 8 corner particles
        Particle[] cubeParticles = new Particle[8];

        // Generate particles at each corner
        for (int i = 0; i < 8; i++)
        {
            Vector3 localPos = cornerOffsets[i] * size * 0.5f;         // Scale to cube size
            Vector3 worldPos = transform.TransformPoint(localPos);      // Convert to world space
            Particle p = new Particle(worldPos, massPerParticle);       // Create particle
            p.body = this.GetComponent<Body>();                         // Set body reference
            p.collisionRadius = size / 4f;                              // Set collision radius
            cubeParticles[i] = p;                                       // Store in array
            bodyComponent.particles.Add(p);                             // Add to body
        }

        // Helper method to add a spring between two particles
        void AddSpring(int particleIndex1, int particleIndex2)
        {
            bodyComponent.constraints.Add(new DistanceConstraint(
                cubeParticles[particleIndex1],
                cubeParticles[particleIndex2],
                springStiffness));
        }

        // Add edge springs (12 edges of the cube)
        AddSpring(0, 1); AddSpring(1, 2); AddSpring(2, 3); AddSpring(3, 0); // Bottom face
        AddSpring(4, 5); AddSpring(5, 6); AddSpring(6, 7); AddSpring(7, 4); // Top face
        AddSpring(0, 4); AddSpring(1, 5); AddSpring(2, 6); AddSpring(3, 7); // Vertical edges

        // Add face diagonal springs (2 per face, 6 faces = 12 diagonals)
        if (addFaceDiagonals)
        {
            AddSpring(0, 2); AddSpring(1, 3); // Bottom face
            AddSpring(4, 6); AddSpring(5, 7); // Top face
            AddSpring(0, 5); AddSpring(1, 4); // Front face
            AddSpring(3, 6); AddSpring(2, 7); // Back face
            AddSpring(0, 7); AddSpring(3, 4); // Left face
            AddSpring(1, 6); AddSpring(2, 5); // Right face
        }

        // Add internal diagonal springs (4 space diagonals)
        if (addInternalDiagonals)
        {
            AddSpring(0, 6); AddSpring(1, 7); AddSpring(2, 4); AddSpring(3, 5);
        }

        // Debug.Log($"CubeBody '{gameObject.name}' generated: {bodyComponent.particles.Count} particles, " +
                //   $"{bodyComponent.constraints.Count} constraints.");
    }
}