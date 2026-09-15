using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GiftActionModalUI : MonoBehaviour
{
    public static GiftActionModalUI Instance { get; private set; }

    [Header("Main Modal References")]
    public GameObject modalRoot;
    public RectTransform rowsContainer;
    public GameObject rowTemplate;
    public Button addActionButton;
    public Button saveButton;
    public Button resetButton;
    public Button closeButton;
    public TextMeshProUGUI statusText;

    [Header("Searchable Gift Picker (Select2-Style)")]
    public GameObject giftPickerPopup;
    public TMP_InputField giftSearchInput;
    public RectTransform giftListContainer;
    public GameObject giftRowTemplate;
    public Button closePickerButton;

    private GiftActionDatabase workingDatabase;
    private int editingItemIndex = -1;
    private List<GameObject> activeRowViews = new List<GameObject>();
    private List<GameObject> activeGiftPickerViews = new List<GameObject>();
    private List<TikTokGiftInfo> allGifts = new List<TikTokGiftInfo>();

    private bool isInitialized = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }
        EnsureInitialized();
    }

    void Start()
    {
        EnsureInitialized();
    }

    public void EnsureInitialized()
    {
        if (isInitialized) return;
        isInitialized = true;

        if (rowTemplate != null) rowTemplate.SetActive(false);
        if (giftRowTemplate != null) giftRowTemplate.SetActive(false);
        if (giftPickerPopup != null) giftPickerPopup.SetActive(false);

        if (addActionButton != null)
        {
            addActionButton.onClick.RemoveAllListeners();
            addActionButton.onClick.AddListener(OnAddActionClicked);
        }

        if (saveButton != null)
        {
            saveButton.onClick.RemoveAllListeners();
            saveButton.onClick.AddListener(OnSaveClicked);
        }

        if (resetButton != null)
        {
            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(OnResetClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseModal);
        }

        if (closePickerButton != null)
        {
            closePickerButton.onClick.RemoveAllListeners();
            closePickerButton.onClick.AddListener(() => {
                if (giftPickerPopup != null) giftPickerPopup.SetActive(false);
            });
        }

        if (giftSearchInput != null)
        {
            giftSearchInput.onValueChanged.RemoveAllListeners();
            giftSearchInput.onValueChanged.AddListener(OnSearchFilterChanged);
        }
    }

    public void OpenModal()
    {
        EnsureInitialized();

        if (modalRoot != null)
        {
            modalRoot.SetActive(true);
            modalRoot.transform.SetAsLastSibling();
        }

        // Clone current database from GiftActionManager
        if (GiftActionManager.Instance != null && GiftActionManager.Instance.database != null && GiftActionManager.Instance.database.actions != null && GiftActionManager.Instance.database.actions.Count > 0)
        {
            string json = JsonUtility.ToJson(GiftActionManager.Instance.database);
            workingDatabase = JsonUtility.FromJson<GiftActionDatabase>(json);
        }
        else
        {
            workingDatabase = GiftActionConfig.LoadConfig();
        }

        if (workingDatabase == null || workingDatabase.actions == null || workingDatabase.actions.Count == 0)
        {
            workingDatabase = GiftActionConfig.CreateDefaultDatabase();
        }

        // Cache available gifts
        if (GiftActionManager.Instance != null && GiftActionManager.Instance.availableGifts != null && GiftActionManager.Instance.availableGifts.Count > 0)
        {
            allGifts = new List<TikTokGiftInfo>(GiftActionManager.Instance.availableGifts);
        }
        else
        {
            allGifts = GiftActionManager.GenerateFallbackGiftCatalog();
        }

        if (statusText != null)
        {
            statusText.text = "<color=#A0A5BD>กำหนด Action ที่ต้องการเมื่อมีคนส่งของขวัญใน TikTok Live</color>";
        }

        RefreshActionRows();
    }

    public void CloseModal()
    {
        if (giftPickerPopup != null) giftPickerPopup.SetActive(false);
        if (modalRoot != null) modalRoot.SetActive(false);
    }

    // ==========================================
    // ACTION ROWS RENDERING
    // ==========================================
    private void RefreshActionRows()
    {
        if (rowsContainer == null || rowTemplate == null || workingDatabase == null) return;

        activeRowViews.RemoveAll(x => x == null);

        int count = workingDatabase.actions.Count;
        while (activeRowViews.Count < count)
        {
            GameObject newRow = Instantiate(rowTemplate, rowsContainer);
            var le = newRow.GetComponent<LayoutElement>();
            if (le == null) le = newRow.AddComponent<LayoutElement>();
            le.minHeight = 52f;
            le.preferredHeight = 52f;
            le.flexibleWidth = 1f;

            var rRect = newRow.GetComponent<RectTransform>();
            rRect.anchorMin = new Vector2(0f, 1f);
            rRect.anchorMax = new Vector2(1f, 1f);
            rRect.pivot = new Vector2(0f, 1f);

            activeRowViews.Add(newRow);
        }

        for (int i = 0; i < activeRowViews.Count; i++)
        {
            if (i < count)
            {
                var item = workingDatabase.actions[i];
                var row = activeRowViews[i];
                row.SetActive(true);

                int index = i;

                // 1. Gift Select Button & Thumbnail
                var giftThumb = row.transform.Find("Img_GiftThumb")?.GetComponent<Image>();
                if (giftThumb == null) giftThumb = row.transform.Find("Btn_Gift/Img_GiftThumb")?.GetComponent<Image>();
                if (giftThumb != null)
                {
                    giftThumb.sprite = GiftIconCache.Instance.GetGiftSprite(item.giftName, item.giftIconUrl);
                    giftThumb.color = Color.white;
                }

                var giftBtn = row.transform.Find("Btn_Gift")?.GetComponent<Button>();
                var giftTxt = row.transform.Find("Btn_Gift/Text")?.GetComponent<TextMeshProUGUI>();
                if (giftTxt != null)
                {
                    string th = TikTokGiftDictionary.GetThaiName(item.giftName);
                    giftTxt.text = $"<b>{item.giftName}</b> <size=80%><color=#A5B4CB>({th})</color></size> <size=85%><color=#FFD700>+{item.diamondCost}C</color></size>";
                }
                if (giftBtn != null)
                {
                    giftBtn.onClick.RemoveAllListeners();
                    giftBtn.onClick.AddListener(() => OpenGiftPicker(index));
                }

                // 2. Action Type Dropdown / Button
                var actionBtn = row.transform.Find("Btn_Action")?.GetComponent<Button>();
                var actionTxt = row.transform.Find("Btn_Action/Text")?.GetComponent<TextMeshProUGUI>();
                UpdateActionLabel(actionTxt, item.actionType);

                if (actionBtn != null)
                {
                    actionBtn.onClick.RemoveAllListeners();
                    actionBtn.onClick.AddListener(() =>
                    {
                        // Cycle to next action type
                        int nextType = ((int)item.actionType + 1) % 4;
                        item.actionType = (GiftActionType)nextType;
                        UpdateActionLabel(actionTxt, item.actionType);
                    });
                }

                // 3. Duration Minus / Plus
                var durTxt = row.transform.Find("Duration/Text")?.GetComponent<TextMeshProUGUI>();
                if (durTxt != null) durTxt.text = $"{item.duration:F1}s";

                var btnMinus = row.transform.Find("Duration/Btn_Minus")?.GetComponent<Button>();
                var btnPlus = row.transform.Find("Duration/Btn_Plus")?.GetComponent<Button>();

                if (btnMinus != null)
                {
                    btnMinus.onClick.RemoveAllListeners();
                    btnMinus.onClick.AddListener(() =>
                    {
                        item.duration = Mathf.Max(1.0f, item.duration - 0.5f);
                        if (durTxt != null) durTxt.text = $"{item.duration:F1}s";
                    });
                }
                if (btnPlus != null)
                {
                    btnPlus.onClick.RemoveAllListeners();
                    btnPlus.onClick.AddListener(() =>
                    {
                        item.duration = Mathf.Min(20.0f, item.duration + 0.5f);
                        if (durTxt != null) durTxt.text = $"{item.duration:F1}s";
                    });
                }

                // 4. Enabled Toggle
                var toggle = row.transform.Find("Toggle_Enabled")?.GetComponent<Toggle>();
                if (toggle != null)
                {
                    toggle.onValueChanged.RemoveAllListeners();
                    toggle.isOn = item.enabled;
                    toggle.onValueChanged.AddListener((val) => item.enabled = val);
                }

                // 5. Delete Button
                var deleteBtn = row.transform.Find("Btn_Delete")?.GetComponent<Button>();
                if (deleteBtn != null)
                {
                    deleteBtn.onClick.RemoveAllListeners();
                    deleteBtn.onClick.AddListener(() => DeleteActionItem(index));
                }
            }
            else
            {
                activeRowViews[i].SetActive(false);
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(rowsContainer);
    }

    private void UpdateActionLabel(TextMeshProUGUI txt, GiftActionType type)
    {
        if (txt == null) return;
        switch (type)
        {
            case GiftActionType.SpeedBoost:
                txt.text = "<color=#00FF88>[NITRO] บูสความเร็ว</color>";
                break;
            case GiftActionType.DropBomb:
                txt.text = "<color=#FF4444>[BOMB] วางระเบิด</color>";
                break;
            case GiftActionType.ShootRPG:
                txt.text = "<color=#FFAA00>[RPG] ยิง RPG รถใกล้ๆ</color>";
                break;
            case GiftActionType.SlowAll:
                txt.text = "<color=#00D2FF>[SLOW] Slow ทุกคัน (EMP)</color>";
                break;
        }
    }

    private void OnAddActionClicked()
    {
        if (workingDatabase == null) return;

        workingDatabase.actions.Add(new GiftActionItem
        {
            giftName = "Rose",
            diamondCost = 1,
            actionType = GiftActionType.SpeedBoost,
            duration = 4.0f,
            intensity = 1.5f,
            enabled = true
        });

        RefreshActionRows();
    }

    private void DeleteActionItem(int index)
    {
        if (workingDatabase != null && index >= 0 && index < workingDatabase.actions.Count)
        {
            workingDatabase.actions.RemoveAt(index);
            RefreshActionRows();
        }
    }

    private void OnSaveClicked()
    {
        if (workingDatabase != null && GiftActionManager.Instance != null)
        {
            GiftActionManager.Instance.database = workingDatabase;
            GiftActionManager.Instance.SaveDatabase();

            if (statusText != null)
            {
                statusText.text = $"<color=#00FF88>[OK] บันทึก {workingDatabase.actions.Count} Actions ลงใน JSON เรียบร้อยแล้ว!</color>";
            }
        }
    }

    private void OnResetClicked()
    {
        workingDatabase = GiftActionConfig.CreateDefaultDatabase();
        RefreshActionRows();

        if (statusText != null)
        {
            statusText.text = "<color=#FFCC00>คืนค่าเริ่มต้นสำเร็จ กรุณากดบันทึกเพื่อนำไปใช้</color>";
        }
    }

    // ==========================================
    // SEARCHABLE SELECT2 GIFT PICKER
    // ==========================================
    public void OpenGiftPicker(int actionIndex)
    {
        editingItemIndex = actionIndex;
        if (giftPickerPopup != null)
        {
            giftPickerPopup.SetActive(true);
            giftPickerPopup.transform.SetAsLastSibling();
        }

        if (giftSearchInput != null)
        {
            giftSearchInput.text = "";
            giftSearchInput.ActivateInputField();
        }

        RenderGiftPickerList("");
    }

    private void OnSearchFilterChanged(string query)
    {
        RenderGiftPickerList(query);
    }

    private void RenderGiftPickerList(string filter)
    {
        if (giftListContainer == null || giftRowTemplate == null) return;

        List<TikTokGiftInfo> matched;
        string q = (filter ?? "").Trim();
        if (string.IsNullOrEmpty(q))
        {
            matched = TikTokGiftDictionary.GetDefaultGifts(20);
        }
        else
        {
            matched = TikTokGiftDictionary.SearchGifts(q, 10);
        }

        int count = matched.Count;
        activeGiftPickerViews.RemoveAll(x => x == null);
        while (activeGiftPickerViews.Count < count)
        {
            GameObject newRow = Instantiate(giftRowTemplate, giftListContainer);
            var le = newRow.GetComponent<LayoutElement>();
            if (le == null) le = newRow.AddComponent<LayoutElement>();
            le.minHeight = 42f;
            le.preferredHeight = 42f;
            le.flexibleWidth = 1f;

            activeGiftPickerViews.Add(newRow);
        }

        for (int i = 0; i < activeGiftPickerViews.Count; i++)
        {
            if (i < count)
            {
                var gift = matched[i];
                var row = activeGiftPickerViews[i];
                row.SetActive(true);

                // Thumbnail icon
                var thumb = row.transform.Find("Img_GiftThumb")?.GetComponent<Image>();
                if (thumb != null)
                {
                    thumb.sprite = GiftIconCache.Instance.GetGiftSprite(gift.name, gift.icon_url);
                    thumb.color = Color.white;
                }

                var txt = row.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null)
                {
                    string th = !string.IsNullOrEmpty(gift.thName) ? gift.thName : TikTokGiftDictionary.GetThaiName(gift.name);
                    txt.text = $"<b>{gift.name}</b> <size=80%><color=#A5B4CB>({th})</color></size>   <color=#FFD700>+{gift.diamonds} Coin</color>";
                }

                var btn = row.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => SelectGiftForAction(gift));
                }
            }
            else
            {
                activeGiftPickerViews[i].SetActive(false);
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(giftListContainer);
    }

    private void SelectGiftForAction(TikTokGiftInfo gift)
    {
        if (workingDatabase != null && editingItemIndex >= 0 && editingItemIndex < workingDatabase.actions.Count)
        {
            var item = workingDatabase.actions[editingItemIndex];
            item.giftName = gift.name;
            item.diamondCost = gift.diamonds;
            item.giftId = gift.id;
            item.giftIconUrl = gift.icon_url;

            RefreshActionRows();
        }

        if (giftPickerPopup != null)
        {
            giftPickerPopup.SetActive(false);
        }
    }
}
