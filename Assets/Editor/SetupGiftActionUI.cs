#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class SetupGiftActionUI
{
    [MenuItem("Tools/Setup Gift Action UI")]
    public static void Execute()
    {
        var hudCanvasObj = GameObject.Find("HUD_Canvas");
        if (hudCanvasObj == null)
        {
            Debug.LogError("HUD_Canvas not found in scene!");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(hudCanvasObj, "Setup Gift Action UI");

        // 1. Ensure Managers on HUD_Canvas
        var giftMgr = hudCanvasObj.GetComponent<GiftActionManager>();
        if (giftMgr == null) giftMgr = hudCanvasObj.AddComponent<GiftActionManager>();
        giftMgr.apiGiftsUrl = "http://127.0.0.1:8765/api/gifts";

        if (hudCanvasObj.GetComponent<LocalizationManager>() == null)
        {
            hudCanvasObj.AddComponent<LocalizationManager>();
        }
        if (hudCanvasObj.GetComponent<GiftIconCache>() == null)
        {
            hudCanvasObj.AddComponent<GiftIconCache>();
        }

        // Find standard TMP Font
        TMP_FontAsset defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (defaultFont == null)
        {
            var allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            if (allFonts != null && allFonts.Length > 0) defaultFont = allFonts[0];
        }

        // 2. Build or Find GiftAction_Modal [F8]
        Transform modalTransform = hudCanvasObj.transform.Find("GiftAction_Modal");
        GameObject modalObj;
        if (modalTransform != null)
        {
            modalObj = modalTransform.gameObject;
        }
        else
        {
            modalObj = new GameObject("GiftAction_Modal", typeof(RectTransform));
            modalObj.transform.SetParent(hudCanvasObj.transform, false);
        }

        var modalRect = modalObj.GetComponent<RectTransform>();
        modalRect.anchorMin = Vector2.zero;
        modalRect.anchorMax = Vector2.one;
        modalRect.offsetMin = Vector2.zero;
        modalRect.offsetMax = Vector2.zero;

        var modalUI = modalObj.GetComponent<GiftActionModalUI>();
        if (modalUI == null) modalUI = modalObj.AddComponent<GiftActionModalUI>();

        // Clear existing children to rebuild cleanly
        while (modalObj.transform.childCount > 0)
        {
            GameObject.DestroyImmediate(modalObj.transform.GetChild(0).gameObject);
        }

        // 3. Modal Root (Backdrop overlay)
        var modalRootObj = new GameObject("ModalRoot", typeof(RectTransform), typeof(Image));
        modalRootObj.transform.SetParent(modalObj.transform, false);
        var rootRect = modalRootObj.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        var rootImg = modalRootObj.GetComponent<Image>();
        rootImg.color = new Color(0f, 0f, 0f, 0.78f);
        rootImg.raycastTarget = true;

        // 4. Main Card (CarStream style)
        var cardObj = new GameObject("Card", typeof(RectTransform), typeof(Image), typeof(Outline));
        cardObj.transform.SetParent(modalRootObj.transform, false);
        var cardRect = cardObj.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(880f, 620f);
        cardRect.anchoredPosition = Vector2.zero;

        var cardImg = cardObj.GetComponent<Image>();
        cardImg.color = new Color(0.08f, 0.10f, 0.16f, 0.98f);
        var cardOutline = cardObj.GetComponent<Outline>();
        cardOutline.effectColor = new Color(0f, 0.85f, 1f, 0.5f);
        cardOutline.effectDistance = new Vector2(2f, -2f);

        // Header
        var headerObj = new GameObject("Header", typeof(RectTransform));
        headerObj.transform.SetParent(cardObj.transform, false);
        var headerRect = headerObj.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.sizeDelta = new Vector2(0f, 70f);
        headerRect.anchoredPosition = Vector2.zero;

        var titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(headerObj.transform, false);
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.5f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(24f, 0f);
        titleRect.offsetMax = new Vector2(-60f, -8f);
        var titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) titleTmp.font = defaultFont;
        titleTmp.text = "<color=#00E5FF>TIKTOK GIFT ACTION MANAGER</color> <size=65%><color=#FFCC00>(CARSTREAM STYLE)</color></size>";
        titleTmp.fontSize = 22f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = Color.white;
        titleTmp.alignment = TextAlignmentOptions.MidlineLeft;

        var subObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        subObj.transform.SetParent(headerObj.transform, false);
        var subRect = subObj.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0f, 0f);
        subRect.anchorMax = new Vector2(1f, 0.5f);
        subRect.offsetMin = new Vector2(24f, 6f);
        subRect.offsetMax = new Vector2(-60f, 0f);
        var subTmp = subObj.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) subTmp.font = defaultFont;
        subTmp.text = "จับคู่ของขวัญ TikTok กับ Action ในเกม (Nitro Boost, วางระเบิด, ยิง RPG, Slow All) บันทึกลง JSON อัตโนมัติ";
        subTmp.fontSize = 13f;
        subTmp.color = new Color(0.65f, 0.72f, 0.85f, 1f);
        subTmp.alignment = TextAlignmentOptions.MidlineLeft;

        // Close Button [X]
        var closeBtnObj = CreateButton("Btn_Close", headerObj.transform, new Vector2(40f, 40f), new Color(0.85f, 0.2f, 0.2f, 0.9f), "X", defaultFont, 18);
        var closeBtnRect = closeBtnObj.GetComponent<RectTransform>();
        closeBtnRect.anchorMin = new Vector2(1f, 1f);
        closeBtnRect.anchorMax = new Vector2(1f, 1f);
        closeBtnRect.pivot = new Vector2(1f, 1f);
        closeBtnRect.anchoredPosition = new Vector2(-16f, -14f);

        // Table Column Header
        var thObj = new GameObject("TableHeader", typeof(RectTransform), typeof(Image));
        thObj.transform.SetParent(cardObj.transform, false);
        var thRect = thObj.GetComponent<RectTransform>();
        thRect.anchorMin = new Vector2(0f, 1f);
        thRect.anchorMax = new Vector2(1f, 1f);
        thRect.pivot = new Vector2(0.5f, 1f);
        thRect.anchoredPosition = new Vector2(0f, -74f);
        thRect.sizeDelta = new Vector2(-36f, 32f);
        thObj.GetComponent<Image>().color = new Color(0.12f, 0.15f, 0.22f, 0.9f);

        CreateHeaderLabel(thObj.transform, "ของขวัญ TIKTOK (GIFT)", new Vector2(16f, 0f), new Vector2(210f, 32f), defaultFont);
        CreateHeaderLabel(thObj.transform, "ACTION EFFECT (ลูกเล่นในเกม)", new Vector2(236f, 0f), new Vector2(230f, 32f), defaultFont);
        CreateHeaderLabel(thObj.transform, "ระยะเวลา (DURATION)", new Vector2(476f, 0f), new Vector2(150f, 32f), defaultFont);
        CreateHeaderLabel(thObj.transform, "เปิดใช้งาน", new Vector2(636f, 0f), new Vector2(90f, 32f), defaultFont);
        CreateHeaderLabel(thObj.transform, "ลบ", new Vector2(736f, 0f), new Vector2(80f, 32f), defaultFont);

        // Scroll Area for Action Rows
        var scrollObj = new GameObject("ScrollArea", typeof(RectTransform), typeof(ScrollRect));
        scrollObj.transform.SetParent(cardObj.transform, false);
        var scrollRect = scrollObj.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0f, 0f);
        scrollRect.anchorMax = new Vector2(1f, 1f);
        scrollRect.offsetMin = new Vector2(18f, 75f);
        scrollRect.offsetMax = new Vector2(-18f, -112f);

        // Viewport with RectMask2D
        var viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportObj.transform.SetParent(scrollObj.transform, false);
        var viewRect = viewportObj.GetComponent<RectTransform>();
        viewRect.anchorMin = Vector2.zero;
        viewRect.anchorMax = Vector2.one;
        viewRect.offsetMin = Vector2.zero;
        viewRect.offsetMax = Vector2.zero;

        var contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObj.transform.SetParent(viewportObj.transform, false);
        var contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        var vGroup = contentObj.GetComponent<VerticalLayoutGroup>();
        vGroup.childAlignment = TextAnchor.UpperLeft;
        vGroup.spacing = 6f;
        vGroup.padding = new RectOffset(8, 8, 6, 6);
        vGroup.childControlWidth = true;
        vGroup.childControlHeight = true;
        vGroup.childForceExpandWidth = true;
        vGroup.childForceExpandHeight = false;

        var fitter = contentObj.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        var sRectComponent = scrollObj.GetComponent<ScrollRect>();
        sRectComponent.content = contentRect;
        sRectComponent.viewport = viewRect;
        sRectComponent.horizontal = false;
        sRectComponent.vertical = true;
        sRectComponent.movementType = ScrollRect.MovementType.Clamped;

        // Row Template
        var rowTemplateObj = CreateRowTemplate(contentObj.transform, defaultFont);
        rowTemplateObj.SetActive(false);

        // Bottom Bar
        var bottomObj = new GameObject("BottomBar", typeof(RectTransform), typeof(Image));
        bottomObj.transform.SetParent(cardObj.transform, false);
        var bottomRect = bottomObj.GetComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0f, 0f);
        bottomRect.anchorMax = new Vector2(1f, 0f);
        bottomRect.pivot = new Vector2(0.5f, 0f);
        bottomRect.sizeDelta = new Vector2(-36f, 68f);
        bottomRect.anchoredPosition = new Vector2(0f, 6f);
        bottomObj.GetComponent<Image>().color = new Color(0.10f, 0.12f, 0.18f, 0.95f);

        // Bottom buttons
        var btnAddObj = CreateButton("Btn_AddAction", bottomObj.transform, new Vector2(180f, 44f), new Color(0f, 0.72f, 0.45f, 1f), "+ เพิ่ม ACTION ใหม่", defaultFont, 14);
        var btnAddRect = btnAddObj.GetComponent<RectTransform>();
        btnAddRect.anchorMin = new Vector2(0f, 0.5f);
        btnAddRect.anchorMax = new Vector2(0f, 0.5f);
        btnAddRect.pivot = new Vector2(0f, 0.5f);
        btnAddRect.anchoredPosition = new Vector2(14f, 0f);

        var btnSaveObj = CreateButton("Btn_Save", bottomObj.transform, new Vector2(170f, 44f), new Color(0f, 0.55f, 0.95f, 1f), "บันทึก JSON", defaultFont, 14);
        var btnSaveRect = btnSaveObj.GetComponent<RectTransform>();
        btnSaveRect.anchorMin = new Vector2(0f, 0.5f);
        btnSaveRect.anchorMax = new Vector2(0f, 0.5f);
        btnSaveRect.pivot = new Vector2(0f, 0.5f);
        btnSaveRect.anchoredPosition = new Vector2(204f, 0f);

        var btnResetObj = CreateButton("Btn_Reset", bottomObj.transform, new Vector2(150f, 44f), new Color(0.40f, 0.45f, 0.55f, 1f), "คืนค่าเริ่มต้น", defaultFont, 13);
        var btnResetRect = btnResetObj.GetComponent<RectTransform>();
        btnResetRect.anchorMin = new Vector2(0f, 0.5f);
        btnResetRect.anchorMax = new Vector2(0f, 0.5f);
        btnResetRect.pivot = new Vector2(0f, 0.5f);
        btnResetRect.anchoredPosition = new Vector2(384f, 0f);

        var statusTextObj = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        statusTextObj.transform.SetParent(bottomObj.transform, false);
        var statusRect = statusTextObj.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 0.5f);
        statusRect.anchorMax = new Vector2(1f, 0.5f);
        statusRect.offsetMin = new Vector2(550f, -22f);
        statusRect.offsetMax = new Vector2(-12f, 22f);
        var statusTmp = statusTextObj.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) statusTmp.font = defaultFont;
        statusTmp.text = "<color=#A0A5BD>พร้อมใช้งาน บันทึกลงใน JSON อัตโนมัติ</color>";
        statusTmp.fontSize = 12f;
        statusTmp.alignment = TextAlignmentOptions.MidlineLeft;

        // 5. Build Select2 Searchable Gift Picker Popup
        var pickerObj = CreateGiftPickerPopup(modalRootObj.transform, defaultFont);
        pickerObj.SetActive(false);

        // Wire fields on GiftActionModalUI
        modalUI.modalRoot = modalRootObj;
        modalUI.rowsContainer = contentRect;
        modalUI.rowTemplate = rowTemplateObj;
        modalUI.addActionButton = btnAddObj.GetComponent<Button>();
        modalUI.saveButton = btnSaveObj.GetComponent<Button>();
        modalUI.resetButton = btnResetObj.GetComponent<Button>();
        modalUI.closeButton = closeBtnObj.GetComponent<Button>();
        modalUI.statusText = statusTmp;

        modalUI.giftPickerPopup = pickerObj;
        modalUI.giftSearchInput = pickerObj.transform.Find("SearchBox/InputField").GetComponent<TMP_InputField>();
        modalUI.giftListContainer = pickerObj.transform.Find("ScrollArea/Viewport/Content").GetComponent<RectTransform>();
        modalUI.giftRowTemplate = pickerObj.transform.Find("ScrollArea/Viewport/Content/GiftRowTemplate").gameObject;
        modalUI.closePickerButton = pickerObj.transform.Find("Header/Btn_ClosePicker").GetComponent<Button>();

        // 6. Connect Button in TikTok_Lobby_Panel (SetupCard)
        var setupCard = hudCanvasObj.transform.Find("TikTok_Lobby_Panel/SetupCard");
        if (setupCard != null)
        {
            var btnGiftInLobby = setupCard.Find("Btn_GIFT_ACTIONS");
            GameObject btnGiftObj;
            if (btnGiftInLobby == null)
            {
                btnGiftObj = CreateButton("Btn_GIFT_ACTIONS", setupCard, new Vector2(400f, 44f), new Color(0.95f, 0.45f, 0.10f, 1f), "<b>ตั้งค่าของขวัญ (GIFT ACTIONS)</b>", defaultFont, 14);
                var bRect = btnGiftObj.GetComponent<RectTransform>();
                bRect.anchoredPosition = new Vector2(0f, -320f);
            }
            else
            {
                btnGiftObj = btnGiftInLobby.gameObject;
                var t = btnGiftObj.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null) t.text = "<b>ตั้งค่าของขวัญ (GIFT ACTIONS)</b>";
            }

            var menuMgr = UnityEngine.Object.FindAnyObjectByType<MenuManager>();
            if (menuMgr != null)
            {
                menuMgr.btnOpenGiftActions = btnGiftObj.GetComponent<Button>();
                EditorUtility.SetDirty(menuMgr);
                Debug.Log("[Setup] Wired MenuManager.btnOpenGiftActions");
            }
        }

        // 7. Connect Button in TikTok_Join_Panel (BottomBar or Header)
        var joinPanel = hudCanvasObj.transform.Find("TikTok_Join_Panel");
        if (joinPanel != null)
        {
            var header = joinPanel.Find("Header");
            Transform parentTarget = header != null ? header : joinPanel;
            var btnGiftInJoin = parentTarget.Find("Btn_GIFT_ACTIONS");
            GameObject btnGiftObj;
            if (btnGiftInJoin == null)
            {
                btnGiftObj = CreateButton("Btn_GIFT_ACTIONS", parentTarget, new Vector2(140f, 36f), new Color(0.95f, 0.45f, 0.10f, 1f), "<b>GIFTS</b>", defaultFont, 13);
                var bRect = btnGiftObj.GetComponent<RectTransform>();
                bRect.anchorMin = new Vector2(1f, 1f);
                bRect.anchorMax = new Vector2(1f, 1f);
                bRect.pivot = new Vector2(1f, 1f);
                bRect.anchoredPosition = new Vector2(-60f, -8f);
            }
            else
            {
                btnGiftObj = btnGiftInJoin.gameObject;
                var t = btnGiftObj.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null) t.text = "<b>GIFTS</b>";
            }

            var joinMgr = UnityEngine.Object.FindAnyObjectByType<TikTokJoinPanelManager>();
            if (joinMgr != null)
            {
                joinMgr.btnOpenGiftActions = btnGiftObj.GetComponent<Button>();
                EditorUtility.SetDirty(joinMgr);
                Debug.Log("[Setup] Wired TikTokJoinPanelManager.btnOpenGiftActions");
            }
        }

        // 8. Build F5 Quick Launcher Modal
        BuildGiftQuickLauncherModal(hudCanvasObj, defaultFont);

        // 9. Connect HUD QuickToggle Bar [F5, F6, F7, F8]
        var qtb = hudCanvasObj.transform.Find("HUD_Panel/QuickToggleBar");
        if (qtb != null)
        {
            // F5 Toggle Button
            var existingF5 = qtb.Find("Btn_TOGGLE_F5");
            GameObject btnF5Obj;
            if (existingF5 == null)
            {
                var f7 = qtb.Find("Btn_TOGGLE_F7");
                btnF5Obj = UnityEngine.Object.Instantiate(f7.gameObject, qtb);
                btnF5Obj.name = "Btn_TOGGLE_F5";
                var txt = btnF5Obj.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = "<b>[F5] ACTIONS</b>";
                var img = btnF5Obj.GetComponent<Image>();
                if (img != null) img.color = new Color(0.85f, 0.15f, 0.65f, 0.90f);
            }
            else
            {
                btnF5Obj = existingF5.gameObject;
                var txt = btnF5Obj.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = "<b>[F5] ACTIONS</b>";
            }
            btnF5Obj.transform.SetSiblingIndex(0);

            // F8 Toggle Button
            var existingF8 = qtb.Find("Btn_TOGGLE_F8");
            GameObject btnF8Obj;
            if (existingF8 == null)
            {
                var f7 = qtb.Find("Btn_TOGGLE_F7");
                btnF8Obj = UnityEngine.Object.Instantiate(f7.gameObject, qtb);
                btnF8Obj.name = "Btn_TOGGLE_F8";
                var txt = btnF8Obj.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = "<b>[F8] SETTINGS</b>";
                var img = btnF8Obj.GetComponent<Image>();
                if (img != null) img.color = new Color(0.95f, 0.45f, 0.10f, 0.85f);
            }
            else
            {
                btnF8Obj = existingF8.gameObject;
                var txt = btnF8Obj.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = "<b>[F8] SETTINGS</b>";
            }

            var joinMgr = UnityEngine.Object.FindAnyObjectByType<TikTokJoinPanelManager>();
            if (joinMgr != null)
            {
                joinMgr.btnToggleF5 = btnF5Obj.GetComponent<Button>();
                joinMgr.btnToggleF8 = btnF8Obj.GetComponent<Button>();

                var f6Obj = qtb.Find("Btn_TOGGLE_F6");
                if (f6Obj != null) joinMgr.btnToggleF6 = f6Obj.GetComponent<Button>();
                var f7Obj = qtb.Find("Btn_TOGGLE_F7");
                if (f7Obj != null) joinMgr.btnToggleF7 = f7Obj.GetComponent<Button>();

                EditorUtility.SetDirty(joinMgr);
            }
        }

        // Initially hide the modal
        modalRootObj.SetActive(false);

        EditorUtility.SetDirty(modalUI);
        EditorUtility.SetDirty(modalObj);
        EditorUtility.SetDirty(hudCanvasObj);
        EditorSceneManager.MarkSceneDirty(hudCanvasObj.scene);
        EditorSceneManager.SaveScene(hudCanvasObj.scene);

        Debug.Log("✅ [SetupGiftActionUI] Successfully built and wired F8 Settings Modal and F5 Quick Launcher Modal!");
    }

    private static void BuildGiftQuickLauncherModal(GameObject hudCanvasObj, TMP_FontAsset defaultFont)
    {
        Transform modalTransform = hudCanvasObj.transform.Find("GiftQuickLauncher_Modal");
        GameObject modalObj;
        if (modalTransform != null)
        {
            modalObj = modalTransform.gameObject;
        }
        else
        {
            modalObj = new GameObject("GiftQuickLauncher_Modal", typeof(RectTransform));
            modalObj.transform.SetParent(hudCanvasObj.transform, false);
        }

        var modalRect = modalObj.GetComponent<RectTransform>();
        modalRect.anchorMin = Vector2.zero;
        modalRect.anchorMax = Vector2.one;
        modalRect.offsetMin = Vector2.zero;
        modalRect.offsetMax = Vector2.zero;

        var launcherUI = modalObj.GetComponent<GiftQuickLauncherUI>();
        if (launcherUI == null) launcherUI = modalObj.AddComponent<GiftQuickLauncherUI>();

        // Clear existing children to rebuild cleanly
        while (modalObj.transform.childCount > 0)
        {
            GameObject.DestroyImmediate(modalObj.transform.GetChild(0).gameObject);
        }

        // 1. Modal Root (Backdrop)
        var modalRootObj = new GameObject("ModalRoot", typeof(RectTransform), typeof(Image));
        modalRootObj.transform.SetParent(modalObj.transform, false);
        var rootRect = modalRootObj.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        var rootImg = modalRootObj.GetComponent<Image>();
        rootImg.color = new Color(0f, 0f, 0f, 0.78f);
        rootImg.raycastTarget = true;

        // 2. Main Card
        var cardObj = new GameObject("Card", typeof(RectTransform), typeof(Image), typeof(Outline));
        cardObj.transform.SetParent(modalRootObj.transform, false);
        var cardRect = cardObj.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(860f, 540f);
        cardRect.anchoredPosition = Vector2.zero;

        var cardImg = cardObj.GetComponent<Image>();
        cardImg.color = new Color(0.08f, 0.10f, 0.16f, 0.98f);
        var cardOutline = cardObj.GetComponent<Outline>();
        cardOutline.effectColor = new Color(1f, 0.25f, 0.75f, 0.65f);
        cardOutline.effectDistance = new Vector2(2f, -2f);

        // Header
        var headerObj = new GameObject("Header", typeof(RectTransform));
        headerObj.transform.SetParent(cardObj.transform, false);
        var headerRect = headerObj.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.sizeDelta = new Vector2(0f, 70f);
        headerRect.anchoredPosition = Vector2.zero;

        var titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(headerObj.transform, false);
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.5f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(24f, 0f);
        titleRect.offsetMax = new Vector2(-60f, -8f);
        var titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) titleTmp.font = defaultFont;
        titleTmp.text = "<color=#00E5FF><b>TIKTOK GIFT QUICK LAUNCHER</b></color> <size=65%><color=#FFD700>[F5]</color></size>";
        titleTmp.fontSize = 22f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = Color.white;
        titleTmp.alignment = TextAlignmentOptions.MidlineLeft;

        var subObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        subObj.transform.SetParent(headerObj.transform, false);
        var subRect = subObj.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0f, 0f);
        subRect.anchorMax = new Vector2(1f, 0.5f);
        subRect.offsetMin = new Vector2(24f, 6f);
        subRect.offsetMax = new Vector2(-60f, 0f);
        var subTmp = subObj.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) subTmp.font = defaultFont;
        subTmp.text = "คลิกของขวัญเพื่อรัน Action ในเกมทันที (สำหรับสตรีมเมอร์ & ทดสอบระบบการส่งของขวัญ)";
        subTmp.fontSize = 13f;
        subTmp.color = new Color(0.65f, 0.72f, 0.85f, 1f);
        subTmp.alignment = TextAlignmentOptions.MidlineLeft;

        // Close Button [X]
        var closeBtnObj = CreateButton("Btn_Close", headerObj.transform, new Vector2(40f, 40f), new Color(0.85f, 0.2f, 0.2f, 0.9f), "X", defaultFont, 18);
        var closeBtnRect = closeBtnObj.GetComponent<RectTransform>();
        closeBtnRect.anchorMin = new Vector2(1f, 1f);
        closeBtnRect.anchorMax = new Vector2(1f, 1f);
        closeBtnRect.pivot = new Vector2(1f, 1f);
        closeBtnRect.anchoredPosition = new Vector2(-16f, -14f);

        // Scroll Area for Cards
        var scrollObj = new GameObject("ScrollArea", typeof(RectTransform), typeof(ScrollRect));
        scrollObj.transform.SetParent(cardObj.transform, false);
        var scrollRect = scrollObj.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0f, 0f);
        scrollRect.anchorMax = new Vector2(1f, 1f);
        scrollRect.offsetMin = new Vector2(18f, 64f);
        scrollRect.offsetMax = new Vector2(-18f, -76f);

        var viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportObj.transform.SetParent(scrollObj.transform, false);
        var viewRect = viewportObj.GetComponent<RectTransform>();
        viewRect.anchorMin = Vector2.zero;
        viewRect.anchorMax = Vector2.one;
        viewRect.offsetMin = Vector2.zero;
        viewRect.offsetMax = Vector2.zero;

        var contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObj.transform.SetParent(viewportObj.transform, false);
        var contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        var vGroup = contentObj.GetComponent<VerticalLayoutGroup>();
        vGroup.childAlignment = TextAnchor.UpperLeft;
        vGroup.spacing = 6f;
        vGroup.padding = new RectOffset(6, 6, 6, 6);
        vGroup.childControlWidth = true;
        vGroup.childControlHeight = true;
        vGroup.childForceExpandWidth = true;
        vGroup.childForceExpandHeight = false;

        var fitter = contentObj.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        var sRectComponent = scrollObj.GetComponent<ScrollRect>();
        sRectComponent.content = contentRect;
        sRectComponent.viewport = viewRect;
        sRectComponent.horizontal = false;
        sRectComponent.vertical = true;
        sRectComponent.movementType = ScrollRect.MovementType.Clamped;

        // Card Template
        var cardTemplateObj = new GameObject("CardTemplate", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        cardTemplateObj.transform.SetParent(contentObj.transform, false);
        var ctRect = cardTemplateObj.GetComponent<RectTransform>();
        ctRect.sizeDelta = new Vector2(0f, 52f);
        var ctImg = cardTemplateObj.GetComponent<Image>();
        ctImg.color = new Color(0.13f, 0.16f, 0.25f, 0.95f);

        var btn = cardTemplateObj.GetComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = new Color(0.13f, 0.16f, 0.25f, 0.95f);
        colors.highlightedColor = new Color(0.20f, 0.26f, 0.40f, 1f);
        colors.pressedColor = new Color(0.10f, 0.12f, 0.20f, 1f);
        btn.colors = colors;

        var le = cardTemplateObj.GetComponent<LayoutElement>();
        le.minHeight = 52f;
        le.preferredHeight = 52f;
        le.flexibleWidth = 1f;

        // Card Thumbnail Icon
        var iconObj = new GameObject("Img_Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(cardTemplateObj.transform, false);
        var ir = iconObj.GetComponent<RectTransform>();
        ir.anchorMin = new Vector2(0f, 0.5f);
        ir.anchorMax = new Vector2(0f, 0.5f);
        ir.pivot = new Vector2(0f, 0.5f);
        ir.anchoredPosition = new Vector2(10f, 0f);
        ir.sizeDelta = new Vector2(36f, 36f);

        // Card Name
        var nameObj = new GameObject("Txt_Name", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObj.transform.SetParent(cardTemplateObj.transform, false);
        var nr = nameObj.GetComponent<RectTransform>();
        nr.anchorMin = new Vector2(0f, 0.5f);
        nr.anchorMax = new Vector2(0f, 0.5f);
        nr.pivot = new Vector2(0f, 0.5f);
        nr.anchoredPosition = new Vector2(56f, 0f);
        nr.sizeDelta = new Vector2(230f, 36f);
        var nt = nameObj.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) nt.font = defaultFont;
        nt.fontSize = 15f;
        nt.fontStyle = FontStyles.Bold;
        nt.alignment = TextAlignmentOptions.MidlineLeft;
        nt.color = Color.white;
        nt.text = "<b>Rose</b> (กุหลาบ)";

        // Card Coins
        var coinObj = new GameObject("Txt_Coins", typeof(RectTransform), typeof(TextMeshProUGUI));
        coinObj.transform.SetParent(cardTemplateObj.transform, false);
        var cr = coinObj.GetComponent<RectTransform>();
        cr.anchorMin = new Vector2(0f, 0.5f);
        cr.anchorMax = new Vector2(0f, 0.5f);
        cr.pivot = new Vector2(0f, 0.5f);
        cr.anchoredPosition = new Vector2(296f, 0f);
        cr.sizeDelta = new Vector2(110f, 36f);
        var ctText = coinObj.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) ctText.font = defaultFont;
        ctText.fontSize = 13f;
        ctText.alignment = TextAlignmentOptions.MidlineLeft;
        ctText.color = new Color(1f, 0.85f, 0.2f);
        ctText.text = "+1 Coin";

        // Badge
        var badgeObj = new GameObject("Badge", typeof(RectTransform), typeof(Image));
        badgeObj.transform.SetParent(cardTemplateObj.transform, false);
        var br = badgeObj.GetComponent<RectTransform>();
        br.anchorMin = new Vector2(0f, 0.5f);
        br.anchorMax = new Vector2(0f, 0.5f);
        br.pivot = new Vector2(0f, 0.5f);
        br.anchoredPosition = new Vector2(416f, 0f);
        br.sizeDelta = new Vector2(240f, 34f);
        badgeObj.GetComponent<Image>().color = new Color(0f, 0.35f, 0.20f, 0.85f);

        var actionTxtObj = new GameObject("Txt_Action", typeof(RectTransform), typeof(TextMeshProUGUI));
        actionTxtObj.transform.SetParent(badgeObj.transform, false);
        var atr = actionTxtObj.GetComponent<RectTransform>();
        atr.anchorMin = Vector2.zero;
        atr.anchorMax = Vector2.one;
        atr.offsetMin = Vector2.zero;
        atr.offsetMax = Vector2.zero;
        var actTmp = actionTxtObj.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) actTmp.font = defaultFont;
        actTmp.text = "<b>[NITRO] บูสความเร็ว 4.0s</b>";
        actTmp.fontSize = 13f;
        actTmp.alignment = TextAlignmentOptions.Center;
        actTmp.color = new Color(0f, 1f, 0.55f);

        // Click hint
        var hintObj = new GameObject("Txt_Hint", typeof(RectTransform), typeof(TextMeshProUGUI));
        hintObj.transform.SetParent(cardTemplateObj.transform, false);
        var hr = hintObj.GetComponent<RectTransform>();
        hr.anchorMin = new Vector2(1f, 0.5f);
        hr.anchorMax = new Vector2(1f, 0.5f);
        hr.pivot = new Vector2(1f, 0.5f);
        hr.anchoredPosition = new Vector2(-16f, 0f);
        hr.sizeDelta = new Vector2(120f, 34f);
        var hintTmp = hintObj.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) hintTmp.font = defaultFont;
        hintTmp.text = "<color=#6B7B99>[คลิกเพื่อรัน]</color>";
        hintTmp.fontSize = 12f;
        hintTmp.alignment = TextAlignmentOptions.MidlineRight;

        cardTemplateObj.SetActive(false);

        // Bottom Bar
        var bottomObj = new GameObject("BottomBar", typeof(RectTransform), typeof(Image));
        bottomObj.transform.SetParent(cardObj.transform, false);
        var bottomRect = bottomObj.GetComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0f, 0f);
        bottomRect.anchorMax = new Vector2(1f, 0f);
        bottomRect.pivot = new Vector2(0.5f, 0f);
        bottomRect.sizeDelta = new Vector2(-36f, 56f);
        bottomRect.anchoredPosition = new Vector2(0f, 6f);
        bottomObj.GetComponent<Image>().color = new Color(0.10f, 0.12f, 0.18f, 0.95f);

        var statusTextObj = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        statusTextObj.transform.SetParent(bottomObj.transform, false);
        var statusRect = statusTextObj.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 0.5f);
        statusRect.anchorMax = new Vector2(1f, 0.5f);
        statusRect.offsetMin = new Vector2(20f, -22f);
        statusRect.offsetMax = new Vector2(-220f, 22f);
        var statusTmp = statusTextObj.GetComponent<TextMeshProUGUI>();
        if (defaultFont != null) statusTmp.font = defaultFont;
        statusTmp.text = "<color=#00FF88>⚡ คลิกที่ของขวัญด้านบนเพื่อรัน Action ในเกมทันที</color>";
        statusTmp.fontSize = 13f;
        statusTmp.alignment = TextAlignmentOptions.MidlineLeft;

        var btnF8Obj = CreateButton("Btn_GoSettings", bottomObj.transform, new Vector2(200f, 38f), new Color(0.95f, 0.45f, 0.10f, 1f), "<b>[F8] ตั้งค่าของขวัญ</b>", defaultFont, 13);
        var bf8Rect = btnF8Obj.GetComponent<RectTransform>();
        bf8Rect.anchorMin = new Vector2(1f, 0.5f);
        bf8Rect.anchorMax = new Vector2(1f, 0.5f);
        bf8Rect.pivot = new Vector2(1f, 0.5f);
        bf8Rect.anchoredPosition = new Vector2(-12f, 0f);

        // Wire Launcher UI references
        launcherUI.modalRoot = modalRootObj;
        launcherUI.cardsContainer = contentRect;
        launcherUI.cardTemplate = cardTemplateObj;
        launcherUI.closeButton = closeBtnObj.GetComponent<Button>();
        launcherUI.titleText = titleTmp;
        launcherUI.statusText = statusTmp;

        modalRootObj.SetActive(false);

        EditorUtility.SetDirty(launcherUI);
        EditorUtility.SetDirty(modalObj);
    }

    private static GameObject CreateRowTemplate(Transform parent, TMP_FontAsset font)
    {
        var rowObj = new GameObject("RowTemplate", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        rowObj.transform.SetParent(parent, false);
        var rRect = rowObj.GetComponent<RectTransform>();
        rRect.anchorMin = new Vector2(0f, 1f);
        rRect.anchorMax = new Vector2(1f, 1f);
        rRect.pivot = new Vector2(0f, 1f);
        rRect.sizeDelta = new Vector2(0f, 52f);
        rowObj.GetComponent<Image>().color = new Color(0.14f, 0.17f, 0.25f, 0.95f);

        var le = rowObj.GetComponent<LayoutElement>();
        le.minHeight = 52f;
        le.preferredHeight = 52f;
        le.flexibleWidth = 1f;

        // 1. Img_GiftThumb (Thumbnail)
        var thumbObj = new GameObject("Img_GiftThumb", typeof(RectTransform), typeof(Image));
        thumbObj.transform.SetParent(rowObj.transform, false);
        var thumbRect = thumbObj.GetComponent<RectTransform>();
        thumbRect.anchorMin = new Vector2(0f, 0.5f);
        thumbRect.anchorMax = new Vector2(0f, 0.5f);
        thumbRect.pivot = new Vector2(0f, 0.5f);
        thumbRect.anchoredPosition = new Vector2(10f, 0f);
        thumbRect.sizeDelta = new Vector2(36f, 36f);

        // 2. Btn_Gift (Select2 Trigger)
        var btnGift = CreateButton("Btn_Gift", rowObj.transform, new Vector2(165f, 38f), new Color(0.20f, 0.24f, 0.36f, 1f), "<b>Rose</b> <size=85%><color=#FFD700>(+1 Coin)</color></size>", font, 13);
        var bgRect = btnGift.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0.5f);
        bgRect.anchorMax = new Vector2(0f, 0.5f);
        bgRect.pivot = new Vector2(0f, 0.5f);
        bgRect.anchoredPosition = new Vector2(52f, 0f);

        // 3. Btn_Action (Cycle effect)
        var btnAction = CreateButton("Btn_Action", rowObj.transform, new Vector2(220f, 38f), new Color(0.18f, 0.26f, 0.38f, 1f), "<color=#00FF88>[NITRO] บูสความเร็ว</color>", font, 13);
        var baRect = btnAction.GetComponent<RectTransform>();
        baRect.anchorMin = new Vector2(0f, 0.5f);
        baRect.anchorMax = new Vector2(0f, 0.5f);
        baRect.pivot = new Vector2(0f, 0.5f);
        baRect.anchoredPosition = new Vector2(224f, 0f);

        // 4. Duration Container
        var durObj = new GameObject("Duration", typeof(RectTransform));
        durObj.transform.SetParent(rowObj.transform, false);
        var durRect = durObj.GetComponent<RectTransform>();
        durRect.anchorMin = new Vector2(0f, 0.5f);
        durRect.anchorMax = new Vector2(0f, 0.5f);
        durRect.pivot = new Vector2(0f, 0.5f);
        durRect.anchoredPosition = new Vector2(460f, 0f);
        durRect.sizeDelta = new Vector2(140f, 38f);

        var btnMinus = CreateButton("Btn_Minus", durObj.transform, new Vector2(32f, 32f), new Color(0.25f, 0.30f, 0.42f, 1f), "-", font, 16);
        btnMinus.GetComponent<RectTransform>().anchoredPosition = new Vector2(-48f, 0f);

        var durTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        durTxtObj.transform.SetParent(durObj.transform, false);
        var dtRect = durTxtObj.GetComponent<RectTransform>();
        dtRect.sizeDelta = new Vector2(56f, 32f);
        dtRect.anchoredPosition = Vector2.zero;
        var dtTmp = durTxtObj.GetComponent<TextMeshProUGUI>();
        if (font != null) dtTmp.font = font;
        dtTmp.text = "4.0s";
        dtTmp.fontSize = 13f;
        dtTmp.alignment = TextAlignmentOptions.Center;
        dtTmp.color = Color.white;

        var btnPlus = CreateButton("Btn_Plus", durObj.transform, new Vector2(32f, 32f), new Color(0.25f, 0.30f, 0.42f, 1f), "+", font, 16);
        btnPlus.GetComponent<RectTransform>().anchoredPosition = new Vector2(48f, 0f);

        // 5. Toggle_Enabled
        var toggleObj = new GameObject("Toggle_Enabled", typeof(RectTransform), typeof(Toggle));
        toggleObj.transform.SetParent(rowObj.transform, false);
        var tRect = toggleObj.GetComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0f, 0.5f);
        tRect.anchorMax = new Vector2(0f, 0.5f);
        tRect.pivot = new Vector2(0.5f, 0.5f);
        tRect.anchoredPosition = new Vector2(660f, 0f);
        tRect.sizeDelta = new Vector2(32f, 32f);

        var tBgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
        tBgObj.transform.SetParent(toggleObj.transform, false);
        var tBgRect = tBgObj.GetComponent<RectTransform>();
        tBgRect.sizeDelta = new Vector2(28f, 28f);
        tBgObj.GetComponent<Image>().color = new Color(0.25f, 0.30f, 0.42f, 1f);

        var tCheckObj = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        tCheckObj.transform.SetParent(tBgObj.transform, false);
        var tcRect = tCheckObj.GetComponent<RectTransform>();
        tcRect.sizeDelta = new Vector2(20f, 20f);
        var checkImg = tCheckObj.GetComponent<Image>();
        checkImg.color = new Color(0f, 0.85f, 0.45f, 1f);

        var toggle = toggleObj.GetComponent<Toggle>();
        toggle.targetGraphic = tBgObj.GetComponent<Image>();
        toggle.graphic = checkImg;
        toggle.isOn = true;

        // 6. Btn_Delete
        var btnDel = CreateButton("Btn_Delete", rowObj.transform, new Vector2(50f, 34f), new Color(0.75f, 0.22f, 0.22f, 0.9f), "ลบ", font, 13);
        var bdRect = btnDel.GetComponent<RectTransform>();
        bdRect.anchorMin = new Vector2(0f, 0.5f);
        bdRect.anchorMax = new Vector2(0f, 0.5f);
        bdRect.pivot = new Vector2(0.5f, 0.5f);
        bdRect.anchoredPosition = new Vector2(760f, 0f);

        return rowObj;
    }

    private static GameObject CreateGiftPickerPopup(Transform parent, TMP_FontAsset font)
    {
        var popupObj = new GameObject("GiftPickerPopup", typeof(RectTransform), typeof(Image), typeof(Outline));
        popupObj.transform.SetParent(parent, false);
        var pRect = popupObj.GetComponent<RectTransform>();
        pRect.anchorMin = new Vector2(0.5f, 0.5f);
        pRect.anchorMax = new Vector2(0.5f, 0.5f);
        pRect.sizeDelta = new Vector2(560f, 520f);
        pRect.anchoredPosition = Vector2.zero;

        popupObj.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.14f, 0.99f);
        var outline = popupObj.GetComponent<Outline>();
        outline.effectColor = new Color(1f, 0.84f, 0f, 0.7f);
        outline.effectDistance = new Vector2(2f, -2f);

        // Header
        var hObj = new GameObject("Header", typeof(RectTransform));
        hObj.transform.SetParent(popupObj.transform, false);
        var hRect = hObj.GetComponent<RectTransform>();
        hRect.anchorMin = new Vector2(0f, 1f);
        hRect.anchorMax = new Vector2(1f, 1f);
        hRect.pivot = new Vector2(0.5f, 1f);
        hRect.sizeDelta = new Vector2(0f, 52f);
        hRect.anchoredPosition = Vector2.zero;

        var titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(hObj.transform, false);
        var tRect = titleObj.GetComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0f, 0f);
        tRect.anchorMax = new Vector2(1f, 1f);
        tRect.offsetMin = new Vector2(20f, 0f);
        tRect.offsetMax = new Vector2(-50f, 0f);
        var tTmp = titleObj.GetComponent<TextMeshProUGUI>();
        if (font != null) tTmp.font = font;
        tTmp.text = "<b>เลือกของขวัญ TikTok (SELECT GIFT)</b>";
        tTmp.fontSize = 17f;
        tTmp.color = new Color(1f, 0.85f, 0.2f, 1f);
        tTmp.alignment = TextAlignmentOptions.MidlineLeft;

        var closeBtn = CreateButton("Btn_ClosePicker", hObj.transform, new Vector2(34f, 34f), new Color(0.75f, 0.22f, 0.22f, 0.9f), "X", font, 16);
        var cbRect = closeBtn.GetComponent<RectTransform>();
        cbRect.anchorMin = new Vector2(1f, 1f);
        cbRect.anchorMax = new Vector2(1f, 1f);
        cbRect.pivot = new Vector2(1f, 1f);
        cbRect.anchoredPosition = new Vector2(-12f, -9f);

        // Select2 Search Input Box
        var searchBoxObj = new GameObject("SearchBox", typeof(RectTransform));
        searchBoxObj.transform.SetParent(popupObj.transform, false);
        var sbRect = searchBoxObj.GetComponent<RectTransform>();
        sbRect.anchorMin = new Vector2(0f, 1f);
        sbRect.anchorMax = new Vector2(1f, 1f);
        sbRect.pivot = new Vector2(0.5f, 1f);
        sbRect.sizeDelta = new Vector2(-36f, 44f);
        sbRect.anchoredPosition = new Vector2(0f, -54f);

        var inputFieldObj = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        inputFieldObj.transform.SetParent(searchBoxObj.transform, false);
        var ifRect = inputFieldObj.GetComponent<RectTransform>();
        ifRect.anchorMin = Vector2.zero;
        ifRect.anchorMax = Vector2.one;
        ifRect.offsetMin = Vector2.zero;
        ifRect.offsetMax = Vector2.zero;
        inputFieldObj.GetComponent<Image>().color = new Color(0.14f, 0.18f, 0.28f, 1f);

        var textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(inputFieldObj.transform, false);
        var txtRect = textObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = new Vector2(14f, 2f);
        txtRect.offsetMax = new Vector2(-14f, -2f);
        var txtTmp = textObj.GetComponent<TextMeshProUGUI>();
        if (font != null) txtTmp.font = font;
        txtTmp.fontSize = 15f;
        txtTmp.color = Color.white;
        txtTmp.alignment = TextAlignmentOptions.MidlineLeft;

        var phObj = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        phObj.transform.SetParent(inputFieldObj.transform, false);
        var phRect = phObj.GetComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.offsetMin = new Vector2(14f, 2f);
        phRect.offsetMax = new Vector2(-14f, -2f);
        var phTmp = phObj.GetComponent<TextMeshProUGUI>();
        if (font != null) phTmp.font = font;
        phTmp.text = "พิมพ์ค้นหา เช่น กุหลาบ, หมวก, ปืน, สิงโต, Rose...";
        phTmp.fontSize = 14f;
        phTmp.fontStyle = FontStyles.Italic;
        phTmp.color = new Color(0.55f, 0.60f, 0.72f, 0.9f);
        phTmp.alignment = TextAlignmentOptions.MidlineLeft;

        var tmpInput = inputFieldObj.GetComponent<TMP_InputField>();
        tmpInput.textViewport = ifRect;
        tmpInput.textComponent = txtTmp;
        tmpInput.placeholder = phTmp;

        // Scroll Area for Gifts
        var scrollObj = new GameObject("ScrollArea", typeof(RectTransform), typeof(ScrollRect));
        scrollObj.transform.SetParent(popupObj.transform, false);
        var scrollRect = scrollObj.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0f, 0f);
        scrollRect.anchorMax = new Vector2(1f, 1f);
        scrollRect.offsetMin = new Vector2(18f, 16f);
        scrollRect.offsetMax = new Vector2(-18f, -106f);

        // Viewport with RectMask2D
        var viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewportObj.transform.SetParent(scrollObj.transform, false);
        var viewRect = viewportObj.GetComponent<RectTransform>();
        viewRect.anchorMin = Vector2.zero;
        viewRect.anchorMax = Vector2.one;
        viewRect.offsetMin = Vector2.zero;
        viewRect.offsetMax = Vector2.zero;

        var contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObj.transform.SetParent(viewportObj.transform, false);
        var contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        var vGroup = contentObj.GetComponent<VerticalLayoutGroup>();
        vGroup.childAlignment = TextAnchor.UpperLeft;
        vGroup.spacing = 4f;
        vGroup.padding = new RectOffset(4, 4, 4, 4);
        vGroup.childControlWidth = true;
        vGroup.childControlHeight = true;
        vGroup.childForceExpandWidth = true;
        vGroup.childForceExpandHeight = false;

        var fitter = contentObj.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        var sRect = scrollObj.GetComponent<ScrollRect>();
        sRect.content = contentRect;
        sRect.viewport = viewRect;
        sRect.horizontal = false;
        sRect.vertical = true;
        sRect.movementType = ScrollRect.MovementType.Clamped;

        // Gift Row Template
        var giftRowObj = CreateButton("GiftRowTemplate", contentObj.transform, new Vector2(0f, 44f), new Color(0.13f, 0.16f, 0.24f, 0.95f), "Rose   +1 Coin", font, 14);
        var le = giftRowObj.AddComponent<LayoutElement>();
        le.minHeight = 44f;
        le.preferredHeight = 44f;
        le.flexibleWidth = 1f;

        // Add Thumbnail Image inside GiftRowTemplate
        var thumbObj = new GameObject("Img_GiftThumb", typeof(RectTransform), typeof(Image));
        thumbObj.transform.SetParent(giftRowObj.transform, false);
        var gThumbRect = thumbObj.GetComponent<RectTransform>();
        gThumbRect.anchorMin = new Vector2(0f, 0.5f);
        gThumbRect.anchorMax = new Vector2(0f, 0.5f);
        gThumbRect.pivot = new Vector2(0f, 0.5f);
        gThumbRect.anchoredPosition = new Vector2(8f, 0f);
        gThumbRect.sizeDelta = new Vector2(30f, 30f);

        var grTmp = giftRowObj.GetComponentInChildren<TextMeshProUGUI>();
        if (grTmp != null) grTmp.alignment = TextAlignmentOptions.MidlineLeft;
        var grRect = grTmp?.GetComponent<RectTransform>();
        if (grRect != null)
        {
            grRect.offsetMin = new Vector2(46f, 0f);
            grRect.offsetMax = new Vector2(-16f, 0f);
        }
        giftRowObj.SetActive(false);

        return popupObj;
    }

    private static GameObject CreateButton(string name, Transform parent, Vector2 size, Color color, string text, TMP_FontAsset font, int fontSize)
    {
        var btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        var rect = btnObj.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        var img = btnObj.GetComponent<Image>();
        img.color = color;

        var btn = btnObj.GetComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.25f;
        colors.pressedColor = color * 0.8f;
        btn.colors = colors;

        var txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(btnObj.transform, false);
        var tRect = txtObj.GetComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.offsetMin = Vector2.zero;
        tRect.offsetMax = Vector2.zero;
        var tmp = txtObj.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return btnObj;
    }

    private static void CreateHeaderLabel(Transform parent, string text, Vector2 pos, Vector2 size, TMP_FontAsset font)
    {
        var labelObj = new GameObject("Th_" + text, typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(parent, false);
        var rect = labelObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        var tmp = labelObj.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = $"<b>{text}</b>";
        tmp.fontSize = 12f;
        tmp.color = new Color(0.65f, 0.75f, 0.90f, 1f);
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
    }
}
#endif
