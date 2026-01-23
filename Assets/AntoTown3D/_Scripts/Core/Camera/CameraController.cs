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
    public float zoomSpeed = 10f;
    public float minZoom = 5f;
    public float maxZoom = 40f;

    [Header("Rotation")]
    public float rotationSpeed = 100f;

    [Header("Bounds")]
    public Vector2 mapMin = new Vector2(-10, -10);
    public Vector2 mapMax = new Vector2(50, 50);

    Vector3 lastMousePosition;
    Vector3 lastRightMousePos;
    bool isRightDragging;

    void Update()
    {
        HandleMovement();
        HandleMouseDrag();      // middle mouse
        HandleRightMouseDrag(); // 🆕 right mouse
        HandleZoom();
        HandleRotation();
        ClampPosition();
    }

    // ================= MOVE =================
    void HandleMovement()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current.wKey.isPressed) input.y += 1;
        if (Keyboard.current.sKey.isPressed) input.y -= 1;
        if (Keyboard.current.dKey.isPressed) input.x += 1;
        if (Keyboard.current.aKey.isPressed) input.x -= 1;

        Vector3 dir = new Vector3(input.x, 0, input.y);

        float speed = moveSpeed;
        if (Keyboard.current.leftShiftKey.isPressed)
            speed *= boostMultiplier;

        transform.Translate(dir * speed * Time.deltaTime, Space.Self);
    }

    // ================= DRAG =================
    void HandleMouseDrag()
    {
        if (Mouse.current.middleButton.isPressed)
        {
            Vector3 delta = Mouse.current.delta.ReadValue();
            Vector3 move = new Vector3(-delta.x, 0, -delta.y) * dragSpeed * Time.deltaTime;
            transform.Translate(move, Space.Self);
        }
    }

    // ================= ZOOM =================
    void HandleZoom()
    {
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) < 0.01f) return;

        Vector3 pos = cameraTransform.localPosition;

        pos.y -= scroll * zoomSpeed * 0.01f; // 🔥 zoom naik-turun
        pos.y = Mathf.Clamp(pos.y, minZoom, maxZoom);

        cameraTransform.localPosition = pos;
    }


    // ================= ROTATE =================
    void HandleRotation()
    {
        if (Keyboard.current.eKey.isPressed)
            transform.Rotate(Vector3.up, -rotationSpeed * Time.deltaTime);

        if (Keyboard.current.qKey.isPressed)
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    // ================= CLAMP =================
    void ClampPosition()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, mapMin.x, mapMax.x);
        pos.z = Mathf.Clamp(pos.z, mapMin.y, mapMax.y);
        transform.position = pos;
    }

    void HandleRightMouseDrag()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            lastRightMousePos = Mouse.current.position.ReadValue();
            isRightDragging = true;
        }

        if (Mouse.current.rightButton.wasReleasedThisFrame)
        {
            isRightDragging = false;
        }

        if (!isRightDragging) return;

        Vector3 currentMousePos = Mouse.current.position.ReadValue();
        Vector3 delta = currentMousePos - lastRightMousePos;

        lastRightMousePos = currentMousePos;

        // 🔥 geser kamera sesuai swipe
        Vector3 move = new Vector3(-delta.x, 0, -delta.y) * dragSpeed * Time.deltaTime;
        transform.Translate(move, Space.Self);
    }

}
