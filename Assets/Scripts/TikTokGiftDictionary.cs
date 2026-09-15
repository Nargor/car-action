using System;
using System.IO;
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

        public GiftCatalogEntry(string id, string name, string thName, int diamonds, string iconUrl, Color badgeColor, params string[] keywords)
        {
            this.id = id;
            this.name = name;
            this.thName = thName;
            this.diamonds = diamonds;
            this.iconUrl = iconUrl ?? "";
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

    private static bool isInitialized = false;

    public static readonly List<GiftCatalogEntry> Catalog = new List<GiftCatalogEntry>
    {
        // 1 - 20 (Default Top 20 Most Popular TikTok Live Gifts with official TikTok CDN URLs)
        new GiftCatalogEntry("5655", "Rose", "กุหลาบ", 1, "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/eba3a9bb85c33e017f3648eaf88d7189~tplv-obj.png", new Color(0.95f, 0.15f, 0.35f), "ดอกกุหลาบ", "flower", "red"),
        new GiftCatalogEntry("5269", "TikTok", "ติ๊กต๊อก", 1, "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/802a21ae29f9fae5abe3693de9f874bd~tplv-obj.png", new Color(0.12f, 0.85f, 0.85f), "tiktok", "โลโก้", "tt"),
        new GiftCatalogEntry("6247", "Heart", "หัวใจ", 1, "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/dd300fd35a757d751301fba862a258f1~tplv-obj.png", new Color(1.0f, 0.25f, 0.50f), "ใจ", "love", "heart", "ชุมชน"),
        new GiftCatalogEntry("5487", "Finger Heart", "มินิฮาร์ท", 5, "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/a4c4dc437fd3a6632aba149769491f49.png~tplv-obj.png", new Color(1.0f, 0.40f, 0.70f), "มินิฮาร์ท", "mini heart", "เกาหลี"),
        new GiftCatalogEntry("19314", "Panda", "แพนด้า", 5, "https://p16-webcast.tiktokcdn.com/img/alisg/webcast-sg/resource/a96c32f3272df1905eb3f3d51b53308b.png~tplv-obj.png", new Color(0.85f, 0.85f, 0.90f), "หมี", "bear", "panda"),
        new GiftCatalogEntry("15199", "Ice Cream", "ไอศกรีม", 1, "https://p16-webcast.tiktokcdn.com/img/alisg/webcast-sg/resource/7f784d1ec7b26d7d8cfd05faede11d76.png~tplv-obj.png", new Color(1.0f, 0.70f, 0.40f), "ไอติม", "cone", "ice cream"),
        new GiftCatalogEntry("5879", "Doughnut", "โดนัท", 30, "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/4e7ad6bdf0a1d860c538f38026d4e812~tplv-obj.png", new Color(0.95f, 0.55f, 0.20f), "โดนัท", "ขนม", "donut"),
        new GiftCatalogEntry("6104", "Cap", "หมวกแก๊ป", 99, "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/6c2ab2da19249ea570a2ece5e3377f04~tplv-obj.png", new Color(0.20f, 0.55f, 0.95f), "หมวก", "หมวกแก๊ป", "hat"),
        new GiftCatalogEntry("5509", "Sunglasses", "แว่นกันแดด", 199, "https://p16-webcast.tiktokcdn.com/img/alisg/webcast-sg/08af67ab13a8053269bf539fd27f3873.png~tplv-obj.png", new Color(0.55f, 0.25f, 0.95f), "แว่น", "แว่นตา", "glasses", "shades"),
        new GiftCatalogEntry("59450", "Boxing Gloves", "นวมมวย", 299, "https://p16-webcast.tiktokcdn.com/img/alisg/webcast-sg/resource/3b77922c8b290b899ae70b6c0e84e437.png~tplv-obj.png", new Color(0.95f, 0.20f, 0.20f), "นวม", "ต่อยมวย", "boxing"),
        new GiftCatalogEntry("6267", "Corgi", "คอร์กี้", 299, "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/148eef0884fdb12058d1c6897d1e02b9~tplv-obj.png", new Color(1.0f, 0.65f, 0.15f), "หมา", "สุนัข", "corgi", "dog"),
        new GiftCatalogEntry("5566", "Mishka Bear", "หมีมิชก้า", 100, "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/d78ed6496fd57286b42ac033acbee299.png~tplv-obj.png", new Color(0.80f, 0.60f, 0.40f), "หมี", "bear", "mishka"),
        new GiftCatalogEntry("7168", "Money Gun", "ปืนยิงเงิน", 500, "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/e0589e95a2b41970f0f30f6202f5fce6~tplv-obj.png", new Color(0.15f, 0.85f, 0.45f), "ปืน", "ยิงเงิน", "money", "gun", "แบงค์"),
        new GiftCatalogEntry("5897", "Swan", "หงส์", 699, "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/97a26919dbf6afe262c97e22a83f4bf1~tplv-obj.png", new Color(0.30f, 0.85f, 1.0f), "หงส์ขาว", "นก", "swan"),
        new GiftCatalogEntry("6064", "GG", "จีจี", 1, "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/3f02fa9594bd1495ff4e8aa5ae265eef~tplv-obj.png", new Color(1.0f, 0.85f, 0.10f), "gg", "เหรียญ", "ทอง", "good game"),
        new GiftCatalogEntry("6090", "Fireworks", "พลุดอกไม้ไฟ", 1088, "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/9494c8a0bc5c03521ef65368e59cc2b8~tplv-obj.png", new Color(1.0f, 0.20f, 0.40f), "พลุ", "ดอกไม้ไฟ", "fireworks", "boom"),
        new GiftCatalogEntry("6437", "Garland", "พวงมาลัย", 199, "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/bdbdd8aeb2b69c173a3ef666e63310f3~tplv-obj.png", new Color(0.40f, 0.90f, 0.30f), "พวงมาลัย", "ดอกไม้", "garland"),
        new GiftCatalogEntry("6820", "Whale", "ปลาวาฬ", 2150, "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/46fa70966d8e931497f5289060f9a794~tplv-obj.png", new Color(0.10f, 0.50f, 0.95f), "วาฬ", "ปลาวาฬ", "whale"),
        new GiftCatalogEntry("5765", "Motorcycle", "มอเตอร์ไซค์", 2988, "https://p16-webcast.tiktokcdn.com/img/alisg/webcast-sg/motor_icon_green.png~tplv-obj.png", new Color(0.45f, 0.55f, 0.65f), "มอไซ", "มอเตอร์ไซค์", "รถเครื่อง", "bike"),
        new GiftCatalogEntry("13061", "Sports Car", "รถสปอร์ต", 4999, "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/resource/201304005bf8f86b533b97ffa88f1abe.png~tplv-obj.png", new Color(0.90f, 0.10f, 0.10f), "รถ", "รถหรู", "ซุปเปอร์คาร์", "car", "ferrari"),

        // 21 - 36 (Extended gifts for search)
        new GiftCatalogEntry("54294", "Crown", "มงกุฎ", 14999, "https://p16-webcast.tiktokcdn.com/img/alisg/webcast-sg/resource/b2566977022eacff45fa1c268f4bea91.png~tplv-obj.png", new Color(1.0f, 0.80f, 0.0f), "มงกุฎ", "ราชา", "king", "crown"),
        new GiftCatalogEntry("8503", "Falcon", "เหยี่ยว", 10999, "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/6026505eea9b9bce071dd699253abf6a~tplv-obj.png", new Color(0.85f, 0.50f, 0.15f), "เหยี่ยว", "นกอินทรี", "bird", "falcon"),
        new GiftCatalogEntry("59711", "Castle", "ปราสาท", 25999, "https://p16-webcast.tiktokcdn.com/img/alisg/webcast-sg/resource/3c6e2297ca0ddd194653cae1d3baaffa.png~tplv-obj.png", new Color(0.60f, 0.20f, 0.85f), "ปราสาท", "วัง", "castle"),
        new GiftCatalogEntry("6369", "Lion", "สิงโต", 29999, "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/4fb89af2082a290b37d704e20f4fe729~tplv-obj.png", new Color(1.0f, 0.75f, 0.05f), "สิงโต", "เจ้าป่า", "lion", "king"),
        new GiftCatalogEntry("9101", "TikTok Universe", "จักรวาล TikTok", 44999, "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/8f471afbcebfda3841a6cc515e381f58~tplv-obj.png", new Color(0.35f, 0.10f, 0.75f), "จักรวาล", "กาแล็กซี", "universe", "galaxy"),
        new GiftCatalogEntry("7319", "Phoenix", "นกฟีนิกซ์", 25999, "https://p16-webcast.tiktokcdn.com/img/alisg/webcast-sg/resource/2c5980ea26241ebeeab76de701d47968.png~tplv-obj.png", new Color(1.0f, 0.35f, 0.05f), "ฟีนิกซ์", "วิหคเพลิง", "phoenix"),
        new GiftCatalogEntry("9427", "Pegasus", "ม้าเพกาซัส", 42999, "https://p16-webcast.tiktokcdn.com/img/maliva/webcast-va/resource/f600a2495ab5d250e7da2066484a9383.png~tplv-obj.png", new Color(0.80f, 0.85f, 1.0f), "ม้าบิน", "เพกาซัส", "pegasus", "horse"),
        new GiftCatalogEntry("7610", "Dragon", "มังกร", 26999, "https://p16-webcast.tiktokcdn.com/img/alisg/webcast-sg/resource/e5af942cbf6c909da2f11950b1df8b8d.png~tplv-obj.png", new Color(0.95f, 0.20f, 0.15f), "มังกร", "dragon"),
        new GiftCatalogEntry("5767", "Private Jet", "เครื่องบินเจ็ท", 4888, "https://p16-webcast.tiktokcdn.com/img/alisg/webcast-sg/airplane_icon_gold.png~tplv-obj.png", new Color(0.70f, 0.75f, 0.85f), "เครื่องบิน", "เจ็ท", "plane", "jet"),
        new GiftCatalogEntry("5585", "Confetti", "พลุกระดาษ", 100, "https://p16-webcast.tiktokcdn.com/img/alisg/webcast-sg/resource/472bf660b0804bb37f616d173edb8a9d.png~tplv-obj.png", new Color(1.0f, 0.85f, 0.25f), "กระดาษ", "ฉลอง", "party", "confetti"),
        new GiftCatalogEntry("7934", "Heart Me", "ฮาร์ทมี", 1, "https://p16-webcast.tiktokcdn.com/img/alisg/webcast-sg/resource/composed.44ad26237d342a9729583a1ae7dffe36.png~tplv-obj.png", new Color(1.0f, 0.40f, 0.50f), "heart me", "ฮาร์ทมี", "ส้ม", "ใจส้ม"),
        new GiftCatalogEntry("62816", "Community Heart", "หัวใจชุมชน", 1, "https://p16-webcast.tiktokcdn.com/img/alisg/webcast-sg/resource/8d2a21bead102d4cc1ffade117c9dc46.png~tplv-obj.png", new Color(1.0f, 0.35f, 0.60f), "หัวใจ", "คอมมูนิตี้", "ใจจุ่ม")
    };

    public static void EnsureInitialized()
    {
        if (isInitialized) return;
        isInitialized = true;
        LoadFromStreamingAssetsCache();
    }

    private static void LoadFromStreamingAssetsCache()
    {
        try
        {
            string cachePath = Path.Combine(Application.streamingAssetsPath, "tiktok_gifts_cache.json");
            if (!File.Exists(cachePath)) return;

            string json = File.ReadAllText(cachePath);
            int added = 0;

            int idx = 0;
            while ((idx = json.IndexOf("\"id\":", idx)) >= 0)
            {
                int blockStart = json.LastIndexOf('{', idx);
                int blockEnd = json.IndexOf('}', idx);
                if (blockStart >= 0 && blockEnd > blockStart)
                {
                    string block = json.Substring(blockStart, blockEnd - blockStart + 1);
                    string id = ExtractJsonField(block, "id");
                    string name = ExtractJsonField(block, "name");
                    string diamondsStr = ExtractJsonField(block, "diamonds");
                    string iconUrl = ExtractJsonField(block, "icon_url");

                    int diamonds = 1;
                    int.TryParse(diamondsStr, out diamonds);

                    if (!string.IsNullOrEmpty(name))
                    {
                        var existing = Catalog.Find(x => x.name.Equals(name, StringComparison.OrdinalIgnoreCase) || x.id == id);
                        if (existing != null)
                        {
                            if (string.IsNullOrEmpty(existing.iconUrl) && !string.IsNullOrEmpty(iconUrl))
                            {
                                existing.iconUrl = FixCdnUrl(iconUrl);
                            }
                        }
                        else
                        {
                            string th = GuessThaiName(name);
                            Catalog.Add(new GiftCatalogEntry(id, name, th, diamonds, FixCdnUrl(iconUrl), new Color(0.4f, 0.6f, 0.9f)));
                            added++;
                        }
                    }
                    idx = blockEnd + 1;
                }
                else
                {
                    idx += 5;
                }
            }

            Debug.Log($"[TikTokGiftDictionary] Initialized catalog. Total items: {Catalog.Count} (+{added} from cache).");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[TikTokGiftDictionary] Cache loading error: {ex.Message}");
        }
    }

    private static string FixCdnUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return "";
        if (url.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
        {
            return url.Substring(0, url.Length - 5) + ".png";
        }
        return url;
    }

    private static string ExtractJsonField(string block, string key)
    {
        string pattern = $"\"{key}\":";
        int idx = block.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return "";
        int start = idx + pattern.Length;
        int end = block.IndexOfAny(new[] { ',', '\n', '}' }, start);
        if (end < 0) end = block.Length;
        string val = block.Substring(start, end - start).Trim().Trim('\"');
        return val;
    }

    private static string GuessThaiName(string name)
    {
        string lower = name.ToLower();
        if (lower.Contains("rose")) return "กุหลาบ";
        if (lower.Contains("heart")) return "หัวใจ";
        if (lower.Contains("bear")) return "หมี";
        if (lower.Contains("car")) return "รถ";
        if (lower.Contains("dog") || lower.Contains("corgi")) return "สุนัข";
        if (lower.Contains("cat")) return "แมว";
        if (lower.Contains("lion")) return "สิงโต";
        if (lower.Contains("dragon")) return "มังกร";
        if (lower.Contains("crown")) return "มงกุฎ";
        if (lower.Contains("star")) return "ดาว";
        if (lower.Contains("flower")) return "ดอกไม้";
        return name;
    }

    public static List<TikTokGiftInfo> GetDefaultGifts(int limit = 20)
    {
        EnsureInitialized();
        int cap = Mathf.Clamp(limit, 1, Catalog.Count);
        var result = new List<TikTokGiftInfo>(cap);
        for (int i = 0; i < cap; i++)
        {
            result.Add(Catalog[i].ToGiftInfo());
        }
        return result;
    }

    public static List<TikTokGiftInfo> SearchGifts(string query, int limit = 10)
    {
        EnsureInitialized();
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

        if (nameLower == q || thLower == q) return 100;
        if (nameLower.StartsWith(q) || thLower.StartsWith(q)) return 80;
        if (nameLower.Contains(q)) return 60;
        if (thLower.Contains(q)) return 55;
        if (diamondStr == q || diamondStr.StartsWith(q)) return 40;

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
        EnsureInitialized();
        if (string.IsNullOrEmpty(giftName)) return null;
        return Catalog.Find(x => x.name.Equals(giftName, StringComparison.OrdinalIgnoreCase) ||
                                (x.thName != null && x.thName.Equals(giftName, StringComparison.OrdinalIgnoreCase)));
    }

    public static string GetThaiName(string giftName)
    {
        var e = FindEntry(giftName);
        return e != null ? e.thName : giftName;
    }

    public static string GetIconUrl(string giftName)
    {
        var e = FindEntry(giftName);
        return e != null ? e.iconUrl : "";
    }

    public static Color GetBadgeColor(string giftName)
    {
        var e = FindEntry(giftName);
        return e != null ? e.badgeColor : new Color(0.25f, 0.45f, 0.85f);
    }
}
