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
                giftIconUrl = "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/eba3a9bb85c33e017f3648eaf88d7189~tplv-obj.png",
                actionType = GiftActionType.SpeedBoost,
                duration = 4.0f,
                intensity = 1.5f,
                enabled = true
            },
            new GiftActionItem
            {
                giftId = "5269",
                giftName = "TikTok",
                diamondCost = 1,
                giftIconUrl = "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/802a21ae29f9fae5abe3693de9f874bd~tplv-obj.png",
                actionType = GiftActionType.DropBomb,
                duration = 3.0f,
                intensity = 1.0f,
                enabled = true
            },
            new GiftActionItem
            {
                giftId = "5879",
                giftName = "Doughnut",
                diamondCost = 30,
                giftIconUrl = "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/4e7ad6bdf0a1d860c538f38026d4e812~tplv-obj.png",
                actionType = GiftActionType.ShootRPG,
                duration = 3.5f,
                intensity = 2.0f,
                enabled = true
            },
            new GiftActionItem
            {
                giftId = "6104",
                giftName = "Cap",
                diamondCost = 99,
                giftIconUrl = "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/6c2ab2da19249ea570a2ece5e3377f04~tplv-obj.png",
                actionType = GiftActionType.SlowAll,
                duration = 5.0f,
                intensity = 0.4f,
                enabled = true
            },
            new GiftActionItem
            {
                giftId = "7168",
                giftName = "Money Gun",
                diamondCost = 500,
                giftIconUrl = "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/e0589e95a2b41970f0f30f6202f5fce6~tplv-obj.png",
                actionType = GiftActionType.SpeedBoost,
                duration = 8.0f,
                intensity = 2.2f,
                enabled = true
            }
        };
        return db;
    }
}
