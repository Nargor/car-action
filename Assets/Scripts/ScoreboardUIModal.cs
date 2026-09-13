using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ScoreboardUIModal : MonoBehaviour
{
    public static ScoreboardUIModal Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ScoreboardUIModal>();
            }
            return _instance;
        }
    }
    private static ScoreboardUIModal _instance;

    [Header("UI Panels & Tabs")]
    public GameObject modalRoot;
    public Button tabStreamerBtn;
    public Button tabWorldBtn;
    public TextMeshProUGUI tabStreamerText;
    public TextMeshProUGUI tabWorldText;

    [Header("Search & Pagination")]
    public TMP_InputField searchInput;
    public Button searchBtn;
    public Button prevPageBtn;
    public Button nextPageBtn;
    public TextMeshProUGUI pageIndicatorText;
    public Button closeBtn;

    [Header("Table Rows Container")]
    public Transform rowsContainer;
    public GameObject rowPrefab;
    public TextMeshProUGUI statusText;
    public TMP_FontAsset thaiFont;

    [Header("State")]
    public bool isStreamerTab = true;
    public int currentPage = 1;
    public int totalPages = 1;
    public int itemsPerPage = 10;
    public string currentSearch = "";

    private List<GameObject> activeRows = new List<GameObject>();
    private bool isLoading = false;

    void Awake()
    {
        if (_instance == null) _instance = this;
        else if (_instance != this) { Destroy(this); return; }
    }

    void Start()
    {
        if (modalRoot != null) modalRoot.SetActive(false);

        if (tabStreamerBtn != null) tabStreamerBtn.onClick.AddListener(() => SwitchTab(true));
        if (tabWorldBtn != null) tabWorldBtn.onClick.AddListener(() => SwitchTab(false));
        if (closeBtn != null) closeBtn.onClick.AddListener(CloseScoreboard);

        if (prevPageBtn != null) prevPageBtn.onClick.AddListener(OnPrevPage);
        if (nextPageBtn != null) nextPageBtn.onClick.AddListener(OnNextPage);

        if (searchInput != null)
        {
            searchInput.onEndEdit.AddListener(OnSearchSubmit);
        }
        if (searchBtn != null)
        {
            searchBtn.onClick.AddListener(() => OnSearchSubmit(searchInput != null ? searchInput.text : ""));
        }
    }

    void Update()
    {
        bool tabPressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame) tabPressed = true;
