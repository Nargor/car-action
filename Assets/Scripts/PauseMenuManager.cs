using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PauseMenuManager>();
            }
            return _instance;
        }
    }
    private static PauseMenuManager _instance;

    [Header("UI References")]
    public GameObject pauseModal;
    public Button resumeButton;
    public Button restartButton;
    public Button mainMenuButton;
    public Button exitButton;

    public bool IsPaused { get; private set; } = false;

    void Awake()
    {
        if (_instance == null) _instance = this;
        else if (_instance != this) { Destroy(this); return; }
    }

    void Start()
    {
        if (pauseModal != null) pauseModal.SetActive(false);

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(ResumeGame);
        }
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartRace);
        }
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(ExitGame);
        }
    }

    void Update()
    {
        bool escPressed = false;

#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            escPressed = true;
        }
#endif

        if (!escPressed && Input.GetKeyDown(KeyCode.Escape))
        {
            escPressed = true;
        }

        if (escPressed)
        {
            HandleEscapePress();
        }
    }

    private void HandleEscapePress()
    {
        // 1. If Scoreboard Modal is open, close it first!
        var scModal = ScoreboardUIModal.Instance;
        if (scModal != null && scModal.modalRoot != null && scModal.modalRoot.activeSelf)
        {
            scModal.CloseScoreboard();
            return;
        }

        // 2. If already paused, resume
        if (IsPaused)
        {
            ResumeGame();
            return;
        }

        // 3. Only allow pause if we are currently in gameplay (HUD is active)
        var mm = MenuManager.Instance;
        if (mm != null && mm.hudPanel != null && mm.hudPanel.activeSelf)
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        IsPaused = true;
        Time.timeScale = 0f;

        if (pauseModal != null)
            pauseModal.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ResumeGame()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        if (pauseModal != null)
            pauseModal.SetActive(false);
    }

    public void RestartRace()
    {
        ResumeGame();

        var rm = RaceManager.Instance;
        var mm = MenuManager.Instance;

        if (rm != null && mm != null)
        {
            if (rm.selectedGameMode == RaceManager.GameMode.TikTokLive)
            {
                rm.StartTikTokRace();
            }
            else
            {
                rm.SetupAndStartRace(mm.selectedLaps, mm.selectedAICount, mm.currentMode);
            }
        }
    }

    public void GoToMainMenu()
    {
        ResumeGame();

        var rm = RaceManager.Instance;
        if (rm != null)
        {
            rm.StopRaceAndReset();
        }

        var mm = MenuManager.Instance;
        if (mm != null)
        {
            mm.ShowTitle();
        }
    }

    public void ExitGame()
    {
        ResumeGame();

        var mm = MenuManager.Instance;
        if (mm != null)
        {
            mm.ExitGame();
        }
        else
        {
            Application.Quit();
        }
    }
}
