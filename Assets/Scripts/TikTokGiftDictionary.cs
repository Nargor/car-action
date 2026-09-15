using System;
using System.Collections.Generic;
using UnityEngine;

public static class TikTokGiftDictionary
{
    public class GiftCatalogEntry
    {
        public string id;
        public string name;
        public string thName;
        public int diamonds;
        public string iconUrl;
        public Color badgeColor;
        public string[] searchKeywords;

        public GiftCatalogEntry(string id, string name, string thName, int diamonds, Color badgeColor, params string[] keywords)
        {
            this.id = id;
            this.name = name;
            this.thName = thName;
            this.diamonds = diamonds;
            this.iconUrl = "";
            this.badgeColor = badgeColor;
            this.searchKeywords = keywords ?? new string[0];
        }

        public TikTokGiftInfo ToGiftInfo()
        {
            return new TikTokGiftInfo
            {
                id = this.id,
                name = this.name,
                thName = this.thName,
                diamonds = this.diamonds,
                icon_url = this.iconUrl
            };
        }
    }

    public static readonly List<GiftCatalogEntry> Catalog = new List<GiftCatalogEntry>
    {
        // 1 - 20 (Default Top 20 Most Popular TikTok Live Gifts)
        new GiftCatalogEntry("5655", "Rose", "กุหลาบ", 1, new Color(0.95f, 0.15f, 0.35f), "ดอกกุหลาบ", "flower", "red"),
        new GiftCatalogEntry("5827", "TikTok", "ติ๊กต๊อก", 1, new Color(0.12f, 0.85f, 0.85f), "tiktok", "โลโก้", "tt"),
        new GiftCatalogEntry("5269", "Heart", "หัวใจ", 1, new Color(1.0f, 0.25f, 0.50f), "ใจ", "love", "heart"),
        new GiftCatalogEntry("5879", "Finger Heart", "มินิฮาร์ท", 5, new Color(1.0f, 0.40f, 0.70f), "มินิฮาร์ท", "mini heart", "เกาหลี"),
        new GiftCatalogEntry("5509", "Panda", "แพนด้า", 5, new Color(0.85f, 0.85f, 0.90f), "หมี", "bear", "panda"),
        new GiftCatalogEntry("6003", "Ice Cream", "ไอศกรีม", 1, new Color(1.0f, 0.70f, 0.40f), "ไอติม", "cone", "ice cream"),
        new GiftCatalogEntry("5656", "Doughnut", "โดนัท", 30, new Color(0.95f, 0.55f, 0.20f), "โดนัท", "ขนม", "donut"),
        new GiftCatalogEntry("5657", "Cap", "หมวกแก๊ป", 99, new Color(0.20f, 0.55f, 0.95f), "หมวก", "หมวกแก๊ป", "hat"),
        new GiftCatalogEntry("5801", "Sunglasses", "แว่นกันแดด", 199, new Color(0.55f, 0.25f, 0.95f), "แว่น", "แว่นตา", "glasses", "shades"),
        new GiftCatalogEntry("5200", "Boxing Gloves", "นวมมวย", 299, new Color(0.95f, 0.20f, 0.20f), "นวม", "ต่อยมวย", "boxing"),
        new GiftCatalogEntry("5587", "Corgi", "คอร์กี้", 299, new Color(1.0f, 0.65f, 0.15f), "หมา", "สุนัข", "corgi", "dog"),
        new GiftCatalogEntry("5240", "Diamond Ring", "แหวนเพชร", 300, new Color(0.20f, 0.90f, 0.95f), "แหวน", "เพชร", "ring", "diamond"),
        new GiftCatalogEntry("6050", "Money Gun", "ปืนยิงเงิน", 500, new Color(0.15f, 0.85f, 0.45f), "ปืน", "ยิงเงิน", "money", "gun", "แบงค์"),
        new GiftCatalogEntry("5660", "Swan", "หงส์", 699, new Color(0.30f, 0.85f, 1.0f), "หงส์ขาว", "นก", "swan"),
        new GiftCatalogEntry("5100", "Gold Mine", "เหมืองทอง", 1000, new Color(1.0f, 0.85f, 0.10f), "ทอง", "เหมือง", "gold", "mine"),
        new GiftCatalogEntry("5760", "Fireworks", "พลุดอกไม้ไฟ", 1088, new Color(1.0f, 0.20f, 0.40f), "พลุ", "ดอกไม้ไฟ", "fireworks", "boom"),
        new GiftCatalogEntry("5230", "Garland", "พวงมาลัย", 1500, new Color(0.40f, 0.90f, 0.30f), "พวงมาลัย", "ดอกไม้", "garland"),
        new GiftCatalogEntry("6055", "Whale", "ปลาวาฬ", 2150, new Color(0.10f, 0.50f, 0.95f), "วาฬ", "ปลาวาฬ", "whale"),
        new GiftCatalogEntry("6056", "Motorcycle", "มอเตอร์ไซค์", 2988, new Color(0.45f, 0.55f, 0.65f), "มอไซ", "มอเตอร์ไซค์", "รถเครื่อง", "bike"),
        new GiftCatalogEntry("6057", "Sports Car", "รถสปอร์ต", 7000, new Color(0.90f, 0.10f, 0.10f), "รถ", "รถหรู", "ซุปเปอร์คาร์", "car", "ferrari"),

        // 21 - 36 (Extended gifts for search)
        new GiftCatalogEntry("5300", "Crown", "มงกุฎ", 9999, new Color(1.0f, 0.80f, 0.0f), "มงกุฎ", "ราชา", "king", "crown"),
        new GiftCatalogEntry("6058", "Falcon", "เหยี่ยว", 10999, new Color(0.85f, 0.50f, 0.15f), "เหยี่ยว", "นกอินทรี", "bird", "falcon"),
        new GiftCatalogEntry("5430", "Planet", "ดาวเคราะห์", 15000, new Color(0.40f, 0.20f, 0.80f), "ดาว", "อวกาศ", "planet", "space"),
        new GiftCatalogEntry("5790", "Castle", "ปราสาท", 20000, new Color(0.60f, 0.20f, 0.85f), "ปราสาท", "วัง", "castle"),
        new GiftCatalogEntry("6059", "Lion", "สิงโต", 29999, new Color(1.0f, 0.75f, 0.05f), "สิงโต", "เจ้าป่า", "lion", "king"),
        new GiftCatalogEntry("6060", "TikTok Universe", "จักรวาล TikTok", 34999, new Color(0.35f, 0.10f, 0.75f), "จักรวาล", "กาแล็กซี", "universe", "galaxy"),
        new GiftCatalogEntry("6100", "Phoenix", "นกฟีนิกซ์", 25999, new Color(1.0f, 0.35f, 0.05f), "ฟีนิกซ์", "วิหคเพลิง", "phoenix"),
        new GiftCatalogEntry("6110", "Pegasus", "ม้าเพกาซัส", 27999, new Color(0.80f, 0.85f, 1.0f), "ม้าบิน", "เพกาซัส", "pegasus", "horse"),
        new GiftCatalogEntry("6120", "Dragon", "มังกร", 26999, new Color(0.95f, 0.20f, 0.15f), "มังกร", "dragon"),
        new GiftCatalogEntry("6130", "Rocket", "จรวด", 1000, new Color(0.95f, 0.40f, 0.10f), "จรวด", "rocket"),
        new GiftCatalogEntry("6140", "Magic Lamp", "ตะเกียงวิเศษ", 1000, new Color(1.0f, 0.85f, 0.20f), "ตะเกียง", "ยักษ์จินนี่", "lamp", "genie"),
        new GiftCatalogEntry("6150", "Speedboat", "เรือเร็ว", 1888, new Color(0.15f, 0.75f, 0.95f), "เรือ", "สปีดโบ๊ท", "boat"),
        new GiftCatalogEntry("6160", "Private Jet", "เครื่องบินเจ็ท", 4888, new Color(0.70f, 0.75f, 0.85f), "เครื่องบิน", "เจ็ท", "plane", "jet"),
        new GiftCatalogEntry("5210", "Hand Hearts", "มือรูปหัวใจ", 100, new Color(1.0f, 0.45f, 0.65f), "มือหัวใจ", "heart hand"),
        new GiftCatalogEntry("5220", "Confetti", "พลุกระดาษ", 100, new Color(1.0f, 0.85f, 0.25f), "กระดาษ", "ฉลอง", "party", "confetti")
    };

