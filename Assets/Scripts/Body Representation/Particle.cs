using UnityEngine;

public class Particle
{
    public Vector3 position;
    public Vector3 prevPosition;
    public float mass = 1f;
    public bool isFixed = false;

    public Particle(Vector3 pos, float mass, bool isFixed = false)
    {
        this.position = pos;
        this.prevPosition = pos;
        this.mass = mass;
        this.isFixed = isFixed;
    }

    public void Integrate(float deltaTime, Vector3 gravity)
    {
        if (isFixed) return;

        Vector3 velocity = position - prevPosition;
        prevPosition = position;
        position += velocity + gravity * (deltaTime * deltaTime);
    }
}