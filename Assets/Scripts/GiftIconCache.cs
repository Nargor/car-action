using System;
using System.Collections;
using System.Collections.Generic;
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
    }

    public static Sprite GetSprite(string giftName, string iconUrl = "")
    {
        return Instance.GetGiftSprite(giftName, iconUrl);
    }

    public Sprite GetGiftSprite(string giftName, string iconUrl = "")
    {
        if (string.IsNullOrEmpty(giftName)) giftName = "Gift";

        string key = giftName.Trim().ToLower();
        if (spriteCache.TryGetValue(key, out Sprite cached) && cached != null)
        {
            return cached;
        }

        // Generate procedural fallback sprite first so UI is never blank
        Sprite proceduralSprite = GenerateProceduralBadge(giftName);
        spriteCache[key] = proceduralSprite;

        // If a remote URL is supplied, try downloading in the background
        if (!string.IsNullOrEmpty(iconUrl) && !pendingDownloads.Contains(iconUrl) && (iconUrl.StartsWith("http://") || iconUrl.StartsWith("https://")))
        {
            StartCoroutine(DownloadIconRoutine(key, iconUrl));
        }

        return proceduralSprite;
    }

    private IEnumerator DownloadIconRoutine(string key, string url)
    {
        pendingDownloads.Add(url);
        using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
        {
            req.timeout = 5;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var tex = DownloadHandlerTexture.GetContent(req);
                if (tex != null)
                {
                    var newSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    spriteCache[key] = newSprite;
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

                // Anti-aliased outer border
                float outerAlpha = Mathf.Clamp01(radius + 1f - dist);

                if (dist >= innerRadius)
                {
                    // Outer ring border
                    Color c = borderColor;
                    c.a *= outerAlpha;
                    tex.SetPixel(x, y, c);
                }
                else
                {
                    // Radial gradient background
                    float t = dist / innerRadius;
                    Color bg = Color.Lerp(baseColor, darkBg, t * 0.85f);

                    // Draw stylized internal glyph based on gift type
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
        float u = (float)x / size; // 0 to 1
        float v = (float)y / size; // 0 to 1
        float nx = u * 2f - 1f;    // -1 to 1
        float ny = v * 2f - 1f;    // -1 to 1

        if (name.Contains("heart"))
        {
            // Heart shape math: (x^2 + y^2 - 1)^3 - x^2 * y^3 <= 0
            float hx = nx * 1.35f;
            float hy = ny * 1.35f + 0.15f;
            float a = hx * hx + hy * hy - 0.45f;
            if (a * a * a - hx * hx * (hy * hy * hy) <= 0.02f && hy < 0.65f) return 1f;
        }
        else if (name.Contains("rose") || name.Contains("flower"))
        {
            // Rose blossom petals
            float r = Mathf.Sqrt(nx * nx + ny * ny);
            float angle = Mathf.Atan2(ny, nx);
            float petal = Mathf.Sin(angle * 5f) * 0.15f + 0.35f;
            if (r < petal) return 1f;
            if (r < 0.12f) return 0.2f;
        }
        else if (name.Contains("star") || name.Contains("firework") || name.Contains("universe"))
        {
            // 4-pointed sparkle
            float d = Mathf.Abs(nx) * Mathf.Abs(ny);
            float r = Mathf.Sqrt(nx * nx + ny * ny);
            if (r < 0.5f && (Mathf.Abs(nx) < 0.12f || Mathf.Abs(ny) < 0.12f || d < 0.025f)) return 1f;
        }
        else if (name.Contains("car") || name.Contains("motorcycle"))
        {
            // Sleek car / vehicle silhouette
            if (ny >= -0.2f && ny <= 0.15f && Mathf.Abs(nx) < 0.55f) return 1f;
            if (ny > 0.15f && ny <= 0.42f && nx >= -0.3f && nx <= 0.22f) return 1f;
            // Wheels
            if (ny < -0.18f && (Mathf.Abs(nx - 0.35f) < 0.12f || Mathf.Abs(nx + 0.35f) < 0.12f)) return 0.2f;
        }
        else if (name.Contains("crown") || name.Contains("castle") || name.Contains("lion"))
        {
            // 3-pointed crown
            if (ny >= -0.35f && ny <= 0.1f && Mathf.Abs(nx) < 0.48f) return 1f;
            if (ny > 0.1f && ny <= 0.48f)
            {
                if (Mathf.Abs(nx) < 0.12f || Mathf.Abs(nx - 0.38f) < 0.12f || Mathf.Abs(nx + 0.38f) < 0.12f) return 1f;
            }
        }
        else if (name.Contains("gun") || name.Contains("money"))
        {
            // Dollar sign $ or gun silhouette
            if (Mathf.Abs(nx) < 0.08f && Mathf.Abs(ny) < 0.52f) return 1f;
            if (ny > 0.12f && ny < 0.32f && nx > -0.3f && nx < 0.25f) return 1f;
            if (ny > -0.32f && ny < -0.12f && nx > -0.25f && nx < 0.3f) return 1f;
        }
        else if (name.Contains("doughnut") || name.Contains("donut") || name.Contains("ring"))
        {
            // Donut ring
            float r = Mathf.Sqrt(nx * nx + ny * ny);
            if (r >= 0.22f && r <= 0.52f) return 1f;
        }
        else if (name.Contains("cap") || name.Contains("hat"))
        {
            // Baseball cap shape
            float r = Mathf.Sqrt(nx * nx + (ny + 0.05f) * (ny + 0.05f));
            if (r < 0.42f && ny >= -0.1f) return 1f;
            if (ny >= -0.28f && ny < -0.08f && nx >= -0.45f && nx <= 0.55f) return 1f;
        }
        else
        {
            // Diamond / gem polygon default
            float diamond = Mathf.Abs(nx) + Mathf.Abs(ny);
            if (diamond <= 0.55f) return 1f;
        }

        return 0f;
    }
}
