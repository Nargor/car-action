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

    [Header("TikTok Live Panel UI")]
    public TMP_InputField tiktokUsernameInput;
    public Button connectTikTokButton;
    public Button startTikTokRaceButton;
    public Button backFromTikTokButton;
    public TextMeshProUGUI tiktokStatusText;
    public TextMeshProUGUI tiktokJoinedCountText;
    public Button simJoinButton;
    public Button simNitroButton;
    public Button simPrankButton;

    [Header("Selected Config")]
    public RaceManager.GameMode currentMode = RaceManager.GameMode.PlayerRace;
    public int selectedLaps = 3;
    public int selectedAICount = 5;

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

        // TikTok Live Panel Buttons
        if (connectTikTokButton != null)
        {
            connectTikTokButton.onClick.RemoveAllListeners();
            connectTikTokButton.onClick.AddListener(ConnectTikTokLive);
        }
        if (startTikTokRaceButton != null)
        {
            startTikTokRaceButton.onClick.RemoveAllListeners();
            startTikTokRaceButton.onClick.AddListener(StartTikTokRace);
        }
        if (backFromTikTokButton != null)
        {
            backFromTikTokButton.onClick.RemoveAllListeners();
            backFromTikTokButton.onClick.AddListener(() => {
                if (TikTokLiveManager.Instance != null) TikTokLiveManager.Instance.Disconnect();
                ShowTitle();
            });
        }

        // Simulator Buttons
        if (simJoinButton != null)
        {
            simJoinButton.onClick.RemoveAllListeners();
            simJoinButton.onClick.AddListener(() => {
                if (TikTokLiveManager.Instance != null) TikTokLiveManager.Instance.SimulateViewerJoin();
            });
        }
        if (simNitroButton != null)
        {
            simNitroButton.onClick.RemoveAllListeners();
            simNitroButton.onClick.AddListener(() => {
                if (TikTokLiveManager.Instance != null) TikTokLiveManager.Instance.SimulateGiftNitro();
            });
        }
        if (simPrankButton != null)
        {
            simPrankButton.onClick.RemoveAllListeners();
            simPrankButton.onClick.AddListener(() => {
                if (TikTokLiveManager.Instance != null) TikTokLiveManager.Instance.SimulatePrank();
            });
        }

        UpdateLobbyDisplay();
    }

    public void ShowTitle()
    {
        if (titlePanel != null) titlePanel.SetActive(true);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(false);
        if (tiktokLobbyPanel != null) tiktokLobbyPanel.SetActive(false);
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
            tiktokStatusText.text = "STATUS: DISCONNECTED (ENTER USERNAME)";
        if (tiktokJoinedCountText != null)
            tiktokJoinedCountText.text = "RACERS JOINED: 0 / 50";

        if (startTikTokRaceButton != null)
            startTikTokRaceButton.interactable = false;
    }

    public void ConnectTikTokLive()
    {
        string uname = (tiktokUsernameInput != null && !string.IsNullOrEmpty(tiktokUsernameInput.text))
            ? tiktokUsernameInput.text.Trim()
            : "tiktok_racing_host";

        if (TikTokLiveManager.Instance != null)
        {
            TikTokLiveManager.Instance.ConnectToLive(uname);
        }

        // Show HUD so streamer can see the starting grid in background while waiting!
        if (hudPanel != null) hudPanel.SetActive(true);

        if (tiktokStatusText != null)
            tiktokStatusText.text = ThaiFontAdjuster.Adjust($"<color=#00FF88>● LIVE CONNECTED: @{uname}</color>\n<size=85%><color=#FFD700>พิมพ์ ''a'' ในช่องแชท TikTok เพื่อลงแข่ง!</color></size>");

        if (startTikTokRaceButton != null)
            startTikTokRaceButton.interactable = true;

        UpdateTikTokLobbyDisplay();
    }

    public void UpdateTikTokLobbyDisplay()
    {
        int count = TikTokLiveManager.Instance != null ? TikTokLiveManager.Instance.joinedRacers.Count : 0;
        if (tiktokJoinedCountText != null)
        {
            tiktokJoinedCountText.text = $"RACERS JOINED: <color=#FFD700><b>{count}</b></color> / 50";
        }
    }

    public void StartTikTokRace()
    {
        // Close TikTok Lobby form and start race
        if (tiktokLobbyPanel != null) tiktokLobbyPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);

        if (TikTokLiveManager.Instance != null)
        {
            TikTokLiveManager.Instance.OnStartRace();
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
