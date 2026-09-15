using System;
using System.Collections.Generic;
using UnityEngine;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var all = Resources.FindObjectsOfTypeAll<LocalizationManager>();
                if (all != null && all.Length > 0) _instance = all[0];
                if (_instance == null)
                {
                    var go = new GameObject("LocalizationManager");
                    _instance = go.AddComponent<LocalizationManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    private static LocalizationManager _instance;

    public static readonly string[] SupportedLanguages = new string[]
    {
        "EN", "TH", "DE", "ES", "ID", "JA", "KO", "MS", "PT", "TL", "TR", "VI"
    };

    public static readonly string[] LanguageDisplayNames = new string[]
    {
        "English (EN)",
        "ไทย (TH)",
        "Deutsch (DE)",
        "Español (ES)",
        "Bahasa Indonesia (ID)",
        "日本語 (JA)",
        "한국어 (KO)",
        "Bahasa Melayu (MS)",
        "Português (PT)",
        "Tagalog (TL)",
        "Türkçe (TR)",
        "Tiếng Việt (VI)"
    };

    public string CurrentLanguage { get; private set; } = "TH";
    public event Action OnLanguageChanged;

    private Dictionary<string, Dictionary<string, string>> stringTable = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this && transform.parent == null)
        {
            Destroy(gameObject);
            return;
        }

        InitDictionary();
        string savedLang = PlayerPrefs.GetString("CarRace_Language", "TH");
        SetLanguage(savedLang);
    }

    public void SetLanguage(string langCode)
    {
        if (string.IsNullOrEmpty(langCode)) langCode = "TH";
        langCode = langCode.ToUpper().Trim();
        bool supported = false;
        foreach (var l in SupportedLanguages)
        {
            if (l.Equals(langCode, StringComparison.OrdinalIgnoreCase))
            {
                supported = true;
                break;
            }
        }
        if (!supported) langCode = "TH";

        CurrentLanguage = langCode;
        PlayerPrefs.SetString("CarRace_Language", langCode);
        PlayerPrefs.Save();

        OnLanguageChanged?.Invoke();
    }

    public static string Tr(string key, string defaultText = "")
    {
        return Instance.GetText(key, defaultText);
    }

    public string GetText(string key, string defaultText = "")
    {
        if (stringTable.TryGetValue(CurrentLanguage, out var langDict))
        {
            if (langDict.TryGetValue(key, out var val)) return val;
        }

        // Fallback to TH or EN
        if (stringTable.TryGetValue("TH", out var thDict))
        {
            if (thDict.TryGetValue(key, out var val)) return val;
        }
        if (stringTable.TryGetValue("EN", out var enDict))
        {
            if (enDict.TryGetValue(key, out var val)) return val;
        }

        return !string.IsNullOrEmpty(defaultText) ? defaultText : key;
    }

    private void Add(string lang, string key, string text)
    {
        if (!stringTable.TryGetValue(lang, out var d))
        {
            d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            stringTable[lang] = d;
        }
        d[key] = text;
    }

    private void InitDictionary()
    {
        // ------------------ THAI (TH) ------------------
        Add("TH", "title_gift_actions", "จัดการ Action ของขวัญ TikTok (CarStream)");
        Add("TH", "subtitle_gift_actions", "จับคู่ของขวัญ TikTok กับ Effect ในเกม บันทึกลง JSON อัตโนมัติ");
        Add("TH", "title_quick_launcher", "เมนูด่วนทดสอบของขวัญ [F5]");
        Add("TH", "subtitle_quick_launcher", "คลิกของขวัญเพื่อรัน Action ในเกมทันที (สำหรับสตรีมเมอร์ & ทดสอบ)");
        Add("TH", "col_gift", "ของขวัญ TIKTOK (GIFT)");
        Add("TH", "col_action", "ACTION EFFECT (ลูกเล่นในเกม)");
        Add("TH", "col_duration", "ระยะเวลา (DURATION)");
        Add("TH", "col_enable", "เปิดใช้งาน");
        Add("TH", "col_delete", "ลบ");
        Add("TH", "btn_add_action", "+ เพิ่ม ACTION ใหม่");
        Add("TH", "btn_save", "บันทึก JSON");
        Add("TH", "btn_reset", "คืนค่าเริ่มต้น");
        Add("TH", "select_gift_title", "เลือกของขวัญ TikTok (SELECT GIFT)");
        Add("TH", "search_placeholder", "พิมพ์ค้นหา เช่น กุหลาบ, หมวก, ปืน, สิงโต, Rose...");
        Add("TH", "action_speed_boost", "[NITRO] บูสความเร็ว");
        Add("TH", "action_drop_bomb", "[BOMB] วางระเบิด");
        Add("TH", "action_shoot_rpg", "[RPG] ยิง RPG รถใกล้ๆ");
        Add("TH", "action_slow_all", "[SLOW] Slow ทุกคัน (EMP)");
        Add("TH", "btn_toggle_f5", "[F5] ACTIONS");
        Add("TH", "btn_toggle_f6", "[F6] LEADERBOARD");
        Add("TH", "btn_toggle_f7", "[F7] JOIN LOBBY");
        Add("TH", "btn_toggle_f8", "[F8] GIFT SETTINGS");
        Add("TH", "quick_no_actions", "ยังไม่มี Action ที่ตั้งค่าไว้ กด [F8] เพื่อตั้งค่าของขวัญ");

        // ------------------ ENGLISH (EN) ------------------
        Add("EN", "title_gift_actions", "TIKTOK GIFT ACTIONS (CARSTREAM)");
        Add("EN", "subtitle_gift_actions", "Map TikTok gifts to race actions, saved to JSON automatically");
        Add("EN", "title_quick_launcher", "GIFT QUICK LAUNCHER [F5]");
        Add("EN", "subtitle_quick_launcher", "Click any gift to trigger race action instantly (Streamer / Test Mode)");
        Add("EN", "col_gift", "TIKTOK GIFT");
        Add("EN", "col_action", "ACTION EFFECT");
        Add("EN", "col_duration", "DURATION");
        Add("EN", "col_enable", "ENABLED");
        Add("EN", "col_delete", "DELETE");
        Add("EN", "btn_add_action", "+ ADD NEW ACTION");
        Add("EN", "btn_save", "SAVE JSON");
        Add("EN", "btn_reset", "RESET DEFAULTS");
        Add("EN", "select_gift_title", "SELECT TIKTOK GIFT");
        Add("EN", "search_placeholder", "Search gifts e.g. Rose, Cap, Lion, Gun, Heart...");
        Add("EN", "action_speed_boost", "[NITRO] Speed Boost");
        Add("EN", "action_drop_bomb", "[BOMB] Drop Landmine");
        Add("EN", "action_shoot_rpg", "[RPG] Shoot Homing RPG");
        Add("EN", "action_slow_all", "[SLOW] Slow All (EMP)");
        Add("EN", "btn_toggle_f5", "[F5] ACTIONS");
        Add("EN", "btn_toggle_f6", "[F6] LEADERBOARD");
        Add("EN", "btn_toggle_f7", "[F7] JOIN LOBBY");
        Add("EN", "btn_toggle_f8", "[F8] GIFT SETTINGS");
        Add("EN", "quick_no_actions", "No actions configured yet. Press [F8] to configure gifts.");

        // ------------------ GERMAN (DE) ------------------
        Add("DE", "title_gift_actions", "TIKTOK GESCHENK-AKTIONEN");
        Add("DE", "title_quick_launcher", "SCHNELLSTART GESCHENKE [F5]");
        Add("DE", "btn_add_action", "+ NEUE AKTION HINZUFÜGEN");
        Add("DE", "btn_save", "JSON SPEICHERN");
        Add("DE", "action_speed_boost", "[NITRO] Geschwindigkeitsboost");
        Add("DE", "action_drop_bomb", "[BOMB] Bombe legen");
        Add("DE", "action_shoot_rpg", "[RPG] RPG schießen");
        Add("DE", "action_slow_all", "[SLOW] Alle verlangsamen (EMP)");

        // ------------------ SPANISH (ES) ------------------
        Add("ES", "title_gift_actions", "ACCIONES DE REGALOS TIKTOK");
        Add("ES", "title_quick_launcher", "LANZADOR RÁPIDO [F5]");
        Add("ES", "btn_add_action", "+ AÑADIR ACCIÓN");
        Add("ES", "btn_save", "GUARDAR JSON");
        Add("ES", "action_speed_boost", "[NITRO] Turbo Nitro");
        Add("ES", "action_drop_bomb", "[BOMB] Colocar Bomba");
        Add("ES", "action_shoot_rpg", "[RPG] Disparar RPG");
        Add("ES", "action_slow_all", "[SLOW] Ralentizar Todos");

        // ------------------ INDONESIAN (ID) ------------------
        Add("ID", "title_gift_actions", "AKSI HADIAH TIKTOK");
        Add("ID", "title_quick_launcher", "PELUNCUR CEPAT [F5]");
        Add("ID", "btn_add_action", "+ TAMBAH AKSI");
        Add("ID", "btn_save", "SIMPAN JSON");
        Add("ID", "action_speed_boost", "[NITRO] Peningkat Kecepatan");
        Add("ID", "action_drop_bomb", "[BOMB] Pasang Bom");
        Add("ID", "action_shoot_rpg", "[RPG] Tembak RPG");
        Add("ID", "action_slow_all", "[SLOW] Lambatkan Semua");

        // ------------------ JAPANESE (JA) ------------------
        Add("JA", "title_gift_actions", "TikTok ギフトアクション設定");
        Add("JA", "title_quick_launcher", "クイックギフトランチャー [F5]");
        Add("JA", "btn_add_action", "+ アクション追加");
        Add("JA", "btn_save", "JSON保存");
        Add("JA", "action_speed_boost", "[NITRO] スピードブースト");
        Add("JA", "action_drop_bomb", "[BOMB] 地雷設置");
        Add("JA", "action_shoot_rpg", "[RPG] RPGロケット発射");
        Add("JA", "action_slow_all", "[SLOW] 全車スローダウン");

        // ------------------ KOREAN (KO) ------------------
        Add("KO", "title_gift_actions", "틱톡 선물 액션 설정");
        Add("KO", "title_quick_launcher", "선물 퀵 런처 [F5]");
        Add("KO", "btn_add_action", "+ 액션 추가");
        Add("KO", "btn_save", "JSON 저장");
        Add("KO", "action_speed_boost", "[NITRO] 스피드 부스트");
        Add("KO", "action_drop_bomb", "[BOMB] 폭탄 설치");
        Add("KO", "action_shoot_rpg", "[RPG] RPG 발사");
        Add("KO", "action_slow_all", "[SLOW] 전체 감속");

        // ------------------ MALAY (MS) ------------------
        Add("MS", "title_gift_actions", "AKSI HADIAH TIKTOK");
        Add("MS", "title_quick_launcher", "PELANCAR PANTAS [F5]");
        Add("MS", "btn_add_action", "+ TAMBAH AKSI");
        Add("MS", "btn_save", "SIMPAN JSON");
        Add("MS", "action_speed_boost", "[NITRO] Pecutan Nitro");
        Add("MS", "action_drop_bomb", "[BOMB] Letak Bom");
        Add("MS", "action_shoot_rpg", "[RPG] Tembak RPG");
        Add("MS", "action_slow_all", "[SLOW] Perlahankan Semua");

        // ------------------ PORTUGUESE (PT) ------------------
        Add("PT", "title_gift_actions", "AÇÕES DE PRESENTES TIKTOK");
        Add("PT", "title_quick_launcher", "LANÇADOR RÁPIDO [F5]");
        Add("PT", "btn_add_action", "+ ADICIONAR AÇÃO");
        Add("PT", "btn_save", "SALVAR JSON");
        Add("PT", "action_speed_boost", "[NITRO] Impulso Nitro");
        Add("PT", "action_drop_bomb", "[BOMB] Lançar Mina");
        Add("PT", "action_shoot_rpg", "[RPG] Disparar RPG");
        Add("PT", "action_slow_all", "[SLOW] Desacelerar Todos");

        // ------------------ TAGALOG (TL) ------------------
        Add("TL", "title_gift_actions", "TIKTOK REGALO ACTIONS");
        Add("TL", "title_quick_launcher", "MABILIS NA LAUNCHER [F5]");
        Add("TL", "btn_add_action", "+ MAGDAGDAG NG ACTION");
        Add("TL", "btn_save", "I-SAVE ANG JSON");
        Add("TL", "action_speed_boost", "[NITRO] Speed Boost");
        Add("TL", "action_drop_bomb", "[BOMB] Maglagay ng Bomba");
        Add("TL", "action_shoot_rpg", "[RPG] Magpaputok ng RPG");
        Add("TL", "action_slow_all", "[SLOW] Pabagalin Lahat");

        // ------------------ TURKISH (TR) ------------------
        Add("TR", "title_gift_actions", "TIKTOK HEDİYE EYLEMLERİ");
        Add("TR", "title_quick_launcher", "HIZLI HEDİYE BAŞLATICI [F5]");
        Add("TR", "btn_add_action", "+ YENİ EYLEM EKLE");
        Add("TR", "btn_save", "JSON KAYDET");
        Add("TR", "action_speed_boost", "[NITRO] Hız Takviyesi");
        Add("TR", "action_drop_bomb", "[BOMB] Mayın Bırak");
        Add("TR", "action_shoot_rpg", "[RPG] RPG Fırlat");
        Add("TR", "action_slow_all", "[SLOW] Herkesi Yavaşlat");

        // ------------------ VIETNAMESE (VI) ------------------
        Add("VI", "title_gift_actions", "HÀNH ĐỘNG QUÀ TIKTOK");
        Add("VI", "title_quick_launcher", "BỘ KHỞI CHẠY NHANH [F5]");
        Add("VI", "btn_add_action", "+ THÊM HÀNH ĐỘNG MỚI");
        Add("VI", "btn_save", "LƯU JSON");
        Add("VI", "action_speed_boost", "[NITRO] Tăng Tốc Nitro");
        Add("VI", "action_drop_bomb", "[BOMB] Thả Bom Mìn");
        Add("VI", "action_shoot_rpg", "[RPG] Bắn Pháo RPG");
        Add("VI", "action_slow_all", "[SLOW] Làm Chậm Toàn Bộ");
    }
}
