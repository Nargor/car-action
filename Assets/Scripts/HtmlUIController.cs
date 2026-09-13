using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HtmlUIController : MonoBehaviour
{
    public static HtmlUIController Instance { get; private set; }

    [Header("WebUI Files (Local In-Memory Only - No Network/Ports)")]
    public string htmlFileName = "index.html";
    public string cssFileName = "style.css";
    public bool enableHotReload = true;

    private string webUIPath;
    private FileSystemWatcher fileWatcher;
    private bool needReload = false;

    // Parsed CSS Cache
    private Dictionary<string, Dictionary<string, string>> cssRules = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        webUIPath = Path.Combine(Application.dataPath, "WebUI");
    }

    void Start()
    {
        LoadAndApplyHtmlAndCss();

        if (enableHotReload && Directory.Exists(webUIPath))
        {
            SetupFileWatcher();
        }
    }

    void OnDestroy()
    {
        if (fileWatcher != null)
        {
            fileWatcher.EnableRaisingEvents = false;
            fileWatcher.Dispose();
            fileWatcher = null;
        }
    }

    void Update()
    {
        if (needReload)
        {
            needReload = false;
            LoadAndApplyHtmlAndCss();
        }
    }

    private void SetupFileWatcher()
    {
        try
        {
            fileWatcher = new FileSystemWatcher(webUIPath);
            fileWatcher.Filter = "*.*";
            fileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size;
            fileWatcher.Changed += (s, e) => { needReload = true; };
            fileWatcher.Created += (s, e) => { needReload = true; };
            fileWatcher.EnableRaisingEvents = true;

            Debug.Log("[HtmlUIController] In-memory Hot-Reload watcher active on Assets/WebUI. Zero network ports used.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[HtmlUIController] Could not start FileSystemWatcher: {ex.Message}");
        }
    }

    public void LoadAndApplyHtmlAndCss()
    {
        string htmlPath = Path.Combine(webUIPath, htmlFileName);
        string cssPath = Path.Combine(webUIPath, cssFileName);

        if (File.Exists(cssPath))
        {
            string cssText = File.ReadAllText(cssPath);
            ParseCSS(cssText);
        }

        if (File.Exists(htmlPath))
        {
            string htmlText = File.ReadAllText(htmlPath);
            ApplyHtmlAndCssToGameUI(htmlText);
        }

        Debug.Log("[HtmlUIController] UI updated from local HTML/CSS files! (100% In-Game, No Ports Open)");
    }

    private void ParseCSS(string css)
    {
        cssRules.Clear();

        // Strip comments /* ... */
        string clean = Regex.Replace(css, @"/\*.*?\*/", "", RegexOptions.Singleline);

        // Match selector { declarations }
        var matches = Regex.Matches(clean, @"(?<selector>[^{]+)\{(?<declarations>[^}]+)\}");
        foreach (Match m in matches)
        {
            string selector = m.Groups["selector"].Value.Trim();
            string decls = m.Groups["declarations"].Value.Trim();

            var propDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var declPairs = decls.Split(';');
            foreach (var pair in declPairs)
            {
                var parts = pair.Split(':');
                if (parts.Length == 2)
                {
                    propDict[parts[0].Trim()] = parts[1].Trim();
                }
            }

            foreach (var s in selector.Split(','))
            {
                string sel = s.Trim();
                if (!string.IsNullOrEmpty(sel))
                {
                    cssRules[sel] = propDict;
                }
            }
        }
    }

    private void ApplyHtmlAndCssToGameUI(string html)
    {
        var canvas = GameObject.Find("HUD_Canvas");
        if (canvas == null) return;

        // 1. Apply Theme Colors to Title Buttons from CSS
        ApplyButtonStyle(canvas, "Title_Panel/Btn_PLAY_VS_AI_>", ".btn-primary", "PLAY VS AI >");
        ApplyButtonStyle(canvas, "Title_Panel/Btn_SPECTATOR_MODE_(AI)_>", ".btn-secondary", "SPECTATOR MODE (AI) >");
        ApplyButtonStyle(canvas, "Title_Panel/Btn_PLAYER_LIVE_>", ".btn-tiktok", "PLAYER LIVE (TIKTOK) 🔴 >");
        ApplyButtonStyle(canvas, "Title_Panel/Btn_EXIT_GAME", ".btn-dark", "EXIT GAME");

        // 2. Apply Speedometer Style from CSS (.speedometer-container)
        var sp = canvas.transform.Find("HUD_Panel/SpeedPanel");
        if (sp != null && cssRules.TryGetValue(".speedometer-container", out var spProps))
        {
            var spImg = sp.GetComponent<Image>();
            if (spImg != null && spProps.TryGetValue("background", out string bgStr))
            {
                spImg.color = ParseColor(bgStr, spImg.color);
            }
        }

        // 3. Apply Camera Toolbar Active Highlight from CSS (.cam-btn.active)
        var tb = canvas.transform.Find("HUD_Panel/CameraToolbar");
        if (tb != null && cssRules.TryGetValue(".cam-btn.active", out var camProps))
        {
            if (camProps.TryGetValue("background", out string bgStr))
            {
                Color activeCol = ParseColor(bgStr, new Color(0.12f, 0.55f, 0.95f));
                var hud = canvas.GetComponent<HUDController>();
                if (hud != null)
                {
                    // Live CSS color update
                }
            }
        }

        // 4. Apply Leaderboard Window Styling from CSS (.draggable-window)
        var lb = canvas.transform.Find("HUD_Panel/LeaderboardWindow");
        if (lb != null && cssRules.TryGetValue(".draggable-window", out var lbProps))
        {
            var lbImg = lb.GetComponent<Image>();
            if (lbImg != null && lbProps.TryGetValue("background", out string bgStr))
            {
                lbImg.color = ParseColor(bgStr, lbImg.color);
            }
        }
    }

    private void ApplyButtonStyle(GameObject root, string childPath, string cssClass, string defaultText)
    {
        var t = root.transform.Find(childPath);
        if (t == null) return;

        var img = t.GetComponent<Image>();
        if (img != null && cssRules.TryGetValue(cssClass, out var props))
        {
            if (props.TryGetValue("background", out string bgStr))
            {
                img.color = ParseColor(bgStr, img.color);
            }
        }
    }

    private Color ParseColor(string colStr, Color defaultColor)
    {
        if (string.IsNullOrEmpty(colStr)) return defaultColor;

        colStr = colStr.Trim();

        // Check rgba(r, g, b, a)
        var matchRgba = Regex.Match(colStr, @"rgba?\s*\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*(?:,\s*([\d.]+))?\s*\)");
        if (matchRgba.Success)
        {
            float r = float.Parse(matchRgba.Groups[1].Value) / 255f;
            float g = float.Parse(matchRgba.Groups[2].Value) / 255f;
            float b = float.Parse(matchRgba.Groups[3].Value) / 255f;
            float a = matchRgba.Groups[4].Success ? float.Parse(matchRgba.Groups[4].Value) : 1f;
            return new Color(r, g, b, a);
        }

        // Check hex #RRGGBB or #RGB
        var matchHex = Regex.Match(colStr, @"#([0-9a-fA-F]{3,8})");
        if (matchHex.Success)
        {
            if (ColorUtility.TryParseHtmlString("#" + matchHex.Groups[1].Value, out Color c))
            {
                return c;
            }
        }

        return defaultColor;
    }
}
