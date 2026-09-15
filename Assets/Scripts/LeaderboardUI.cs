using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LeaderboardUI : MonoBehaviour
{
    [Header("Panel References")]
    public RectTransform windowPanel;
    public RectTransform contentContainer; // Under ScrollRect
    public ScrollRect scrollRect;
    public TextMeshProUGUI titleText;

    [Header("Close Button")]
    public Button closeButton;

    [Header("Embedded Camera Toolbar")]
    public GameObject cameraBar;
    public Button btnFPS;
    public Button btnTPS;
    public Button btnTPS2;
    public Button btnBirdeye;
    public Button btnNoClip;

    [Header("Row Prefab / Template")]
    public GameObject rowTemplate; // Disabled template row

    private class RowView
    {
        public GameObject root;
        public Image bgImage;
        public Image colorSwatch;
        public TextMeshProUGUI posText;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI statusText;
        public Button button;
        public Transform racerTransform;
    }

    private List<RowView> activeRows = new List<RowView>();
    private float updateTimer = 0f;
    private const float UPDATE_INTERVAL = 0.25f; // 4 times a second for smooth UI
    private ChaseCameraController.CameraMode lastHighlightedMode = (ChaseCameraController.CameraMode)(-1);

    void Awake()
    {
        WireButtons();
    }

    void Start()
    {
        if (rowTemplate != null)
            rowTemplate.SetActive(false);

        WireButtons();
        UpdateCameraButtonsHighlight();
    }

    void OnEnable()
    {
        WireButtons();
        lastHighlightedMode = (ChaseCameraController.CameraMode)(-1);
        UpdateCameraButtonsHighlight();
        RefreshLeaderboard();
    }

    public void WireButtons()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() =>
            {
                if (TikTokJoinPanelManager.Instance != null)
                {
                    TikTokJoinPanelManager.Instance.SetLeaderboardAndCameraVisible(false);
                }
                else
                {
                    gameObject.SetActive(false);
                }
            });
        }

        if (btnFPS != null)
        {
            btnFPS.onClick.RemoveAllListeners();
            btnFPS.onClick.AddListener(() => SetCameraMode(ChaseCameraController.CameraMode.FPS));
        }
        if (btnTPS != null)
        {
            btnTPS.onClick.RemoveAllListeners();
            btnTPS.onClick.AddListener(() => SetCameraMode(ChaseCameraController.CameraMode.TPS));
        }
        if (btnTPS2 != null)
        {
            btnTPS2.onClick.RemoveAllListeners();
            btnTPS2.onClick.AddListener(() => SetCameraMode(ChaseCameraController.CameraMode.TPS2));
        }
        if (btnBirdeye != null)
        {
            btnBirdeye.onClick.RemoveAllListeners();
            btnBirdeye.onClick.AddListener(() => SetCameraMode(ChaseCameraController.CameraMode.Birdeye));
        }
        if (btnNoClip != null)
        {
            btnNoClip.onClick.RemoveAllListeners();
            btnNoClip.onClick.AddListener(() => SetCameraMode(ChaseCameraController.CameraMode.NoClip));
        }
    }

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

        Color activeCol = new Color(0.12f, 0.65f, 1.0f, 1f); // Bright cyan active
        Color idleCol   = new Color(0.18f, 0.22f, 0.32f, 0.95f); // Dark slate idle

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
        UpdateCameraButtonsHighlight();

        if (RaceManager.Instance == null) return;

        bool hasRacers = RaceManager.Instance.totalRacers > 0;
        if (!RaceManager.Instance.isRaceActive && !RaceManager.Instance.isCountdownActive && !hasRacers)
            return;

        updateTimer += Time.deltaTime;
        if (updateTimer >= UPDATE_INTERVAL)
        {
            updateTimer = 0f;
            RefreshLeaderboard();
        }
    }

    public void RefreshLeaderboard()
    {
        var racers = RaceManager.Instance.GetRacersLeaderboard();
        if (racers == null || contentContainer == null) return;

        // Ensure we have enough row views
        while (activeRows.Count < racers.Count)
        {
            CreateNewRow();
        }

        Transform currentCamTarget = ChaseCameraController.Instance != null ? ChaseCameraController.Instance.target : null;

        for (int i = 0; i < activeRows.Count; i++)
        {
            if (i < racers.Count)
            {
                var r = racers[i];
                var row = activeRows[i];
                row.root.SetActive(true);
                row.racerTransform = r.transform;

                // Position
                string posStr = (r.position == 1) ? "<color=#FFD700>1</color>"
                              : (r.position == 2) ? "<color=#E0E0E0>2</color>"
                              : (r.position == 3) ? "<color=#CD7F32>3</color>"
                              : $"{r.position}";
                row.posText.text = posStr;

                // Color swatch
                row.colorSwatch.color = r.carColor;

                // Name
                row.nameText.text = r.isPlayer ? $"<b><color=#00FFFF>{r.name}</color></b>" : r.name;

                // Status
                row.statusText.text = $"L{r.currentLap}";

                // Highlight if currently focused by camera
                bool isFocused = (currentCamTarget != null && currentCamTarget == r.transform);
                row.bgImage.color = isFocused 
                    ? new Color(0.25f, 0.45f, 0.75f, 0.90f) 
                    : (i % 2 == 0 ? new Color(0.12f, 0.14f, 0.20f, 0.85f) : new Color(0.08f, 0.10f, 0.15f, 0.85f));
            }
            else
            {
                activeRows[i].root.SetActive(false);
            }
        }
    }

    private void CreateNewRow()
    {
        if (rowTemplate == null || contentContainer == null) return;

        GameObject newObj = Instantiate(rowTemplate, contentContainer);
        newObj.name = $"Row_{activeRows.Count}";
        newObj.SetActive(true);

        var view = new RowView
        {
            root = newObj,
            bgImage = newObj.GetComponent<Image>(),
            colorSwatch = newObj.transform.Find("ColorSwatch")?.GetComponent<Image>(),
            posText = newObj.transform.Find("PosText")?.GetComponent<TextMeshProUGUI>(),
            nameText = newObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>(),
            statusText = newObj.transform.Find("StatusText")?.GetComponent<TextMeshProUGUI>(),
            button = newObj.GetComponent<Button>()
        };

        if (view.button != null)
        {
            view.button.onClick.AddListener(() =>
            {
                if (view.racerTransform != null && ChaseCameraController.Instance != null)
                {
                    ChaseCameraController.Instance.SetTarget(view.racerTransform);
                    RefreshLeaderboard();
                }
            });
        }

        activeRows.Add(view);
    }
}
