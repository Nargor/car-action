using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class TikTokJoinPanelManager : MonoBehaviour
{
    public static TikTokJoinPanelManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var all = Resources.FindObjectsOfTypeAll<TikTokJoinPanelManager>();
                if (all != null && all.Length > 0) _instance = all[0];
            }
            return _instance;
        }
    }
    private static TikTokJoinPanelManager _instance;

    [Header("Main Panel References")]
    public GameObject joinPanel;
    public TextMeshProUGUI headerTitleText;
    public TextMeshProUGUI joinedCountText;
    public TextMeshProUGUI viewingTargetText;
    public RectTransform racerListContainer;
    public GameObject racerRowTemplate;

    [Header("Action Buttons")]
    public Button addBotButton;
    public Button startRaceButton;
    public Button closeButton;

    [Header("Hotkey References")]
    public GameObject leaderboardWindow;
    public GameObject cameraToolbar;

    private List<GameObject> activeRowObjects = new List<GameObject>();

    void Awake()
    {
        if (_instance == null) _instance = this;
    }

    void Start()
    {
        if (racerRowTemplate != null)
            racerRowTemplate.SetActive(false);

        if (addBotButton != null)
        {
            addBotButton.onClick.RemoveAllListeners();
            addBotButton.onClick.AddListener(OnAddBotClicked);
        }

        if (startRaceButton != null)
        {
            startRaceButton.onClick.RemoveAllListeners();
            startRaceButton.onClick.AddListener(OnStartRaceClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => SetJoinPanelVisible(false));
        }

        // Hook into TikTokLiveManager event
        if (TikTokLiveManager.Instance != null)
        {
            TikTokLiveManager.Instance.OnRacersChanged += RefreshRacerList;
        }
    }

    void OnDestroy()
    {
        if (TikTokLiveManager.Instance != null)
        {
            TikTokLiveManager.Instance.OnRacersChanged -= RefreshRacerList;
        }
    }

    void Update()
    {
        HandleHotkeys();
    }

    // ==========================================
    // HOTKEYS: F7 (Join Panel), F6 / F4 (Leaderboard & Camera)
    // ==========================================
    private void HandleHotkeys()
    {
        bool f7 = false;
        bool f6 = false;
        bool f4 = false;

        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.f7Key.wasPressedThisFrame) f7 = true;
            if (kb.f6Key.wasPressedThisFrame) f6 = true;
            if (kb.f4Key.wasPressedThisFrame) f4 = true;
        }

        try
        {
            if (!f7) f7 = Input.GetKeyDown(KeyCode.F7);
            if (!f6) f6 = Input.GetKeyDown(KeyCode.F6);
            if (!f4) f4 = Input.GetKeyDown(KeyCode.F4);
        }
        catch { }

        // F7: Toggle In-Track Join Panel
        if (f7 && joinPanel != null)
        {
            // Only toggle if in TikTok Live mode
            if (RaceManager.Instance != null && RaceManager.Instance.selectedGameMode == RaceManager.GameMode.TikTokLive)
            {
                SetJoinPanelVisible(!joinPanel.activeSelf);
            }
        }

        // F6: Open Leaderboard + Show Camera Toolbar
        if (f6)
        {
            SetLeaderboardAndCameraVisible(true);
        }

        // F4: Close Leaderboard
        if (f4)
        {
            SetLeaderboardAndCameraVisible(false);
        }
    }

    public void SetJoinPanelVisible(bool visible)
    {
        if (joinPanel != null)
        {
            joinPanel.SetActive(visible);
            if (visible)
            {
                RefreshRacerList();
            }
        }
    }

    public void SetLeaderboardAndCameraVisible(bool visible)
    {
        if (leaderboardWindow != null)
        {
            leaderboardWindow.SetActive(visible);
            if (visible)
            {
                var lb = leaderboardWindow.GetComponent<LeaderboardUI>();
                if (lb != null) lb.RefreshLeaderboard();
            }
        }

        if (cameraToolbar != null)
        {
            cameraToolbar.SetActive(visible);
        }
    }

    // ==========================================
    // REFRESH IN-TRACK RACER LIST
    // ==========================================
    public void RefreshRacerList()
    {
        if (TikTokLiveManager.Instance == null || racerListContainer == null || racerRowTemplate == null)
            return;

        var racers = TikTokLiveManager.Instance.joinedRacers;
        int count = racers.Count;

        if (headerTitleText != null)
        {
            string host = TikTokLiveManager.Instance.streamerUsername;
            headerTitleText.text = $"<b><color=#00FF88>●</color> LIVE: @{host}</b>";
        }

        if (joinedCountText != null)
        {
            joinedCountText.text = $"RACERS: <color=#FFD700><b>{count}</b></color> / {TikTokLiveManager.Instance.maxRacers}";
        }

        // Ensure enough row items
        while (activeRowObjects.Count < count)
        {
            GameObject newRow = Instantiate(racerRowTemplate, racerListContainer);
            activeRowObjects.Add(newRow);
        }

        for (int i = 0; i < activeRowObjects.Count; i++)
        {
            if (i < count)
            {
                var r = racers[i];
                var row = activeRowObjects[i];
                row.SetActive(true);

                // Slot number
                var slotTxt = row.transform.Find("SlotText")?.GetComponent<TextMeshProUGUI>();
                if (slotTxt != null) slotTxt.text = $"#{i + 1:D2}";

                // Name
                var nameTxt = row.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
                if (nameTxt != null)
                {
                    bool isBot = r.username.StartsWith("bot_", System.StringComparison.OrdinalIgnoreCase);
                    nameTxt.text = isBot 
                        ? $"<color=#A0A5BD>[BOT] {r.username}</color>" 
                        : $"<b><color=#00FFFF>{r.username}</color></b>";
                }

                // Color swatch
                var swatch = row.transform.Find("ColorSwatch")?.GetComponent<Image>();
                if (swatch != null) swatch.color = r.carColor;

                // Click to focus camera on this racer
                var btn = row.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    Transform targetCar = r.carObject != null ? r.carObject.transform : null;
                    string uName = r.username;
                    btn.onClick.AddListener(() =>
                    {
                        FocusCameraOnRacer(targetCar, uName);
                    });
                }
            }
            else
            {
                activeRowObjects[i].SetActive(false);
            }
        }
    }

    public void FocusCameraOnRacer(Transform carTransform, string racerName)
    {
        if (carTransform != null && ChaseCameraController.Instance != null)
        {
            ChaseCameraController.Instance.SetTarget(carTransform);
            Debug.Log($"[Camera] Switched camera focus to: {racerName}");

            if (viewingTargetText != null)
            {
                viewingTargetText.text = $"<size=80%>กำลังดูกล้อง:</size> <color=#FFD700><b>{racerName}</b></color>";
            }
        }
    }

    private void OnAddBotClicked()
    {
        if (TikTokLiveManager.Instance != null)
        {
            TikTokLiveManager.Instance.AddBotRacer();
        }
    }

    private void OnStartRaceClicked()
    {
        // Hide join panel and start race!
        SetJoinPanelVisible(false);

        if (TikTokLiveManager.Instance != null)
        {
            TikTokLiveManager.Instance.OnStartRace();
        }
    }
}
