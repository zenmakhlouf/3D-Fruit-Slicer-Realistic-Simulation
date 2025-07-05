using UnityEngine;

public class KnifeController : MonoBehaviour
{
    public Camera mainCamera;
    public float knifeDepth = 0f; // Start a few units in front of camera
    public float scrollSpeed = 10f;
    public float minDepth = 0.5f; // Closest to camera
    public float maxDepth = 100f; // Furthest away

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void Update()
    {
        HandleDepthScroll();
        MoveKnifeWithMouse();
    }

    void HandleDepthScroll()
    {
        // Scroll wheel: Up (positive) -> forward (closer), Down (negative) -> backward (further)
        float scrollDelta = -Input.mouseScrollDelta.y; // NOTE the minus sign!
        knifeDepth += scrollDelta * scrollSpeed;
        knifeDepth = Mathf.Clamp(knifeDepth, minDepth, maxDepth);
    }

    void MoveKnifeWithMouse()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = knifeDepth;
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
        transform.position = worldPos;
    }
}
