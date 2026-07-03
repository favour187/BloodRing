using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;

#region Data Models
[System.Serializable] public class AuthRequest { public string username; public string password; public string recoveryCode; }
[System.Serializable] public class OAuthRequest { public string provider; public string providerId; public string email; public string displayName; }
[System.Serializable] public class AuthResponse { public string message; public string token; public int userId; public string username; public string recoveryCode; public string error; }
[System.Serializable] public class ProfileData { public int userId; public string displayName; public int level; public int xp; public int bloodCoins; public int diamonds; public string selectedCharacter; public string rank_tier; public int rank_points; public int win_streak; public int kill_milestone; public int battle_pass_level; public string error; }
[System.Serializable] public class MatchmakeRequest { public string action; public string joinCode; public string gameMode; public string region; public bool isRanked; }
[System.Serializable] public class MatchmakeResponse { public string message; public int lobbyId; public string joinCode; public int hostId; public string gameMode; public string region; public int maxPlayers; public string error; }
[System.Serializable] public class MatchResultRequest { public int kills; public int placement; public float damageDealt; public float matchDuration; public string gameMode; public bool isRanked; }
[System.Serializable] public class MatchResultResponse { public string message; public int earnedXP; public int earnedCoins; public int newLevel; public int newXP; public int newCoins; public string newRankTier; public int newRankPoints; public int winStreak; public string error; }
[System.Serializable] public class LeaderboardEntry { public string displayName; public int level; public int bloodCoins; public int totalKills; }
[System.Serializable] public class LeaderboardResponse { public List<LeaderboardEntry> leaderboard; public string error; }
[System.Serializable] public class FriendEntry { public int friendId; public string displayName; public string rankTier; public bool isOnline; public string status; }
[System.Serializable] public class FriendsResponse { public List<FriendEntry> friends; public string error; }
[System.Serializable] public class GuildMemberEntry { public string displayName; public string rankTier; public string role; }
[System.Serializable] public class GuildData { public int id; public string name; public int leader_id; public int level; public int members_count; }
[System.Serializable] public class GuildResponse { public GuildData guild; public List<GuildMemberEntry> members; public string error; }
[System.Serializable] public class ChatMessage { public string senderName; public string message; public string createdAt; }
[System.Serializable] public class ChatResponse { public List<ChatMessage> messages; public string error; }
[System.Serializable] public class StoreItem { public string id; public string name; public string type; public int priceDiamonds; public int priceCoins; }
[System.Serializable] public class InvItem { public string itemType; public string itemId; public int level; public int fragments; }
[System.Serializable] public class StoreResponse { public List<StoreItem> storeItems; public List<InvItem> inventory; public string error; }
[System.Serializable] public class BuyResponse { public string message; public StoreItem item; public string error; }
[System.Serializable] public class SpinResponse { public string message; public string reward; public string error; }
[System.Serializable] public class MissionEntry { public string id; public string desc; public int target; public int progress; public int rewardCoins; public int rewardXP; public bool is_claimed; }
[System.Serializable] public class MissionsResponse { public int bpLevel; public List<MissionEntry> missions; public string error; }
#endregion

/// <summary>
/// All backend calls use real HTTP requests to the production Node.js/SQLite API.
/// The server URL is read from StreamingAssets/server_config.txt at startup,
/// so it can be changed without a rebuild. No placeholder gameplay, store, profile, leaderboard, clan, or inventory data is generated here.
/// Failed requests return structured error JSON for the UI/reconnect scene to handle.
/// </summary>
public class BackendAPI : MonoBehaviour
{
    private static BackendAPI instance;
    public static BackendAPI Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("[BackendAPI]");
                instance = go.AddComponent<BackendAPI>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    // ── Server URL ────────────────────────────────────────────────────────────
    // Default is your Render.com deployment. Overridden by server_config.txt.
    private string baseUrl = "https://academyroyalebackend.onrender.com/api";

    public string AuthToken
    {
        get => PlayerPrefs.GetString("JWT_TOKEN", "");
        set { PlayerPrefs.SetString("JWT_TOKEN", value); PlayerPrefs.Save(); }
    }
    public ProfileData CurrentProfile { get; private set; }
    public bool IsLoggedIn => !string.IsNullOrEmpty(AuthToken);

    private void Awake()
    {
        if (instance == null) { instance = this; DontDestroyOnLoad(gameObject); }
        else if (instance != this) { Destroy(gameObject); return; }
        StartCoroutine(LoadServerConfig());
    }