#endif
        if (Input.GetKeyDown(KeyCode.Tab)) tabPressed = true;

        if (tabPressed)
        {
            ToggleScoreboard();
        }

        // Close on ESC if open
        if (modalRoot != null && modalRoot.activeSelf)
        {
            bool escPressed = Input.GetKeyDown(KeyCode.Escape);
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) escPressed = true;
#endif
            if (escPressed) CloseScoreboard();
        }
    }

    public bool HasStreamerIdentified()
    {
        if (TikTokLiveManager.Instance != null
            && TikTokLiveManager.Instance.currentState != TikTokLiveManager.LiveState.Disconnected
            && !string.IsNullOrEmpty(TikTokLiveManager.Instance.streamerUsername))
        {
            return true;
        }

        var mm = MenuManager.Instance;
        if (mm != null && mm.tiktokLobbyPanel != null && mm.tiktokLobbyPanel.activeSelf)
        {
            if (mm.tiktokUsernameInput != null && !string.IsNullOrEmpty(mm.tiktokUsernameInput.text.Trim()))
            {
                return true;
            }
        }

        return false;
    }

    public string GetCurrentStreamerUsername()
    {
        if (TikTokLiveManager.Instance != null && !string.IsNullOrEmpty(TikTokLiveManager.Instance.streamerUsername))
        {
            return TikTokLiveManager.Instance.streamerUsername;
        }

        var mm = MenuManager.Instance;
        if (mm != null && mm.tiktokUsernameInput != null && !string.IsNullOrEmpty(mm.tiktokUsernameInput.text.Trim()))
        {
            return mm.tiktokUsernameInput.text.Trim().Replace("@", "");
        }

        return "";
    }

    public void ToggleScoreboard()
    {
        if (modalRoot == null) return;
        bool newState = !modalRoot.activeSelf;
        modalRoot.SetActive(newState);

        if (newState)
        {
            bool hasStreamer = HasStreamerIdentified();
            if (!hasStreamer)
            {
                isStreamerTab = false; // Only show World scoreboard
            }
            else
            {
                var mm = MenuManager.Instance;
                if (mm != null && mm.currentMode == RaceManager.GameMode.TikTokLive)
                    isStreamerTab = true;
            }

            currentPage = 1;
            currentSearch = "";
            if (searchInput != null) searchInput.text = "";
            UpdateTabVisuals();
            FetchScores();
        }
    }

    public void OpenScoreboard(bool streamerTab = true)
    {
        if (modalRoot != null) modalRoot.SetActive(true);

        bool hasStreamer = HasStreamerIdentified();
        isStreamerTab = hasStreamer ? streamerTab : false;

        currentPage = 1;
        currentSearch = "";
        if (searchInput != null) searchInput.text = "";
        UpdateTabVisuals();
        FetchScores();
    }

    public void CloseScoreboard()
    {
        if (modalRoot != null) modalRoot.SetActive(false);
    }

    public void SwitchTab(bool streamerTab)
    {
        if (streamerTab && !HasStreamerIdentified()) return;
        if (isStreamerTab == streamerTab) return;
        isStreamerTab = streamerTab;
        currentPage = 1;
        currentSearch = "";
        if (searchInput != null) searchInput.text = "";
        UpdateTabVisuals();
        FetchScores();
    }

    private void UpdateTabVisuals()
    {
        Color activeCol = new Color(0.00f, 0.83f, 1.00f); // Bright Cyan
        Color inactiveCol = new Color(0.20f, 0.25f, 0.35f); // Dark Grey

        bool hasStreamer = HasStreamerIdentified();

        if (tabStreamerBtn != null)
        {
            tabStreamerBtn.gameObject.SetActive(hasStreamer);
            if (hasStreamer)
            {
                var img = tabStreamerBtn.GetComponent<Image>();
                if (img != null) img.color = isStreamerTab ? activeCol : inactiveCol;
            }
        }

        if (tabStreamerText != null && hasStreamer)
        {
            string sName = GetCurrentStreamerUsername();
            tabStreamerText.text = $"STREAMER (@{sName})";
            tabStreamerText.color = isStreamerTab ? Color.black : Color.white;
            tabStreamerText.fontStyle = isStreamerTab ? FontStyles.Bold : FontStyles.Normal;
        }

        if (!hasStreamer)
        {
            isStreamerTab = false;
        }

        if (tabWorldBtn != null)
        {
            var img = tabWorldBtn.GetComponent<Image>();
            if (img != null) img.color = !isStreamerTab ? activeCol : inactiveCol;
        }
        if (tabWorldText != null)
        {
            tabWorldText.color = !isStreamerTab ? Color.black : Color.white;
            tabWorldText.fontStyle = !isStreamerTab ? FontStyles.Bold : FontStyles.Normal;
        }
    }

    private void OnPrevPage()
    {
        if (isLoading || currentPage <= 1) return;
        currentPage--;
        FetchScores();
    }

    private void OnNextPage()
    {
        if (isLoading || currentPage >= totalPages) return;
        currentPage++;
        FetchScores();
    }

    private void OnSearchSubmit(string query)
    {
        currentSearch = query.Trim();
        currentPage = 1;
        FetchScores();
    }

    public void FetchScores()
    {
        if (ScoreboardAPIManager.Instance == null)
        {
            ShowStatus("ScoreboardAPIManager not found.");
            return;
        }

        isLoading = true;
        ShowStatus("Loading scores from API...");
        ClearRows();

        bool hasStreamer = HasStreamerIdentified();
        if (!hasStreamer)
        {
            isStreamerTab = false;
        }

        if (isStreamerTab && hasStreamer)
        {
            string sName = GetCurrentStreamerUsername();
            if (string.IsNullOrEmpty(sName)) sName = "tiktok_streamer";

            ScoreboardAPIManager.Instance.GetStreamerScoreboard(sName, currentPage, itemsPerPage, currentSearch, OnScoresReceived);
        }
        else
        {
            ScoreboardAPIManager.Instance.GetWorldScoreboard(currentPage, itemsPerPage, currentSearch, OnScoresReceived);
        }
    }

    private void OnScoresReceived(ScoreboardAPIManager.ScoreboardListResponse response)
    {
        isLoading = false;

        if (response == null || response.data == null)
        {
            ShowStatus("Failed to load scores or no response from API.");
            UpdatePagination(1, 1);
            return;
        }

        if (response.data.Count == 0)
        {
            ShowStatus("No players found on the leaderboard.");
            UpdatePagination(1, 1);
            return;
        }

        if (statusText != null) statusText.gameObject.SetActive(false);

        if (response.pagination != null)
        {
            UpdatePagination(response.pagination.current_page, response.pagination.total_pages);
        }

        PopulateTableRows(response.data);
    }

    private void UpdatePagination(int cur, int tot)
    {
        currentPage = Mathf.Max(1, cur);
        totalPages = Mathf.Max(1, tot);

        if (pageIndicatorText != null)
        {
            pageIndicatorText.text = $"PAGE {currentPage} / {totalPages}";
        }

        if (prevPageBtn != null) prevPageBtn.interactable = (currentPage > 1);
        if (nextPageBtn != null) nextPageBtn.interactable = (currentPage < totalPages);
    }

    private void ClearRows()
    {
        if (rowsContainer != null)
        {
            for (int i = rowsContainer.childCount - 1; i >= 0; i--)
            {
                var child = rowsContainer.GetChild(i).gameObject;
                child.SetActive(false);
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }
        activeRows.Clear();
    }

    private void ShowStatus(string msg)
    {
        ClearRows();
        if (statusText != null)
        {
            statusText.gameObject.SetActive(true);
            statusText.text = msg;
        }
    }

    private void PopulateTableRows(List<ScoreboardAPIManager.ScoreboardEntry> list)
    {
        if (rowsContainer == null) return;

        ClearRows();

        for (int i = 0; i < list.Count; i++)
        {
            var item = list[i];
            int displayRank = item.global_rank > 0 ? item.global_rank : ((currentPage - 1) * itemsPerPage + i + 1);

            GameObject row = null;
            if (rowPrefab != null)
            {
                row = Instantiate(rowPrefab, rowsContainer);
            }
            else
            {
                row = CreateDefaultRow(rowsContainer, displayRank, item);
            }

            if (row != null)
            {
                row.SetActive(true);
                activeRows.Add(row);

                // If using prefab, fill components
                var texts = row.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 4)
                {
                    texts[0].text = FormatRank(displayRank);
                    texts[1].text = $"<b>{item.tiktok_nickname}</b> <size=80%><color=#88A0C0>@{item.tiktok_username}</color></size>";
                    texts[2].text = $"<color=#FFD700><b>{item.total_score:N0}</b></color> <size=75%>PTS</size>";
                    texts[3].text = (item.best_score_time > 0) ? FormatSeconds(item.best_score_time) : "--:--";
                }
            }
        }
    }

    private string FormatRank(int rank)
    {
        if (rank == 1) return "<color=#FFD700><b>1st [TOP]</b></color>";
        if (rank == 2) return "<color=#E0E0E0><b>2nd</b></color>";
        if (rank == 3) return "<color=#CD7F32><b>3rd</b></color>";
        return $"<color=#80B0E0><b>#{rank}</b></color>";
    }

    private string FormatSeconds(int totalSec)
    {
        int min = totalSec / 60;
        int sec = totalSec % 60;
        return $"{min:00}:{sec:00}";
    }

    private GameObject CreateDefaultRow(Transform parent, int rank, ScoreboardAPIManager.ScoreboardEntry item)
    {
        var rowGo = new GameObject($"Row_{rank}", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
        rowGo.transform.SetParent(parent, false);

        var rt = rowGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 36);

        var img = rowGo.GetComponent<Image>();
        img.color = (rank % 2 == 0) ? new Color(0.08f, 0.12f, 0.18f, 0.85f) : new Color(0.05f, 0.08f, 0.13f, 0.85f);

        var hlg = rowGo.GetComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.spacing = 8;
        hlg.padding = new RectOffset(12, 12, 4, 4);

        // Col 1: Rank (width 80)
        CreateCell(rowGo.transform, 80, FormatRank(rank), TextAlignmentOptions.Center, 13, false);

        // Col 2: Player Nickname & Username (width 310, flex 1)
        string displayName = ThaiFontAdjuster.Adjust(item.tiktok_nickname);
        CreateCell(rowGo.transform, 310, $"<b>{displayName}</b> <size=80%><color=#7095B5>@{item.tiktok_username}</color></size>", TextAlignmentOptions.Left, 13, true);

        // Col 3: Score (width 130)
        CreateCell(rowGo.transform, 130, $"<color=#FFD700><b>{item.total_score:N0}</b></color> <size=75%>PTS</size>", TextAlignmentOptions.Right, 13, false);

        // Col 4: Best Time (width 110)
        string timeStr = (item.best_score_time > 0) ? FormatSeconds(item.best_score_time) : "--:--";
        CreateCell(rowGo.transform, 110, $"<color=#80E0FF>{timeStr}</color>", TextAlignmentOptions.Right, 13, false);

        return rowGo;
    }

    private TextMeshProUGUI CreateCell(Transform parent, float width, string text, TextAlignmentOptions align, float fontSize, bool flexible)
    {
        var go = new GameObject("Cell", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, 36);

        var le = go.GetComponent<LayoutElement>();
        le.preferredWidth = width;
        le.minWidth = width;
        if (flexible) le.flexibleWidth = 1;

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = Color.white;
        if (thaiFont != null) tmp.font = thaiFont;
#if UNITY_EDITOR
        else
        {
            var f = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/ThaiFont_SDF.asset");
            if (f != null) tmp.font = f;
        }
#endif

        return tmp;
    }
}
