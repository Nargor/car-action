using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GiftActionManager : MonoBehaviour
{
    public static GiftActionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var all = Resources.FindObjectsOfTypeAll<GiftActionManager>();
                if (all != null && all.Length > 0) _instance = all[0];
            }
            return _instance;
        }
    }
    private static GiftActionManager _instance;

    [Header("Configuration")]
    public GiftActionDatabase database;
    public List<TikTokGiftInfo> availableGifts = new List<TikTokGiftInfo>();
    public string apiGiftsUrl = "http://127.0.0.1:8765/api/gifts";

    public event Action OnConfigChanged;
    public event Action OnGiftsLoaded;

    void Awake()
    {
        if (_instance == null) _instance = this;
        else if (_instance != this && transform.parent == null)
        {
            _instance = this;
        }

        if (database == null || database.actions == null || database.actions.Count == 0)
        {
            LoadDatabase();
        }
    }

    void Start()
    {
        LoadDatabase();
        StartCoroutine(FetchGiftsRoutine());
    }

    public void LoadDatabase()
    {
        database = GiftActionConfig.LoadConfig();
        OnConfigChanged?.Invoke();
    }

    public void SaveDatabase()
    {
        if (database != null)
        {
            GiftActionConfig.SaveConfig(database);
            OnConfigChanged?.Invoke();
        }
    }

    public void ResetToDefaults()
    {
        database = GiftActionConfig.CreateDefaultDatabase();
        SaveDatabase();
    }

    public IEnumerator FetchGiftsRoutine(Action<bool> onComplete = null)
    {
        bool apiSuccess = false;
        string[] candidates = new string[]
        {
            apiGiftsUrl,
            "http://127.0.0.1:8765/gifts",
            "http://localhost:8765/api/gifts",
            "http://localhost:8088/api/gifts"
        };

        foreach (var url in candidates)
        {
            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                req.timeout = 2;
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string json = req.downloadHandler.text;
                        if (json.TrimStart().StartsWith("["))
                        {
                            json = "{\"gifts\":" + json + "}";
                        }
                        var resp = JsonUtility.FromJson<TikTokGiftListResponse>(json);
                        if (resp != null && resp.gifts != null && resp.gifts.Count > 0)
                        {
                            availableGifts = resp.gifts;
                            apiSuccess = true;
                            Debug.Log($"[GiftActionManager] Loaded {availableGifts.Count} gifts from API ({url})");
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[GiftActionManager] Parse gifts error from {url}: {ex.Message}");
                    }
                }
            }
        }

        if (!apiSuccess || availableGifts == null || availableGifts.Count == 0)
        {
            availableGifts = GenerateFallbackGiftCatalog();
            Debug.Log($"[GiftActionManager] Using fallback TikTok gift catalog with {availableGifts.Count} gifts.");
        }

        OnGiftsLoaded?.Invoke();
        onComplete?.Invoke(apiSuccess);
    }

    // ==========================================
    // EXECUTE GIFT ACTION
    // ==========================================
    public void ExecuteGiftAction(string username, string giftName)
    {
        if (string.IsNullOrEmpty(giftName)) return;

        // 1. Find matched enabled action rule
        GiftActionItem matchedRule = null;
        if (database != null && database.actions != null)
        {
            foreach (var rule in database.actions)
            {
                if (rule != null && rule.enabled && rule.giftName.Equals(giftName, StringComparison.OrdinalIgnoreCase))
                {
                    matchedRule = rule;
                    break;
                }
            }
        }

        // Find sender car
        GameObject senderCar = null;
        if (TikTokLiveManager.Instance != null && TikTokLiveManager.Instance.userToCar != null)
        {
            TikTokLiveManager.Instance.userToCar.TryGetValue(username, out senderCar);
        }

        if (senderCar == null)
        {
            var pObj = GameObject.Find("PlayerCar");
            if (pObj != null)
            {
                senderCar = pObj;
            }
            else
            {
                var pCtrl = UnityEngine.Object.FindAnyObjectByType<CarController>();
                if (pCtrl != null) senderCar = pCtrl.gameObject;
                else
                {
                    var ai = UnityEngine.Object.FindAnyObjectByType<AICarController>();
                    if (ai != null) senderCar = ai.gameObject;
                }
            }
        }

        if (matchedRule == null)
        {
            // Default action if no custom rule configured: Speed Boost
            ApplySpeedBoost(senderCar, username, 4.0f, giftName);
            return;
        }

        switch (matchedRule.actionType)
        {
            case GiftActionType.SpeedBoost:
                ApplySpeedBoost(senderCar, username, matchedRule.duration, giftName);
                break;

            case GiftActionType.DropBomb:
                ApplyDropBomb(senderCar, username, giftName);
                break;

            case GiftActionType.ShootRPG:
                ApplyShootRPG(senderCar, username, giftName);
                break;

            case GiftActionType.SlowAll:
                ApplySlowAll(senderCar, username, matchedRule.duration, matchedRule.intensity, giftName);
                break;
        }
    }

    private void ApplySpeedBoost(GameObject senderCar, string username, float duration, string giftName)
    {
        if (senderCar != null)
        {
            var ai = senderCar.GetComponent<AICarController>();
            if (ai != null) ai.ApplyNitro(duration);

            var player = senderCar.GetComponent<CarController>();
            if (player != null) player.ApplyNitro(duration);

            Debug.Log($"[GiftAction] ⚡ SPEED BOOST for @{username} by gift '{giftName}' ({duration}s)");
        }
    }

    private void ApplyDropBomb(GameObject senderCar, string username, string giftName)
    {
        if (senderCar == null) return;

        Vector3 spawnPos = senderCar.transform.position - senderCar.transform.forward * 4.2f + Vector3.up * 0.1f;
        TrackBomb.CreateBomb(spawnPos, senderCar.transform.rotation, senderCar.transform);

        Debug.Log($"[GiftAction] 💣 LANDMINE dropped by @{username} via gift '{giftName}'!");
    }

    private void ApplyShootRPG(GameObject senderCar, string username, string giftName)
    {
        if (senderCar == null) return;

        // Find nearest rival car in front or closest
        Transform targetRival = FindNearestRival(senderCar.transform);
        if (targetRival == null)
        {
            // If no rival, convert to speed boost!
            ApplySpeedBoost(senderCar, username, 5.0f, giftName);
            return;
        }

        Vector3 spawnPos = senderCar.transform.position + senderCar.transform.forward * 2.6f + Vector3.up * 1.2f;
        HomingMissile.Launch(spawnPos, senderCar.transform.rotation, senderCar.transform, targetRival);

        string targetName = targetRival.name;
        var targetAi = targetRival.GetComponent<AICarController>();
        if (targetAi != null) targetName = targetAi.racerName;

        Debug.Log($"[GiftAction] 🚀 RPG launched by @{username} targeting @{targetName} via gift '{giftName}'!");
    }

    private void ApplySlowAll(GameObject senderCar, string username, float duration, float multiplier, string giftName)
    {
        float slowMult = (multiplier > 0.05f && multiplier < 0.95f) ? multiplier : 0.40f;
        int affectedCount = 0;

        if (TikTokLiveManager.Instance != null && TikTokLiveManager.Instance.userToCar != null)
        {
            foreach (var kvp in TikTokLiveManager.Instance.userToCar)
            {
                var car = kvp.Value;
                if (car != null && car != senderCar)
                {
                    var ai = car.GetComponent<AICarController>();
                    if (ai != null)
                    {
                        ai.ApplySlow(slowMult, duration);
                        affectedCount++;
                    }

                    var p = car.GetComponent<CarController>();
                    if (p != null)
                    {
                        p.ApplySlow(slowMult, duration);
                        affectedCount++;
                    }
                }
            }
        }

        Debug.Log($"[GiftAction] ❄️ EMP SLOW ALL triggered by @{username} via '{giftName}' ({affectedCount} cars slowed for {duration}s)!");
    }

    private Transform FindNearestRival(Transform myCar)
    {
        Transform bestTarget = null;
        float bestDist = float.MaxValue;

        if (TikTokLiveManager.Instance != null && TikTokLiveManager.Instance.userToCar != null)
        {
            foreach (var kvp in TikTokLiveManager.Instance.userToCar)
            {
                var car = kvp.Value;
                if (car == null || car.transform == myCar) continue;

                Vector3 diff = car.transform.position - myCar.position;
                float dist = diff.magnitude;

                // Prioritize cars in front
                float forwardDot = Vector3.Dot(myCar.forward, diff.normalized);
                if (forwardDot > 0.1f && dist < bestDist && dist < 180f)
                {
                    bestDist = dist;
                    bestTarget = car.transform;
                }
            }

            // If no car in front, pick closest overall
            if (bestTarget == null)
            {
                foreach (var kvp in TikTokLiveManager.Instance.userToCar)
                {
                    var car = kvp.Value;
                    if (car == null || car.transform == myCar) continue;
                    float dist = Vector3.Distance(myCar.position, car.transform.position);
                    if (dist < bestDist && dist < 120f)
                    {
                        bestDist = dist;
                        bestTarget = car.transform;
                    }
                }
            }
        }

        return bestTarget;
    }

    // ==========================================
    // FALLBACK OFFICIAL TIKTOK GIFT CATALOG
    // ==========================================
    public static List<TikTokGiftInfo> GenerateFallbackGiftCatalog()
    {
        var list = new List<TikTokGiftInfo>();
        foreach (var entry in TikTokGiftDictionary.Catalog)
        {
            list.Add(entry.ToGiftInfo());
        }
        return list;
    }
}
