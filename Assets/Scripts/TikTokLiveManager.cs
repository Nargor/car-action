using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class TikTokLiveManager : MonoBehaviour
{
    public static TikTokLiveManager Instance { get; private set; }

    public enum LiveState
    {
        Disconnected,
        Connecting,
        WaitingLobby,
        RacingLocked
    }

    [Header("State")]
    public LiveState currentState = LiveState.Disconnected;
    public string streamerUsername = "tiktok_streamer";
    public int maxRacers = 50;

    [Header("Local Bridge Settings")]
    public string bridgeBaseUrl = "http://127.0.0.1:8765";
    public bool isBridgeConnected = false;

    [System.Serializable]
    public class TikTokRacerEntry
    {
        public string username;
        public GameObject carObject;
        public Texture2D avatarTexture;
        public Color carColor;
        public int gridSlot;
    }

    [System.Serializable]
    public class CheckLiveResponse
    {
        public bool success;
        public string username;
        public bool is_live;
        public string error;
    }

    [System.Serializable]
    public class BridgeEventItem
    {
        public string type;
        public string username;
        public string nickname;
        public string message;
        public string gift_name;
        public string avatar_url;
    }

    [System.Serializable]
    public class BridgeEventsResponse
    {
        public bool success;
        public List<BridgeEventItem> events;
    }

    public List<TikTokRacerEntry> joinedRacers = new List<TikTokRacerEntry>();
    private HashSet<string> joinedUsernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, GameObject> userToCar = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

    public GameObject GetCarByUsername(string username)
    {
        if (string.IsNullOrEmpty(username)) return null;
        if (userToCar != null && userToCar.TryGetValue(username, out var car))
            return car;
        return null;
    }

    public List<GameObject> GetAllCars()
    {
        var list = new List<GameObject>();
        if (userToCar != null)
        {
            foreach (var kvp in userToCar)
            {
                if (kvp.Value != null) list.Add(kvp.Value);
            }
        }
        return list;
    }

    public event Action OnRacersChanged;

    private Coroutine pollCoroutine;
    private static System.Diagnostics.Process bridgeProcess;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        EnsureBridgeServerRunning();
    }

    void OnDestroy()
    {
        Disconnect();
    }

    void OnApplicationQuit()
    {
        StopBridgeServerProcess();
    }

    public void EnsureBridgeServerRunning()
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        StartCoroutine(CheckAndStartBridgeCoroutine());
#endif
    }

    private IEnumerator CheckAndStartBridgeCoroutine()
    {
        // 1. Check if already running on 8765
        using (UnityWebRequest req = UnityWebRequest.Get($"{bridgeBaseUrl}/health"))
        {
            req.timeout = 2;
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                isBridgeConnected = true;
                yield break;
            }
        }

        // 2. Not running: launch python script invisibly in background (NO CONSOLE WINDOW!)
        string scriptPath = null;
        string[] candidates = new string[]
        {
            System.IO.Path.Combine(Application.streamingAssetsPath, "TikTokBridgeServer.py"),
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), "TikTokBridgeServer.py"),
            System.IO.Path.Combine(Application.dataPath, "..", "TikTokBridgeServer.py")
        };

        foreach (var p in candidates)
        {
            if (System.IO.File.Exists(p))
            {
                scriptPath = System.IO.Path.GetFullPath(p);
                break;
            }
        }

        if (!string.IsNullOrEmpty(scriptPath))
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = $"\"{scriptPath}\" 8765",
                    UseShellExecute = false,
                    CreateNoWindow = true, // Completely silent, NO CONSOLE WINDOW!
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };
                bridgeProcess = System.Diagnostics.Process.Start(psi);
                Debug.Log($"[TikTokLive] Auto-started background bridge server invisibly (PID: {bridgeProcess?.Id})");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TikTokLive] Could not auto-launch python bridge: {ex.Message}");
            }
        }
    }

    private void StopBridgeServerProcess()
    {
        if (bridgeProcess != null)
        {
            try
            {
                if (!bridgeProcess.HasExited) bridgeProcess.Kill();
            }
            catch {}
            bridgeProcess = null;
        }
    }

    // ==========================================
    // 1. CHECK LIVE STATUS (BEFORE CREATING ROOM)
    // ==========================================
    public void CheckIsLive(string username, Action<bool, string> callback)
    {
        StartCoroutine(CheckIsLiveCoroutine(username, callback));
    }

    private IEnumerator CheckIsLiveCoroutine(string username, Action<bool, string> callback)
    {
        string cleanUser = username.Replace("@", "").Trim();

        // 1. Try local bridge server first
        string url = $"{bridgeBaseUrl}/check_live?username={UnityWebRequest.EscapeURL(cleanUser)}";
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.timeout = 5;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var res = JsonUtility.FromJson<CheckLiveResponse>(req.downloadHandler.text);
                    if (res != null && res.is_live)
                    {
                        isBridgeConnected = true;
                        callback?.Invoke(true, "");
                        yield break;
                    }
                    else
                    {
                        string msg = (res != null && !string.IsNullOrEmpty(res.error)) 
                            ? res.error 
                            : "TikTok @" + cleanUser + " ยังไม่ได้เริ่ม Live ในขณะนี้";
                        callback?.Invoke(false, msg);
                        yield break;
                    }
                }
                catch {}
            }
        }

        // 2. Direct fallback: query TikTok webpage directly (works without python / in WebGL!)
        string directUrl = $"https://www.tiktok.com/@{cleanUser}/live";
        using (UnityWebRequest directReq = UnityWebRequest.Get(directUrl))
        {
            directReq.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            directReq.timeout = 8;
            yield return directReq.SendWebRequest();

            if (directReq.result == UnityWebRequest.Result.Success)
            {
                string html = directReq.downloadHandler.text;
                bool isLive = html.Contains("\"status\":2");
                if (isLive)
                {
                    callback?.Invoke(true, "");
                    yield break;
                }
                else
                {
                    callback?.Invoke(false, "TikTok @" + cleanUser + " ยังไม่ได้เริ่ม Live ในขณะนี้\n(กรุณาเริ่ม Live บน TikTok ก่อนสร้างห้อง)");
                    yield break;
                }
            }
            else
            {
                callback?.Invoke(false, "ไม่สามารถตรวจสอบสถานะกับ TikTok ได้: " + directReq.error);
            }
        }
    }

    // ==========================================
    // 2. CONNECT TO LIVE & ENTER TRACK
    // ==========================================
    public void ConnectToLive(string username, int laps = 3)
    {
        streamerUsername = string.IsNullOrEmpty(username) ? "tiktok_streamer" : username.Replace("@", "").Trim();
        currentState = LiveState.WaitingLobby;

        joinedRacers.Clear();
        joinedUsernames.Clear();
        userToCar.Clear();

        Debug.Log($"[TikTokLive] Starting TikTok Live room for @{streamerUsername} with {laps} laps!");

        // Prepare race track starting grid
        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.PrepareTikTokLobby(laps);
        }

        // Send start signal to bridge server and begin polling chat
        StartCoroutine(StartBridgeListeningCoroutine(streamerUsername));

        if (pollCoroutine != null) StopCoroutine(pollCoroutine);
        pollCoroutine = StartCoroutine(PollBridgeEventsCoroutine());

        OnRacersChanged?.Invoke();
    }

    private IEnumerator StartBridgeListeningCoroutine(string username)
    {
        string url = $"{bridgeBaseUrl}/start?username={UnityWebRequest.EscapeURL(username)}";
        using (UnityWebRequest req = UnityWebRequest.PostWwwForm(url, ""))
        {
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[TikTokLive] Bridge is now listening to live chat of @{username}!");
            }
        }
    }

    private IEnumerator PollBridgeEventsCoroutine()
    {
        while (currentState == LiveState.WaitingLobby || currentState == LiveState.RacingLocked)
        {
            yield return new WaitForSeconds(0.6f);

            string url = $"{bridgeBaseUrl}/events";
            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var res = JsonUtility.FromJson<BridgeEventsResponse>(req.downloadHandler.text);
                        if (res != null && res.events != null && res.events.Count > 0)
                        {
                            foreach (var ev in res.events)
                            {
                                if (ev.type == "chat_a")
                                {
                                    ProcessChatMessage(ev.username, "a", null, ev.avatar_url);
                                }
                                else if (ev.type == "gift")
                                {
                                    ProcessGift(ev.username, ev.gift_name);
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
        }
    }

    public void Disconnect()
    {
        currentState = LiveState.Disconnected;
        if (pollCoroutine != null)
        {
            StopCoroutine(pollCoroutine);
            pollCoroutine = null;
        }

        // Notify bridge to stop
        StartCoroutine(StopBridgeCoroutine());

        joinedRacers.Clear();
        joinedUsernames.Clear();
        userToCar.Clear();
        OnRacersChanged?.Invoke();
        Debug.Log("[TikTokLive] Disconnected from TikTok Live.");
    }

    private IEnumerator StopBridgeCoroutine()
    {
        string url = $"{bridgeBaseUrl}/stop";
        using (UnityWebRequest req = UnityWebRequest.PostWwwForm(url, ""))
        {
            yield return req.SendWebRequest();
        }
    }

    public void OnStartRace()
    {
        if (currentState != LiveState.WaitingLobby) return;

        currentState = LiveState.RacingLocked;
        Debug.Log("[TikTokLive] RACE STARTED! Joining is now LOCKED. No new cars can join.");

        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.StartTikTokRace();
        }
    }

    // ==========================================
    // 3. PROCESS VIEWER CHAT & SPAWN CAR
    // ==========================================
    public bool ProcessChatMessage(string username, string message, Texture2D avatarTex = null, string avatarUrl = null)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(message)) return false;

        string cleanMsg = message.Trim().ToLower();

        // 1. Join with "a" or "A"
        if (cleanMsg == "a")
        {
            if (currentState == LiveState.WaitingLobby)
            {
                if (joinedUsernames.Contains(username))
                {
                    Debug.Log($"[TikTokLive] @{username} already joined the race!");
                    return false;
                }

                if (joinedRacers.Count >= maxRacers)
                {
                    Debug.Log($"[TikTokLive] Starting grid is FULL ({maxRacers} racers max)!");
                    return false;
                }

                // Register and spawn car on the grid!
                SpawnViewerCar(username, avatarTex, avatarUrl);
                return true;
            }
            else if (currentState == LiveState.RacingLocked)
            {
                Debug.Log($"[TikTokLive] @{username} tried to join, but race is currently in progress!");
                return false;
            }
        }

        return false;
    }

    public void ProcessGift(string username, string giftName)
    {
        if (currentState != LiveState.RacingLocked) return;

        if (GiftActionManager.Instance != null)
        {
            GiftActionManager.Instance.ExecuteGiftAction(username, giftName);
            return;
        }

        if (userToCar.TryGetValue(username, out GameObject car) && car != null)
        {
            var ai = car.GetComponent<AICarController>();
            if (ai != null)
            {
                ai.ApplyNitro(5.0f);
                Debug.Log($"[TikTokLive] ⚡ NITRO BOOST activated for @{username} by gift '{giftName}'!");
            }
        }
    }

    public void ProcessPrank(string sender, string targetUsername, string prankType = "banana")
    {
        if (currentState != LiveState.RacingLocked) return;

        GameObject targetCar = null;
        if (!string.IsNullOrEmpty(targetUsername) && userToCar.TryGetValue(targetUsername, out GameObject c))
        {
            targetCar = c;
        }
        else
        {
            // Pick a random rival car
            foreach (var kvp in userToCar)
            {
                if (!kvp.Key.Equals(sender, StringComparison.OrdinalIgnoreCase) && kvp.Value != null)
                {
                    targetCar = kvp.Value;
                    targetUsername = kvp.Key;
                    break;
                }
            }
        }

        if (targetCar != null)
        {
            var ai = targetCar.GetComponent<AICarController>();
            if (ai != null)
            {
                ai.ApplyPrank(prankType, 2.5f);
                Debug.Log($"[TikTokLive] 🍌 @{sender} PRANKED @{targetUsername} with {prankType}!");
            }
        }
    }

    private void SpawnViewerCar(string username, Texture2D avatarTex, string avatarUrl = null)
    {
        if (RaceManager.Instance == null) return;

        int slotIdx = joinedRacers.Count;
        GameObject carObj = RaceManager.Instance.SpawnSingleTikTokRacer(slotIdx, username, avatarTex);
        if (carObj != null)
        {
            joinedUsernames.Add(username);
            userToCar[username] = carObj;

            var ai = carObj.GetComponent<AICarController>();
            Color col = ai != null ? ai.carColor : Color.white;

            var entry = new TikTokRacerEntry
            {
                username = username,
                carObject = carObj,
                avatarTexture = avatarTex,
                carColor = col,
                gridSlot = slotIdx
            };
            joinedRacers.Add(entry);

            // If avatarUrl provided, start background download coroutine
            if (!string.IsNullOrEmpty(avatarUrl))
            {
                StartCoroutine(DownloadAvatarCoroutine(username, avatarUrl, entry, carObj));
            }

            Debug.Log($"[TikTokLive] 🏎️ @{username} JOINED! Placed at Grid #{slotIdx + 1} ({joinedRacers.Count}/{maxRacers})");

            OnRacersChanged?.Invoke();
        }
    }

    private IEnumerator DownloadAvatarCoroutine(string username, string url, TikTokRacerEntry entry, GameObject carObj)
    {
        if (string.IsNullOrEmpty(url)) yield break;

        Debug.Log($"[TikTokLive] 📥 Downloading avatar for @{username} from: {url}");
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            req.timeout = 10;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success && req.downloadHandler != null)
            {
                byte[] data = req.downloadHandler.data;
                if (data != null && data.Length > 0)
                {
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(data))
                    {
                        tex.filterMode = FilterMode.Bilinear;
                        tex.wrapMode = TextureWrapMode.Clamp;
                        tex.name = $"Avatar_{username}";

                        if (entry != null) entry.avatarTexture = tex;
                        if (carObj != null)
                        {
                            var overhead = carObj.GetComponentInChildren<RacerOverheadUI>();
                            if (overhead != null)
                            {
                                overhead.SetAvatar(tex);
                            }
                        }
                        OnRacersChanged?.Invoke();
                        Debug.Log($"[TikTokLive] ✅ Successfully loaded avatar for @{username} ({tex.width}x{tex.height})!");
                        yield break;
                    }
                }
            }
            Debug.LogWarning($"[TikTokLive] ⚠️ Could not download avatar for @{username}: {req.error}");
        }
    }

    // ==========================================
    // 4. BOT RACER ADDITION (bot_<random number>)
    // ==========================================
    public void AddBotRacer()
    {
        if (currentState != LiveState.WaitingLobby)
        {
            Debug.LogWarning("[TikTokLive] Cannot add bot: not in waiting lobby!");
            return;
        }

        if (joinedRacers.Count >= maxRacers)
        {
            Debug.LogWarning($"[TikTokLive] Grid is full ({maxRacers} racers max)!");
            return;
        }

        // Name format as requested: bot_<random number>
        string bName;
        int attempts = 0;
        do
        {
            bName = $"bot_{UnityEngine.Random.Range(1000, 9999)}";
            attempts++;
        } while (joinedUsernames.Contains(bName) && attempts < 20);

        string botAvatar = $"https://api.dicebear.com/7.x/bottts/png?seed={bName}&size=128";
        ProcessChatMessage(bName, "a", null, botAvatar);
        Debug.Log($"[TikTokLive] 🤖 Added {bName} into the race! Total racers: {joinedRacers.Count}/{maxRacers}");
    }

    public void AddMultipleBots(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (joinedRacers.Count >= maxRacers) break;
            AddBotRacer();
        }
    }

    public void SimulateViewerJoin()
    {
        if (currentState != LiveState.WaitingLobby) return;
        string uname = $"viewer_{UnityEngine.Random.Range(100, 999)}";
        string simAvatar = $"https://api.dicebear.com/7.x/bottts/png?seed={uname}&size=128";
        ProcessChatMessage(uname, "a", null, simAvatar);
    }
}
