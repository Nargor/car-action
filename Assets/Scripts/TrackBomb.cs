using System.Collections;
using UnityEngine;

public class TrackBomb : MonoBehaviour
{
    [Header("Bomb Settings")]
    public Transform ownerCar;
    public float blastRadius = 5.0f;
    public float blastForce = 15000f;
    public float spinTorque = 12000f;
    public float lifetime = 40f;

    private bool hasExploded = false;
    private float spawnTime;
    private Light pulseLight;
    private Material ledMat;

    public static GameObject CreateBomb(Vector3 position, Quaternion rotation, Transform owner)
    {
        GameObject bombObj = new GameObject("TrackBomb_Mine");
        bombObj.transform.position = position;
        bombObj.transform.rotation = rotation;

        var bomb = bombObj.AddComponent<TrackBomb>();
        bomb.ownerCar = owner;
        bomb.BuildProceduralVisuals();

        return bombObj;
    }

    void Start()
    {
        spawnTime = Time.time;
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (hasExploded) return;

        // 1. Pulse red warning beacon
        float pulse = Mathf.PingPong((Time.time - spawnTime) * 5f, 1f);
        if (pulseLight != null)
        {
            pulseLight.intensity = Mathf.Lerp(2.0f, 8.0f, pulse);
        }
        if (ledMat != null)
        {
            Color c = Color.Lerp(new Color(0.4f, 0f, 0f), Color.red, pulse);
            ledMat.color = c;
            ledMat.SetColor("_EmissionColor", c * 3f);
        }

        // 2. Proximity Detection: check if any car runs over the mine!
        if (Time.time - spawnTime > 0.4f)
        {
            if (RaceManager.Instance != null && RaceManager.Instance.allRacers != null)
            {
                for (int i = 0; i < RaceManager.Instance.allRacers.Count; i++)
                {
                    var r = RaceManager.Instance.allRacers[i];
                    if (r.transform == null) continue;
                    if (r.transform == ownerCar && Time.time - spawnTime < 1.8f) continue;

                    Vector3 diff = r.transform.position - transform.position;
                    diff.y *= 0.5f; // Flatten vertical to easily detect chassis and wheels
                    if (diff.sqrMagnitude <= 3.2f * 3.2f)
                    {
                        Detonate(r.transform);
                        break;
                    }
                }
            }
        }
    }

    private void BuildProceduralVisuals()
    {
        // 1. Heavy Metal Base Disc (Flat on track ground)
        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "DiscBase";
        disc.transform.SetParent(transform, false);
        disc.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        disc.transform.localScale = new Vector3(1.6f, 0.08f, 1.6f);

        var discCol = disc.GetComponent<Collider>();
        if (discCol != null) Destroy(discCol);

        var discRend = disc.GetComponent<Renderer>();
        if (discRend != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(0.12f, 0.13f, 0.16f);
            mat.SetFloat("_Metallic", 0.90f);
            mat.SetFloat("_Smoothness", 0.6f);
            discRend.material = mat;
        }

        // 2. Glowing Red LED Dome
        GameObject dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dome.name = "LedDome";
        dome.transform.SetParent(transform, false);
        dome.transform.localPosition = new Vector3(0f, 0.16f, 0f);
        dome.transform.localScale = new Vector3(0.65f, 0.35f, 0.65f);

        var domeCol = dome.GetComponent<Collider>();
        if (domeCol != null) Destroy(domeCol);

        var domeRend = dome.GetComponent<Renderer>();
        if (domeRend != null)
        {
            ledMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            ledMat.color = Color.red;
            ledMat.EnableKeyword("_EMISSION");
            ledMat.SetColor("_EmissionColor", Color.red * 3f);
            domeRend.material = ledMat;
        }

        // 3. Warning Point Light
        GameObject lightObj = new GameObject("WarningLight");
        lightObj.transform.SetParent(transform, false);
        lightObj.transform.localPosition = new Vector3(0f, 0.25f, 0f);
        pulseLight = lightObj.AddComponent<Light>();
        pulseLight.type = LightType.Point;
        pulseLight.color = Color.red;
        pulseLight.range = 8.0f;
        pulseLight.intensity = 4.0f;

        // 4. Trigger Sphere Collider (covers ground up to car roof)
        var trig = gameObject.AddComponent<SphereCollider>();
        trig.isTrigger = true;
        trig.radius = 2.4f;
        trig.center = new Vector3(0f, 0.8f, 0f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasExploded) return;

        // Ignore owner within first 1.8 seconds
        if (Time.time - spawnTime < 1.8f && other.transform.root == ownerCar)
            return;

        // Check if car component or rigidbody hit
        var ai = other.GetComponentInParent<AICarController>();
        var player = other.GetComponentInParent<CarController>();
        var rb = other.GetComponentInParent<Rigidbody>();

        if (ai != null || player != null || rb != null)
        {
            Detonate(other.transform.root);
        }
    }

