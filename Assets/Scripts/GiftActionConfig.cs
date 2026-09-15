using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public enum GiftActionType
{
    SpeedBoost = 0, // บูสความเร็ว (Nitro)
    DropBomb   = 1, // วางระเบิด (Landmine)
    ShootRPG   = 2, // ยิง RPG ใส่รถใกล้ๆ (Homing Missile)
    SlowAll    = 3  // Slow ทุกคัน (EMP Wave)
}

[Serializable]
public class GiftActionItem
{
    public string giftId = "";
    public string giftName = "Rose";
    public int diamondCost = 1;
    public string giftIconUrl = "";
    public GiftActionType actionType = GiftActionType.SpeedBoost;
    public float duration = 4.0f;
    public float intensity = 1.5f;
    public bool enabled = true;

    public GiftActionItem Clone()
    {
        return new GiftActionItem
        {
            giftId = this.giftId,
            giftName = this.giftName,
            diamondCost = this.diamondCost,
            giftIconUrl = this.giftIconUrl,
            actionType = this.actionType,
            duration = this.duration,
            intensity = this.intensity,
            enabled = this.enabled
        };
    }
}

[Serializable]
public class GiftActionDatabase
{
    public List<GiftActionItem> actions = new List<GiftActionItem>();
}

[Serializable]
public class TikTokGiftInfo
{
    public string id = "";
    public string name = "";
    public string thName = "";
    public int diamonds = 1;
    public string icon_url = "";
}

[Serializable]
public class TikTokGiftListResponse
{
    public List<TikTokGiftInfo> gifts = new List<TikTokGiftInfo>();
}

public static class GiftActionConfig
{
    public static string ConfigFilePath
    {
        get
        {
            return Path.Combine(Application.persistentDataPath, "gift_actions.json");
        }
    }

    public static GiftActionDatabase LoadConfig()
    {
        try
        {
            string path = ConfigFilePath;
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                var db = JsonUtility.FromJson<GiftActionDatabase>(json);
                if (db != null && db.actions != null && db.actions.Count > 0)
                {
                    Debug.Log($"[GiftConfig] Loaded {db.actions.Count} gift actions from: {path}");
                    return db;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GiftConfig] Error loading config from disk: {ex.Message}. Using defaults.");
        }

        var defaultDb = CreateDefaultDatabase();
        SaveConfig(defaultDb);
        return defaultDb;
    }

    public static bool SaveConfig(GiftActionDatabase db)
    {
        if (db == null) return false;
        try
        {
            string path = ConfigFilePath;
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = JsonUtility.ToJson(db, true);
            File.WriteAllText(path, json);
            Debug.Log($"[GiftConfig] Successfully saved {db.actions.Count} gift actions to: {path}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GiftConfig] Failed to save config: {ex.Message}");
            return false;
        }
    }

    public static GiftActionDatabase CreateDefaultDatabase()
    {
        var db = new GiftActionDatabase();
        db.actions = new List<GiftActionItem>
        {
            new GiftActionItem
            {
                giftId = "5655",
                giftName = "Rose",
                diamondCost = 1,
                actionType = GiftActionType.SpeedBoost,
                duration = 4.0f,
                intensity = 1.5f,
                enabled = true
            },
            new GiftActionItem
            {
                giftId = "5827",
                giftName = "TikTok",
                diamondCost = 1,
                actionType = GiftActionType.DropBomb,
                duration = 3.0f,
                intensity = 1.0f,
                enabled = true
            },
            new GiftActionItem
            {
                giftId = "5656",
                giftName = "Doughnut",
                diamondCost = 30,
                actionType = GiftActionType.ShootRPG,
                duration = 3.5f,
                intensity = 2.0f,
                enabled = true
            },
            new GiftActionItem
            {
                giftId = "5657",
                giftName = "Cap",
                diamondCost = 99,
                actionType = GiftActionType.SlowAll,
                duration = 5.0f,
                intensity = 0.4f,
                enabled = true
            },
            new GiftActionItem
            {
                giftId = "6050",
                giftName = "Money Gun",
                diamondCost = 500,
                actionType = GiftActionType.SpeedBoost,
                duration = 8.0f,
                intensity = 2.2f,
                enabled = true
            }
        };
        return db;
    }
}