    /// <summary>
    /// Returns default gifts (capped at 20 to avoid lag/freeze).
    /// </summary>
    public static List<TikTokGiftInfo> GetDefaultGifts(int limit = 20)
    {
        int cap = Mathf.Clamp(limit, 1, Catalog.Count);
        var result = new List<TikTokGiftInfo>(cap);
        for (int i = 0; i < cap; i++)
        {
            result.Add(Catalog[i].ToGiftInfo());
        }
        return result;
    }

    /// <summary>
    /// Search gifts matching English name, Thai name, coin amount, or keywords.
    /// Caps at 10 items for optimal performance and clean UX.
    /// </summary>
    public static List<TikTokGiftInfo> SearchGifts(string query, int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return GetDefaultGifts(20);
        }

        string q = query.Trim().ToLower();
        List<KeyValuePair<int, GiftCatalogEntry>> scored = new List<KeyValuePair<int, GiftCatalogEntry>>();

        foreach (var entry in Catalog)
        {
            int score = CalculateMatchScore(entry, q);
            if (score > 0)
            {
                scored.Add(new KeyValuePair<int, GiftCatalogEntry>(score, entry));
            }
        }

        // Sort by highest score first, then by diamond cost
        scored.Sort((a, b) =>
        {
            int cmp = b.Key.CompareTo(a.Key);
            if (cmp != 0) return cmp;
            return a.Value.diamonds.CompareTo(b.Value.diamonds);
        });

