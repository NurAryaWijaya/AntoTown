using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 20f;
    public float dragSpeed = 1.5f;
    public float boostMultiplier = 2f;

    [Header("Zoom")]
    public Transform cameraTransform;
    public float zoomSpeed = 3.5f;
    public float minZoom = 5f;
    public float maxZoom = 40f;

    [Header("Rotation")]
    public float rotationSpeed = 100f;

    [Header("Bounds")]
    public Vector2 mapMin = new Vector2(-10, -10);
    public Vector2 mapMax = new Vector2(50, 50);

    public bool allowDrag = true;

    [Header("Drag Threshold")]
    public float dragThreshold = 15f; // pixel
    Vector2 lastTouchPos;
    bool isTouchDragging;
    bool hasExceededThreshold;


    void Update()
    {
        //HandleMovement();
        HandleDrag();      // middle mouse
        //HandleRightMouseDrag(); // 🆕 right mouse
        HandleZoom();
        //HandleRotation();
        ClampPosition();
    }

    // ================= DRAG =================
    void HandleDrag()
    {
        if (!allowDrag) return;

        // 🔻 sensitivitas dikurangi setengah
        float finalSpeed = dragSpeed * 0.40f;

        // ================= ANDROID =================
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;

            if (touch.press.wasPressedThisFrame)
            {
                lastTouchPos = touch.position.ReadValue();
                isTouchDragging = true;
                hasExceededThreshold = false;
            }

            if (touch.press.isPressed && isTouchDragging)
            {
                Vector2 currentPos = touch.position.ReadValue();
                Vector2 delta = currentPos - lastTouchPos;

                // 🚫 kalau belum melewati threshold → jangan gerakkan kamera
                if (!hasExceededThreshold)
                {
                    if (delta.magnitude < dragThreshold)
                        return;

                    hasExceededThreshold = true;
                }

                lastTouchPos = currentPos;

                Vector3 move = new Vector3(-delta.x, 0, -delta.y)
                               * finalSpeed * Time.deltaTime;

                transform.Translate(move, Space.Self);
                return;
            }

            if (touch.press.wasReleasedThisFrame)
            {
                isTouchDragging = false;
            }
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        // ================= PC =================
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();

            // 🚫 klik kecil tidak menggeser kamera
            if (delta.magnitude < dragThreshold)
                return;

            Vector3 move = new Vector3(-delta.x, 0, -delta.y)
                           * finalSpeed * Time.deltaTime;

            transform.Translate(move, Space.Self);
        }
#endif
    }

    // ================= ZOOM =================
    void HandleZoom()
    {
        if(!allowDrag) return;
        // ANDROID PINCH
        if (Touchscreen.current != null && Touchscreen.current.touches.Count >= 2)
        {
            var t0 = Touchscreen.current.touches[0];
            var t1 = Touchscreen.current.touches[1];

            if (!t0.press.isPressed || !t1.press.isPressed)
                return;

            float prevDist = Vector2.Distance(
                t0.position.ReadValue() - t0.delta.ReadValue(),
                t1.position.ReadValue() - t1.delta.ReadValue()
            );

            float currDist = Vector2.Distance(
                t0.position.ReadValue(),
                t1.position.ReadValue()
            );

            float delta = currDist - prevDist;

            ApplyZoom(delta * 0.001f);
            return;
        }

        // PC SCROLL
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
                ApplyZoom(scroll * 0.1f);
        }
    }

    void ApplyZoom(float amount)
    {
        Vector3 pos = cameraTransform.localPosition;
        pos.y -= amount * zoomSpeed;
        pos.y = Mathf.Clamp(pos.y, minZoom, maxZoom);
        cameraTransform.localPosition = pos;
    }


    // ================= CLAMP =================
    void ClampPosition()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, mapMin.x, mapMax.x);
        pos.z = Mathf.Clamp(pos.z, mapMin.y, mapMax.y);
        transform.position = pos;
    }

}
