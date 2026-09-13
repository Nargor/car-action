using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ScoreboardAPIManager : MonoBehaviour
{
    public static ScoreboardAPIManager Instance { get; private set; }

    [Header("API Configuration")]
    public string apiBaseUrl = "https://shop.thungthao.online";
    public string apiToken = "107d42e0a559b8c2f8514e3159c6d5b7b2978846a116679606e5dd39796cc1e7";
    public string gameName = "caraction";

    // Data Models
    [System.Serializable]
    public class InsertScorePayload
    {
        public string tiktok_username;
        public string tiktok_nickname;
        public string tiktok_streamer_username;
        public string tiktok_streamer_nickname;
        public int score;
        public int score_time;
        public string game_name;
    }

    [System.Serializable]
    public class ApiResponse<T>
    {
        public string status;
        public string res_code;
        public int status_code;
        public string message;
        public T data;
        public Pagination pagination;
    }

    [System.Serializable]
    public class ScoreboardEntry
    {
        public string tiktok_username;
        public string tiktok_nickname;
        public string tiktok_streamer_username;
        public string tiktok_streamer_nickname;
        public string game_name;
        public int total_score;
        public int total_score_time;
        public int best_score_time;
        public int global_rank;
    }

    [System.Serializable]
    public class ScoreboardListResponse
    {
        public string status;
        public string res_code;
        public int status_code;
        public List<ScoreboardEntry> data;
        public Pagination pagination;
    }

    [System.Serializable]
    public class Pagination
    {
        public int total;
        public int per_page;
        public int current_page;
        public int total_pages;
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    // ==================== 1. INSERT SCORE ====================
    public void InsertScore(string username, string nickname, string streamerUser, string streamerNick, int score, int scoreTimeSec, Action<bool, string> onComplete = null)
    {
        StartCoroutine(InsertScoreCoroutine(username, nickname, streamerUser, streamerNick, score, scoreTimeSec, onComplete));
    }

    private IEnumerator InsertScoreCoroutine(string username, string nickname, string streamerUser, string streamerNick, int score, int scoreTimeSec, Action<bool, string> onComplete)
    {
        string url = $"{apiBaseUrl}/api/fivem/scoreboard/insert";

        var payload = new InsertScorePayload
        {
            tiktok_username = username,
            tiktok_nickname = string.IsNullOrEmpty(nickname) ? username : nickname,
            tiktok_streamer_username = streamerUser,
            tiktok_streamer_nickname = string.IsNullOrEmpty(streamerNick) ? streamerUser : streamerNick,
            score = score,
            score_time = scoreTimeSec,
            game_name = gameName
        };

        string json = JsonUtility.ToJson(payload);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Accept", "application/json");
            req.SetRequestHeader("Authorization", $"Bearer {apiToken}");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[ScoreboardAPI] Score inserted: @{username} +{score} pts, time={scoreTimeSec}s");
                onComplete?.Invoke(true, req.downloadHandler.text);
            }
            else
            {
                Debug.LogWarning($"[ScoreboardAPI] Failed to insert score for @{username}: {req.error} | {req.downloadHandler.text}");
                onComplete?.Invoke(false, req.error);
            }
        }
    }

    // ==================== 2. GET WORLD SCOREBOARD ====================
    public void GetWorldScoreboard(int page, int limit, string search, Action<ScoreboardListResponse> onComplete)
    {
        StartCoroutine(GetWorldScoreboardCoroutine(page, limit, search, onComplete));
    }

    private IEnumerator GetWorldScoreboardCoroutine(int page, int limit, string search, Action<ScoreboardListResponse> onComplete)
    {
        string query = $"game_name={UnityWebRequest.EscapeURL(gameName)}&page={page}&limit={limit}";
        if (!string.IsNullOrEmpty(search))
        {
            query += $"&search={UnityWebRequest.EscapeURL(search)}";
        }

        string url = $"{apiBaseUrl}/api/fivem/scoreboard/getall?{query}";

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("Accept", "application/json");
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string json = req.downloadHandler.text;
                try
                {
                    var res = JsonUtility.FromJson<ScoreboardListResponse>(json);
                    onComplete?.Invoke(res);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ScoreboardAPI] JSON parse error: {ex.Message}");
                    onComplete?.Invoke(null);
                }
            }
            else
            {
                Debug.LogWarning($"[ScoreboardAPI] GetWorldScoreboard failed: {req.error}");
                onComplete?.Invoke(null);
            }
        }
    }

    // ==================== 3. GET STREAMER SCOREBOARD ====================
    public void GetStreamerScoreboard(string streamerUsername, int page, int limit, string search, Action<ScoreboardListResponse> onComplete)
    {
        StartCoroutine(GetStreamerScoreboardCoroutine(streamerUsername, page, limit, search, onComplete));
    }

    private IEnumerator GetStreamerScoreboardCoroutine(string streamerUsername, int page, int limit, string search, Action<ScoreboardListResponse> onComplete)
    {
        if (string.IsNullOrEmpty(streamerUsername)) streamerUsername = "tiktok_streamer";

        string query = $"username={UnityWebRequest.EscapeURL(streamerUsername)}&game_name={UnityWebRequest.EscapeURL(gameName)}&page={page}&limit={limit}";
        if (!string.IsNullOrEmpty(search))
        {
            query += $"&search={UnityWebRequest.EscapeURL(search)}";
        }

        string url = $"{apiBaseUrl}/api/fivem/scoreboard/getbystreamer?{query}";

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("Accept", "application/json");
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string json = req.downloadHandler.text;
                try
                {
                    var res = JsonUtility.FromJson<ScoreboardListResponse>(json);
                    onComplete?.Invoke(res);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ScoreboardAPI] JSON parse error: {ex.Message}");
                    onComplete?.Invoke(null);
                }
            }
            else
            {
                Debug.LogWarning($"[ScoreboardAPI] GetStreamerScoreboard failed: {req.error}");
                onComplete?.Invoke(null);
            }
        }
    }

    // ==================== 4. SUBMIT RACE RESULTS (HUMANS ONLY) ====================
    /// <summary>
    /// Filters out bots, calculates scores based on human player count, and submits to API.
    /// e.g. If N human players: Rank 1 gets N, Rank 2 gets N - 2, Rank 3 gets N - 4...
    /// </summary>
    public void SubmitRaceResults(List<RaceManager.RacerInfo> allRacers, string streamerUser, string streamerNick, float raceTimeSeconds)
    {
        if (allRacers == null || allRacers.Count == 0) return;

        // 1. Filter out bots (only human players)
        var humanRacers = new List<RaceManager.RacerInfo>();
        foreach (var r in allRacers)
        {
            if (string.IsNullOrEmpty(r.name)) continue;
            // Ignore bot names
            if (r.name.StartsWith("BOT_", StringComparison.OrdinalIgnoreCase)) continue;
            humanRacers.Add(r);
        }

        int humanCount = humanRacers.Count;
        if (humanCount == 0)
        {
            Debug.Log("[ScoreboardAPI] No human players to record scores (all were bots).");
            return;
        }

        Debug.Log($"[ScoreboardAPI] 🏁 Recording scores for {humanCount} human players...");

        int timeSec = Mathf.Max(1, Mathf.RoundToInt(raceTimeSeconds));

        // 2. Submit each human player with calculated score
        for (int i = 0; i < humanCount; i++)
        {
            var racer = humanRacers[i];
            int humanRank = i + 1; // 1-indexed rank among humans

            // Score formula: Rank 1 = humanCount, Rank 2 = humanCount - 2, Rank 3 = humanCount - 4 ...
            int score = Mathf.Max(1, humanCount - (humanRank - 1) * 2);

            string username = racer.name.Replace("@", "").Trim();
            string nickname = racer.name;

            InsertScore(username, nickname, streamerUser, streamerNick, score, timeSec);
        }
    }
}
