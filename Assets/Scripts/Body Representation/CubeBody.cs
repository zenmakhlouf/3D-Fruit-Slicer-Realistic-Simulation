using UnityEngine;

/// <summary>
/// Generates the particles and constraints for a cube-shaped soft body.
/// </summary>
[RequireComponent(typeof(Body))]
public class CubeBody : MonoBehaviour
{
    [Header("Cube Properties")]
    public float size = 1f;
    [Range(0.01f, 100f)]
    public float massPerParticle = 0.125f;
    [Range(0.01f, 1.0f)]
    public float springStiffness = 1f;

    [Header("Structural Rigidity")]
    public bool addFaceDiagonals = true;    // Adds springs across each face for shear resistance
    public bool addInternalDiagonals = false; // Adds springs through the cube's center for volume preservation

    void Start()
    {
        Body body = GetComponent<Body>();
        GenerateCube(body);
    }

    void GenerateCube(Body bodyComponent)
    {
        bodyComponent.particles.Clear();
        bodyComponent.constraints.Clear();

        // Define 8 corner points of a cube
        Vector3[] cornerOffsets = new Vector3[]
        {
            new Vector3(-1,-1,-1), new Vector3(1,-1,-1), new Vector3(1,-1,1), new Vector3(-1,-1,1), // Bottom face
            new Vector3(-1,1,-1),  new Vector3(1,1,-1),  new Vector3(1,1,1),  new Vector3(-1,1,1)  // Top face
        };

        Particle[] cubeParticles = new Particle[8];
        for (int i = 0; i < 8; i++)
        {
            Vector3 localPos = cornerOffsets[i] * size * 0.5f;
            Vector3 worldPos = transform.TransformPoint(localPos); // Position relative to this GameObject
            cubeParticles[i] = new Particle(worldPos, massPerParticle);
            bodyComponent.particles.Add(cubeParticles[i]);
        }

        // Helper to create a distance constraint (spring)
        void AddSpring(int particleIndex1, int particleIndex2)
        {
            bodyComponent.constraints.Add(new DistanceConstraint(cubeParticles[particleIndex1], cubeParticles[particleIndex2], springStiffness));
        }

        // Create springs for the 12 edges
        AddSpring(0, 1); AddSpring(1, 2); AddSpring(2, 3); AddSpring(3, 0); // Bottom face edges
        AddSpring(4, 5); AddSpring(5, 6); AddSpring(6, 7); AddSpring(7, 4); // Top face edges
        AddSpring(0, 4); AddSpring(1, 5); AddSpring(2, 6); AddSpring(3, 7); // Vertical edges

        if (addFaceDiagonals)
        {
            // Springs across the 6 faces (2 per face)
            AddSpring(0, 2); AddSpring(1, 3); // Bottom face diagonals
            AddSpring(4, 6); AddSpring(5, 7); // Top face diagonals
            AddSpring(0, 5); AddSpring(1, 4); // Front face diagonals (0,1,5,4)
            AddSpring(3, 6); AddSpring(2, 7); // Back face diagonals (3,2,6,7)
            AddSpring(0, 7); AddSpring(3, 4); // Left face diagonals (0,3,7,4)
            AddSpring(1, 6); AddSpring(2, 5); // Right face diagonals (1,2,6,5)
        }

        if (addInternalDiagonals)
        {
            // Springs connecting opposite corners through the cube's volume
            AddSpring(0, 6); AddSpring(1, 7); AddSpring(2, 4); AddSpring(3, 5);
        }

        Debug.Log($"CubeBody '{gameObject.name}' generated: {bodyComponent.particles.Count} particles, {bodyComponent.constraints.Count} constraints.");
    }
}
