using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Handles Unity Relay-backed multiplayer with automatic fallback to LAN.
/// Exposes host/client start, player data sync RPCs, and connection state.
/// </summary>
public class NetworkController : MonoBehaviour
{
    private static NetworkController instance;
    public static NetworkController Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("[NetworkController]");
                instance = go.AddComponent<NetworkController>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    public NetworkManager netManager;
    public UnityTransport transport;

    public string joinCode = "";
    public bool isOnlineMode = false;
    public bool isConnected = false;

    private bool servicesReady = false;

    private Dictionary<ulong, string> playerNicknames = new Dictionary<ulong, string>();
    private Dictionary<ulong, string> playerCharacters = new Dictionary<ulong, string>();

    // Fired on server when a player's data arrives via RPC
    public System.Action<ulong, string, string> OnPlayerDataReceived;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            StartCoroutine(InitializeServicesCoroutine());
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    // ── Unity Services init (non-blocking) ───────────────────────────────────

    private IEnumerator InitializeServicesCoroutine()
    {
        Debug.Log("[NetworkController] Initialising Unity Services...");
        var initTask = UnityServices.InitializeAsync();
        yield return new WaitUntil(() => initTask.IsCompleted);

        if (initTask.Exception != null)
        {
            Debug.LogWarning("[NetworkController] Unity Services init failed – Relay unavailable. LAN fallback active. " + initTask.Exception.Message);
            SetupNetworkManager();
            yield break;
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            var authTask = AuthenticationService.Instance.SignInAnonymouslyAsync();
            yield return new WaitUntil(() => authTask.IsCompleted);
            if (authTask.Exception != null)
                Debug.LogWarning("[NetworkController] Anonymous sign-in failed: " + authTask.Exception.Message);
            else
                Debug.Log("[NetworkController] Signed in. PlayerID: " + AuthenticationService.Instance.PlayerId);
        }

        servicesReady = true;
        SetupNetworkManager();
    }

    private void SetupNetworkManager()
    {
        if (netManager != null) return;

        netManager = gameObject.AddComponent<NetworkManager>();
        transport  = gameObject.AddComponent<UnityTransport>();

        netManager.NetworkConfig = new NetworkConfig
        {
            NetworkTransport = transport,
            TickRate = 30
        };

        // Minimal player prefab required by NGO
        GameObject dummyPrefab = new GameObject("DummyNetworkPrefab");
        dummyPrefab.AddComponent<NetworkObject>();
        dummyPrefab.SetActive(false);
        DontDestroyOnLoad(dummyPrefab);
        netManager.NetworkConfig.PlayerPrefab = dummyPrefab;

        netManager.OnClientConnectedCallback  += OnClientConnected;
        netManager.OnClientDisconnectCallback += OnClientDisconnect;

        Debug.Log("[NetworkController] NetworkManager ready.");
    }

    // ── Wait helper for callers that access NetworkController before Awake completes ──

    public IEnumerator WaitUntilReady()
    {
        yield return new WaitUntil(() => netManager != null);
    }

    // ── Host ──────────────────────────────────────────────────────────────────

    public async Task<bool> StartOnlineHost()
    {
        isOnlineMode = true;

        // Ensure NetworkManager is ready (services may still be initialising)
        float waited = 0f;
        while (netManager == null && waited < 5f) { await Task.Delay(100); waited += 0.1f; }
        if (netManager == null) return false;

        if (servicesReady)
        {
            try
            {
                Allocation alloc = await RelayService.Instance.CreateAllocationAsync(19); // 19 slots + host = 20
                joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);
                Debug.Log("[NetworkController] Relay Host ready. Join Code: " + joinCode);

                transport.SetRelayServerData(
                    alloc.RelayServer.IpV4,
                    (ushort)alloc.RelayServer.Port,
                    alloc.AllocationIdBytes,
                    alloc.Key,
                    alloc.ConnectionData
                );

                bool ok = netManager.StartHost();
                isConnected = ok;
                if (ok) RegisterLocalPlayerData();
                return ok;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[NetworkController] Relay allocation failed – falling back to LAN. " + e.Message);
            }
        }