        int takeCount = Mathf.Min(limit, scored.Count);
        var result = new List<TikTokGiftInfo>(takeCount);
        for (int i = 0; i < takeCount; i++)
        {
            result.Add(scored[i].Value.ToGiftInfo());
        }

        return result;
    }

    private static int CalculateMatchScore(GiftCatalogEntry entry, string q)
    {
        string nameLower = entry.name.ToLower();
        string thLower = (entry.thName ?? "").ToLower();
        string diamondStr = entry.diamonds.ToString();

        // Exact matches (highest priority)
        if (nameLower == q || thLower == q) return 100;

        // Starts with query
        if (nameLower.StartsWith(q) || thLower.StartsWith(q)) return 80;

        // Contains query
        if (nameLower.Contains(q)) return 60;
        if (thLower.Contains(q)) return 55;

        // Diamond amount match
        if (diamondStr == q || diamondStr.StartsWith(q)) return 40;

        // Keyword matches
        if (entry.searchKeywords != null)
        {
            foreach (var kw in entry.searchKeywords)
            {
                string kwLower = kw.ToLower();
                if (kwLower == q) return 50;
                if (kwLower.StartsWith(q)) return 35;
                if (kwLower.Contains(q)) return 25;
            }
        }

        return 0;
    }

    public static GiftCatalogEntry FindEntry(string giftName)
    {
        if (string.IsNullOrEmpty(giftName)) return null;
        return Catalog.Find(x => x.name.Equals(giftName, StringComparison.OrdinalIgnoreCase) ||
                                (x.thName != null && x.thName.Equals(giftName, StringComparison.OrdinalIgnoreCase)));
    }

    public static string GetThaiName(string giftName)
    {
        var e = FindEntry(giftName);
        return e != null ? e.thName : giftName;
    }

    public static Color GetBadgeColor(string giftName)
    {
        var e = FindEntry(giftName);
        return e != null ? e.badgeColor : new Color(0.25f, 0.45f, 0.85f);
    }
}
