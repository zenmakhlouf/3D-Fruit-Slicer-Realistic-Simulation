using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class CustomPhysics : MonoBehaviour
{
    public float mass = 1f;
    public Vector3 velocity;
    public Vector3 acceleration;
    public Vector3 initialForce = new Vector3(0, 0, 0);

    void Start()
    {
        ApplyForce(initialForce);
    }
    // Update is called once per frame
    void Update()
    {
        velocity += acceleration * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;

        acceleration = Vector3.zero;

        ApplyGravity();

        float groundY = 0f;
        float bottom = transform.position.y - GetComponent<SimpleCollider>().radius;

        if (bottom < groundY)
        {
            Debug.Log(name + " hit the ground");
            if (velocity.y < 0)
            {
                velocity.y = -velocity.y * 0.8f;

                float radius = GetComponent<SimpleCollider>().radius;
                transform.position = new Vector3(transform.position.x, groundY + radius, transform.position.z);

                GetComponent<MeshDeformer>().ApplyDeformation(transform.position + Vector3.down * GetComponent<SimpleCollider>().radius, 0.5f);
            }
        }

    }
    public void ApplyForce(Vector3 force)
    {
        acceleration += force / mass;
    }
    public void ApplyGravity()
    {
        ApplyForce(new Vector3(0, -9.81f * mass, 0));

    }
}
