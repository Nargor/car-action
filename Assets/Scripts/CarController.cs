using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Wheel Colliders")]
    public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider rearLeftWheel;
    public WheelCollider rearRightWheel;

    [Header("Wheel Meshes")]
    public Transform frontLeftMesh;
    public Transform frontRightMesh;
    public Transform rearLeftMesh;
    public Transform rearRightMesh;

    [Header("Engine & Speed")]
    public float maxMotorTorque = 4800f;
    public float maxReverseTorque = 2500f;
    public float maxSpeed = 220f;        // km/h
    public float maxReverseSpeed = 50f;  // km/h

    [Header("Steering (Smooth & Progressive)")]
    public float maxSteeringAngle = 28f;
    public float highSpeedSteeringAngle = 12f; // Stable at high speed
    public float steeringResponseSpeed = 6.0f;  // Smooth interpolation rate

    [Header("Brakes")]
    public float maxBrakeTorque = 14000f;
    public float handbrakeTorque = 18000f;

    [Header("Stability Control (ESP & TCS)")]
    public bool enableESP = true;
    public float espDamping = 1.8f;             // Continuous smooth yaw stabilization
    public float downforceCoefficient = 60f;
    public float antiRollForce = 6000f;

    [Header("Race State")]
    public bool controlsEnabled = true;

    private Rigidbody rb;
    private float currentSpeedKmh;
    private float targetSteerAngle;
    private float smoothedSteerAngle;

    public float SpeedKmh => currentSpeedKmh;
    public Rigidbody CarRigidbody => rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = 1450f;
        rb.linearDamping = 0.08f;
        rb.angularDamping = 1.2f;
        rb.interpolation = RigidbodyInterpolation.Interpolate; // Crucial for silky-smooth motion!
        rb.centerOfMass = new Vector3(0f, -0.45f, 0.0f);

        // Configure suspension spring & dampers on all 4 wheels
        SetupWheel(frontLeftWheel,  2.5f, 3.2f);
        SetupWheel(frontRightWheel, 2.5f, 3.2f);
        SetupWheel(rearLeftWheel,   2.8f, 3.4f);
        SetupWheel(rearRightWheel,  2.8f, 3.4f);
    }

    void SetupWheel(WheelCollider wc, float fwdStiff, float sideStiff)
    {
        if (wc == null) return;

        // Smooth suspension damping
        JointSpring sp = wc.suspensionSpring;
        sp.spring = 40000f;
        sp.damper = 6500f; // Eliminates spring oscillation/bouncing
        sp.targetPosition = 0.5f;
        wc.suspensionSpring = sp;
        wc.suspensionDistance = 0.15f;
        wc.wheelDampingRate = 0.4f;

        // Super-grip racing tire curves
        WheelFrictionCurve fwd = wc.forwardFriction;
        fwd.stiffness = fwdStiff;
        fwd.extremumSlip = 0.35f; fwd.extremumValue = 1.0f;
        fwd.asymptoteSlip = 0.70f; fwd.asymptoteValue = 0.85f;
        wc.forwardFriction = fwd;

        WheelFrictionCurve side = wc.sidewaysFriction;
        side.stiffness = sideStiff;
        side.extremumSlip = 0.30f; side.extremumValue = 1.0f;
        side.asymptoteSlip = 0.60f; side.asymptoteValue = 0.85f;
        wc.sidewaysFriction = side;
    }

    void Update()
    {
        UpdateWheelMesh(frontLeftWheel,  frontLeftMesh);
        UpdateWheelMesh(frontRightWheel, frontRightMesh);
        UpdateWheelMesh(rearLeftWheel,   rearLeftMesh);
        UpdateWheelMesh(rearRightWheel,  rearRightMesh);
    }

    void FixedUpdate()
    {
        float vInput = 0f;
        float hInput = 0f;
        bool spaceBrake = false;

        if (controlsEnabled)
        {
            // Read Keyboard
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    vInput += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  vInput -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) hInput += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  hInput -= 1f;
                if (kb.spaceKey.isPressed) spaceBrake = true;
            }

            // Read Gamepad
            var gp = Gamepad.current;
            if (gp != null)
            {
                float triggerFwd = gp.rightTrigger.ReadValue();
                float triggerRev = gp.leftTrigger.ReadValue();
                if (triggerFwd > 0.05f) vInput += triggerFwd;
                if (triggerRev > 0.05f) vInput -= triggerRev;

                float stickX = gp.leftStick.x.ReadValue();
                if (Mathf.Abs(stickX) > 0.05f) hInput += stickX;

                if (gp.buttonSouth.isPressed) spaceBrake = true;
            }

            // Legacy Fallback
            try
            {
                if (Mathf.Approximately(vInput, 0f)) vInput = Input.GetAxisRaw("Vertical");
                if (Mathf.Approximately(hInput, 0f)) hInput = Input.GetAxisRaw("Horizontal");
                if (!spaceBrake) spaceBrake = Input.GetKey(KeyCode.Space);
            }
            catch { }
        }

        vInput = Mathf.Clamp(vInput, -1f, 1f);
        hInput = Mathf.Clamp(hInput, -1f, 1f);

        // Speed calculation
        Vector3 forward = transform.forward;
        float forwardVelocity = Vector3.Dot(rb.linearVelocity, forward);
        currentSpeedKmh = rb.linearVelocity.magnitude * 3.6f;

        // 1. Progressive, Smooth Steering (no snapping)
        float speedRatio = Mathf.Clamp01(currentSpeedKmh / maxSpeed);
        float effectiveMaxSteer = Mathf.Lerp(maxSteeringAngle, highSpeedSteeringAngle, speedRatio);
        targetSteerAngle = hInput * effectiveMaxSteer;
        smoothedSteerAngle = Mathf.MoveTowards(smoothedSteerAngle, targetSteerAngle, Time.fixedDeltaTime * steeringResponseSpeed * effectiveMaxSteer);

        if (frontLeftWheel != null)  frontLeftWheel.steerAngle  = smoothedSteerAngle;
        if (frontRightWheel != null) frontRightWheel.steerAngle = smoothedSteerAngle;

        // 2. Throttle & Smart Braking (smooth transitions)
        float motorTorque = 0f;
        float brakeTorque = 0f;

        if (spaceBrake)
        {
            brakeTorque = handbrakeTorque;
            motorTorque = 0f;
        }
        else if (vInput > 0.05f)
        {
            if (forwardVelocity < -0.8f)
            {
                // Moving backwards -> brake
                brakeTorque = maxBrakeTorque * vInput;
                motorTorque = 0f;
            }
            else
            {
                if (currentSpeedKmh < maxSpeed)
                {
                    float torqueMult = Mathf.Clamp01(1f - (currentSpeedKmh / maxSpeed));
                    motorTorque = vInput * maxMotorTorque * torqueMult;
                }
                brakeTorque = 0f;
            }
        }
        else if (vInput < -0.05f)
        {
            if (forwardVelocity > 0.8f)
            {
                // Running forward -> S acts as smooth service brake
                brakeTorque = maxBrakeTorque * (-vInput);
                motorTorque = 0f;
            }
            else
            {
                // Stopped or reversing
                if (currentSpeedKmh < maxReverseSpeed)
                {
                    motorTorque = vInput * maxReverseTorque;
                }
                brakeTorque = 0f;
            }
        }
        else
        {
            // Coasting smoothly without artificial brake shudder
            motorTorque = 0f;
            brakeTorque = 0f;
        }

        // Apply Motor Torque (All-Wheel Traction)
        if (rearLeftWheel != null)   rearLeftWheel.motorTorque  = motorTorque * 0.5f;
        if (rearRightWheel != null)  rearRightWheel.motorTorque = motorTorque * 0.5f;
        if (frontLeftWheel != null)  frontLeftWheel.motorTorque  = motorTorque * 0.5f;
        if (frontRightWheel != null) frontRightWheel.motorTorque = motorTorque * 0.5f;

        // Apply Brake Torque (60% front, 40% rear bias)
        if (frontLeftWheel != null)  frontLeftWheel.brakeTorque  = brakeTorque * 0.6f;
        if (frontRightWheel != null) frontRightWheel.brakeTorque = brakeTorque * 0.6f;
        if (rearLeftWheel != null)   rearLeftWheel.brakeTorque   = brakeTorque * 0.4f;
        if (rearRightWheel != null)  rearRightWheel.brakeTorque  = brakeTorque * 0.4f;

        // 3. Continuous Smooth ESP (No step-function chattering!)
        if (enableESP && currentSpeedKmh > 10f)
        {
            ApplySmoothESP();
        }

        // 4. Downforce (smoothly clamped)
        float downforce = Mathf.Min(currentSpeedKmh * downforceCoefficient, 12000f);
        rb.AddForce(-transform.up * downforce);

        // 5. Anti-Roll Bars (softened)
        ApplyAntiRollBar(frontLeftWheel, frontRightWheel);
        ApplyAntiRollBar(rearLeftWheel,  rearRightWheel);
    }

    private void ApplySmoothESP()
    {
        // Calculate desired yaw from steer angle & speed
        float expectedYawRate = (smoothedSteerAngle * Mathf.Deg2Rad) * (currentSpeedKmh / 3.6f) / 2.6f;
        float actualYawRate = rb.angularVelocity.y;
        float yawError = Mathf.Clamp(actualYawRate - expectedYawRate, -2.5f, 2.5f);

        // Smooth proportional damping (zero deadband chatter)
        float stabilizingTorque = -yawError * espDamping * rb.mass;
        rb.AddTorque(Vector3.up * stabilizingTorque, ForceMode.Force);
    }

    private void ApplyAntiRollBar(WheelCollider wheelL, WheelCollider wheelR)
    {
        if (wheelL == null || wheelR == null) return;

        float travelL = 1.0f;
        float travelR = 1.0f;

        bool groundedL = wheelL.GetGroundHit(out WheelHit hitL);
        if (groundedL)
            travelL = (-wheelL.transform.InverseTransformPoint(hitL.point).y - wheelL.radius) / wheelL.suspensionDistance;

        bool groundedR = wheelR.GetGroundHit(out WheelHit hitR);
        if (groundedR)
            travelR = (-wheelR.transform.InverseTransformPoint(hitR.point).y - wheelR.radius) / wheelR.suspensionDistance;

        float antiRoll = Mathf.Clamp((travelL - travelR) * antiRollForce, -10000f, 10000f);

        if (groundedL)
            rb.AddForceAtPosition(wheelL.transform.up * -antiRoll, wheelL.transform.position);
        if (groundedR)
            rb.AddForceAtPosition(wheelR.transform.up * antiRoll, wheelR.transform.position);
    }

    private void UpdateWheelMesh(WheelCollider col, Transform mesh)
    {
        if (col == null || mesh == null) return;
        col.GetWorldPose(out Vector3 pos, out Quaternion rot);
        mesh.position = pos;
        mesh.rotation = rot;
    }
}
