using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RaceTrack;

public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance { get; private set; }

    public enum GameMode
    {
        PlayerRace,
        Spectator,
        TikTokLive
    }

    [Header("Race Configuration")]
    public GameMode selectedGameMode = GameMode.PlayerRace;
    [Range(1, 10)]
    public int totalLaps = 3;
    [Range(1, 50)]
    public int aiCount = 5;

    [Header("References")]
    public RaceTrackGenerator trackGenerator;
    public CarController playerCar;
    public HUDController hudController;
    public MenuManager menuManager;

    [Header("Race State")]
    public bool isRaceActive = false;
    public bool isCountdownActive = false;
    public int playerPosition = 1;
    public int totalRacers = 1;

    public class RacerInfo
    {
        public string name;
        public bool isPlayer;
        public Transform transform;
        public Color carColor;
        public int currentLap;
        public int currentWaypoint;
        public float totalDistance;
        public int position;
    }

    private List<RacerInfo> allRacers = new List<RacerInfo>();
    private List<GameObject> spawnedAICars = new List<GameObject>();
    private List<Pose> cachedGridPoses = new List<Pose>();

    private readonly Color[] supercarColors = new Color[]
    {
        new Color(0.88f, 0.04f, 0.04f), // Rosso Corsa Red
        new Color(0.08f, 0.35f, 0.95f), // Sapphire Racing Blue
        new Color(0.98f, 0.82f, 0.04f), // Modena Yellow
        new Color(0.98f, 0.42f, 0.02f), // Papaya Orange
        new Color(0.05f, 0.75f, 0.40f), // British Racing Green
        new Color(0.65f, 0.12f, 0.88f), // Ultraviolet Purple
        new Color(0.02f, 0.85f, 0.92f), // Cyan Blue
        new Color(0.85f, 0.85f, 0.88f), // Titanium Silver
        new Color(0.95f, 0.95f, 0.96f), // Pearl White
        new Color(0.12f, 0.12f, 0.14f), // Stealth Black
        new Color(0.85f, 0.65f, 0.45f), // Rose Gold
        new Color(0.08f, 0.15f, 0.35f)  // Midnight Navy
    };

    private readonly string[] aiDriverNames = new string[]
    {
        "Vortex", "Apex", "Phantom", "Thunder", "Blaze", "Shadow", "Falcon",
        "Specter", "Tempest", "Ghost", "Nitro", "Titan", "Turbo", "Bullet",
        "Raptor", "Stratos", "Zephyr", "Onyx", "Venom", "Hyperion", "Cobalt",
        "Inferno", "Drift", "Pulse", "Storm", "Velocity", "Aero", "Ignite",
        "Viper", "Eclipse", "Havoc", "Echo", "Maverick", "Cosmo", "Zenith",
        "Razor", "Strike", "Mirage", "Overdrive", "Alpha", "Blitz", "Tornado",
        "Dynamo", "Nova", "Surge", "Quantum", "Rocket", "Stallion", "Centurion"
    };

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (trackGenerator == null)
            trackGenerator = FindObjectOfType<RaceTrackGenerator>();
        if (playerCar == null)
            playerCar = FindObjectOfType<CarController>();
        if (hudController == null)
            hudController = FindObjectOfType<HUDController>();
        if (menuManager == null)
            menuManager = FindObjectOfType<MenuManager>();
    }

    public List<RacerInfo> GetRacersLeaderboard()
    {
        return allRacers;
    }

    public void SetupAndStartRace(int laps, int numRacers, GameMode mode)
    {
        selectedGameMode = mode;
        totalLaps = laps;
        ClearPreviousRace();
        allRacers.Clear();

        if (mode == GameMode.PlayerRace)
        {
            aiCount = Mathf.Clamp(numRacers, 0, 49);
            totalRacers = 1 + aiCount;

            List<Pose> gridPoses = CalculateGridPositions(totalRacers);

            if (playerCar != null && gridPoses.Count > 0)
            {
                playerCar.gameObject.SetActive(true);
                playerCar.transform.position = gridPoses[0].position;
                playerCar.transform.rotation = gridPoses[0].rotation;
                var rb = playerCar.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                }
                playerCar.controlsEnabled = false;

                allRacers.Add(new RacerInfo
                {
                    name = "Player",
                    isPlayer = true,
                    transform = playerCar.transform,
                    carColor = new Color(0.88f, 0.04f, 0.04f),
                    currentLap = 1,
                    currentWaypoint = 0,
                    totalDistance = 0f,
                    position = 1
                });

                if (ChaseCameraController.Instance != null)
                {
                    ChaseCameraController.Instance.SetTarget(playerCar.transform);
                }
            }

            SpawnAICars(gridPoses, 1, aiCount);
        }
        else // Spectator Mode
        {
            aiCount = Mathf.Clamp(numRacers, 2, 50);
            totalRacers = aiCount;

            if (playerCar != null)
            {
                playerCar.controlsEnabled = false;
                playerCar.gameObject.SetActive(false);
            }

            List<Pose> gridPoses = CalculateGridPositions(totalRacers);
            SpawnAICars(gridPoses, 0, totalRacers);

            if (spawnedAICars.Count > 0 && ChaseCameraController.Instance != null)
            {
                ChaseCameraController.Instance.SetTarget(spawnedAICars[0].transform);
            }
        }

        var lapTimer = FindObjectOfType<LapTimer>();
        if (lapTimer != null)
        {
            lapTimer.totalLaps = totalLaps;
        }

        StartCoroutine(RaceCountdownSequence());
    }

    // ===== TIKTOK LIVE INTERACTIVE METHODS =====
    public void PrepareTikTokLobby(int laps = 3)
    {
        selectedGameMode = GameMode.TikTokLive;
        totalLaps = laps;
        ClearPreviousRace();
        allRacers.Clear();

        if (playerCar != null)
        {
            playerCar.controlsEnabled = false;
            playerCar.gameObject.SetActive(false);
        }

        // Cache full 50 grid positions
        cachedGridPoses = CalculateGridPositions(50);

        var lapTimer = FindObjectOfType<LapTimer>();
        if (lapTimer != null)
        {
            lapTimer.totalLaps = totalLaps;
        }

        // Camera looks at first grid position
        if (cachedGridPoses.Count > 0 && ChaseCameraController.Instance != null)
        {
            ChaseCameraController.Instance.transform.position = cachedGridPoses[0].position + Vector3.up * 7f - cachedGridPoses[0].rotation * Vector3.forward * 10f;
            ChaseCameraController.Instance.transform.LookAt(cachedGridPoses[0].position + Vector3.up * 1f);
        }
    }

    public GameObject SpawnSingleTikTokRacer(int slotIndex, string username, Texture2D avatarTex)
    {
        if (playerCar == null || cachedGridPoses == null || slotIndex >= cachedGridPoses.Count)
            return null;

        bool origActive = playerCar.gameObject.activeSelf;
        playerCar.gameObject.SetActive(true);

        Pose slotPose = cachedGridPoses[slotIndex];
        GameObject aiObj = Instantiate(playerCar.gameObject, slotPose.position, slotPose.rotation);
        aiObj.name = $"TikTokCar_{slotIndex + 1}_{username}";
        aiObj.tag = "Untagged";
        foreach (var col in aiObj.GetComponentsInChildren<Collider>(true))
        {
            col.gameObject.tag = "Untagged";
        }

        var pController = aiObj.GetComponent<CarController>();
        DestroyImmediate(pController);

        var ai = aiObj.AddComponent<AICarController>();
        ai.frontLeftWheel  = aiObj.transform.Find("WheelFL_Collider")?.GetComponent<WheelCollider>();
        ai.frontRightWheel = aiObj.transform.Find("WheelFR_Collider")?.GetComponent<WheelCollider>();
        ai.rearLeftWheel   = aiObj.transform.Find("WheelRL_Collider")?.GetComponent<WheelCollider>();
        ai.rearRightWheel  = aiObj.transform.Find("WheelRR_Collider")?.GetComponent<WheelCollider>();

        ai.frontLeftMesh  = aiObj.transform.Find("WheelFL_Mesh");
        ai.frontRightMesh = aiObj.transform.Find("WheelFR_Mesh");
        ai.rearLeftMesh   = aiObj.transform.Find("WheelRL_Mesh");
        ai.rearRightMesh  = aiObj.transform.Find("WheelRR_Mesh");

        var rb = aiObj.GetComponent<Rigidbody>();
        if (rb != null) rb.interpolation = RigidbodyInterpolation.Interpolate;

        Color carCol = supercarColors[slotIndex % supercarColors.Length];
        ApplyAICarColor(aiObj, carCol);

        float lateralLane = (slotIndex % 2 == 0) ? -5.5f : 5.5f;
        ai.Initialize(trackGenerator.pathPoints, 0, lateralLane, carCol, username);
        ai.isRacing = false; // Waiting on starting grid

        // Create Overhead Billboard UI
        var overheadObj = new GameObject("OverheadUI");
        overheadObj.transform.SetParent(aiObj.transform, false);
        overheadObj.transform.localPosition = new Vector3(0f, 1.85f, 0f);

        var overheadUI = overheadObj.AddComponent<RacerOverheadUI>();
        overheadUI.SetRacer(username, carCol, avatarTex);
        ai.overheadUI = overheadUI;

        spawnedAICars.Add(aiObj);

        // Register in Leaderboard
        allRacers.Add(new RacerInfo
        {
            name = username,
            isPlayer = false,
            transform = aiObj.transform,
            carColor = carCol,
            currentLap = 1,
            currentWaypoint = 0,
            totalDistance = 0f,
            position = allRacers.Count + 1
        });

        totalRacers = allRacers.Count;

        // Auto-target first joined car
        if (slotIndex == 0 && ChaseCameraController.Instance != null)
        {
            ChaseCameraController.Instance.SetTarget(aiObj.transform);
        }

        playerCar.gameObject.SetActive(origActive);
        return aiObj;
    }

    public void StartTikTokRace()
    {
        totalRacers = allRacers.Count;
        StartCoroutine(RaceCountdownSequence());
    }

    public void StopRaceAndReset()
    {
        StopAllCoroutines();
        isRaceActive = false;
        isCountdownActive = false;
        ClearPreviousRace();
        allRacers.Clear();

        if (playerCar != null)
        {
            playerCar.controlsEnabled = false;
            var rb = playerCar.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        var lapTimer = FindObjectOfType<LapTimer>();
        if (lapTimer != null)
        {
            lapTimer.ResetTimer();
        }

        if (hudController != null)
        {
            hudController.HideCountdown();
            hudController.HideFinishScreen();
        }
    }

    private void ClearPreviousRace()
    {
        foreach (var ai in spawnedAICars)
        {
            if (ai != null) Destroy(ai);
        }
        spawnedAICars.Clear();
    }

    private List<Pose> CalculateGridPositions(int count)
    {
        List<Pose> poses = new List<Pose>();
        if (trackGenerator == null || trackGenerator.pathPoints == null || trackGenerator.pathPoints.Length == 0)
            return poses;

        int totalWp = trackGenerator.pathPoints.Length;
        float accumulatedDist = 0f;
        int currentIdx = 0;

        for (int i = 0; i < count; i++)
        {
            float targetDist = 12.0f + i * 8.0f;

            while (accumulatedDist < targetDist)
            {
                int prevIdx = (currentIdx - 1 + totalWp) % totalWp;
                float segLen = Vector3.Distance(trackGenerator.pathPoints[currentIdx], trackGenerator.pathPoints[prevIdx]);
                if (accumulatedDist + segLen >= targetDist)
                {
                    float frac = (targetDist - accumulatedDist) / segLen;
                    Vector3 center = Vector3.Lerp(trackGenerator.pathPoints[currentIdx], trackGenerator.pathPoints[prevIdx], frac);
                    Vector3 tangent = Vector3.Slerp(trackGenerator.pathTangents[currentIdx], trackGenerator.pathTangents[prevIdx], frac).normalized;

                    Vector3 normal = Vector3.Slerp(trackGenerator.pathNormals[currentIdx], trackGenerator.pathNormals[prevIdx], frac).normalized;
                    float bank = Mathf.Lerp(trackGenerator.pathBankAngles[currentIdx], trackGenerator.pathBankAngles[prevIdx], frac) * Mathf.Deg2Rad;
                    Vector3 surfaceRight = normal * Mathf.Cos(bank) + Vector3.up * Mathf.Sin(bank);
                    Vector3 surfaceUp = Vector3.up * Mathf.Cos(bank) - normal * Mathf.Sin(bank);

                    float lateral = (i % 2 == 0) ? -6.0f : 6.0f;
                    Vector3 slotPos = center + surfaceRight * lateral + surfaceUp * 0.08f;
                    Quaternion slotRot = Quaternion.LookRotation(tangent, surfaceUp);

                    poses.Add(new Pose(slotPos, slotRot));
                    break;
                }
                accumulatedDist += segLen;
                currentIdx = prevIdx;
            }
        }
        return poses;
    }

    private void SpawnAICars(List<Pose> gridPoses, int startSlot, int count)
    {
        if (playerCar == null) return;

        bool originalActive = playerCar.gameObject.activeSelf;
        playerCar.gameObject.SetActive(true);

        for (int i = 0; i < count; i++)
        {
            int slotIdx = startSlot + i;
            if (slotIdx >= gridPoses.Count) break;

            GameObject aiObj = Instantiate(playerCar.gameObject, gridPoses[slotIdx].position, gridPoses[slotIdx].rotation);
            aiObj.name = $"AICar_{i + 1:D2}";
            aiObj.tag = "Untagged";
            foreach (var col in aiObj.GetComponentsInChildren<Collider>(true))
            {
                col.gameObject.tag = "Untagged";
            }

            var pController = aiObj.GetComponent<CarController>();
            DestroyImmediate(pController);

            var ai = aiObj.AddComponent<AICarController>();
            ai.frontLeftWheel  = aiObj.transform.Find("WheelFL_Collider")?.GetComponent<WheelCollider>();
            ai.frontRightWheel = aiObj.transform.Find("WheelFR_Collider")?.GetComponent<WheelCollider>();
            ai.rearLeftWheel   = aiObj.transform.Find("WheelRL_Collider")?.GetComponent<WheelCollider>();
            ai.rearRightWheel  = aiObj.transform.Find("WheelRR_Collider")?.GetComponent<WheelCollider>();

            ai.frontLeftMesh  = aiObj.transform.Find("WheelFL_Mesh");
            ai.frontRightMesh = aiObj.transform.Find("WheelFR_Mesh");
            ai.rearLeftMesh   = aiObj.transform.Find("WheelRL_Mesh");
            ai.rearRightMesh  = aiObj.transform.Find("WheelRR_Mesh");

            var rb = aiObj.GetComponent<Rigidbody>();
            if (rb != null) rb.interpolation = RigidbodyInterpolation.Interpolate;

            Color aiColor = supercarColors[i % supercarColors.Length];
            ApplyAICarColor(aiObj, aiColor);

            string driverName = (i < aiDriverNames.Length) ? aiDriverNames[i] : $"Racer {i + 1}";
            float lateralLane = (slotIdx % 2 == 0) ? -5.5f : 5.5f;

            ai.Initialize(trackGenerator.pathPoints, 0, lateralLane, aiColor, driverName);
            spawnedAICars.Add(aiObj);

            allRacers.Add(new RacerInfo
            {
                name = driverName,
                isPlayer = false,
                transform = aiObj.transform,
                carColor = aiColor,
                currentLap = 1,
                currentWaypoint = 0,
                totalDistance = 0f,
                position = allRacers.Count + 1
            });
        }

        playerCar.gameObject.SetActive(originalActive);
    }

    private void ApplyAICarColor(GameObject carObj, Color col)
    {
        var bodyObj = carObj.transform.Find("Visual_Body");
        if (bodyObj != null)
        {
            var litShader = Shader.Find("Universal Render Pipeline/Lit");
            var aiMat = new Material(litShader);
            aiMat.color = col;
            aiMat.SetColor("_BaseColor", col);
            aiMat.SetFloat("_Metallic", 0.82f);
            aiMat.SetFloat("_Smoothness", 0.94f);

            foreach (var mr in bodyObj.GetComponentsInChildren<MeshRenderer>())
            {
                mr.sharedMaterial = aiMat;
            }
        }
    }

    private IEnumerator RaceCountdownSequence()
    {
        isCountdownActive = true;
        isRaceActive = false;

        if (hudController != null) hudController.ShowCountdown("3");
        yield return new WaitForSeconds(1.0f);

        if (hudController != null) hudController.ShowCountdown("2");
        yield return new WaitForSeconds(1.0f);

        if (hudController != null) hudController.ShowCountdown("1");
        yield return new WaitForSeconds(1.0f);

        if (hudController != null) hudController.ShowCountdown("GO!");

        isCountdownActive = false;
        isRaceActive = true;

        var lapTimer = FindObjectOfType<LapTimer>();
        if (lapTimer != null)
        {
            lapTimer.StartRace(totalLaps);
        }

        if (selectedGameMode == GameMode.PlayerRace && playerCar != null)
            playerCar.controlsEnabled = true;

        foreach (var ai in spawnedAICars)
        {
            if (ai != null)
            {
                var aiCtrl = ai.GetComponent<AICarController>();
                if (aiCtrl != null) aiCtrl.isRacing = true;
            }
        }

        yield return new WaitForSeconds(1.2f);
        if (hudController != null) hudController.HideCountdown();
    }

    void Update()
    {
        if (!isRaceActive) return;
        UpdateRacePositions();
    }

    private void UpdateRacePositions()
    {
        if (trackGenerator == null || trackGenerator.pathPoints == null || trackGenerator.pathPoints.Length == 0)
            return;

        int totalWp = trackGenerator.pathPoints.Length;
        float trackCircumference = trackGenerator.targetCircumference;

        foreach (var racer in allRacers)
        {
            if (racer.transform == null) continue;

            int closest = racer.currentWaypoint;
            float minD = float.MaxValue;
            for (int offset = -4; offset <= 8; offset++)
            {
                int idx = (racer.currentWaypoint + offset + totalWp) % totalWp;
                float d = Vector3.Distance(racer.transform.position, trackGenerator.pathPoints[idx]);
                if (d < minD)
                {
                    minD = d;
                    closest = idx;
                }
            }
            racer.currentWaypoint = closest;

            float lapFraction = (float)closest / (float)totalWp;
            racer.totalDistance = (racer.currentLap - 1) * trackCircumference + (lapFraction * trackCircumference);
        }

        allRacers.Sort((a, b) => b.totalDistance.CompareTo(a.totalDistance));

        for (int pos = 0; pos < allRacers.Count; pos++)
        {
            allRacers[pos].position = pos + 1;
            if (allRacers[pos].isPlayer)
            {
                playerPosition = pos + 1;
            }
        }

        if (hudController != null)
        {
            hudController.UpdatePositionText(playerPosition, totalRacers);
        }
    }
}
