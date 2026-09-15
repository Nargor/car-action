using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

public class GiftIconCache : MonoBehaviour
{
    public static GiftIconCache Instance
    {
        get
        {
            if (_instance == null)
            {
                var all = Resources.FindObjectsOfTypeAll<GiftIconCache>();
                if (all != null && all.Length > 0) _instance = all[0];
                if (_instance == null)
                {
                    var go = new GameObject("GiftIconCache");
                    _instance = go.AddComponent<GiftIconCache>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    private static GiftIconCache _instance;

    public static event Action<string, Sprite> OnIconLoaded;

    private Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> pendingDownloads = new HashSet<string>();

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this && transform.parent == null)
        {
            Destroy(gameObject);
            return;
        }

        EnsureDiskCacheDirectory();
    }

    private void EnsureDiskCacheDirectory()
    {
        try
        {
            string dir = Path.Combine(Application.persistentDataPath, "GiftIcons");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GiftIconCache] Could not create disk cache dir: {ex.Message}");
        }
    }

    private string GetDiskCachePath(string key)
    {
        string dir = Path.Combine(Application.persistentDataPath, "GiftIcons");
        string safeKey = Regex.Replace(key, @"[^a-zA-Z0-9_\-]", "_");
        return Path.Combine(dir, safeKey + ".png");
    }

    public static Sprite GetSprite(string giftName, string iconUrl = "")
    {
        return Instance.GetGiftSprite(giftName, iconUrl);
    }

    public Sprite GetGiftSprite(string giftName, string iconUrl = "")
    {
        if (string.IsNullOrEmpty(giftName)) giftName = "Gift";
        string key = giftName.Trim().ToLower();

        // 1. In-memory cache check
        if (spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
        {
            return cached;
        }

        // 2. Persistent Disk Cache check
        string diskPath = GetDiskCachePath(key);
        if (File.Exists(diskPath))
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(diskPath);
                Texture2D diskTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (diskTex.LoadImage(bytes))
                {
                    diskTex.filterMode = FilterMode.Bilinear;
                    Sprite diskSprite = Sprite.Create(diskTex, new Rect(0, 0, diskTex.width, diskTex.height), new Vector2(0.5f, 0.5f));
                    spriteCache[key] = diskSprite;
                    return diskSprite;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GiftIconCache] Disk cache read error: {ex.Message}");
            }
        }

        // 3. Fallback URL lookup if not explicitly passed
        if (string.IsNullOrEmpty(iconUrl))
        {
            iconUrl = TikTokGiftDictionary.GetIconUrl(giftName);
        }

        // 4. Procedural fallback sprite so UI is NEVER blank or pink
        Sprite proceduralSprite = GenerateProceduralBadge(giftName);
        spriteCache[key] = proceduralSprite;

        // 5. Download in background if valid remote URL
        if (!string.IsNullOrEmpty(iconUrl) && !pendingDownloads.Contains(iconUrl) &&
            (iconUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || iconUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
        {
            StartCoroutine(DownloadIconRoutine(key, iconUrl));
        }

        return proceduralSprite;
    }

    private IEnumerator DownloadIconRoutine(string key, string url)
    {
        pendingDownloads.Add(url);

        // Normalize URL: TikTok CDN supports .png directly for all webp endpoints
        string fetchUrl = url;
        if (fetchUrl.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
        {
            fetchUrl = fetchUrl.Substring(0, fetchUrl.Length - 5) + ".png";
        }

        using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(fetchUrl))
        {
            req.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            req.timeout = 8;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var tex = DownloadHandlerTexture.GetContent(req);
                if (tex != null)
                {
                    tex.filterMode = FilterMode.Bilinear;
                    var newSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    spriteCache[key] = newSprite;

                    // Save to persistent disk cache
                    try
                    {
                        string diskPath = GetDiskCachePath(key);
                        byte[] pngBytes = tex.EncodeToPNG();
                        if (pngBytes != null && pngBytes.Length > 0)
                        {
                            File.WriteAllBytes(diskPath, pngBytes);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[GiftIconCache] Failed to write disk cache for {key}: {ex.Message}");
                    }

                    // Notify UI listeners
                    OnIconLoaded?.Invoke(key, newSprite);
                }
            }
            else
            {
                // Fallback to original url if png failed
                if (fetchUrl != url)
                {
                    using (UnityWebRequest reqOriginal = UnityWebRequestTexture.GetTexture(url))
                    {
                        reqOriginal.timeout = 8;
                        yield return reqOriginal.SendWebRequest();
                        if (reqOriginal.result == UnityWebRequest.Result.Success)
                        {
                            var tex2 = DownloadHandlerTexture.GetContent(reqOriginal);
                            if (tex2 != null)
                            {
                                var s2 = Sprite.Create(tex2, new Rect(0, 0, tex2.width, tex2.height), new Vector2(0.5f, 0.5f));
                                spriteCache[key] = s2;
                                OnIconLoaded?.Invoke(key, s2);
                            }
                        }
                    }
                }
            }
        }
        pendingDownloads.Remove(url);
    }

    private Sprite GenerateProceduralBadge(string giftName)
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color baseColor = TikTokGiftDictionary.GetBadgeColor(giftName);
        Color borderColor = Color.Lerp(baseColor, Color.white, 0.45f);
        Color darkBg = Color.Lerp(baseColor, Color.black, 0.55f);

        float center = size * 0.5f;
        float radius = size * 0.46f;
        float innerRadius = radius - 3.5f;

        string nameLower = giftName.ToLower();

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist > radius + 1f)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                float outerAlpha = Mathf.Clamp01(radius + 1f - dist);

                if (dist >= innerRadius)
                {
                    Color c = borderColor;
                    c.a *= outerAlpha;
                    tex.SetPixel(x, y, c);
                }
                else
                {
                    float t = dist / innerRadius;
                    Color bg = Color.Lerp(baseColor, darkBg, t * 0.85f);
                    float symbolAlpha = GetSymbolMask(nameLower, x, y, size);
                    if (symbolAlpha > 0.05f)
                    {
                        bg = Color.Lerp(bg, Color.white, symbolAlpha * 0.95f);
                    }
                    bg.a = outerAlpha;
                    tex.SetPixel(x, y, bg);
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private float GetSymbolMask(string name, int x, int y, int size)
    {
        float u = (float)x / size;
        float v = (float)y / size;
        float nx = u * 2f - 1f;
        float ny = v * 2f - 1f;

        if (name.Contains("heart"))
        {
            float hx = nx * 1.35f;
            float hy = ny * 1.35f + 0.15f;
            float a = hx * hx + hy * hy - 0.45f;
            if (a * a * a - hx * hx * (hy * hy * hy) <= 0.02f && hy < 0.65f) return 1f;
        }
        else if (name.Contains("rose") || name.Contains("flower"))
        {
            float r = Mathf.Sqrt(nx * nx + ny * ny);
            float angle = Mathf.Atan2(ny, nx);
            float petal = Mathf.Sin(angle * 5f) * 0.15f + 0.35f;
            if (r < petal) return 1f;
            if (r < 0.12f) return 0.2f;
        }
        else if (name.Contains("car") || name.Contains("motorcycle"))
        {
            if (ny >= -0.2f && ny <= 0.15f && Mathf.Abs(nx) < 0.55f) return 1f;
            if (ny > 0.15f && ny <= 0.42f && nx >= -0.3f && nx <= 0.22f) return 1f;
            if (ny < -0.18f && (Mathf.Abs(nx - 0.35f) < 0.12f || Mathf.Abs(nx + 0.35f) < 0.12f)) return 0.2f;
        }
        else if (name.Contains("doughnut") || name.Contains("donut"))
        {
            float r = Mathf.Sqrt(nx * nx + ny * ny);
            if (r >= 0.22f && r <= 0.52f) return 1f;
        }
        else if (name.Contains("cap") || name.Contains("hat"))
        {
            float r = Mathf.Sqrt(nx * nx + (ny + 0.05f) * (ny + 0.05f));
            if (r < 0.42f && ny >= -0.1f) return 1f;
            if (ny >= -0.28f && ny < -0.08f && nx >= -0.45f && nx <= 0.55f) return 1f;
        }
        else
        {
            float diamond = Mathf.Abs(nx) + Mathf.Abs(ny);
            if (diamond <= 0.55f) return 1f;
        }

        return 0f;
    }
}
