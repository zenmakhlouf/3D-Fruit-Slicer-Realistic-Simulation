using UnityEngine;

public class BasketFollowCamera : MonoBehaviour
{
    public float distanceFromCamera = 5f;      // المسافة أمام الكاميرا
    public float scrollSpeed = 2f;
    public float minDistance = 2f;
    public float maxDistance = 15f;

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }
void Update()
{
    // التحكم بالمسافة من الكاميرا عن طريق عجلة الماوس
    float scroll = Input.GetAxis("Mouse ScrollWheel");
    if (Mathf.Abs(scroll) > 0.001f)
    {
        distanceFromCamera -= scroll * scrollSpeed;
        distanceFromCamera = Mathf.Clamp(distanceFromCamera, minDistance, maxDistance);
    }

    // حساب موضع السلة بناءً على مؤشر الماوس
    Vector3 mousePos = Input.mousePosition;

    // تعيين z كمقدار المسافة المطلوبة من الكاميرا
    mousePos.z = distanceFromCamera;

    // تحويل إحداثيات الماوس إلى إحداثيات في العالم
    Vector3 worldPos = cam.ScreenToWorldPoint(mousePos);

    // ❗️ تثبيت ارتفاع السلة
    worldPos.y = -0.1f;

    transform.position = worldPos;

    // توجيه السلة للأعلى فقط (اختياري)
    transform.rotation = Quaternion.Euler(0, cam.transform.eulerAngles.y, 0);
}

}
