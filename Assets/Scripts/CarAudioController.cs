using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarAudioController : MonoBehaviour
{
    [Header("Audio Sources")]
    [Tooltip("Idle engine audio source (low RPM burble)")]
    public AudioSource idleSource;
    [Tooltip("Running engine audio source (high RPM race roar)")]
    public AudioSource runningSource;
    [Tooltip("Tire skid audio source (real tire screech)")]
    public AudioSource skidSource;
    [Tooltip("Crash audio source (real metallic collision)")]
    public AudioSource crashSource;

    [Header("Real Audio Clips")]
    public AudioClip engineIdleClip;
    public AudioClip engineHighClip;
    public AudioClip skidClip;
    public AudioClip crashClip;

    [Header("Engine Audio Tuning")]
    public float idleVolume = 0.70f;
    public float maxRunningVolume = 0.88f;

    [Header("Skid Audio Tuning")]
    public float skidMinSpeed = 22f;          // km/h - No squeal at parking/low speeds
    public float skidLateralThreshold = 6.5f; // m/s - High threshold so normal turns are 100% silent!
    public float skidBrakeThreshold = 0.40f;  // Heavy braking required
    public float maxSkidVolume = 0.80f;

    [Header("Crash Audio Tuning")]
    public float crashMinVelocity = 3.5f;  // m/s
    public float crashMaxVelocity = 24.0f; // m/s

    // Static cached references from Resources
    private static AudioClip s_ResIdle;
    private static AudioClip s_ResHigh;
    private static AudioClip s_ResSkid;
    private static AudioClip s_ResCrash;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticClips()
    {
        s_ResIdle = null;
        s_ResHigh = null;
        s_ResSkid = null;
        s_ResCrash = null;
    }

    private Rigidbody rb;
    private CarController playerCar;
    private AICarController aiCar;
    private float lastCrashTime = -1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCar = GetComponent<CarController>();
        aiCar = GetComponent<AICarController>();

        LoadClips();
        SetupAudioSources();
    }

    private void LoadClips()
    {
        if (s_ResIdle == null)  s_ResIdle  = Resources.Load<AudioClip>("Audio/Engine_Idle");
        if (s_ResHigh == null)  s_ResHigh  = Resources.Load<AudioClip>("Audio/Engine_High");
        if (s_ResSkid == null)  s_ResSkid  = Resources.Load<AudioClip>("Audio/CarSkid_Real");
        if (s_ResCrash == null) s_ResCrash = Resources.Load<AudioClip>("Audio/CarCrash_Real");

        if (engineIdleClip == null) engineIdleClip = s_ResIdle;
        if (engineHighClip == null) engineHighClip = s_ResHigh;
        if (skidClip == null)       skidClip       = s_ResSkid;
        if (crashClip == null)      crashClip      = s_ResCrash;
    }

    private void SetupAudioSources()
    {
        // 1. Idle Source (Low RPM deep burble)
        if (idleSource == null)
        {
            idleSource = gameObject.AddComponent<AudioSource>();
        }
        idleSource.playOnAwake = false;
        idleSource.loop = true;
        idleSource.clip = engineIdleClip;
        idleSource.volume = idleVolume;
        idleSource.pitch = 1.0f;
        idleSource.spatialBlend = (playerCar != null) ? 0f : 1f;
        idleSource.minDistance = 4f;
        idleSource.maxDistance = 50f;
        idleSource.rolloffMode = AudioRolloffMode.Logarithmic;

        // 2. Running Source (High RPM screaming race roar)
        if (runningSource == null)
        {
            runningSource = gameObject.AddComponent<AudioSource>();
        }
        runningSource.playOnAwake = false;
        runningSource.loop = true;
        runningSource.clip = engineHighClip;
        runningSource.volume = 0f;
        runningSource.pitch = 0.85f;
        runningSource.spatialBlend = (playerCar != null) ? 0f : 1f;
        runningSource.minDistance = 5f;
        runningSource.maxDistance = 60f;
        runningSource.rolloffMode = AudioRolloffMode.Logarithmic;
        runningSource.dopplerLevel = 0.8f;

        // 3. Skid Source (Real tire screech)
        if (skidSource == null)
        {
            skidSource = gameObject.AddComponent<AudioSource>();
        }
        skidSource.playOnAwake = false;
        skidSource.loop = true;
        skidSource.clip = skidClip;
        skidSource.volume = 0f;
        skidSource.pitch = 1.0f;
        skidSource.spatialBlend = (playerCar != null) ? 0f : 1f;
        skidSource.minDistance = 4f;
        skidSource.maxDistance = 50f;
        skidSource.rolloffMode = AudioRolloffMode.Logarithmic;

        // 4. Crash Source (Real metal collision)
        if (crashSource == null)
        {
            crashSource = gameObject.AddComponent<AudioSource>();
        }
        crashSource.playOnAwake = false;
        crashSource.loop = false;
        crashSource.volume = 1f;
        crashSource.spatialBlend = (playerCar != null) ? 0f : 1f;
        crashSource.minDistance = 6f;
        crashSource.maxDistance = 80f;
        crashSource.rolloffMode = AudioRolloffMode.Logarithmic;
    }

    void OnEnable()
    {
        if (idleSource != null && idleSource.clip != null && !idleSource.isPlaying) idleSource.Play();
        if (runningSource != null && runningSource.clip != null && !runningSource.isPlaying) runningSource.Play();
    }

    void OnDisable()
    {
        if (idleSource != null && idleSource.isPlaying) idleSource.Stop();
        if (runningSource != null && runningSource.isPlaying) runningSource.Stop();
        if (skidSource != null && skidSource.isPlaying) skidSource.Stop();
    }

    void Start()
    {
        if (idleSource != null && idleSource.clip != null && !idleSource.isPlaying) idleSource.Play();
        if (runningSource != null && runningSource.clip != null && !runningSource.isPlaying) runningSource.Play();
    }

    void Update()
    {
        if (playerCar == null && aiCar == null)
        {
            playerCar = GetComponent<CarController>();
            aiCar = GetComponent<AICarController>();
        }

        float speedKmh = 0f;
        float throttle = 0f;
        float brake = 0f;
        bool isHandbraking = false;
        bool isSpinning = false;

        if (playerCar != null)
        {
            speedKmh = playerCar.SpeedKmh;
            throttle = playerCar.CurrentThrottle;
            brake = playerCar.CurrentBrake;
            isHandbraking = playerCar.IsHandbraking;
        }
        else if (aiCar != null)
        {
            speedKmh = aiCar.SpeedKmh;
            throttle = aiCar.CurrentThrottle;
            brake = aiCar.CurrentBrake;
            isSpinning = aiCar.isPranked;
        }
        else if (rb != null)
        {
            speedKmh = rb.linearVelocity.magnitude * 3.6f;
        }

        // 1. Camera focus determines 2D stereo vs 3D spatial sound
        bool isFocused = (playerCar != null && (ChaseCameraController.Instance == null || ChaseCameraController.Instance.target == transform || ChaseCameraController.Instance.target == null))
                         || (ChaseCameraController.Instance != null && ChaseCameraController.Instance.target == transform);

        float targetSpatial = isFocused ? 0.0f : 1.0f;
        if (idleSource != null)    idleSource.spatialBlend    = Mathf.MoveTowards(idleSource.spatialBlend, targetSpatial, Time.deltaTime * 4f);
        if (runningSource != null) runningSource.spatialBlend = Mathf.MoveTowards(runningSource.spatialBlend, targetSpatial, Time.deltaTime * 4f);
        if (skidSource != null)    skidSource.spatialBlend    = Mathf.MoveTowards(skidSource.spatialBlend, targetSpatial, Time.deltaTime * 4f);
        if (crashSource != null)   crashSource.spatialBlend   = Mathf.MoveTowards(crashSource.spatialBlend, targetSpatial, Time.deltaTime * 4f);

        // 2. Engine Audio Crossfade (Idle vs High-RPM roar)
        UpdateEngineAudio(speedKmh, throttle);

        // 3. Skid Audio (Real tire screech on heavy drift/brake only)
        UpdateSkidAudio(speedKmh, brake, isHandbraking, isSpinning);
    }

    private void UpdateEngineAudio(float speedKmh, float throttle)
    {
        if (idleSource == null || runningSource == null) return;

        // Running engine pitch across 5 gears
        float gearProgress;
        float gearBasePitch;
        float gearMaxPitch;

        if (speedKmh < 45f)
        {
            gearProgress = Mathf.Clamp01(speedKmh / 45f);
            gearBasePitch = 0.75f;
            gearMaxPitch = 1.35f;
        }
        else if (speedKmh < 90f)
        {
            gearProgress = Mathf.Clamp01((speedKmh - 45f) / 45f);
            gearBasePitch = 0.85f;
            gearMaxPitch = 1.40f;
        }
        else if (speedKmh < 135f)
        {
            gearProgress = Mathf.Clamp01((speedKmh - 90f) / 45f);
            gearBasePitch = 0.90f;
            gearMaxPitch = 1.45f;
        }
        else if (speedKmh < 180f)
        {
            gearProgress = Mathf.Clamp01((speedKmh - 135f) / 45f);
            gearBasePitch = 0.95f;
            gearMaxPitch = 1.50f;
        }
        else
        {
            gearProgress = Mathf.Clamp01((speedKmh - 180f) / 55f);
            gearBasePitch = 1.00f;
            gearMaxPitch = 1.60f;
        }

        float targetRunningPitch = Mathf.Lerp(gearBasePitch, gearMaxPitch, gearProgress);
        runningSource.pitch = Mathf.MoveTowards(runningSource.pitch, targetRunningPitch, Time.deltaTime * 2.5f);

        // Crossfade: at low speed, Idle dominates; at high speed/throttle, Running dominates
        float speedRatio = Mathf.Clamp01(speedKmh / 50f);
        float runWeight = Mathf.Max(speedRatio, throttle);

        float targetIdleVol = Mathf.Lerp(idleVolume, 0.12f, runWeight);
        float targetRunVol  = Mathf.Lerp(0.04f, maxRunningVolume, runWeight);

        idleSource.volume = Mathf.MoveTowards(idleSource.volume, targetIdleVol, Time.deltaTime * 3.5f);
        runningSource.volume = Mathf.MoveTowards(runningSource.volume, targetRunVol, Time.deltaTime * 4.0f);
    }

    private void UpdateSkidAudio(float speedKmh, float brake, bool isHandbraking, bool isSpinning)
    {
        if (skidSource == null) return;

        float lateralSpeed = 0f;
        if (rb != null)
        {
            lateralSpeed = Mathf.Abs(Vector3.Dot(rb.linearVelocity, transform.right));
        }

        bool isSkidding = false;
        float skidIntensity = 0f;

        // ONLY trigger on genuine aggressive skidding (handbrake, hard brake, or high-speed sideways slide)
        if (speedKmh > skidMinSpeed)
        {
            if (isHandbraking || isSpinning)
            {
                isSkidding = true;
                skidIntensity = 0.90f;
            }
            else if (brake > skidBrakeThreshold)
            {
                isSkidding = true;
                skidIntensity = Mathf.Clamp01((brake - skidBrakeThreshold) / (1f - skidBrakeThreshold));
            }
            else if (lateralSpeed > skidLateralThreshold && speedKmh > 30f)
            {
                isSkidding = true;
                skidIntensity = Mathf.Clamp01((lateralSpeed - skidLateralThreshold) / 6.0f);
            }
        }

        if (isSkidding)
        {
            if (!skidSource.isPlaying && skidClip != null)
            {
                skidSource.Play();
            }
            float targetVol = skidIntensity * maxSkidVolume;
            skidSource.volume = Mathf.MoveTowards(skidSource.volume, targetVol, Time.deltaTime * 12.0f);
            skidSource.pitch = Mathf.Lerp(0.95f, 1.15f, speedKmh / 150f);
        }
        else
        {
            // Rapidly fade out to absolute silence
            skidSource.volume = Mathf.MoveTowards(skidSource.volume, 0f, Time.deltaTime * 8.0f);
            if (skidSource.volume <= 0.01f && skidSource.isPlaying)
            {
                skidSource.Stop();
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed < crashMinVelocity) return;

        if (Time.time - lastCrashTime < 0.20f) return;
        lastCrashTime = Time.time;

        if (crashSource != null && crashClip != null)
        {
            float impactRatio = Mathf.Clamp01((impactSpeed - crashMinVelocity) / (crashMaxVelocity - crashMinVelocity));
            float volume = Mathf.Lerp(0.40f, 1.0f, impactRatio);
            crashSource.pitch = Random.Range(0.88f, 1.12f);
            crashSource.PlayOneShot(crashClip, volume);
        }
    }
}
