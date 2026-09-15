using System.Collections;
using UnityEngine;

public class HomingMissile : MonoBehaviour
{
    [Header("Missile Specs")]
    public Transform target;
    public Transform ownerCar;
    public float speed = 52.0f;          // m/s
    public float turnSpeed = 160.0f;     // deg/s
    public float blastForce = 15000f;
    public float spinTorque = 12000f;
    public float maxLifetime = 7.0f;

    private float launchTime;
    private bool hasExploded = false;
    private TrailRenderer trail;

    public static GameObject Launch(Vector3 spawnPos, Quaternion spawnRot, Transform owner, Transform targetCar)
    {
        GameObject missileObj = new GameObject("RPG_HomingMissile");
        missileObj.transform.position = spawnPos;
        missileObj.transform.rotation = spawnRot;

        var missile = missileObj.AddComponent<HomingMissile>();
        missile.ownerCar = owner;
        missile.target = targetCar;
        missile.BuildProceduralVisuals();

        return missileObj;
    }

    void Start()
    {
        launchTime = Time.time;
        Destroy(gameObject, maxLifetime);
    }

    void Update()
    {
        if (hasExploded) return;

        // Home towards target
        if (target != null)
        {
            Vector3 targetPos = target.position + Vector3.up * 0.6f;
            Vector3 dirToTarget = (targetPos - transform.position).normalized;
            Quaternion lookRot = Quaternion.LookRotation(dirToTarget);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRot, turnSpeed * Time.deltaTime);

            // Check distance
            if (Vector3.Distance(transform.position, targetPos) < 2.4f)
            {
                Explode(target);
                return;
            }
        }

        // Fly forward
        transform.position += transform.forward * (speed * Time.deltaTime);
    }

    private void BuildProceduralVisuals()
    {
        // 1. Rocket Fuselage
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "Fuselage";
        body.transform.SetParent(transform, false);
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        body.transform.localScale = new Vector3(0.28f, 0.65f, 0.28f);
        Destroy(body.GetComponent<Collider>());

        var bRend = body.GetComponent<Renderer>();
        if (bRend != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(0.22f, 0.55f, 0.35f); // Military green
            mat.SetFloat("_Metallic", 0.7f);
            bRend.material = mat;
        }

        // 2. Nose Cone
        GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        nose.name = "NoseCone";
        nose.transform.SetParent(transform, false);
        nose.transform.localPosition = new Vector3(0f, 0f, 0.65f);
        nose.transform.localScale = new Vector3(0.28f, 0.28f, 0.45f);
        Destroy(nose.GetComponent<Collider>());

        var nRend = nose.GetComponent<Renderer>();
        if (nRend != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = Color.red;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.red * 1.5f);
            nRend.material = mat;
        }

        // 3. Thruster Light & Glow
        GameObject thruster = new GameObject("ThrusterLight");
        thruster.transform.SetParent(transform, false);
        thruster.transform.localPosition = new Vector3(0f, 0f, -0.65f);
        var tLight = thruster.AddComponent<Light>();
        tLight.type = LightType.Point;
        tLight.color = new Color(1f, 0.6f, 0.1f);
        tLight.range = 8f;
        tLight.intensity = 5f;

        // 4. Trail Renderer
        trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.5f;
        trail.startWidth = 0.35f;
        trail.endWidth = 0.05f;
        trail.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default"));
        trail.startColor = new Color(1f, 0.8f, 0.2f, 0.8f);
        trail.endColor = new Color(0.7f, 0.7f, 0.7f, 0f);

        // 5. Trigger Collider
        var col = gameObject.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 1.0f;
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasExploded) return;
        if (Time.time - launchTime < 0.2f && other.transform.root == ownerCar) return;

        var carRoot = other.transform.root;
        if (carRoot != ownerCar && (carRoot.GetComponent<AICarController>() != null || carRoot.GetComponent<CarController>() != null))
        {
            Explode(carRoot);
        }
    }

    private void Explode(Transform victim)
    {
        if (hasExploded) return;
        hasExploded = true;

        Vector3 pos = transform.position;
        TrackBomb.SpawnExplosionFX(pos);

        if (victim != null)
        {
            var ai = victim.GetComponent<AICarController>();
            if (ai != null)
            {
                ai.ApplyExplosion(pos, blastForce, spinTorque);
            }

            var player = victim.GetComponent<CarController>();
            if (player != null)
            {
                player.ApplyExplosion(pos, blastForce, spinTorque);
            }
        }

        Destroy(gameObject);
    }
}
