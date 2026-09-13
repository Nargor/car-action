using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    [Header("References")]
    public CarController car;
    public LapTimer      lapTimer;

    [Header("Speedometer (Center Compact)")]
    public RectTransform   speedPanel;
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI driverNameText;
    public Image           speedBar;

    [Header("Race Info UI")]
    public TextMeshProUGUI lapText;
    public TextMeshProUGUI currentLapTimeText;
    public TextMeshProUGUI bestLapTimeText;
    public TextMeshProUGUI totalTimeText;
    public TextMeshProUGUI positionText;

    [Header("Camera Toolbar Buttons")]
    public Button btnFPS;
    public Button btnTPS;
    public Button btnTPS2;
    public Button btnBirdeye;
    public Button btnNoClip;

    [Header("Countdown")]
    public GameObject      countdownBanner;
    public TextMeshProUGUI countdownText;

    [Header("Finish Screen")]
    public GameObject      finishPanel;
    public TextMeshProUGUI finishText;
    public Button          returnToLobbyButton;
    public Button          mainMenuButton;

    private ChaseCameraController.CameraMode lastHighlightedMode = (ChaseCameraController.CameraMode)(-1);

    void Start()
    {
        if (finishPanel != null) finishPanel.SetActive(false);
        if (countdownBanner != null) countdownBanner.SetActive(false);

        // Wire Camera Buttons
        if (btnFPS != null)
        {
            btnFPS.onClick.RemoveAllListeners();
            btnFPS.onClick.AddListener(SelectFPS);
        }
        if (btnTPS != null)
        {
            btnTPS.onClick.RemoveAllListeners();
            btnTPS.onClick.AddListener(SelectTPS);
        }
        if (btnTPS2 != null)
        {
            btnTPS2.onClick.RemoveAllListeners();
            btnTPS2.onClick.AddListener(SelectTPS2);
        }
        if (btnBirdeye != null)
        {
            btnBirdeye.onClick.RemoveAllListeners();
            btnBirdeye.onClick.AddListener(SelectBirdeye);
        }
        if (btnNoClip != null)
        {
            btnNoClip.onClick.RemoveAllListeners();
            btnNoClip.onClick.AddListener(SelectNoClip);
        }

        if (lapTimer != null)
        {
            lapTimer.onRaceFinished.RemoveAllListeners();
            lapTimer.onRaceFinished.AddListener(ShowFinishScreen);
        }

        if (returnToLobbyButton != null)
        {
            returnToLobbyButton.onClick.RemoveAllListeners();
            returnToLobbyButton.onClick.AddListener(() =>
            {
                if (finishPanel != null) finishPanel.SetActive(false);
                var mm = FindObjectOfType<MenuManager>();
                if (mm != null) mm.ShowLobby();
            });
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(() =>
            {
                if (finishPanel != null) finishPanel.SetActive(false);
                var mm = FindObjectOfType<MenuManager>();
                if (mm != null) mm.ShowTitle();
            });
        }

        UpdateCameraButtonsHighlight();
    }

    public void SelectFPS()     => SetCameraMode(ChaseCameraController.CameraMode.FPS);
    public void SelectTPS()     => SetCameraMode(ChaseCameraController.CameraMode.TPS);
    public void SelectTPS2()    => SetCameraMode(ChaseCameraController.CameraMode.TPS2);
    public void SelectBirdeye() => SetCameraMode(ChaseCameraController.CameraMode.Birdeye);
    public void SelectNoClip()  => SetCameraMode(ChaseCameraController.CameraMode.NoClip);

    public void SetCameraMode(ChaseCameraController.CameraMode mode)
    {
        if (ChaseCameraController.Instance != null)
        {
            ChaseCameraController.Instance.SetCameraMode(mode);
        }
        UpdateCameraButtonsHighlight();
    }

    private void UpdateCameraButtonsHighlight()
    {
        if (ChaseCameraController.Instance == null) return;
        var mode = ChaseCameraController.Instance.currentMode;
        if (mode == lastHighlightedMode) return;
        lastHighlightedMode = mode;

        Color activeCol = new Color(0.12f, 0.55f, 0.95f, 1f); // Bright blue highlight
        Color idleCol   = new Color(0.18f, 0.22f, 0.32f, 1f); // Dark idle

        SetBtnColor(btnFPS, mode == ChaseCameraController.CameraMode.FPS ? activeCol : idleCol);
        SetBtnColor(btnTPS, mode == ChaseCameraController.CameraMode.TPS ? activeCol : idleCol);
        SetBtnColor(btnTPS2, mode == ChaseCameraController.CameraMode.TPS2 ? activeCol : idleCol);
        SetBtnColor(btnBirdeye, mode == ChaseCameraController.CameraMode.Birdeye ? activeCol : idleCol);
        SetBtnColor(btnNoClip, mode == ChaseCameraController.CameraMode.NoClip ? activeCol : idleCol);
    }

    private void SetBtnColor(Button btn, Color c)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = c;
    }

    void Update()
    {
        UpdateSpeedometer();
        UpdateRaceStats();
        UpdateCameraButtonsHighlight();
    }

    private void UpdateSpeedometer()
    {
        Transform watchedTarget = null;
        if (ChaseCameraController.Instance != null && ChaseCameraController.Instance.target != null)
        {
            watchedTarget = ChaseCameraController.Instance.target;
        }
        else if (car != null && car.gameObject.activeInHierarchy)
        {
            watchedTarget = car.transform;
        }

        float speed = 0f;
        string driver = "NO TARGET";
        Color driverColor = Color.white;

        if (watchedTarget != null)
        {
            var aiCar = watchedTarget.GetComponent<AICarController>();
            var pCar  = watchedTarget.GetComponent<CarController>();
            var rb    = watchedTarget.GetComponent<Rigidbody>();

            if (aiCar != null)
            {
                speed = aiCar.SpeedKmh;
                driver = aiCar.racerName;
                driverColor = aiCar.carColor;
            }
            else if (pCar != null)
            {
                speed = pCar.SpeedKmh;
                driver = "PLAYER";
                driverColor = new Color(0.2f, 0.85f, 1.0f);
            }
            else if (rb != null)
            {
                speed = rb.linearVelocity.magnitude * 3.6f;
                driver = watchedTarget.name;
            }
        }

        if (speedText != null)
        {
            speedText.text = $"{(int)speed} <size=50%>KM/H</size>";
        }

        if (driverNameText != null)
        {
            string hex = ColorUtility.ToHtmlStringRGB(driverColor);
            driverNameText.text = $"<color=#{hex}>●</color> {driver.ToUpper()}";
        }

        if (speedBar != null)
        {
            speedBar.fillAmount = Mathf.Clamp01(speed / 240f);
        }
    }

    private void UpdateRaceStats()
    {
        if (lapTimer == null) return;

        if (lapText != null)
        {
            int displayLap = Mathf.Max(1, lapTimer.CurrentLap);
            lapText.text = $"LAP {displayLap} / {lapTimer.totalLaps}";
        }

        if (currentLapTimeText != null)
            currentLapTimeText.text = LapTimer.FormatTime(lapTimer.CurrentLapTime);

        if (bestLapTimeText != null)
        {
            string best = lapTimer.BestLapTime < float.MaxValue
                ? LapTimer.FormatTime(lapTimer.BestLapTime)
                : "--:--.---";
            bestLapTimeText.text = $"BEST  {best}";
        }

        if (totalTimeText != null)
            totalTimeText.text = LapTimer.FormatTime(lapTimer.TotalRaceTime);
    }

    public void UpdatePositionText(int pos, int total)
    {
        if (positionText == null) return;
        string suffix = (pos == 1) ? "ST" : (pos == 2) ? "ND" : (pos == 3) ? "RD" : "TH";
        positionText.text = $"POS  <color=#FFD700><size=120%>{pos}</size>{suffix}</color> / {total}";
    }

    public void ShowCountdown(string txt)
    {
        if (countdownBanner != null) countdownBanner.SetActive(true);
        if (countdownText != null)
        {
            countdownText.text = txt;
            if (txt == "GO!") countdownText.color = Color.green;
            else countdownText.color = Color.yellow;
        }
    }

    public void HideCountdown()
    {
        if (countdownBanner != null) countdownBanner.SetActive(false);
    }

    public void ShowFinishScreen()
    {
        if (finishPanel != null)
        {
            finishPanel.SetActive(true);
            if (finishText != null)
            {
                int pos = RaceManager.Instance != null ? RaceManager.Instance.playerPosition : 1;
                int total = RaceManager.Instance != null ? RaceManager.Instance.totalRacers : 1;
                string rankBadge = (pos == 1) ? "<color=#FFD700>1st PLACE! 🏆 WINNER</color>"
                                 : (pos == 2) ? "<color=#E0E0E0>2nd PLACE! 🥈 PODIUM</color>"
                                 : (pos == 3) ? "<color=#CD7F32>3rd PLACE! 🥉 PODIUM</color>"
                                 : $"<color=#80C0FF>{pos}th PLACE</color>";

                finishText.text = $"<b>RACE FINISHED!</b>\n\n{rankBadge}\nPosition: {pos} / {total}\n\n<size=80%>Total Time: {LapTimer.FormatTime(lapTimer.TotalRaceTime)}\nBest Lap:   {LapTimer.FormatTime(lapTimer.BestLapTime)}</size>\n\n<size=75%><color=#00FF88>✓ Scores recorded to Thungthao Scoreboard API!</color>\n<color=#FFD700>[Press TAB for Scoreboard]</color></size>";
            }

            // Submit Scores to Scoreboard API (Humans only)
            if (ScoreboardAPIManager.Instance != null && RaceManager.Instance != null)
            {
                string sUser = (TikTokLiveManager.Instance != null && !string.IsNullOrEmpty(TikTokLiveManager.Instance.streamerUsername))
                    ? TikTokLiveManager.Instance.streamerUsername
                    : "tiktok_streamer";
                float tSec = (lapTimer != null) ? lapTimer.TotalRaceTime : 60f;
                var racers = RaceManager.Instance.GetRacersLeaderboard();
                ScoreboardAPIManager.Instance.SubmitRaceResults(racers, sUser, sUser, tSec);
            }
        }
    }
}
