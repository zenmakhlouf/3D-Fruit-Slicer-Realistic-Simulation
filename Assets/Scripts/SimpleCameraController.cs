using UnityEngine;

public class SimpleCameraController : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float lookSpeed = 10f;
    private float rotationX = 0f;
    private float rotationY = 0f;


    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(1))
        {
            rotationX += Input.GetAxis("Mouse X") * lookSpeed;
            rotationY -= Input.GetAxis("Mouse Y") * lookSpeed;
            rotationY = Mathf.Clamp(rotationY, -90, 90);
            transform.localRotation = Quaternion.Euler(rotationY, rotationX, 0);
        }
        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) move += transform.forward;
        if (Input.GetKey(KeyCode.S)) move -= transform.forward;
        if (Input.GetKey(KeyCode.A)) move -= transform.right;
        if (Input.GetKey(KeyCode.D)) move += transform.right;
        if (Input.GetKey(KeyCode.Space)) move += transform.up;
        if (Input.GetKey(KeyCode.LeftShift)) move -= transform.up;

        transform.position += move * moveSpeed * Time.deltaTime;
        
    }
}