        // LAN fallback
        joinCode = "LAN_" + Random.Range(1000, 9999);
        transport.SetConnectionData("0.0.0.0", 7777);
        bool success = netManager.StartHost();
        isConnected = success;
        if (success) RegisterLocalPlayerData();
        return success;
    }

    // ── Client ────────────────────────────────────────────────────────────────

    public async Task<bool> StartOnlineClient(string code)
    {
        isOnlineMode = true;
        joinCode = code;

        float waited = 0f;
        while (netManager == null && waited < 5f) { await Task.Delay(100); waited += 0.1f; }
        if (netManager == null) return false;

        if (servicesReady && !code.Contains(".") && !code.StartsWith("LAN_"))
        {
            try
            {
                JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(code);
                Debug.Log("[NetworkController] Relay Client joined.");

                transport.SetRelayServerData(
                    joinAlloc.RelayServer.IpV4,
                    (ushort)joinAlloc.RelayServer.Port,
                    joinAlloc.AllocationIdBytes,
                    joinAlloc.Key,
                    joinAlloc.ConnectionData,
                    joinAlloc.HostConnectionData
                );

                bool ok = netManager.StartClient();
                isConnected = ok;
                if (ok) StartCoroutine(SendLocalDataWhenConnected());
                return ok;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[NetworkController] Relay client join failed: " + e.Message);
            }
        }

        // LAN fallback – code is an IP or LAN_ token
        string ip = code.StartsWith("LAN_") ? "127.0.0.1" : (code.Contains(".") ? code : "127.0.0.1");
        transport.SetConnectionData(ip, 7777);
        bool success = netManager.StartClient();
        isConnected = success;
        if (success) StartCoroutine(SendLocalDataWhenConnected());
        return success;
    }

    // ── Callbacks ─────────────────────────────────────────────────────────────

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log("[NetworkController] Client connected: " + clientId);
        // If we are the server, the connecting client will send their data via RPC shortly
    }

    private void OnClientDisconnect(ulong clientId)
    {
        Debug.Log("[NetworkController] Client disconnected: " + clientId);
        playerNicknames.Remove(clientId);
        playerCharacters.Remove(clientId);
    }

    // ── Player data: registered locally (host) or sent via RPC (client) ──────

    private void RegisterLocalPlayerData()
    {
        ulong id = netManager.LocalClientId;
        playerNicknames[id]  = PlayerPrefs.GetString("PlayerNickname",    "Striker_Hero");
        playerCharacters[id] = PlayerPrefs.GetString("SelectedCharacter", "DJNeon");
    }

    private IEnumerator SendLocalDataWhenConnected()
    {
        yield return new WaitUntil(() => netManager.IsConnectedClient);
        yield return new WaitForSeconds(0.5f); // let scene settle
        SendPlayerDataServerRpc(
            PlayerPrefs.GetString("PlayerNickname",    "Guest_Player"),
            PlayerPrefs.GetString("SelectedCharacter", "DJNeon")
        );
    }

    // ── RPCs ──────────────────────────────────────────────────────────────────
    // These live on a separate NetworkBehaviour component we create at runtime.

    private PlayerDataSender dataSender;

    public void EnsureDataSenderReady()
    {
        if (dataSender != null) return;
        dataSender = gameObject.AddComponent<PlayerDataSender>();
        dataSender.controller = this;
    }

    public void SendPlayerDataServerRpc(string nickname, string character)
    {
        EnsureDataSenderReady();
        dataSender.SendDataToServer(nickname, character);
    }

    // Called by PlayerDataSender when server receives RPC
    public void RegisterRemotePlayerData(ulong clientId, string nickname, string character)
    {
        playerNicknames[clientId]  = nickname;
        playerCharacters[clientId] = character;
        Debug.Log("[NetworkController] Registered player " + clientId + " as " + nickname + " (" + character + ")");
        OnPlayerDataReceived?.Invoke(clientId, nickname, character);
    }

    // ── Public getters ────────────────────────────────────────────────────────

    public string GetPlayerNickname(ulong clientId)
        => playerNicknames.ContainsKey(clientId) ? playerNicknames[clientId] : "Player_" + clientId;

    public string GetPlayerCharacter(ulong clientId)
        => playerCharacters.ContainsKey(clientId) ? playerCharacters[clientId] : "DJNeon";

    public Dictionary<ulong, string> GetAllPlayerNicknames() => playerNicknames;

    public void SetClientData(ulong clientId, string nick, string charChoice)
    {
        playerNicknames[clientId]  = nick;
        playerCharacters[clientId] = charChoice;
    }
}

// ── Separate NetworkBehaviour so we can use RPCs ──────────────────────────────

public class PlayerDataSender : NetworkBehaviour
{
    public NetworkController controller;

    public void SendDataToServer(string nickname, string character)
    {
        if (IsSpawned)
            SubmitPlayerDataServerRpc(nickname, character);
        else
            StartCoroutine(WaitAndSend(nickname, character));
    }

    private System.Collections.IEnumerator WaitAndSend(string nickname, string character)
    {
        yield return new WaitUntil(() => IsSpawned);
        SubmitPlayerDataServerRpc(nickname, character);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitPlayerDataServerRpc(string nickname, string character, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        controller?.RegisterRemotePlayerData(senderId, nickname, character);
        // Broadcast updated lobby list to all clients
        BroadcastPlayerListClientRpc(senderId, nickname, character);
    }

    [ClientRpc]
    private void BroadcastPlayerListClientRpc(ulong clientId, string nickname, string character)
    {
        // Non-server clients can update their local lobby UI
        if (!IsServer)
            controller?.RegisterRemotePlayerData(clientId, nickname, character);
    }
}


