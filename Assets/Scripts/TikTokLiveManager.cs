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
    }

    [System.Serializable]
    public class BridgeEventsResponse
    {
        public bool success;
        public List<BridgeEventItem> events;
    }

    public List<TikTokRacerEntry> joinedRacers = new List<TikTokRacerEntry>();
    private HashSet<string> joinedUsernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, GameObject> userToCar = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

    public event Action OnRacersChanged;

    private Coroutine pollCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void OnDestroy()
    {
        Disconnect();
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
        string url = $"{bridgeBaseUrl}/check_live?username={UnityWebRequest.EscapeURL(cleanUser)}";

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.timeout = 10;
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
                            : "TikTok @ " + cleanUser + " ยังไม่ได้เริ่ม Live ในขณะนี้";
                        callback?.Invoke(false, msg);
                        yield break;
                    }
                }
                catch (Exception ex)
                {
                    callback?.Invoke(false, "เกิดข้อผิดพลาดในการตรวจสอบสถานะ: " + ex.Message);
                    yield break;
                }
            }
            else
            {
                // Bridge server is not reachable
                callback?.Invoke(false, "ไม่สามารถเชื่อมต่อกับ TikTok Bridge Server (127.0.0.1:8765) ได้\nกรุณารัน 'start_tiktok_bridge.bat' ก่อนสร้างห้อง");
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
                                    ProcessChatMessage(ev.username, "a", null);
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
    public bool ProcessChatMessage(string username, string message, Texture2D avatarTex = null)
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
                SpawnViewerCar(username, avatarTex);
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

    private void SpawnViewerCar(string username, Texture2D avatarTex)
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

            joinedRacers.Add(new TikTokRacerEntry
            {
                username = username,
                carObject = carObj,
                avatarTexture = avatarTex,
                carColor = col,
                gridSlot = slotIdx
            });

            Debug.Log($"[TikTokLive] 🏎️ @{username} JOINED! Placed at Grid #{slotIdx + 1} ({joinedRacers.Count}/{maxRacers})");

            OnRacersChanged?.Invoke();
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

        ProcessChatMessage(bName, "a", null);
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
        ProcessChatMessage(uname, "a", null);
    }
}
