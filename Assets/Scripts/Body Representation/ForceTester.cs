using UnityEngine;

/// <summary>
/// A test script to demonstrate how to add forces to a Body during runtime.
/// Attach this to any GameObject in your scene.
/// </summary>
public class ForceTester : MonoBehaviour
{
    [Header("Target Body")]
    public Body targetBody; // Assign this in the inspector

    [Header("Force Settings")]
    public float forceMagnitude = 10f;
    public KeyCode applyForceKeyRight = KeyCode.E;
    public KeyCode applyForceKeyLeft = KeyCode.Q;

    public KeyCode addForceKey = KeyCode.F;
    public KeyCode impulseKey = KeyCode.I;

    void Update()
    {
        if (targetBody == null) return;

        // Apply a constant force while holding space
        if (Input.GetKey(applyForceKeyRight))
        {
            targetBody.ApplyForce(Vector3.right * forceMagnitude);
        }
        if (Input.GetKey(applyForceKeyLeft))
        {
            targetBody.ApplyForce(Vector3.left * forceMagnitude);
        }

        // Add a force impulse when pressing F
        if (Input.GetKeyDown(addForceKey))
        {
            targetBody.AddForce(Vector3.up * forceMagnitude);
        }

        // Apply an impulse (instant velocity change) when pressing I
        if (Input.GetKeyDown(impulseKey))
        {
            targetBody.SetVelocity(Vector3.forward * forceMagnitude);
        }

        // Example of applying force in the direction the camera is looking
        if (Input.GetKey(KeyCode.C))
        {
            Vector3 cameraDirection = Camera.main.transform.forward;
            targetBody.ApplyForce(cameraDirection * forceMagnitude);
        }
    }
}