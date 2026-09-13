using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    [System.Serializable]
    public class TikTokRacerEntry
    {
        public string username;
        public GameObject carObject;
        public Texture2D avatarTexture;
        public Color carColor;
        public int gridSlot;
    }

    public List<TikTokRacerEntry> joinedRacers = new List<TikTokRacerEntry>();
    private HashSet<string> joinedUsernames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, GameObject> userToCar = new Dictionary<string, GameObject>(System.StringComparer.OrdinalIgnoreCase);

    private readonly string[] sampleSimulatedUsers = new string[]
    {
        "somchai_racing", "gamer_pro_th", "speedy_anna", "alex_drift", "nong_ploy",
        "ninja_turbo", "boss_viper", "kitty_racer", "tony_apex", "fern_speed",
        "super_mario", "fast_and_fun", "tiktok_star", "drift_king_99", "lucky_boy",
        "shadow_runner", "fire_blaze", "cyber_racer", "turbo_max", "hyper_sonic"
    };

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ConnectToLive(string username)
    {
        streamerUsername = string.IsNullOrEmpty(username) ? "tiktok_streamer" : username.Replace("@", "");
        currentState = LiveState.WaitingLobby;

        joinedRacers.Clear();
        joinedUsernames.Clear();
        userToCar.Clear();

        Debug.Log($"[TikTokLive] Connected to Live stream of @{streamerUsername}! Waiting for viewers to type ''a''...");

        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.PrepareTikTokLobby();
        }
    }

    public void Disconnect()
    {
        currentState = LiveState.Disconnected;
        joinedRacers.Clear();
        joinedUsernames.Clear();
        userToCar.Clear();
        Debug.Log("[TikTokLive] Disconnected.");
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

                // Register and spawn car!
                SpawnViewerCar(username, avatarTex);
                return true;
            }
            else if (currentState == LiveState.RacingLocked)
            {
                Debug.Log($"[TikTokLive] @{username} tried to join, but race is currently in progress! Must wait for next race.");
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
                Debug.Log($"[TikTokLive] ⚡ NITRO BOOST activated for @{username} by gift ''{giftName}''!");
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
                if (!kvp.Key.Equals(sender, System.StringComparison.OrdinalIgnoreCase) && kvp.Value != null)
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

            var mm = FindObjectOfType<MenuManager>();
            if (mm != null) mm.UpdateTikTokLobbyDisplay();
        }
    }

    // ===== DEV SIMULATOR METHODS =====
    public void SimulateViewerJoin()
    {
        if (currentState != LiveState.WaitingLobby)
        {
            Debug.LogWarning("[TikTokLive] Cannot join: not in waiting lobby!");
            return;
        }

        string uname = $"user_{joinedRacers.Count + 1}";
        if (joinedRacers.Count < sampleSimulatedUsers.Length)
        {
            uname = sampleSimulatedUsers[joinedRacers.Count];
        }

        ProcessChatMessage(uname, "a", null);
    }

    public void SimulateGiftNitro()
    {
        if (joinedRacers.Count == 0) return;
        int randIdx = Random.Range(0, joinedRacers.Count);
        string uname = joinedRacers[randIdx].username;
        ProcessGift(uname, "Rose");
    }

    public void SimulatePrank()
    {
        if (joinedRacers.Count < 2) return;
        int sIdx = Random.Range(0, joinedRacers.Count);
        int tIdx = (sIdx + 1) % joinedRacers.Count;
        ProcessPrank(joinedRacers[sIdx].username, joinedRacers[tIdx].username, "banana");
    }
}
