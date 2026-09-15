using System.Collections;
using UnityEngine;

public class TrackBomb : MonoBehaviour
{
    [Header("Bomb Settings")]
    public Transform ownerCar;
    public float blastRadius = 4.5f;
    public float blastForce = 13500f;
    public float spinTorque = 10000f;
    public float lifetime = 35f;

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

        // Pulse red warning light
        float pulse = Mathf.PingPong((Time.time - spawnTime) * 4f, 1f);
        if (pulseLight != null)
        {
            pulseLight.intensity = Mathf.Lerp(1.5f, 6.0f, pulse);
        }
        if (ledMat != null)
        {
            Color c = Color.Lerp(new Color(0.4f, 0f, 0f), Color.red, pulse);
            ledMat.color = c;
            ledMat.SetColor("_EmissionColor", c * 2.5f);
        }
    }

    private void BuildProceduralVisuals()
    {
        // 1. Base Disc
        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "DiscBase";
        disc.transform.SetParent(transform, false);
        disc.transform.localPosition = new Vector3(0f, 0.12f, 0f);
        disc.transform.localScale = new Vector3(1.4f, 0.12f, 1.4f);

        var discCol = disc.GetComponent<Collider>();
        if (discCol != null) Destroy(discCol);

        var discRend = disc.GetComponent<Renderer>();
        if (discRend != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(0.15f, 0.16f, 0.20f);
            mat.SetFloat("_Metallic", 0.85f);
            mat.SetFloat("_Smoothness", 0.5f);
            discRend.material = mat;
        }

        // 2. Red LED Dome
        GameObject dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dome.name = "LedDome";
        dome.transform.SetParent(transform, false);
        dome.transform.localPosition = new Vector3(0f, 0.22f, 0f);
        dome.transform.localScale = new Vector3(0.55f, 0.4f, 0.55f);

        var domeCol = dome.GetComponent<Collider>();
        if (domeCol != null) Destroy(domeCol);

        var domeRend = dome.GetComponent<Renderer>();
        if (domeRend != null)
        {
            ledMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            ledMat.color = Color.red;
            ledMat.EnableKeyword("_EMISSION");
            ledMat.SetColor("_EmissionColor", Color.red * 2f);
            domeRend.material = ledMat;
        }

        // 3. Proximity Light
        GameObject lightObj = new GameObject("WarningLight");
        lightObj.transform.SetParent(transform, false);
        lightObj.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        pulseLight = lightObj.AddComponent<Light>();
        pulseLight.type = LightType.Point;
        pulseLight.color = Color.red;
        pulseLight.range = 7.0f;
        pulseLight.intensity = 3.5f;

        // 4. Trigger Sphere Collider
        var trig = gameObject.AddComponent<SphereCollider>();
        trig.isTrigger = true;
        trig.radius = 1.6f;
        trig.center = new Vector3(0f, 0.4f, 0f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasExploded) return;

        // Ignore owner within first 1.5 seconds to avoid self-hits
        if (Time.time - spawnTime < 1.5f && other.transform.root == ownerCar)
            return;

        // Check if car
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

        // 1. Play Explosion Sound & VFX
        SpawnExplosionFX(pos);

        // 2. Apply blast to victim
        if (victimRoot != null)
        {
            var ai = victimRoot.GetComponent<AICarController>();
            if (ai != null)
            {
                ai.ApplyExplosion(pos, blastForce, spinTorque);
            }

            var pCar = victimRoot.GetComponent<CarController>();
            if (pCar != null)
            {
                pCar.ApplyExplosion(pos, blastForce, spinTorque);
            }
        }

        Destroy(gameObject);
    }

    public static void SpawnExplosionFX(Vector3 pos)
    {
        // Procedural Flash & Shockwave
        GameObject fx = new GameObject("Explosion_FX");
        fx.transform.position = pos;

        // Audio Source
        var aSrc = fx.AddComponent<AudioSource>();
        var clip = Resources.Load<AudioClip>("Audio/CarCrash_Real");
        if (clip != null)
        {
            aSrc.clip = clip;
            aSrc.spatialBlend = 0.5f;
            aSrc.volume = 1.0f;
            aSrc.pitch = Random.Range(0.85f, 1.05f);
            aSrc.Play();
        }

        // Explosion Light Flash
        var flash = fx.AddComponent<Light>();
        flash.type = LightType.Point;
        flash.color = new Color(1f, 0.65f, 0.2f);
        flash.range = 24f;
        flash.intensity = 15f;

        // Fireball Sphere
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.SetParent(fx.transform, false);
        sphere.transform.localScale = Vector3.one * 3.5f;
        Destroy(sphere.GetComponent<Collider>());
        var sRend = sphere.GetComponent<Renderer>();
        if (sRend != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(1f, 0.5f, 0.1f, 0.8f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(1f, 0.4f, 0.05f) * 4f);
            sRend.material = mat;
        }

        fx.AddComponent<SelfDestructExplosion>();
    }
}

public class SelfDestructExplosion : MonoBehaviour
{
    private float timer = 0f;
    private Light lgt;
    private Transform sphere;

    void Start()
    {
        lgt = GetComponent<Light>();
        sphere = transform.Find("Sphere");
        Destroy(gameObject, 1.5f);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (lgt != null)
        {
            lgt.intensity = Mathf.Lerp(15f, 0f, timer / 0.5f);
        }
        if (sphere != null)
        {
            sphere.localScale = Vector3.Lerp(Vector3.one * 2.5f, Vector3.one * 6.5f, timer / 0.4f);
            if (timer > 0.4f) sphere.gameObject.SetActive(false);
        }
    }
}
