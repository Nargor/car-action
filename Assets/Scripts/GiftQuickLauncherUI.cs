using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GiftQuickLauncherUI : MonoBehaviour
{
    public static GiftQuickLauncherUI Instance { get; private set; }

    [Header("Modal Root & Structure")]
    public GameObject modalRoot;
    public RectTransform cardsContainer;
    public GameObject cardTemplate;
    public Button closeButton;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI statusText;
    public GameObject emptyNotice;

    private List<GameObject> activeCardViews = new List<GameObject>();
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

        if (cardTemplate != null) cardTemplate.SetActive(false);
        if (modalRoot != null) modalRoot.SetActive(false);

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseModal);
        }
    }

    public void ToggleModal()
    {
        EnsureInitialized();
        if (modalRoot != null && modalRoot.activeSelf)
        {
            CloseModal();
        }
        else
        {
            OpenModal();
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

        if (titleText != null)
        {
            titleText.text = "<color=#00E5FF><b>TIKTOK GIFT QUICK LAUNCHER</b></color> <size=65%><color=#FFD700>[F5]</color></size>";
        }

        if (statusText != null)
        {
            statusText.text = "<color=#9EA7BE>คลิกที่ของขวัญเพื่อรัน Action ในเกมทันที (เทสระบบ & สตรีมเมอร์กดเอง)</color>";
        }

        RefreshActionCards();
    }

    public void CloseModal()
    {
        if (modalRoot != null)
        {
            modalRoot.SetActive(false);
        }
    }

    public void RefreshActionCards()
    {
        if (cardsContainer == null || cardTemplate == null) return;

        // Ensure latest config loaded from disk/manager
        if (GiftActionManager.Instance != null && (GiftActionManager.Instance.database == null || GiftActionManager.Instance.database.actions == null))
        {
            GiftActionManager.Instance.LoadDatabase();
        }

        var db = GiftActionManager.Instance != null ? GiftActionManager.Instance.database : GiftActionConfig.LoadConfig();
        var actions = db != null && db.actions != null ? db.actions : new List<GiftActionItem>();

        activeCardViews.RemoveAll(x => x == null);

        int count = actions.Count;
        if (emptyNotice != null)
        {
            emptyNotice.SetActive(count == 0);
        }

        while (activeCardViews.Count < count)
        {
            GameObject newCard = Instantiate(cardTemplate, cardsContainer);
            activeCardViews.Add(newCard);
        }

        for (int i = 0; i < activeCardViews.Count; i++)
        {
            if (i < count)
            {
                var action = actions[i];
                var card = activeCardViews[i];
                card.SetActive(true);

                // 1. Gift Icon
                var imgIcon = card.transform.Find("IconContainer/Img_Icon")?.GetComponent<Image>();
                if (imgIcon == null) imgIcon = card.transform.Find("Img_Icon")?.GetComponent<Image>();
                if (imgIcon != null)
                {
                    imgIcon.sprite = GiftIconCache.Instance.GetGiftSprite(action.giftName, action.giftIconUrl);
                    imgIcon.color = Color.white;
                }

                // 2. Gift Title (EN + TH)
                string thName = TikTokGiftDictionary.GetThaiName(action.giftName);
                var txtName = card.transform.Find("Txt_Name")?.GetComponent<TextMeshProUGUI>();
                if (txtName != null)
                {
                    txtName.text = $"<b>{action.giftName}</b> <size=80%><color=#A5B4CB>({thName})</color></size>";
                }

                // 3. Diamonds Cost
                var txtCoins = card.transform.Find("Txt_Coins")?.GetComponent<TextMeshProUGUI>();
                if (txtCoins != null)
                {
                    txtCoins.text = $"<color=#FFD700>+{action.diamondCost} Coin</color>";
                }

                // 4. Action Badge
                var txtBadge = card.transform.Find("Badge/Txt_Action")?.GetComponent<TextMeshProUGUI>();
                if (txtBadge == null) txtBadge = card.transform.Find("Txt_Action")?.GetComponent<TextMeshProUGUI>();
                var badgeImg = card.transform.Find("Badge")?.GetComponent<Image>();

                string actionLabel = "";
                Color badgeColor = Color.white;

                switch (action.actionType)
                {
                    case GiftActionType.SpeedBoost:
                        actionLabel = $"[NITRO] บูส {action.duration:F1}s";
                        badgeColor = new Color(0f, 0.85f, 0.45f, 1f);
                        break;
                    case GiftActionType.DropBomb:
                        actionLabel = "[BOMB] วางระเบิด";
                        badgeColor = new Color(0.95f, 0.25f, 0.25f, 1f);
                        break;
                    case GiftActionType.ShootRPG:
                        actionLabel = "[RPG] ยิง RPG รถใกล้";
                        badgeColor = new Color(1f, 0.65f, 0.1f, 1f);
                        break;
                    case GiftActionType.SlowAll:
                        actionLabel = $"[SLOW] EMP ทุกคัน {action.duration:F1}s";
                        badgeColor = new Color(0f, 0.85f, 1f, 1f);
                        break;
                }

                if (txtBadge != null)
                {
                    txtBadge.text = $"<b>{actionLabel}</b>";
                    txtBadge.color = badgeColor;
                }
                if (badgeImg != null)
                {
                    badgeImg.color = new Color(badgeColor.r * 0.2f, badgeColor.g * 0.2f, badgeColor.b * 0.2f, 0.9f);
                }

                // 5. Card Click Button
                var btn = card.GetComponent<Button>();
                if (btn != null)
                {
                    var captured = action;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => ExecuteActionDirectly(captured));
                }
            }
            else
            {
                activeCardViews[i].SetActive(false);
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(cardsContainer);
    }

    private void ExecuteActionDirectly(GiftActionItem action)
    {
        if (action == null) return;

        Debug.Log($"[GiftQuickLauncher] Triggered action for gift '{action.giftName}' ({action.actionType})");

        if (GiftActionManager.Instance != null)
        {
            GiftActionManager.Instance.ExecuteGiftAction("player", action.giftName);
        }

        if (statusText != null)
        {
            string th = TikTokGiftDictionary.GetThaiName(action.giftName);
            statusText.text = $"<color=#00FF88>⚡ ยิง Effect <b>{action.giftName} ({th})</b> -> <b>{action.actionType}</b> เรียบร้อยแล้ว!</color>";
        }
    }
}