    private IEnumerator LoadServerConfig()
    {
        string configPath = Path.Combine(Application.streamingAssetsPath, "server_config.txt");
        using (UnityWebRequest req = UnityWebRequest.Get(configPath))
        {
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                string url = req.downloadHandler.text.Trim();
                if (!string.IsNullOrEmpty(url))
                {
                    baseUrl = url.TrimEnd('/') + "/api";
                    Debug.Log("[BackendAPI] Server URL loaded from config: " + baseUrl);
                }
            }
            else
            {
                Debug.Log("[BackendAPI] No server_config.txt found, using default: " + baseUrl);
            }
        }
    }

    // ── Auth ──────────────────────────────────────────────────────────────────

    public async Task<AuthResponse> RegisterAsync(string username, string password)
    {
        string json = JsonUtility.ToJson(new AuthRequest { username = username, password = password });
        string resJson = await PostJsonAsync(baseUrl + "/auth/register", json, false);
        AuthResponse res = JsonUtility.FromJson<AuthResponse>(resJson);
        if (res != null && !string.IsNullOrEmpty(res.token)) { AuthToken = res.token; PlayerPrefs.SetString("PlayerNickname", res.username); PlayerPrefs.Save(); await GetProfileAsync(); }
        return res;
    }

    public async Task<AuthResponse> LoginAsync(string username, string password)
    {
        string json = JsonUtility.ToJson(new AuthRequest { username = username, password = password });
        string resJson = await PostJsonAsync(baseUrl + "/auth/login", json, false);
        AuthResponse res = JsonUtility.FromJson<AuthResponse>(resJson);
        if (res != null && !string.IsNullOrEmpty(res.token)) { AuthToken = res.token; PlayerPrefs.SetString("PlayerNickname", res.username); PlayerPrefs.Save(); await GetProfileAsync(); }
        return res;
    }

    public async Task<AuthResponse> GuestLoginAsync(string recovery = "")
    {
        string json = JsonUtility.ToJson(new AuthRequest { recoveryCode = recovery });
        string resJson = await PostJsonAsync(baseUrl + "/auth/guest", json, false);
        AuthResponse res = JsonUtility.FromJson<AuthResponse>(resJson);
        if (res != null && !string.IsNullOrEmpty(res.token))
        {
            AuthToken = res.token;
            PlayerPrefs.SetString("PlayerNickname", res.username);
            if (!string.IsNullOrEmpty(res.recoveryCode)) PlayerPrefs.SetString("GuestRecoveryCode", res.recoveryCode);
            PlayerPrefs.Save();
            await GetProfileAsync();
        }
        return res;
    }

    public async Task<AuthResponse> OAuthLoginAsync(string provider, string pId, string email, string dName)
    {
        string json = JsonUtility.ToJson(new OAuthRequest { provider = provider, providerId = pId, email = email, displayName = dName });
        string resJson = await PostJsonAsync(baseUrl + "/auth/oauth", json, false);
        AuthResponse res = JsonUtility.FromJson<AuthResponse>(resJson);
        if (res != null && !string.IsNullOrEmpty(res.token)) { AuthToken = res.token; PlayerPrefs.SetString("PlayerNickname", res.username); PlayerPrefs.Save(); await GetProfileAsync(); }
        return res;
    }

    public async Task<bool> LinkAccountAsync(string provider, string pId)
    {
        string json = "{\"provider\":\"" + provider + "\",\"providerId\":\"" + pId + "\"}";
        string resJson = await PostJsonAsync(baseUrl + "/auth/link", json, true);
        return resJson.Contains("linked") || resJson.Contains("Successfully");
    }

    public void Logout() { AuthToken = ""; CurrentProfile = null; PlayerPrefs.DeleteKey("JWT_TOKEN"); PlayerPrefs.Save(); }

    // ── Profile ───────────────────────────────────────────────────────────────

    public async Task<ProfileData> GetProfileAsync()
    {
        if (!IsLoggedIn) return null;
        string resJson = await GetAsync(baseUrl + "/profile", true);
        CurrentProfile = JsonUtility.FromJson<ProfileData>(resJson);
        if (CurrentProfile != null && !string.IsNullOrEmpty(CurrentProfile.selectedCharacter)) { PlayerPrefs.SetString("SelectedCharacter", CurrentProfile.selectedCharacter); PlayerPrefs.Save(); }
        return CurrentProfile;
    }

    public async Task<bool> UpdateCharacterAsync(string charName)
    {
        if (!IsLoggedIn) return false;
        string json = "{\"characterName\":\"" + charName + "\"}";
        string resJson = await PutJsonAsync(baseUrl + "/profile/character", json, true);
        return resJson.Contains("successfully");
    }

    // ── Social ────────────────────────────────────────────────────────────────

    public async Task<FriendsResponse> GetFriendsAsync()
    {
        string resJson = await GetAsync(baseUrl + "/social/friends", true);
        return JsonUtility.FromJson<FriendsResponse>(resJson);
    }

    public async Task<bool> AddFriendAsync(string username)
    {
        string json = "{\"friendUsername\":\"" + username + "\"}";
        string resJson = await PostJsonAsync(baseUrl + "/social/friends/add", json, true);
        return resJson.Contains("Successfully");
    }

    public async Task<GuildResponse> GetGuildAsync()
    {
        string resJson = await GetAsync(baseUrl + "/social/guilds", true);
        return JsonUtility.FromJson<GuildResponse>(resJson);
    }

    public async Task<bool> CreateGuildAsync(string gName)
    {
        string json = "{\"guildName\":\"" + gName + "\"}";
        string resJson = await PostJsonAsync(baseUrl + "/social/guilds/create", json, true);
        return resJson.Contains("successfully");
    }

    public async Task<ChatResponse> GetChatMessagesAsync(string region = "GLOBAL")
    {
        string resJson = await GetAsync(baseUrl + "/social/chat?region=" + region, true);
        return JsonUtility.FromJson<ChatResponse>(resJson);
    }

    public async Task<bool> SendChatMessageAsync(string msg, string region = "GLOBAL")
    {
        string json = "{\"message\":\"" + msg + "\",\"region\":\"" + region + "\"}";
        string resJson = await PostJsonAsync(baseUrl + "/social/chat/send", json, true);
        return resJson.Contains("sent");
    }

    public async Task<bool> ShareSocialAsync(string platform, string shareType)
    {
        string json = "{\"platform\":\"" + platform + "\",\"shareType\":\"" + shareType + "\"}";
        string resJson = await PostJsonAsync(baseUrl + "/social/share", json, true);
        return resJson.Contains("Successfully");
    }

    // ── Store ─────────────────────────────────────────────────────────────────

    public async Task<StoreResponse> GetStoreAsync()
    {
        string resJson = await GetAsync(baseUrl + "/store", true);
        return JsonUtility.FromJson<StoreResponse>(resJson);
    }

    public async Task<BuyResponse> BuyStoreItemAsync(string itemId, string currency = "GEMS")
    {
        string json = "{\"itemId\":\"" + itemId + "\",\"currency\":\"" + currency + "\"}";
        string resJson = await PostJsonAsync(baseUrl + "/store/buy", json, true);
        BuyResponse res = JsonUtility.FromJson<BuyResponse>(resJson);
        if (res != null && string.IsNullOrEmpty(res.error)) await GetProfileAsync();
        return res;
    }

    public async Task<SpinResponse> SpinLuckyWheelAsync()
    {
        string resJson = await PostJsonAsync(baseUrl + "/store/luckyspin", "{}", true);
        SpinResponse res = JsonUtility.FromJson<SpinResponse>(resJson);
        if (res != null && string.IsNullOrEmpty(res.error)) await GetProfileAsync();
        return res;
    }

    public async Task<bool> ClaimDailyRewardAsync()
    {
        string resJson = await PostJsonAsync(baseUrl + "/store/daily", "{}", true);
        bool s = resJson.Contains("Claimed");
        if (s) await GetProfileAsync();
        return s;
    }

    public async Task<MissionsResponse> GetMissionsAsync()
    {
        string resJson = await GetAsync(baseUrl + "/store/missions", true);
        return JsonUtility.FromJson<MissionsResponse>(resJson);
    }

    // ── Matchmaking & Results ─────────────────────────────────────────────────

    public async Task<MatchmakeResponse> RequestHostMatchmakeAsync(string joinCode, string mode = "CLASSIC", string reg = "US", bool ranked = false)
    {
        string json = JsonUtility.ToJson(new MatchmakeRequest { action = "HOST", joinCode = joinCode, gameMode = mode, region = reg, isRanked = ranked });
        string resJson = await PostJsonAsync(baseUrl + "/match/matchmake", json, true);
        return JsonUtility.FromJson<MatchmakeResponse>(resJson);
    }

    public async Task<MatchmakeResponse> RequestJoinMatchmakeAsync(string mode = "CLASSIC", string reg = "US", bool ranked = false)
    {
        string json = JsonUtility.ToJson(new MatchmakeRequest { action = "JOIN", gameMode = mode, region = reg, isRanked = ranked });
        string resJson = await PostJsonAsync(baseUrl + "/match/matchmake", json, true);
        return JsonUtility.FromJson<MatchmakeResponse>(resJson);
    }

    public async Task<MatchResultResponse> SubmitMatchResultAsync(int kills, int placement, float damageDealt, float matchDuration, string mode = "CLASSIC", bool ranked = false)
    {
        if (!IsLoggedIn) return null;
        string json = JsonUtility.ToJson(new MatchResultRequest { kills = kills, placement = placement, damageDealt = damageDealt, matchDuration = matchDuration, gameMode = mode, isRanked = ranked });
        string resJson = await PostJsonAsync(baseUrl + "/match/result", json, true);
        MatchResultResponse res = JsonUtility.FromJson<MatchResultResponse>(resJson);
        if (res != null && string.IsNullOrEmpty(res.error) && CurrentProfile != null)
        {
            CurrentProfile.level = res.newLevel;
            CurrentProfile.xp = res.newXP;
            CurrentProfile.bloodCoins = res.newCoins;
            CurrentProfile.rank_tier = res.newRankTier;
            CurrentProfile.rank_points = res.newRankPoints;
            CurrentProfile.win_streak = res.winStreak;
        }
        return res;
    }

    public async Task<bool> LogAntiCheatViolationAsync(string violationType, string details)
    {
        string json = "{\"violationType\":\"" + violationType + "\",\"details\":\"" + details + "\"}";
        string resJson = await PostJsonAsync(baseUrl + "/match/anticheat", json, true);
        return resJson.Contains("logged");
    }

    public async Task<string> SyncCloudSaveAsync(string saveData = "")
    {
        string json = "{\"saveData\":\"" + saveData + "\"}";
        return await PostJsonAsync(baseUrl + "/match/cloudsave", json, true);
    }

    public async Task<LeaderboardResponse> GetLeaderboardAsync()
    {
        string resJson = await GetAsync(baseUrl + "/leaderboard", false);
        return JsonUtility.FromJson<LeaderboardResponse>(resJson);
    }

    // ── HTTP helpers (main-thread coroutines → Tasks) ─────────────────────────

    private Task<string> PostJsonAsync(string url, string json, bool useAuth)
    {
        var tcs = new TaskCompletionSource<string>();
        StartCoroutine(PostCoroutine(url, json, useAuth, tcs));
        return tcs.Task;
    }

    private IEnumerator PostCoroutine(string url, string json, bool useAuth, TaskCompletionSource<string> tcs)
    {
        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler   = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            if (useAuth && IsLoggedIn) req.SetRequestHeader("Authorization", "Bearer " + AuthToken);
            req.timeout = 8;
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[BackendAPI] POST failed (" + url + "): " + req.error);
                tcs.SetResult(MakeErrorJson(req.error));
            }
            else { tcs.SetResult(req.downloadHandler.text); }
        }
    }

    private Task<string> PutJsonAsync(string url, string json, bool useAuth)
    {
        var tcs = new TaskCompletionSource<string>();
        StartCoroutine(PutCoroutine(url, json, useAuth, tcs));
        return tcs.Task;
    }

    private IEnumerator PutCoroutine(string url, string json, bool useAuth, TaskCompletionSource<string> tcs)
    {
        using (UnityWebRequest req = new UnityWebRequest(url, "PUT"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler   = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            if (useAuth && IsLoggedIn) req.SetRequestHeader("Authorization", "Bearer " + AuthToken);
            req.timeout = 8;
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) { tcs.SetResult(MakeErrorJson(req.error)); }
            else { tcs.SetResult(req.downloadHandler.text); }
        }
    }

    private Task<string> GetAsync(string url, bool useAuth)
    {
        var tcs = new TaskCompletionSource<string>();
        StartCoroutine(GetCoroutine(url, useAuth, tcs));
        return tcs.Task;
    }

    private IEnumerator GetCoroutine(string url, bool useAuth, TaskCompletionSource<string> tcs)
    {
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            if (useAuth && IsLoggedIn) req.SetRequestHeader("Authorization", "Bearer " + AuthToken);
            req.timeout = 8;
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[BackendAPI] GET failed (" + url + "): " + req.error);
                tcs.SetResult(MakeErrorJson(req.error));
            }
            else { tcs.SetResult(req.downloadHandler.text); }
        }
    }

    private string MakeErrorJson(string error)
    {
        if (string.IsNullOrEmpty(error)) error = "Network request failed";
        error = error.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return "{\"error\":\"" + error + "\"}";
    }

}