    public void Detonate(Transform victimRoot)
    {
        if (hasExploded) return;
        hasExploded = true;

        Vector3 pos = transform.position;
        Debug.Log($"[TrackBomb] 💥 DETONATED at {pos}! Direct Hit on: {(victimRoot != null ? victimRoot.name : "Car")}");

        // 1. Play Full Explosion VFX & Sound
        SpawnExplosionFX(pos);

        // 2. Blast direct victim
        if (victimRoot != null)
        {
            var ai = victimRoot.GetComponent<AICarController>();
            if (ai != null)
            {
                ai.ApplyExplosion(pos, blastForce, spinTorque);
                if (ai.overheadUI != null) ai.overheadUI.ShowPrank("BOOM!", true);
            }

            var pCar = victimRoot.GetComponent<CarController>();
            if (pCar != null)
            {
                pCar.ApplyExplosion(pos, blastForce, spinTorque);
                var pOverhead = pCar.GetComponentInChildren<RacerOverheadUI>();
                if (pOverhead != null) pOverhead.ShowPrank("BOOM!", true);
            }
        }

        // 3. Blast any nearby cars within blastRadius
        if (RaceManager.Instance != null && RaceManager.Instance.allRacers != null)
        {
            foreach (var r in RaceManager.Instance.allRacers)
            {
                if (r.transform == null || r.transform == victimRoot) continue;
                float d = Vector3.Distance(pos, r.transform.position);
                if (d <= blastRadius)
                {
                    float ratio = 1f - (d / blastRadius);
                    var aiOther = r.transform.GetComponent<AICarController>();
                    if (aiOther != null)
                    {
                        aiOther.ApplyExplosion(pos, blastForce * ratio, spinTorque * ratio);
                    }
                    var pOther = r.transform.GetComponent<CarController>();
                    if (pOther != null)
                    {
                        pOther.ApplyExplosion(pos, blastForce * ratio, spinTorque * ratio);
                    }
                }
            }
        }

        Destroy(gameObject);
    }

    public static void SpawnExplosionFX(Vector3 pos)
    {
        GameObject fx = new GameObject("Explosion_FX");
        fx.transform.position = pos;

        // 1. Audio Source (Loud Crash / Explosion)
        var aSrc = fx.AddComponent<AudioSource>();
        var clip = Resources.Load<AudioClip>("Audio/CarCrash_Real");
        if (clip != null)
        {
            aSrc.clip = clip;
            aSrc.spatialBlend = 0.4f;
            aSrc.volume = 1.3f;
            aSrc.pitch = Random.Range(0.85f, 1.05f);
            aSrc.Play();
        }

        // 2. Explosion Light Flash
        var flash = fx.AddComponent<Light>();
        flash.type = LightType.Point;
        flash.color = new Color(1f, 0.70f, 0.25f);
        flash.range = 30f;
        flash.intensity = 25f;

        // 3. Expanding Fireball Sphere
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Sphere";
        sphere.transform.SetParent(fx.transform, false);
        sphere.transform.localPosition = Vector3.up * 0.5f;
        sphere.transform.localScale = Vector3.one * 1.5f;
        Destroy(sphere.GetComponent<Collider>());
        var sRend = sphere.GetComponent<Renderer>();
        if (sRend != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(1f, 0.45f, 0.05f, 0.9f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(1f, 0.4f, 0.05f) * 6f);
            sRend.material = mat;
        }

        // 4. Ground Shockwave Disc
        GameObject wave = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        wave.name = "Shockwave";
        wave.transform.SetParent(fx.transform, false);
        wave.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        wave.transform.localScale = new Vector3(1f, 0.02f, 1f);
        Destroy(wave.GetComponent<Collider>());
        var wRend = wave.GetComponent<Renderer>();
        if (wRend != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(1f, 0.6f, 0.1f, 0.7f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(1f, 0.5f, 0.1f) * 4f);
            wRend.material = mat;
        }

        // 5. Procedural Spark Particle System
        var ps = fx.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 1.0f;
        main.loop = false;
        main.startLifetime = 0.7f;
        main.startSpeed = 18f;
        main.startSize = 0.35f;
        main.startColor = new Color(1f, 0.75f, 0.2f);
        main.gravityModifier = 1.5f;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 45) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.6f;

        var psRend = fx.GetComponent<ParticleSystemRenderer>();
        if (psRend != null)
        {
            var pMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            pMat.color = Color.yellow;
            pMat.EnableKeyword("_EMISSION");
            pMat.SetColor("_EmissionColor", Color.yellow * 3f);
            psRend.material = pMat;
        }

        ps.Play();
        fx.AddComponent<SelfDestructExplosion>();
    }
}

public class SelfDestructExplosion : MonoBehaviour
{
    private float timer = 0f;
    private Light lgt;
    private Transform sphere;
    private Transform wave;

    void Start()
    {
        lgt = GetComponent<Light>();
        sphere = transform.Find("Sphere");
        wave = transform.Find("Shockwave");
        Destroy(gameObject, 1.6f);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (lgt != null)
        {
            lgt.intensity = Mathf.Lerp(25f, 0f, timer / 0.5f);
        }
        if (sphere != null)
        {
            sphere.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.one * 7.5f, timer / 0.45f);
            if (timer > 0.45f) sphere.gameObject.SetActive(false);
        }
        if (wave != null)
        {
            float wScale = Mathf.Lerp(1f, 10f, timer / 0.4f);
            wave.localScale = new Vector3(wScale, 0.02f, wScale);
            if (timer > 0.4f) wave.gameObject.SetActive(false);
        }
    }
}
