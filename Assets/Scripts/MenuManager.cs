using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }

    [Header("Panels")]
    public GameObject titlePanel;
    public GameObject lobbyPanel;
    public GameObject hudPanel;
    public GameObject tiktokLobbyPanel;

    [Header("Title Screen Buttons")]
    public Button playVsAIButton;
    public Button spectatorButton;
    public Button playerLiveButton;
    public Button exitButton;

    [Header("Lobby Screen")]
    public TextMeshProUGUI lobbyHeaderTitle;
    public Button startRaceButton;
    public Button backButton;
    public Slider lapsSlider;
    public Slider aiCountSlider;
    public TextMeshProUGUI lapsValueText;
    public TextMeshProUGUI aiCountValueText;
    public TextMeshProUGUI totalRacersText;

    [Header("TikTok Live Setup Form UI")]
    public TMP_InputField tiktokUsernameInput;
    public Button createTikTokRoomButton;
    public Button backFromTikTokButton;
    public TextMeshProUGUI tiktokStatusText;
    public Slider tiktokLapsSlider;
    public TextMeshProUGUI tiktokLapsValueText;
    public Button btnTikTokLapsMinus;
    public Button btnTikTokLapsPlus;
    public TikTokJoinPanelManager tiktokJoinPanel;
    public Button btnOpenGiftActions;

    [Header("Selected Config")]
    public RaceManager.GameMode currentMode = RaceManager.GameMode.PlayerRace;
    public int selectedLaps = 3;
    public int selectedAICount = 5;
    public int selectedTikTokLaps = 3;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ShowTitle();

        if (playVsAIButton != null)
        {
            playVsAIButton.onClick.RemoveAllListeners();
            playVsAIButton.onClick.AddListener(() => OpenLobby(RaceManager.GameMode.PlayerRace));
        }
        if (spectatorButton != null)
        {
            spectatorButton.onClick.RemoveAllListeners();
            spectatorButton.onClick.AddListener(() => OpenLobby(RaceManager.GameMode.Spectator));
        }
        if (playerLiveButton != null)
        {
            playerLiveButton.onClick.RemoveAllListeners();
            playerLiveButton.onClick.AddListener(OpenTikTokLobby);
        }
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(ExitGame);
        }

        // Standard Lobby
        if (startRaceButton != null)
        {
            startRaceButton.onClick.RemoveAllListeners();
            startRaceButton.onClick.AddListener(StartRace);
        }
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(ShowTitle);
        }

        if (lapsSlider != null)
        {
            lapsSlider.minValue = 1; lapsSlider.maxValue = 10;
            lapsSlider.value = selectedLaps;
            lapsSlider.wholeNumbers = true;
            lapsSlider.onValueChanged.RemoveAllListeners();
            lapsSlider.onValueChanged.AddListener(OnLapsChanged);
        }

        // TikTok Live Setup Form
        if (createTikTokRoomButton != null)
        {
            createTikTokRoomButton.onClick.RemoveAllListeners();
            createTikTokRoomButton.onClick.AddListener(OnCreateTikTokRoomClicked);
        }
        if (backFromTikTokButton != null)
        {
            backFromTikTokButton.onClick.RemoveAllListeners();
            backFromTikTokButton.onClick.AddListener(() => {
                if (TikTokLiveManager.Instance != null) TikTokLiveManager.Instance.Disconnect();
                ShowTitle();
            });
        }
        if (tiktokLapsSlider != null)
        {
            tiktokLapsSlider.minValue = 1;
            tiktokLapsSlider.maxValue = 10;
            tiktokLapsSlider.value = selectedTikTokLaps;
            tiktokLapsSlider.wholeNumbers = true;
            tiktokLapsSlider.onValueChanged.RemoveAllListeners();
            tiktokLapsSlider.onValueChanged.AddListener(OnTikTokLapsChanged);
        }
        if (btnTikTokLapsMinus != null)
        {
            btnTikTokLapsMinus.onClick.RemoveAllListeners();
            btnTikTokLapsMinus.onClick.AddListener(() => {
                selectedTikTokLaps = Mathf.Max(1, selectedTikTokLaps - 1);
                if (tiktokLapsSlider != null) tiktokLapsSlider.value = selectedTikTokLaps;
                UpdateTikTokLapsDisplay();
            });
        }
        if (btnTikTokLapsPlus != null)
        {
            btnTikTokLapsPlus.onClick.RemoveAllListeners();
            btnTikTokLapsPlus.onClick.AddListener(() => {
                selectedTikTokLaps = Mathf.Min(10, selectedTikTokLaps + 1);
                if (tiktokLapsSlider != null) tiktokLapsSlider.value = selectedTikTokLaps;
                UpdateTikTokLapsDisplay();
            });
        }

        if (btnOpenGiftActions != null)
        {
            btnOpenGiftActions.onClick.RemoveAllListeners();
            btnOpenGiftActions.onClick.AddListener(() =>
            {
                if (GiftActionModalUI.Instance != null)
                {
                    GiftActionModalUI.Instance.OpenModal();
                }
            });
        }

        UpdateLobbyDisplay();
        UpdateTikTokLapsDisplay();
    }

    public void ShowTitle()
    {
        if (titlePanel != null) titlePanel.SetActive(true);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(false);
        if (tiktokLobbyPanel != null) tiktokLobbyPanel.SetActive(false);

        if (RaceManager.Instance != null && RaceManager.Instance.playerCar != null)
        {
            RaceManager.Instance.playerCar.controlsEnabled = false;
            RaceManager.Instance.playerCar.gameObject.SetActive(false);
        }
    }

    public void ShowLobby()
    {
        OpenLobby(currentMode);
    }

    public void OpenLobby(RaceManager.GameMode mode)
    {
        currentMode = mode;
        if (titlePanel != null) titlePanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
        if (hudPanel != null) hudPanel.SetActive(false);
        if (tiktokLobbyPanel != null) tiktokLobbyPanel.SetActive(false);

        if (lobbyHeaderTitle != null)
        {
            lobbyHeaderTitle.text = (mode == RaceManager.GameMode.PlayerRace)
                ? "RACE LOBBY (PLAYER VS AI)"
                : "SPECTATOR LOBBY (AI CHAMPIONSHIP)";
        }

        if (aiCountSlider != null)
        {
            aiCountSlider.onValueChanged.RemoveAllListeners();
            if (mode == RaceManager.GameMode.PlayerRace)
            {
                aiCountSlider.minValue = 0;
                aiCountSlider.maxValue = 49;
                if (selectedAICount > 49) selectedAICount = 5;
            }
            else
            {
                aiCountSlider.minValue = 2;
                aiCountSlider.maxValue = 50;
                if (selectedAICount < 2) selectedAICount = 10;
            }
            aiCountSlider.value = selectedAICount;
            aiCountSlider.wholeNumbers = true;
            aiCountSlider.onValueChanged.AddListener(OnAICountChanged);
        }

        UpdateLobbyDisplay();
    }

    public void OpenTikTokLobby()
    {
        currentMode = RaceManager.GameMode.TikTokLive;
        if (titlePanel != null) titlePanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(false);
        if (tiktokLobbyPanel != null) tiktokLobbyPanel.SetActive(true);

        if (tiktokStatusText != null)
            tiktokStatusText.text = "กรุณากรอก TikTok Username และเลือกจำนวนรอบแข่ง จากนั้นกดสร้างห้อง";

        UpdateTikTokLapsDisplay();
    }

    public void OnTikTokLapsChanged(float v)
    {
        selectedTikTokLaps = (int)v;
        UpdateTikTokLapsDisplay();
    }

    public void UpdateTikTokLapsDisplay()
    {
        if (tiktokLapsValueText != null)
        {
            tiktokLapsValueText.text = $"{selectedTikTokLaps} LAPS";
        }
    }

    public void OnCreateTikTokRoomClicked()
    {
        string uname = (tiktokUsernameInput != null && !string.IsNullOrEmpty(tiktokUsernameInput.text))
            ? tiktokUsernameInput.text.Trim().Replace("@", "")
            : "";

        if (string.IsNullOrEmpty(uname))
        {
            if (tiktokStatusText != null)
            {
                tiktokStatusText.text = "<color=#FF4444>⚠️ กรุณากรอก TikTok Username ก่อนกดสร้างห้อง</color>";
            }
            return;
        }

        bool isDevMode = uname.StartsWith("test", StringComparison.OrdinalIgnoreCase) ||
                         uname.StartsWith("demo", StringComparison.OrdinalIgnoreCase) ||
                         uname.StartsWith("sim", StringComparison.OrdinalIgnoreCase);

        if (isDevMode)
        {
            if (createTikTokRoomButton != null) createTikTokRoomButton.interactable = true;
            EnterTikTokTrack(uname, selectedTikTokLaps);
            return;
        }

        if (createTikTokRoomButton != null)
            createTikTokRoomButton.interactable = false;

        if (tiktokStatusText != null)
            tiktokStatusText.text = $"<color=#00D2FF>⏳ กำลังตรวจสอบสถานะ TikTok Live ของ @{uname}...</color>";

        if (TikTokLiveManager.Instance != null)
        {
            TikTokLiveManager.Instance.CheckIsLive(uname, (isLive, errMsg) =>
            {
                if (createTikTokRoomButton != null)
                    createTikTokRoomButton.interactable = true;

                if (!isLive)
                {
                    if (tiktokStatusText != null)
                    {
                        tiktokStatusText.text = $"<color=#FF4444>⚠️ ไม่สามารถสร้างห้องได้!\n{errMsg}\nกรุณาเริ่ม Live บน TikTok ก่อนสร้างห้อง</color>";
                    }
                    return;
                }

                // If live: enter track!
                EnterTikTokTrack(uname, selectedTikTokLaps);
            });
        }
    }

    public void EnterTikTokTrack(string uname, int laps)
    {
        if (tiktokLobbyPanel != null) tiktokLobbyPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);

        if (TikTokLiveManager.Instance != null)
        {
            TikTokLiveManager.Instance.ConnectToLive(uname, laps);
        }

        // Show the In-Track Join UI and hide Leaderboard until F6
        var joinMgr = tiktokJoinPanel != null ? tiktokJoinPanel : TikTokJoinPanelManager.Instance;
        if (joinMgr != null)
        {
            joinMgr.SetJoinPanelVisible(true);
            joinMgr.SetLeaderboardAndCameraVisible(false);
        }
    }

    public void ShowHUD()
    {
        if (titlePanel != null) titlePanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (tiktokLobbyPanel != null) tiktokLobbyPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);
    }

    public void OnLapsChanged(float v)
    {
        selectedLaps = (int)v;
        UpdateLobbyDisplay();
    }

    public void OnAICountChanged(float v)
    {
        selectedAICount = (int)v;
        UpdateLobbyDisplay();
    }

    public void UpdateLobbyDisplay()
    {
        if (lapsValueText != null)
            lapsValueText.text = $"{selectedLaps} LAPS";

        if (aiCountValueText != null)
        {
            aiCountValueText.text = (currentMode == RaceManager.GameMode.PlayerRace)
                ? $"{selectedAICount} AI OPPONENTS"
                : $"{selectedAICount} AI RACERS";
        }

        if (totalRacersText != null)
        {
            if (currentMode == RaceManager.GameMode.PlayerRace)
                totalRacersText.text = $"TOTAL RACERS: {1 + selectedAICount} / 50 (POLE POSITION + AI)";
            else
                totalRacersText.text = $"TOTAL RACERS: {selectedAICount} / 50 (AI BATTLE)";
        }
    }

    public void StartRace()
    {
        ShowHUD();
        var rm = FindObjectOfType<RaceManager>();
        if (rm != null)
        {
            rm.SetupAndStartRace(selectedLaps, selectedAICount, currentMode);
        }
    }

    public void ExitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
