using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class ChaseCameraController : MonoBehaviour
{
    public static ChaseCameraController Instance { get; private set; }

    public enum CameraMode
    {
        FPS,     // First-person cockpit / hood view
        TPS,     // Close chase camera
        TPS2,    // Far cinematic chase camera
        Birdeye, // Overhead top-down view
        NoClip   // Free flying spectator camera
    }

    [Header("Camera State")]
    public CameraMode currentMode = CameraMode.TPS;
    public Transform target;

    [Header("Orbit Controls (Mouse Drag)")]
    public float mouseSensitivity = 0.25f;
    public float orbitYaw   = 0f;
    public float orbitPitch = 0f;

    [Header("NoClip Settings")]
    public float noClipSpeed = 35f;
    public float noClipFastSpeed = 100f;
    public float noClipRotSpeed = 2.5f;

    [Header("Smoothing")]
    public float smoothPosSpeed = 12f;
    public float smoothRotSpeed = 10f;

    private Vector3 currentVelocity;
    private Rigidbody targetRb;
    private bool isDraggingOrbit = false;
    private Vector2 lastMousePos;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        SetTarget(target);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            targetRb = target.GetComponent<Rigidbody>();
        }
    }

    public void SetCameraMode(CameraMode mode)
    {
        currentMode = mode;
        orbitYaw = 0f;
        orbitPitch = 0f;

        if (mode == CameraMode.NoClip && target != null)
        {
            transform.position = target.position + Vector3.up * 8f - target.forward * 12f;
            transform.LookAt(target.position + Vector3.up * 2f);
        }

        Debug.Log($"[Camera] Switched to mode: {mode}");
    }

    void Update()
    {
        HandleInputShortcuts();
        HandleMouseOrbit();

        if (currentMode == CameraMode.NoClip)
        {
            HandleNoClipMovement();
        }
    }

    private void HandleInputShortcuts()
    {
        bool cPressed = false;
        bool k1 = false, k2 = false, k3 = false, k4 = false, k5 = false;
        bool rPressed = false;

        // New Input System
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.cKey.wasPressedThisFrame) cPressed = true;
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) k1 = true;
            if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) k2 = true;
            if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) k3 = true;
            if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) k4 = true;
            if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame) k5 = true;
            if (kb.rKey.wasPressedThisFrame) rPressed = true;
        }

        // Legacy Input Fallback
        try
        {
            if (!cPressed) cPressed = Input.GetKeyDown(KeyCode.C);
            if (!k1) k1 = Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1);
            if (!k2) k2 = Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2);
            if (!k3) k3 = Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3);
            if (!k4) k4 = Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4);
            if (!k5) k5 = Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5);
            if (!rPressed) rPressed = Input.GetKeyDown(KeyCode.R);
        }
        catch { }

        if (cPressed)
        {
            int next = ((int)currentMode + 1) % 5;
            SetCameraMode((CameraMode)next);
        }

        if (k1) SetCameraMode(CameraMode.FPS);
        if (k2) SetCameraMode(CameraMode.TPS);
        if (k3) SetCameraMode(CameraMode.TPS2);
        if (k4) SetCameraMode(CameraMode.Birdeye);
        if (k5) SetCameraMode(CameraMode.NoClip);

        if (rPressed)
        {
            orbitYaw = 0f;
            orbitPitch = 0f;
        }
    }

    private void HandleMouseOrbit()
    {
        if (currentMode == CameraMode.NoClip) return;

        bool leftPressed = false;
        bool rightPressed = false;
        Vector2 mousePos = Vector2.zero;

        var mouse = Mouse.current;
        if (mouse != null)
        {
            leftPressed = mouse.leftButton.isPressed;
            rightPressed = mouse.rightButton.isPressed;
            mousePos = mouse.position.ReadValue();

            if (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    isDraggingOrbit = false;
                    return;
                }
                isDraggingOrbit = true;
                lastMousePos = mousePos;
            }
        }
        else
        {
            try
            {
                leftPressed = Input.GetMouseButton(0);
                rightPressed = Input.GetMouseButton(1);
                mousePos = Input.mousePosition;
                if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
                {
                    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    {
                        isDraggingOrbit = false;
                        return;
                    }
                    isDraggingOrbit = true;
                    lastMousePos = mousePos;
                }
            }
            catch { }
        }

        if (leftPressed || rightPressed)
        {
            if (isDraggingOrbit)
            {
                Vector2 delta = mousePos - lastMousePos;
                lastMousePos = mousePos;

                orbitYaw += delta.x * mouseSensitivity;
                orbitPitch -= delta.y * mouseSensitivity;

                if (currentMode == CameraMode.FPS)
                    orbitPitch = Mathf.Clamp(orbitPitch, -40f, 40f);
                else if (currentMode == CameraMode.Birdeye)
                    orbitPitch = Mathf.Clamp(orbitPitch, -20f, 15f);
                else
                    orbitPitch = Mathf.Clamp(orbitPitch, -25f, 65f);
            }
        }
        else
        {
            isDraggingOrbit = false;
        }
    }

    private void HandleNoClipMovement()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;

        // Free Look with Right Click Drag
        if (mouse != null && mouse.rightButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue();
            transform.Rotate(Vector3.up, delta.x * noClipRotSpeed * 0.1f, Space.World);
            transform.Rotate(Vector3.right, -delta.y * noClipRotSpeed * 0.1f, Space.Self);
        }

        float speed = (kb != null && kb.leftShiftKey.isPressed) ? noClipFastSpeed : noClipSpeed;

        Vector3 moveDir = Vector3.zero;
        if (kb != null)
        {
            if (kb.wKey.isPressed) moveDir += transform.forward;
            if (kb.sKey.isPressed) moveDir -= transform.forward;
            if (kb.dKey.isPressed) moveDir += transform.right;
            if (kb.aKey.isPressed) moveDir -= transform.right;
            if (kb.eKey.isPressed) moveDir += Vector3.up;
            if (kb.qKey.isPressed) moveDir -= Vector3.up;
        }

        try
        {
            if (Input.GetKey(KeyCode.W)) moveDir += transform.forward;
            if (Input.GetKey(KeyCode.S)) moveDir -= transform.forward;
            if (Input.GetKey(KeyCode.D)) moveDir += transform.right;
            if (Input.GetKey(KeyCode.A)) moveDir -= transform.right;
            if (Input.GetKey(KeyCode.E)) moveDir += Vector3.up;
            if (Input.GetKey(KeyCode.Q)) moveDir -= Vector3.up;
            if (Input.GetKey(KeyCode.LeftShift)) speed = noClipFastSpeed;
        }
        catch { }

        if (moveDir.sqrMagnitude > 0.001f)
        {
            transform.position += moveDir.normalized * (speed * Time.deltaTime);
        }
    }

    void LateUpdate()
    {
        if (currentMode == CameraMode.NoClip) return;
        if (target == null) return;

        switch (currentMode)
        {
            case CameraMode.FPS:
                UpdateFPS();
                break;
            case CameraMode.TPS:
                UpdateTPS(7.0f, 2.2f, 3.5f);
                break;
            case CameraMode.TPS2:
                UpdateTPS(13.5f, 4.2f, 4.0f);
                break;
            case CameraMode.Birdeye:
                UpdateBirdeye();
                break;
        }
    }

    private void UpdateFPS()
    {
        Vector3 cockpitOffset = new Vector3(0f, 0.95f, 0.45f);
        Vector3 targetPos = target.position + target.rotation * cockpitOffset;

        transform.position = targetPos;

        Quaternion baseRot = target.rotation;
        Quaternion orbitRot = Quaternion.Euler(orbitPitch, orbitYaw, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, baseRot * orbitRot, Time.deltaTime * 25f);
    }

    private void UpdateTPS(float distance, float height, float lookAhead)
    {
        Vector3 forward = target.forward;
        Vector3 flatForward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
        if (flatForward.sqrMagnitude < 0.001f) flatForward = Vector3.forward;

        Quaternion yawRot = Quaternion.AngleAxis(orbitYaw, Vector3.up);
        Vector3 camDir = yawRot * flatForward;

        float effectiveHeight = height + Mathf.Sin(orbitPitch * Mathf.Deg2Rad) * distance * 0.7f;
        float effectiveDistance = distance * Mathf.Cos(orbitPitch * Mathf.Deg2Rad);
        effectiveHeight = Mathf.Max(0.8f, effectiveHeight);
        effectiveDistance = Mathf.Max(2.5f, effectiveDistance);

        Vector3 desiredPos = target.position - camDir * effectiveDistance + Vector3.up * effectiveHeight;

        transform.position = Vector3.SmoothDamp(
            transform.position, desiredPos,
            ref currentVelocity, 1f / smoothPosSpeed);

        Vector3 lookTarget = target.position + camDir * lookAhead + Vector3.up * (height * 0.4f);
        Quaternion desiredRot = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);

        transform.rotation = Quaternion.Slerp(
            transform.rotation, desiredRot,
            Time.deltaTime * smoothRotSpeed);
    }

    private void UpdateBirdeye()
    {
        float height = 36f;
        float distBehind = 4f;

        Vector3 forward = target.forward;
        Vector3 flatForward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
        if (flatForward.sqrMagnitude < 0.001f) flatForward = Vector3.forward;

        Quaternion yawRot = Quaternion.AngleAxis(orbitYaw, Vector3.up);
        Vector3 camDir = yawRot * flatForward;

        Vector3 desiredPos = target.position - camDir * distBehind + Vector3.up * height;

        transform.position = Vector3.SmoothDamp(
            transform.position, desiredPos,
            ref currentVelocity, 1f / smoothPosSpeed);

        Vector3 lookTarget = target.position + camDir * 2f;
        Quaternion desiredRot = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);

        transform.rotation = Quaternion.Slerp(
            transform.rotation, desiredRot,
            Time.deltaTime * smoothRotSpeed);
    }
}
