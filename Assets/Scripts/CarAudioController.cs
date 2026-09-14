using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarAudioController : MonoBehaviour
{
    [Header("Audio Sources")]
    [Tooltip("Engine audio source (procedural looping RPM)")]
    public AudioSource engineSource;
    [Tooltip("Tire skid audio source (procedural tire squeal)")]
    public AudioSource skidSource;
    [Tooltip("Crash audio source (procedural metal/impact sound)")]
    public AudioSource crashSource;

    [Header("Engine Audio Tuning")]
    public float idlePitch = 0.72f;
    public float maxPitch = 2.15f;
    public float idleVolume = 0.40f;
    public float maxVolume = 0.88f;

    [Header("Skid Audio Tuning")]
    public float skidMinSpeed = 12f;       // km/h
    public float skidLateralThreshold = 3.2f; // m/s
    public float maxSkidVolume = 0.82f;

    [Header("Crash Audio Tuning")]
    public float crashMinVelocity = 2.8f;  // m/s
    public float crashMaxVelocity = 22.0f; // m/s

    // Cached procedural clips shared by all cars
    private static AudioClip s_EngineClip;
    private static AudioClip s_SkidClip;
    private static AudioClip s_CrashClip;
    private static readonly object s_Lock = new object();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticAudioClips()
    {
        s_EngineClip = null;
        s_SkidClip = null;
        s_CrashClip = null;
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

        EnsureClips();
        SetupAudioSources();
    }

    private void SetupAudioSources()
    {
        // 1. Engine AudioSource
        if (engineSource == null)
        {
            engineSource = gameObject.AddComponent<AudioSource>();
        }
        engineSource.playOnAwake = false;
        engineSource.loop = true;
        engineSource.clip = s_EngineClip;
        engineSource.volume = idleVolume;
        engineSource.pitch = idlePitch;
        engineSource.spatialBlend = (playerCar != null) ? 0f : 1f;
        engineSource.minDistance = 4f;
        engineSource.maxDistance = 55f;
        engineSource.rolloffMode = AudioRolloffMode.Logarithmic;
        engineSource.dopplerLevel = 0.8f;

        // 2. Skid AudioSource
        if (skidSource == null)
        {
            skidSource = gameObject.AddComponent<AudioSource>();
        }
        skidSource.playOnAwake = false;
        skidSource.loop = true;
        skidSource.clip = s_SkidClip;
        skidSource.volume = 0f;
        skidSource.pitch = 1.0f;
        skidSource.spatialBlend = (playerCar != null) ? 0f : 1f;
        skidSource.minDistance = 4f;
        skidSource.maxDistance = 50f;
        skidSource.rolloffMode = AudioRolloffMode.Logarithmic;

        // 3. Crash AudioSource
        if (crashSource == null)
        {
            crashSource = gameObject.AddComponent<AudioSource>();
        }
        crashSource.playOnAwake = false;
        crashSource.loop = false;
        crashSource.volume = 1f;
        crashSource.spatialBlend = (playerCar != null) ? 0f : 1f;
        crashSource.minDistance = 5f;
        crashSource.maxDistance = 75f;
        crashSource.rolloffMode = AudioRolloffMode.Logarithmic;
    }

    void OnEnable()
    {
        if (engineSource != null && engineSource.clip != null && !engineSource.isPlaying)
        {
            engineSource.Play();
        }
        if (skidSource != null && skidSource.clip != null && !skidSource.isPlaying)
        {
            skidSource.Play();
        }
    }

    void OnDisable()
    {
        if (engineSource != null && engineSource.isPlaying) engineSource.Stop();
        if (skidSource != null && skidSource.isPlaying) skidSource.Stop();
    }

    void Start()
    {
        if (engineSource != null && engineSource.clip != null && !engineSource.isPlaying)
        {
            engineSource.Play();
        }
        if (skidSource != null && skidSource.clip != null && !skidSource.isPlaying)
        {
            skidSource.Play();
        }
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

        // 1. Camera focus determines 2D vs 3D spatial sound
        bool isFocused = (playerCar != null && (ChaseCameraController.Instance == null || ChaseCameraController.Instance.target == transform || ChaseCameraController.Instance.target == null))
                         || (ChaseCameraController.Instance != null && ChaseCameraController.Instance.target == transform);

        float targetSpatial = isFocused ? 0.0f : 1.0f;
        if (engineSource != null) engineSource.spatialBlend = Mathf.MoveTowards(engineSource.spatialBlend, targetSpatial, Time.deltaTime * 4f);
        if (skidSource != null)   skidSource.spatialBlend   = Mathf.MoveTowards(skidSource.spatialBlend, targetSpatial, Time.deltaTime * 4f);
        if (crashSource != null)  crashSource.spatialBlend  = Mathf.MoveTowards(crashSource.spatialBlend, targetSpatial, Time.deltaTime * 4f);

        // 2. Engine Pitch & Volume Calculation (Simulated 5-speed gearbox)
        UpdateEngineAudio(speedKmh, throttle);

        // 3. Skid Audio Calculation
        UpdateSkidAudio(speedKmh, brake, isHandbraking, isSpinning);
    }

    private void UpdateEngineAudio(float speedKmh, float throttle)
    {
        if (engineSource == null) return;

        // Simulated gear ratios
        // Gear 1: 0-40, Gear 2: 40-80, Gear 3: 80-125, Gear 4: 125-170, Gear 5: 170-230
        float gearProgress;
        float gearBasePitch;
        float gearMaxPitch;

        if (speedKmh < 40f)
        {
            gearProgress = Mathf.Clamp01(speedKmh / 40f);
            gearBasePitch = idlePitch;
            gearMaxPitch = 1.60f;
        }
        else if (speedKmh < 80f)
        {
            gearProgress = Mathf.Clamp01((speedKmh - 40f) / 40f);
            gearBasePitch = 1.05f;
            gearMaxPitch = 1.75f;
        }
        else if (speedKmh < 125f)
        {
            gearProgress = Mathf.Clamp01((speedKmh - 80f) / 45f);
            gearBasePitch = 1.15f;
            gearMaxPitch = 1.85f;
        }
        else if (speedKmh < 170f)
        {
            gearProgress = Mathf.Clamp01((speedKmh - 125f) / 45f);
            gearBasePitch = 1.20f;
            gearMaxPitch = 1.95f;
        }
        else
        {
            gearProgress = Mathf.Clamp01((speedKmh - 170f) / 60f);
            gearBasePitch = 1.25f;
            gearMaxPitch = maxPitch;
        }

        float targetPitch = Mathf.Lerp(gearBasePitch, gearMaxPitch, gearProgress);
        
        // Slight RPM wobble when flooring throttle
        if (throttle > 0.5f)
        {
            targetPitch += Mathf.Sin(Time.time * 30f) * 0.012f;
        }

        engineSource.pitch = Mathf.MoveTowards(engineSource.pitch, targetPitch, Time.deltaTime * 2.2f);

        // Volume: rises with throttle and speed
        float targetVol = Mathf.Lerp(idleVolume, maxVolume, Mathf.Max(throttle * 0.8f, (speedKmh / 220f) * 0.7f));
        engineSource.volume = Mathf.MoveTowards(engineSource.volume, targetVol, Time.deltaTime * 3.0f);
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

        if (speedKmh > skidMinSpeed)
        {
            if (isHandbraking || isSpinning)
            {
                isSkidding = true;
                skidIntensity = 0.95f;
            }
            else if (brake > 0.15f)
            {
                isSkidding = true;
                skidIntensity = Mathf.Clamp01(brake);
            }
            else if (lateralSpeed > skidLateralThreshold)
            {
                isSkidding = true;
                skidIntensity = Mathf.Clamp01((lateralSpeed - skidLateralThreshold) / 6.0f);
            }
        }

        float targetSkidVol = isSkidding ? (skidIntensity * maxSkidVolume) : 0f;
        float changeRate = (targetSkidVol > skidSource.volume) ? 12.0f : 4.5f; // Fast attack, smooth decay
        skidSource.volume = Mathf.MoveTowards(skidSource.volume, targetSkidVol, Time.deltaTime * changeRate);

        if (isSkidding)
        {
            skidSource.pitch = Mathf.Lerp(0.95f, 1.25f, speedKmh / 160f);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed < crashMinVelocity) return;

        // Debounce repeated micro-hits
        if (Time.time - lastCrashTime < 0.15f) return;
        lastCrashTime = Time.time;

        if (crashSource != null && s_CrashClip != null)
        {
            float impactRatio = Mathf.Clamp01((impactSpeed - crashMinVelocity) / (crashMaxVelocity - crashMinVelocity));
            float volume = Mathf.Lerp(0.35f, 1.0f, impactRatio);
            crashSource.pitch = Random.Range(0.85f, 1.15f);
            crashSource.PlayOneShot(s_CrashClip, volume);
        }
    }

    // ==========================================
    // PROCEDURAL AUDIO SYNTHESIZERS
    // ==========================================
    private static void EnsureClips()
    {
        if (s_EngineClip != null && s_SkidClip != null && s_CrashClip != null)
            return;

        lock (s_Lock)
        {
            int sampleRate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000;

            // 1. Engine Procedural Clip (1.0s seamless loop)
            if (s_EngineClip == null)
            {
                int samplesCount = sampleRate;
                float[] data = new float[samplesCount];
                float baseFreq = 52.0f; // ~1560 RPM base tone

                for (int i = 0; i < samplesCount; i++)
                {
                    float t = (float)i / sampleRate;
                    float phase = (t * baseFreq) % 1.0f;
                    float s = (phase - 0.5f) * 2.0f; // Fundamental sawtooth

                    // Rich engine harmonics
                    s += 0.45f * Mathf.Sin(2f * Mathf.PI * baseFreq * 2f * t);
                    s += 0.30f * Mathf.Sin(2f * Mathf.PI * baseFreq * 3f * t);
                    s += 0.20f * Mathf.Sin(2f * Mathf.PI * baseFreq * 4f * t);
                    s += 0.15f * Mathf.Sin(2f * Mathf.PI * baseFreq * 6f * t);

                    // Cylinder combustion pulse
                    float firingPop = Mathf.Sin(2f * Mathf.PI * baseFreq * 0.5f * t);
                    s *= (0.85f + 0.15f * firingPop);

                    // Soft saturation
                    s = Mathf.Clamp(s * 0.5f, -0.9f, 0.9f);

                    // Seamless crossfade loop
                    if (i < 250) s *= (float)i / 250f;
                    else if (i > samplesCount - 250) s *= (float)(samplesCount - i) / 250f;

                    data[i] = s;
                }

                s_EngineClip = AudioClip.Create("Procedural_Engine_Loop", samplesCount, 1, sampleRate, false);
                s_EngineClip.SetData(data, 0);
            }

            // 2. Skid Squeal Procedural Clip (1.0s seamless loop)
            if (s_SkidClip == null)
            {
                int samplesCount = sampleRate;
                float[] data = new float[samplesCount];
                System.Random rnd = new System.Random(1337);

                for (int i = 0; i < samplesCount; i++)
                {
                    float t = (float)i / sampleRate;
                    float squeal1 = Mathf.Sin(2f * Mathf.PI * (2150f + 120f * Mathf.Sin(2f * Mathf.PI * 18f * t)) * t);
                    float squeal2 = Mathf.Sin(2f * Mathf.PI * (2680f + 80f * Mathf.Cos(2f * Mathf.PI * 23f * t)) * t);
                    float squeal3 = Mathf.Sin(2f * Mathf.PI * 1520f * t);
                    float noise = ((float)rnd.NextDouble() * 2f - 1f);

                    float s = 0.35f * squeal1 + 0.25f * squeal2 + 0.15f * squeal3 + 0.25f * noise;
                    s = Mathf.Clamp(s * 0.6f, -0.9f, 0.9f);

                    if (i < 300) s *= (float)i / 300f;
                    else if (i > samplesCount - 300) s *= (float)(samplesCount - i) / 300f;

                    data[i] = s;
                }

                s_SkidClip = AudioClip.Create("Procedural_Skid_Loop", samplesCount, 1, sampleRate, false);
                s_SkidClip.SetData(data, 0);
            }

            // 3. Crash Metal Procedural Clip (0.65s one-shot)
            if (s_CrashClip == null)
            {
                int samplesCount = (int)(sampleRate * 0.65f);
                float[] data = new float[samplesCount];
                System.Random rnd = new System.Random(2026);

                for (int i = 0; i < samplesCount; i++)
                {
                    float t = (float)i / sampleRate;

                    // Low-frequency impact punch
                    float punch = Mathf.Sin(2f * Mathf.PI * (85f - 35f * t) * t) * Mathf.Exp(-t * 18f);

                    // High-frequency metal crumple and shatter
                    float noise = ((float)rnd.NextDouble() * 2f - 1f);
                    float metalCrunch = (Mathf.Sin(2f * Mathf.PI * 720f * t) * 0.3f +
                                         Mathf.Sin(2f * Mathf.PI * 1450f * t) * 0.3f +
                                         noise * 0.7f) * Mathf.Exp(-t * 9f);

                    // Sheet metal rattle tail
                    float tail = noise * Mathf.Exp(-t * 4.5f) * 0.35f;

                    float s = Mathf.Clamp(punch * 0.65f + metalCrunch * 0.6f + tail * 0.3f, -0.95f, 0.95f);
                    data[i] = s;
                }

                s_CrashClip = AudioClip.Create("Procedural_Crash_Hit", samplesCount, 1, sampleRate, false);
                s_CrashClip.SetData(data, 0);
            }
        }
    }
}
