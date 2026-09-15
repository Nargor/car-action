using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AICarController : MonoBehaviour
{
    [Header("Racer Profile")]
    public string racerName = "AI Racer";
    public Color carColor = Color.red;

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

    [Header("Performance Settings")]
    public float maxMotorTorque = 4600f;
    public float maxBrakeTorque = 12000f;
    public float topSpeed = 195f;        // km/h
    public float cornerSpeed = 95f;      // km/h on sharp corners
    public float maxSteeringAngle = 30f;

    [Header("Navigation Settings")]
    public float lookaheadDistance = 35f;
    public float laneOffset = 0f;        // Random lane preference (-8m to +8m)

    [Header("State")]
    public bool isRacing = false;
    public int currentWaypointIndex = 0;
    public float lapProgressDistance = 0f;

    [Header("TikTok Live Interactions")]
    public RacerOverheadUI overheadUI;
    public bool isNitroActive = false;
    public bool isPranked = false;
    private string currentPrankType = "";

    private Rigidbody rb;
    private Vector3[] waypoints;
    private float currentSpeedKmh;
    private float stuckTimer = 0f;
    private bool isUnstucking = false;
    private float unstuckEndTime = 0f;

    public float SpeedKmh => currentSpeedKmh;
    public Rigidbody CarRigidbody => rb;
    public float CurrentThrottle { get; private set; }
    public float CurrentBrake { get; private set; }

    public void Initialize(Vector3[] trackWaypoints, int startWaypointIdx, float lateralOffset, Color color, string name)
    {
        waypoints = trackWaypoints;
        currentWaypointIndex = startWaypointIdx;
        laneOffset = lateralOffset;
        carColor = color;
        racerName = name;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = 1450f;
        rb.linearDamping = 0.05f;
        rb.angularDamping = 1.0f;
        rb.centerOfMass = new Vector3(0f, -0.45f, 0.0f);

        TuneFriction(frontLeftWheel,  2.4f, 3.0f);
        TuneFriction(frontRightWheel, 2.4f, 3.0f);
        TuneFriction(rearLeftWheel,   2.6f, 3.2f);
        TuneFriction(rearRightWheel,  2.6f, 3.2f);
    }

    void TuneFriction(WheelCollider wc, float fwdStiff, float sideStiff)
    {
        if (wc == null) return;
        var fwd = wc.forwardFriction;
        fwd.stiffness = fwdStiff;
        fwd.extremumSlip = 0.35f; fwd.extremumValue = 1f;
        fwd.asymptoteSlip = 0.7f; fwd.asymptoteValue = 0.85f;
        wc.forwardFriction = fwd;

        var side = wc.sidewaysFriction;
        side.stiffness = sideStiff;
        side.extremumSlip = 0.30f; side.extremumValue = 1f;
        side.asymptoteSlip = 0.6f; side.asymptoteValue = 0.85f;
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
        currentSpeedKmh = rb.linearVelocity.magnitude * 3.6f;

        if (!isRacing || waypoints == null || waypoints.Length == 0)
        {
            ApplyBrakes(maxBrakeTorque);
            return;
        }

        // 1. Check Unstuck routine
        if (isUnstucking)
        {
            if (Time.time < unstuckEndTime)
            {
                // Reverse and steer opposite
                ApplyDrive(-maxMotorTorque * 0.6f);
                SetSteer(maxSteeringAngle * 0.7f);
                return;
            }
            else
            {
                isUnstucking = false;
                stuckTimer = 0f;
            }
        }

        // Detect if stuck
        if (currentSpeedKmh < 5f && isRacing)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer > 4.5f)
            {
                isUnstucking = true;
                unstuckEndTime = Time.time + 2.0f;
                return;
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        // 2. Handle Pranks (Spin or Brake freeze)
        if (isPranked)
        {
            if (currentPrankType == "banana" || currentPrankType == "spin")
            {
                // Uncontrollable spin
                rb.AddTorque(Vector3.up * 7500f, ForceMode.Force);
                ApplyDrive(maxMotorTorque * 0.2f);
                return;
            }
            else if (currentPrankType == "brake")
            {
                // Emergency freeze brake
                ApplyBrakes(maxBrakeTorque);
                return;
            }
        }

        // 3. Find closest waypoint and target lookahead
        UpdateWaypointProgress();

        Vector3 targetPoint = GetTargetPointWithOffset();

        // 4. Steer towards target point
        Vector3 localTarget = transform.InverseTransformPoint(targetPoint);
        float targetAngle = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;

        // Whiskers Obstacle Avoidance
        float avoidanceSteer = CalculateObstacleAvoidance();
        targetAngle += avoidanceSteer;

        float steer = Mathf.Clamp(targetAngle, -maxSteeringAngle, maxSteeringAngle);
        SetSteer(steer);

        // 5. Corner Braking and Speed Management (with Nitro Boost!)
        float targetSpeed = CalculateTargetSpeed(targetAngle);
        if (isNitroActive)
        {
            targetSpeed += 60f; // Boost target speed up to 255 km/h!
        }
        if (isSlowed)
        {
            targetSpeed *= slowFactor;
        }

        float effectiveMotor = isNitroActive ? (maxMotorTorque * 1.7f) : maxMotorTorque;
        if (isSlowed)
        {
            effectiveMotor *= slowFactor;
        }

        if (currentSpeedKmh > targetSpeed + 5f)
        {
            // Need braking for sharp corner
            float brakeStrength = Mathf.Clamp01((currentSpeedKmh - targetSpeed) / 25f);
            ApplyBrakes(maxBrakeTorque * brakeStrength);
            ApplyDrive(0f);
            CurrentBrake = brakeStrength;
            CurrentThrottle = 0f;
        }
        else
        {
            // Accelerate
            float throttle = Mathf.Clamp01(1f - (currentSpeedKmh / targetSpeed));
            float throttleVal = Mathf.Max(0.35f, throttle);
            ApplyDrive(effectiveMotor * throttleVal);
            ApplyBrakes(0f);
            CurrentBrake = 0f;
            CurrentThrottle = throttleVal;
        }

        // Gentle downforce
        rb.AddForce(-transform.up * Mathf.Min(currentSpeedKmh * 35f, 6000f));
    }

    private void UpdateWaypointProgress()
    {
        int totalWp = waypoints.Length;
        float minD = float.MaxValue;
        int closest = currentWaypointIndex;

        // Search in local window around current index
        for (int i = -3; i <= 6; i++)
        {
            int idx = (currentWaypointIndex + i + totalWp) % totalWp;
            float d = Vector3.Distance(transform.position, waypoints[idx]);
            if (d < minD)
            {
                minD = d;
                closest = idx;
            }
        }
        currentWaypointIndex = closest;
    }

    private Vector3 GetTargetPointWithOffset()
    {
        int totalWp = waypoints.Length;
        float accumulated = 0f;
        int targetIdx = currentWaypointIndex;

        while (accumulated < lookaheadDistance)
        {
            int nextIdx = (targetIdx + 1) % totalWp;
            accumulated += Vector3.Distance(waypoints[targetIdx], waypoints[nextIdx]);
            targetIdx = nextIdx;
        }

        Vector3 wp = waypoints[targetIdx];
        int prevIdx = (targetIdx - 1 + totalWp) % totalWp;
        Vector3 fwd = (wp - waypoints[prevIdx]).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

        return wp + right * laneOffset;
    }

    private float CalculateTargetSpeed(float currentTurnAngle)
    {
        float absTurn = Mathf.Abs(currentTurnAngle);
        if (absTurn > 18f)
        {
            return cornerSpeed;
        }
        else if (absTurn > 8f)
        {
            return Mathf.Lerp(topSpeed, cornerSpeed, (absTurn - 8f) / 10f);
        }
        return topSpeed;
    }

    private float CalculateObstacleAvoidance()
    {
        float avoidSteer = 0f;
        Vector3 origin = transform.position + Vector3.up * 0.4f;

        RaycastHit hit;
        // Left whisker
        Vector3 leftDir = Quaternion.Euler(0f, -22f, 0f) * transform.forward;
        if (Physics.Raycast(origin, leftDir, out hit, 16f))
        {
            if (hit.collider.transform.root != transform)
            {
                avoidSteer += (1f - (hit.distance / 16f)) * 30f;
            }
        }

        // Right whisker
        Vector3 rightDir = Quaternion.Euler(0f, 22f, 0f) * transform.forward;
        if (Physics.Raycast(origin, rightDir, out hit, 16f))
        {
            if (hit.collider.transform.root != transform)
            {
                avoidSteer -= (1f - (hit.distance / 16f)) * 30f;
            }
        }

        return avoidSteer;
    }

    private void SetSteer(float angle)
    {
        if (frontLeftWheel != null) frontLeftWheel.steerAngle = angle;
        if (frontRightWheel != null) frontRightWheel.steerAngle = angle;
    }

    private void ApplyDrive(float torque)
    {
        if (rearLeftWheel != null) rearLeftWheel.motorTorque = torque * 0.5f;
        if (rearRightWheel != null) rearRightWheel.motorTorque = torque * 0.5f;
        if (frontLeftWheel != null) frontLeftWheel.motorTorque = torque * 0.5f;
        if (frontRightWheel != null) frontRightWheel.motorTorque = torque * 0.5f;
    }

    private void ApplyBrakes(float torque)
    {
        if (frontLeftWheel != null) frontLeftWheel.brakeTorque = torque * 0.6f;
        if (frontRightWheel != null) frontRightWheel.brakeTorque = torque * 0.6f;
        if (rearLeftWheel != null) rearLeftWheel.brakeTorque = torque * 0.4f;
        if (rearRightWheel != null) rearRightWheel.brakeTorque = torque * 0.4f;
    }

    private void UpdateWheelMesh(WheelCollider col, Transform mesh)
    {
        if (col == null || mesh == null) return;
        col.GetWorldPose(out Vector3 pos, out Quaternion rot);
        mesh.position = pos;
        mesh.rotation = rot;
    }

    public void ApplyNitro(float duration = 4.5f)
    {
        StopCoroutine("NitroCoroutine");
        StartCoroutine(NitroCoroutine(duration));
    }

    private IEnumerator NitroCoroutine(float duration)
    {
        isNitroActive = true;
        if (overheadUI != null) overheadUI.ShowNitro(true);
        yield return new WaitForSeconds(duration);
        isNitroActive = false;
        if (overheadUI != null) overheadUI.ShowNitro(false);
    }

    public void ApplyPrank(string type = "banana", float duration = 2.2f)
    {
        StopCoroutine("PrankCoroutine");
        StartCoroutine(PrankCoroutine(type, duration));
    }

    private IEnumerator PrankCoroutine(string type, float duration)
    {
        isPranked = true;
        currentPrankType = type;
        if (overheadUI != null) overheadUI.ShowPrank(type, true);
        yield return new WaitForSeconds(duration);
        isPranked = false;
        if (overheadUI != null) overheadUI.ShowPrank(type, false);
    }

    [HideInInspector] public bool isSlowed = false;
    private float slowFactor = 1.0f;

    public void ApplySlow(float multiplier = 0.40f, float duration = 5.0f)
    {
        StopCoroutine("SlowCoroutine");
        StartCoroutine(SlowCoroutine(multiplier, duration));
    }

    private IEnumerator SlowCoroutine(float multiplier, float duration)
    {
        isSlowed = true;
        slowFactor = Mathf.Clamp(multiplier, 0.2f, 0.85f);
        if (overheadUI != null) overheadUI.ShowPrank("slow", true);
        yield return new WaitForSeconds(duration);
        isSlowed = false;
        slowFactor = 1.0f;
        if (overheadUI != null) overheadUI.ShowPrank("slow", false);
    }

    public void ApplyExplosion(Vector3 blastOrigin, float blastForce = 13500f, float spinTorque = 10000f)
    {
        if (rb != null)
        {
            Vector3 blastDir = (transform.position - blastOrigin).normalized;
            blastDir.y = Mathf.Max(0.6f, blastDir.y); // upward kick
            rb.AddForce(blastDir * blastForce + Vector3.up * (blastForce * 0.4f), ForceMode.Impulse);

            float randomSpin = (Random.value > 0.5f ? 1f : -1f) * spinTorque;
            rb.AddTorque(Vector3.up * randomSpin + transform.right * (spinTorque * 0.35f), ForceMode.Impulse);
        }

        // Stun for 2.4 seconds
        ApplyPrank("explosion", 2.4f);
    }
}
