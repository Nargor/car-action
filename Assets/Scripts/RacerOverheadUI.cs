using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RacerOverheadUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Canvas worldCanvas;
    public RawImage avatarImage;
    public Image avatarBorder;
    public Image avatarMaskImage;
    public TextMeshProUGUI avatarInitialsText;

    public RectTransform rankBadgeRoot;
    public Image rankBadgeBorder;
    public Image rankBadgeBg;
    public TextMeshProUGUI rankBadgeText;

    public TextMeshProUGUI nameText;
    public Image nameBadgeBg;

    public GameObject nitroBadge;
    public GameObject prankBadge;
    public TextMeshProUGUI prankText;

    [Header("Settings")]
    public Vector3 offset = new Vector3(0f, 2.2f, 0f);

    private Transform targetCar;
    private Camera mainCam;
    private int currentRank = -1;
    private Color racerColor = Color.white;
    private string currentUsername = "";
    private Coroutine rankPunchCoroutine;

    // Static procedural asset caches
    private static Sprite cachedCircleSprite;
    private static Sprite cachedPillSprite;
    private static TMP_FontAsset cachedFont;

    void Awake()
    {
        EnsureUIHierarchy();
    }

    void Start()
    {
        if (targetCar == null && transform.parent != null)
        {
            targetCar = transform.parent;
        }
        mainCam = Camera.main;

        if (nitroBadge != null) nitroBadge.SetActive(false);
        if (prankBadge != null) prankBadge.SetActive(false);
    }

    void LateUpdate()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        // Smoothly position above the target car
        if (targetCar != null)
        {
            transform.position = targetCar.position + offset;
        }

        // Billboard: always face the camera directly without skew
        transform.rotation = mainCam.transform.rotation;
    }

    public void SetTarget(Transform car)
    {
        targetCar = car;
    }

    public void SetRacer(string username, Color carColor, Texture2D avatarTex = null)
    {
        EnsureUIHierarchy();

        currentUsername = username;
        racerColor = carColor;

        if (nameText != null)
        {
            nameText.text = username;
        }

        if (avatarBorder != null)
        {
            avatarBorder.color = carColor;
        }

        if (avatarTex != null)
        {
            SetAvatar(avatarTex);
        }
        else
        {
            // Show initial letter with colored background
            if (avatarInitialsText != null)
            {
                string initial = !string.IsNullOrEmpty(username) ? username.Substring(0, 1).ToUpper() : "?";
                avatarInitialsText.text = initial;
                avatarInitialsText.gameObject.SetActive(true);
            }
            if (avatarImage != null)
            {
                avatarImage.gameObject.SetActive(false);
            }
        }
    }

    public void SetAvatar(Texture2D tex)
    {
        EnsureUIHierarchy();

        if (avatarImage != null && tex != null)
        {
            avatarImage.texture = tex;
            avatarImage.gameObject.SetActive(true);

            if (avatarInitialsText != null)
            {
                avatarInitialsText.gameObject.SetActive(false);
            }
        }
    }

    public void SetRank(int rank)
    {
        EnsureUIHierarchy();

        if (currentRank == rank) return;
        int prevRank = currentRank;
        currentRank = rank;

        if (rankBadgeText != null)
        {
            rankBadgeText.text = rank.ToString();
        }

        // Medal colors based on rank
        Color bgColor;
        Color borderColor;
        Color textColor;

        switch (rank)
        {
            case 1:
                bgColor = new Color(1.0f, 0.843f, 0.0f);     // Shiny Gold #FFD700
                borderColor = new Color(1.0f, 0.95f, 0.46f); // Bright Gold #FFF275
                textColor = new Color(0.12f, 0.08f, 0.0f);   // Dark Gold/Black
                break;
            case 2:
                bgColor = new Color(0.886f, 0.91f, 0.941f);  // Silver #E2E8F0
                borderColor = Color.white;
                textColor = new Color(0.06f, 0.09f, 0.16f);  // Slate Dark
                break;
            case 3:
                bgColor = new Color(0.804f, 0.498f, 0.196f); // Bronze #CD7F32
                borderColor = new Color(0.965f, 0.678f, 0.333f);
                textColor = Color.white;
                break;
            default:
                bgColor = new Color(0.06f, 0.09f, 0.16f, 0.92f); // Deep Titanium Slate #0F172A
                borderColor = new Color(0.22f, 0.74f, 0.97f, 0.85f); // Neon Cyan #38BDF8
                textColor = Color.white;
                break;
        }

        if (rankBadgeBg != null) rankBadgeBg.color = bgColor;
        if (rankBadgeBorder != null) rankBadgeBorder.color = borderColor;
        if (rankBadgeText != null) rankBadgeText.color = textColor;

        // Visual punch on rank overtake
        if (prevRank > 0 && prevRank != rank && gameObject.activeInHierarchy)
        {
            if (rankPunchCoroutine != null) StopCoroutine(rankPunchCoroutine);
            rankPunchCoroutine = StartCoroutine(PunchRankBadge());
        }
    }

    private IEnumerator PunchRankBadge()
    {
        if (rankBadgeRoot == null) yield break;
        Vector3 origScale = Vector3.one;
        Vector3 targetScale = Vector3.one * 1.35f;

        float t = 0f;
        while (t < 0.12f)
        {
            t += Time.deltaTime;
            rankBadgeRoot.localScale = Vector3.Lerp(origScale, targetScale, t / 0.12f);
            yield return null;
        }

        t = 0f;
        while (t < 0.18f)
        {
            t += Time.deltaTime;
            rankBadgeRoot.localScale = Vector3.Lerp(targetScale, origScale, t / 0.18f);
            yield return null;
        }
        rankBadgeRoot.localScale = origScale;
    }

    public void ShowNitro(bool active)
    {
        if (nitroBadge != null)
            nitroBadge.SetActive(active);
    }

    public void ShowPrank(string prankType, bool active)
    {
        if (prankBadge != null)
        {
            prankBadge.SetActive(active);
            if (prankText != null && active)
            {
                prankText.text = prankType.ToUpper() + "!";
            }
        }
    }

    // =====================================================
    // PROCEDURAL WORLD-SPACE UI HIERARCHY BUILDER
    // =====================================================
    private void EnsureUIHierarchy()
    {
        if (worldCanvas != null && avatarImage != null && rankBadgeText != null)
            return;

        // 1. Root Canvas
        worldCanvas = GetComponent<Canvas>();
        if (worldCanvas == null)
            worldCanvas = gameObject.AddComponent<Canvas>();

        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.worldCamera = Camera.main;
        worldCanvas.sortingOrder = 50;

        var scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100f;

        var rt = GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200f, 200f);
        rt.localScale = new Vector3(0.012f, 0.012f, 0.012f);

        Sprite circle = GetCircleSprite();
        Sprite pill = GetPillSprite();
        TMP_FontAsset font = GetTMPFont();

        // 2. Avatar Container (Centered at 0, 8)
        var avatarRoot = new GameObject("AvatarRoot", typeof(RectTransform));
        avatarRoot.transform.SetParent(transform, false);
        var rtAvatarRoot = avatarRoot.GetComponent<RectTransform>();
        rtAvatarRoot.anchoredPosition = new Vector2(0f, 8f);
        rtAvatarRoot.sizeDelta = new Vector2(76f, 76f);

        // 2a. Outer Ring Border (Image)
        var borderObj = new GameObject("AvatarBorder", typeof(RectTransform), typeof(Image));
        borderObj.transform.SetParent(avatarRoot.transform, false);
        var rtBorder = borderObj.GetComponent<RectTransform>();
        rtBorder.sizeDelta = new Vector2(76f, 76f);
        avatarBorder = borderObj.GetComponent<Image>();
        avatarBorder.sprite = circle;
        avatarBorder.color = racerColor;

        // 2b. Circle Mask Object (Image + Mask)
        var maskObj = new GameObject("AvatarMask", typeof(RectTransform), typeof(Image), typeof(Mask));
        maskObj.transform.SetParent(avatarRoot.transform, false);
        var rtMask = maskObj.GetComponent<RectTransform>();
        rtMask.sizeDelta = new Vector2(68f, 68f);
        avatarMaskImage = maskObj.GetComponent<Image>();
        avatarMaskImage.sprite = circle;
        avatarMaskImage.color = new Color(0.12f, 0.16f, 0.23f, 1f); // Dark Slate inside circle
        var mask = maskObj.GetComponent<Mask>();
        mask.showMaskGraphic = true;

        // 2c. Fallback Initials inside Mask
        var initialsObj = new GameObject("AvatarInitials", typeof(RectTransform), typeof(TextMeshProUGUI));
        initialsObj.transform.SetParent(maskObj.transform, false);
        var rtInitials = initialsObj.GetComponent<RectTransform>();
        rtInitials.sizeDelta = new Vector2(68f, 68f);
        avatarInitialsText = initialsObj.GetComponent<TextMeshProUGUI>();
        avatarInitialsText.text = "?";
        avatarInitialsText.fontSize = 32f;
        avatarInitialsText.fontStyle = FontStyles.Bold;
        avatarInitialsText.alignment = TextAlignmentOptions.Center;
        avatarInitialsText.color = Color.white;
        if (font != null) avatarInitialsText.font = font;

        // 2d. RawImage for downloaded texture inside Mask
        var rawObj = new GameObject("AvatarRawImage", typeof(RectTransform), typeof(RawImage));
        rawObj.transform.SetParent(maskObj.transform, false);
        var rtRaw = rawObj.GetComponent<RectTransform>();
        rtRaw.sizeDelta = new Vector2(68f, 68f);
        avatarImage = rawObj.GetComponent<RawImage>();
        avatarImage.gameObject.SetActive(false);

        // 3. Rank Badge ("บนตัวรถ แสดงอันดับตัวเลขบินตามรถไปด้วย")
        // Positioned at top-left of the avatar circle: (-28, 38)
        var badgeRootObj = new GameObject("RankBadgeRoot", typeof(RectTransform));
        badgeRootObj.transform.SetParent(transform, false);
        rankBadgeRoot = badgeRootObj.GetComponent<RectTransform>();
        rankBadgeRoot.anchoredPosition = new Vector2(-28f, 38f);
        rankBadgeRoot.sizeDelta = new Vector2(34f, 34f);

        // Rank Border (36x36)
        var rBorderObj = new GameObject("RankBorder", typeof(RectTransform), typeof(Image));
        rBorderObj.transform.SetParent(badgeRootObj.transform, false);
        var rtRBorder = rBorderObj.GetComponent<RectTransform>();
        rtRBorder.sizeDelta = new Vector2(36f, 36f);
        rankBadgeBorder = rBorderObj.GetComponent<Image>();
        rankBadgeBorder.sprite = circle;
        rankBadgeBorder.color = Color.white;

        // Rank Bg (32x32)
        var rBgObj = new GameObject("RankBg", typeof(RectTransform), typeof(Image));
        rBgObj.transform.SetParent(badgeRootObj.transform, false);
        var rtRBg = rBgObj.GetComponent<RectTransform>();
        rtRBg.sizeDelta = new Vector2(32f, 32f);
        rankBadgeBg = rBgObj.GetComponent<Image>();
        rankBadgeBg.sprite = circle;
        rankBadgeBg.color = new Color(1.0f, 0.843f, 0.0f); // Default Gold

        // Rank Text (TextMeshProUGUI)
        var rTextObj = new GameObject("RankText", typeof(RectTransform), typeof(TextMeshProUGUI));
        rTextObj.transform.SetParent(badgeRootObj.transform, false);
        var rtRText = rTextObj.GetComponent<RectTransform>();
        rtRText.sizeDelta = new Vector2(32f, 32f);
        rankBadgeText = rTextObj.GetComponent<TextMeshProUGUI>();
        rankBadgeText.text = "1";
        rankBadgeText.fontSize = 20f;
        rankBadgeText.fontStyle = FontStyles.Bold;
        rankBadgeText.alignment = TextAlignmentOptions.Center;
        rankBadgeText.color = new Color(0.12f, 0.08f, 0f);
        if (font != null) rankBadgeText.font = font;

        // 4. Name Badge (Positioned at 0, -34)
        var nameRootObj = new GameObject("NameBadgeRoot", typeof(RectTransform));
        nameRootObj.transform.SetParent(transform, false);
        var rtNameRoot = nameRootObj.GetComponent<RectTransform>();
        rtNameRoot.anchoredPosition = new Vector2(0f, -34f);
        rtNameRoot.sizeDelta = new Vector2(140f, 24f);

        // Nameplate Background Pill
        var nameBgObj = new GameObject("NameBg", typeof(RectTransform), typeof(Image));
        nameBgObj.transform.SetParent(nameRootObj.transform, false);
        var rtNameBg = nameBgObj.GetComponent<RectTransform>();
        rtNameBg.sizeDelta = new Vector2(140f, 24f);
        nameBadgeBg = nameBgObj.GetComponent<Image>();
        nameBadgeBg.sprite = pill;
        nameBadgeBg.type = Image.Type.Sliced;
        nameBadgeBg.color = new Color(0.06f, 0.09f, 0.16f, 0.88f);

        // Nameplate Text
        var nameTxtObj = new GameObject("NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameTxtObj.transform.SetParent(nameRootObj.transform, false);
        var rtNameTxt = nameTxtObj.GetComponent<RectTransform>();
        rtNameTxt.sizeDelta = new Vector2(130f, 22f);
        nameText = nameTxtObj.GetComponent<TextMeshProUGUI>();
        nameText.text = !string.IsNullOrEmpty(currentUsername) ? currentUsername : "RACER";
        nameText.fontSize = 13f;
        nameText.fontStyle = FontStyles.Bold;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = Color.white;
        nameText.overflowMode = TextOverflowModes.Ellipsis;
        if (font != null) nameText.font = font;

        // 5. Nitro Badge (Positioned at 0, 54)
        nitroBadge = new GameObject("NitroBadge", typeof(RectTransform), typeof(Image));
        nitroBadge.transform.SetParent(transform, false);
        var rtNitro = nitroBadge.GetComponent<RectTransform>();
        rtNitro.anchoredPosition = new Vector2(0f, 54f);
        rtNitro.sizeDelta = new Vector2(100f, 22f);
        var imgNitro = nitroBadge.GetComponent<Image>();
        imgNitro.sprite = pill;
        imgNitro.type = Image.Type.Sliced;
        imgNitro.color = new Color(0.92f, 0.70f, 0.03f, 0.95f); // Amber Yellow

        var nitroTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        nitroTxtObj.transform.SetParent(nitroBadge.transform, false);
        var rtNitroTxt = nitroTxtObj.GetComponent<RectTransform>();
        rtNitroTxt.sizeDelta = new Vector2(96f, 20f);
        var nTxt = nitroTxtObj.GetComponent<TextMeshProUGUI>();
        nTxt.text = "⚡ NITRO!";
        nTxt.fontSize = 13f;
        nTxt.fontStyle = FontStyles.Bold;
        nTxt.alignment = TextAlignmentOptions.Center;
        nTxt.color = Color.black;
        if (font != null) nTxt.font = font;
        nitroBadge.SetActive(false);

        // 6. Prank Badge (Positioned at 0, 54)
        prankBadge = new GameObject("PrankBadge", typeof(RectTransform), typeof(Image));
        prankBadge.transform.SetParent(transform, false);
        var rtPrank = prankBadge.GetComponent<RectTransform>();
        rtPrank.anchoredPosition = new Vector2(0f, 54f);
        rtPrank.sizeDelta = new Vector2(100f, 22f);
        var imgPrank = prankBadge.GetComponent<Image>();
        imgPrank.sprite = pill;
        imgPrank.type = Image.Type.Sliced;
        imgPrank.color = new Color(0.93f, 0.27f, 0.27f, 0.95f); // Red

        var prankTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        prankTxtObj.transform.SetParent(prankBadge.transform, false);
        var rtPrankTxt = prankTxtObj.GetComponent<RectTransform>();
        rtPrankTxt.sizeDelta = new Vector2(96f, 20f);
        prankText = prankTxtObj.GetComponent<TextMeshProUGUI>();
        prankText.text = "🍌 SLIP!";
        prankText.fontSize = 13f;
        prankText.fontStyle = FontStyles.Bold;
        prankText.alignment = TextAlignmentOptions.Center;
        prankText.color = Color.white;
        if (font != null) prankText.font = font;
        prankBadge.SetActive(false);
    }

    // =====================================================
    // PROCEDURAL SPRITE & FONT HELPERS
    // =====================================================
    private static Sprite GetCircleSprite()
    {
        if (cachedCircleSprite != null) return cachedCircleSprite;

        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] cols = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = (size / 2f) - 1.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                cols[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        cachedCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return cachedCircleSprite;
    }

    private static Sprite GetPillSprite()
    {
        if (cachedPillSprite != null) return cachedPillSprite;

        int width = 64;
        int height = 32;
        int radius = 14;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] cols = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dist;
                if (x < radius)
                {
                    dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, height / 2f));
                }
                else if (x > width - radius)
                {
                    dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(width - radius, height / 2f));
                }
                else
                {
                    dist = Mathf.Abs((y + 0.5f) - (height / 2f));
                }

                float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                cols[y * width + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        // 9-slice border: left/right = 16, top/bottom = 14
        cachedPillSprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(16, 14, 16, 14));
        return cachedPillSprite;
    }

    private static TMP_FontAsset GetTMPFont()
    {
        if (cachedFont != null) return cachedFont;

#if UNITY_EDITOR
        cachedFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/ThaiFont_SDF.asset");
        if (cachedFont != null) return cachedFont;
#endif
        if (TMP_Settings.defaultFontAsset != null)
        {
            cachedFont = TMP_Settings.defaultFontAsset;
        }
        return cachedFont;
    }
}
