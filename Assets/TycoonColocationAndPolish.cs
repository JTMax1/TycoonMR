using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using TMPro;
using Unity.Netcode;

/// <summary>
/// Phase 18 & 20: Multi-Headset Local WiFi Colocation Sync & 3D Executive Seat Nameplates
/// Automatically manages 4 physical seat plaques around the table perimeter, binds to Unity Netcode
/// for GameObjects (NGO) when active, and broadcasts game state & real-time events (dice tosses, auction bids)
/// across local WiFi for multi-headset mixed reality sessions.
/// </summary>
[RequireComponent(typeof(TycoonBoardManager))]
public class TycoonColocationAndPolish : MonoBehaviour
{
    private TycoonBoardManager boardManager;

    [Header("1. Local WiFi Colocation Settings")]
    public bool enableLocalWiFiColocationSync = true;
    public int broadcastPort = 47777;

    [Header("2. Network Identity")]
    private string localDeviceId;
    private UdpClient udpBroadcaster;
    private UdpClient udpListener;
    private Thread listenerThread;
    private volatile bool isListening = false;
    private ConcurrentQueue<string> incomingPacketsQueue = new ConcurrentQueue<string>();

    [Header("3. 3D Seat Nameplate Components")]
    private GameObject[] seatNameplateRoots = new GameObject[4];
    private TextMeshProUGUI[] seatNameTexts = new TextMeshProUGUI[4];
    private TextMeshProUGUI[] seatCashTexts = new TextMeshProUGUI[4];
    private Renderer[] turnGlowStrips = new Renderer[4];
    private Material activeGlowMaterial;
    private Material inactiveGlowMaterial;

    [System.Serializable]
    public class TycoonNetworkPacket
    {
        public string packetType = "STATE_SYNC"; // "STATE_SYNC", "DICE_THROW", "AUCTION_BID"
        public string senderId;
        public long timestamp;
        public int currentTurn;
        public int[] playerCash;
        public int[] playerPos;
        public int[] tileOwners;
        public int[] tileLevels;
        public string anchorUuid;

        // Phase 20 Real-Time Event Sync Fields
        public Vector3 diceVelocity;
        public int die1Target = 6;
        public int die2Target = 6;
        public int auctionBidder = -1;
        public int auctionBidAmount = 0;
    }

    void Awake()
    {
        boardManager = GetComponent<TycoonBoardManager>();
        localDeviceId = SystemInfo.deviceUniqueIdentifier ?? Guid.NewGuid().ToString().Substring(0, 8);
    }

    void Start()
    {
        // 1. Create Materials
        activeGlowMaterial = boardManager.CreateColoredMaterial(new Color(0.0f, 1.0f, 0.55f), 0.95f);
        inactiveGlowMaterial = boardManager.CreateColoredMaterial(new Color(0.12f, 0.15f, 0.20f), 0.35f);

        // 2. Spawn 3D Executive Seat Nameplates
        SpawnExecutiveSeatNameplates();

        // 3. Initialize Local WiFi Sync Broadcast & Listener (cooperative fallback)
        if (enableLocalWiFiColocationSync)
        {
            InitializeLocalWiFiBridge();
        }

        // 4. Hook Game State Change Events
        boardManager.OnGameStateModified += HandleLocalGameStateChanged;
        boardManager.OnTurnStarted += HandleTurnStarted;
        boardManager.OnMultiplayerMatchRequested += HandleMultiplayerMatchRequested;

        // Initial refresh
        UpdateSeatNameplatesVisuals();
    }

    void OnDestroy()
    {
        if (boardManager != null)
        {
            boardManager.OnGameStateModified -= HandleLocalGameStateChanged;
            boardManager.OnTurnStarted -= HandleTurnStarted;
            boardManager.OnMultiplayerMatchRequested -= HandleMultiplayerMatchRequested;
        }

        ShutdownWiFiBridge();
    }

    void OnApplicationQuit()
    {
        ShutdownWiFiBridge();
    }

    private void HandleMultiplayerMatchRequested(TycoonBoardManager.MatchMultiplayerMode mode)
    {
        if (mode == TycoonBoardManager.MatchMultiplayerMode.LocalWiFiColocation)
        {
            try
            {
                if (NetworkManager.Singleton != null)
                {
                    if (!NetworkManager.Singleton.IsListening)
                    {
                        NetworkManager.Singleton.StartHost();
                        boardManager.RefreshDashboard(
                            "LOCAL WIFI COLOCATION HOST",
                            "Netcode Host started! Colocation building blocks active. Waiting for client headsets..."
                        );
                    }
                }
                else
                {
                    boardManager.RefreshDashboard(
                        "LOCAL WIFI COLOCATION READY",
                        $"UDP Colocation Bridge active on port {broadcastPort} (Single-Device Fallback)."
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TycoonColocation] Netcode start host note: {ex.Message}");
            }
        }
        else if (mode == TycoonBoardManager.MatchMultiplayerMode.OnlineMultiplayer)
        {
            try
            {
                if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
                {
                    NetworkManager.Singleton.StartHost();
                    boardManager.RefreshDashboard(
                        "ONLINE MULTIPLAYER SESSION",
                        "Netcode Online session initialized! Meta Matchmaking building blocks active in scene."
                    );
                }
                else
                {
                    boardManager.RefreshDashboard(
                        "ONLINE MULTIPLAYER READY",
                        "Global matchmaking session requested. Waiting for peer connections..."
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TycoonColocation] Netcode online start note: {ex.Message}");
            }
        }
    }

    void Update()
    {
        // 1. Process any incoming network packets from other Quest headsets on main thread
        while (incomingPacketsQueue.TryDequeue(out string rawJson))
        {
            ApplyIncomingNetworkPacket(rawJson);
        }

        // 2. Refresh Seat Nameplates
        UpdateSeatNameplatesVisuals();
    }

    #region A. 3D Executive Seat Nameplates

    private void SpawnExecutiveSeatNameplates()
    {
        // Board extent: 0.50m (bezel edge sits at ~0.50m).
        // Outer radius 0.59m places plaques cleanly outside the gold bezel with zero tile overlap.
        float offset = 0.59f;
        float yPos = 0.015f;

        Vector3[] seatOffsets = new Vector3[]
        {
            new Vector3(0f, yPos, -offset),  // Seat 0: South (YOU)
            new Vector3(0f, yPos, offset),   // Seat 1: North (ALPHA)
            new Vector3(-offset, yPos, 0f),  // Seat 2: West (BETA)
            new Vector3(offset, yPos, 0f)    // Seat 3: East (GAMMA)
        };

        Quaternion[] seatRotations = new Quaternion[]
        {
            Quaternion.Euler(22f, 0f, 0f),    // Facing South spectator
            Quaternion.Euler(22f, 180f, 0f),  // Facing North spectator
            Quaternion.Euler(22f, -90f, 0f),  // Facing West spectator
            Quaternion.Euler(22f, 90f, 0f)    // Facing East spectator
        };

        for (int p = 0; p < 4; p++)
        {
            if (seatNameplateRoots[p] != null) Destroy(seatNameplateRoots[p]);

            GameObject plaqueRoot = new GameObject($"Executive_Seat_Nameplate_Player_{p}");
            plaqueRoot.transform.SetParent(transform, false);
            plaqueRoot.transform.localPosition = seatOffsets[p];
            plaqueRoot.transform.localRotation = seatRotations[p];
            seatNameplateRoots[p] = plaqueRoot;

            Color pCol = boardManager.GetPlayerColorPublic(p);

            // Sleek dark marble base plaque
            GameObject basePlinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            basePlinth.name = "Plaque_Plinth";
            basePlinth.transform.SetParent(plaqueRoot.transform, false);
            basePlinth.transform.localPosition = Vector3.zero;
            basePlinth.transform.localRotation = Quaternion.identity;
            basePlinth.transform.localScale = new Vector3(0.18f, 0.012f, 0.075f);
            Destroy(basePlinth.GetComponent<Collider>());
            basePlinth.GetComponent<Renderer>().material = boardManager.CreateColoredMaterial(new Color(0.08f, 0.10f, 0.13f), 0.92f);

            // Gold beveled border trim
            GameObject goldTrim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            goldTrim.name = "Gold_Trim";
            goldTrim.transform.SetParent(plaqueRoot.transform, false);
            goldTrim.transform.localPosition = new Vector3(0f, -0.002f, 0f);
            goldTrim.transform.localRotation = Quaternion.identity;
            goldTrim.transform.localScale = new Vector3(0.188f, 0.008f, 0.083f);
            Destroy(goldTrim.GetComponent<Collider>());
            goldTrim.GetComponent<Renderer>().material = boardManager.CreateColoredMaterial(new Color(0.85f, 0.70f, 0.28f), 0.96f);

            // Glowing neon indicator strip (emerald/gold when turn active)
            GameObject glowStrip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glowStrip.name = "Turn_Glow_Strip";
            glowStrip.transform.SetParent(plaqueRoot.transform, false);
            glowStrip.transform.localPosition = new Vector3(0f, 0.007f, 0.033f);
            glowStrip.transform.localRotation = Quaternion.identity;
            glowStrip.transform.localScale = new Vector3(0.165f, 0.003f, 0.006f);
            Destroy(glowStrip.GetComponent<Collider>());
            turnGlowStrips[p] = glowStrip.GetComponent<Renderer>();
            turnGlowStrips[p].material = inactiveGlowMaterial;

            // Player Accent Strip
            GameObject accentBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            accentBar.name = "Player_Accent_Bar";
            accentBar.transform.SetParent(plaqueRoot.transform, false);
            accentBar.transform.localPosition = new Vector3(-0.078f, 0.007f, 0f);
            accentBar.transform.localRotation = Quaternion.identity;
            accentBar.transform.localScale = new Vector3(0.006f, 0.003f, 0.055f);
            Destroy(accentBar.GetComponent<Collider>());
            accentBar.GetComponent<Renderer>().material = boardManager.CreateColoredMaterial(pCol, 0.90f);

            // World Space UI Canvas for Name and Live Cash
            GameObject canvasGO = new GameObject("Plaque_Canvas");
            canvasGO.transform.SetParent(plaqueRoot.transform, false);
            canvasGO.transform.localPosition = new Vector3(0.008f, 0.007f, 0f);
            canvasGO.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            canvasGO.transform.localScale = Vector3.one * 0.0014f;

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform crt = canvasGO.GetComponent<RectTransform>();
            crt.sizeDelta = new Vector2(110f, 48f);

            // Player Name Text
            GameObject nameGO = new GameObject("PlayerName");
            nameGO.transform.SetParent(canvasGO.transform, false);
            seatNameTexts[p] = nameGO.AddComponent<TextMeshProUGUI>();
            seatNameTexts[p].rectTransform.sizeDelta = new Vector2(110f, 22f);
            seatNameTexts[p].rectTransform.anchoredPosition = new Vector2(0f, 10f);
            seatNameTexts[p].alignment = TextAlignmentOptions.Left;
            seatNameTexts[p].fontSize = 14f;
            seatNameTexts[p].fontStyle = FontStyles.Bold;
            seatNameTexts[p].color = Color.white;

            // Cash Text
            GameObject cashGO = new GameObject("PlayerCash");
            cashGO.transform.SetParent(canvasGO.transform, false);
            seatCashTexts[p] = cashGO.AddComponent<TextMeshProUGUI>();
            seatCashTexts[p].rectTransform.sizeDelta = new Vector2(110f, 22f);
            seatCashTexts[p].rectTransform.anchoredPosition = new Vector2(0f, -10f);
            seatCashTexts[p].alignment = TextAlignmentOptions.Left;
            seatCashTexts[p].fontSize = 15f;
            seatCashTexts[p].fontStyle = FontStyles.Bold;
            seatCashTexts[p].color = new Color(0.25f, 0.95f, 0.45f);
        }
    }

    private void UpdateSeatNameplatesVisuals()
    {
        if (boardManager == null) return;

        int activeTurn = boardManager.CurrentTurnPlayer;
        int[] cash = boardManager.GetPlayerCashArray();

        for (int p = 0; p < 4; p++)
        {
            if (seatNameTexts[p] != null)
            {
                string pName = boardManager.GetPlayerNamePublic(p);
                bool isTurn = (p == activeTurn);
                seatNameTexts[p].text = isTurn ? $"<color=#00FF88><b>> {pName}</b></color>" : pName;
            }

            if (seatCashTexts[p] != null && cash != null && p < cash.Length)
            {
                seatCashTexts[p].text = $"${cash[p]}";
            }

            if (turnGlowStrips[p] != null)
            {
                bool isTurn = (p == activeTurn);
                turnGlowStrips[p].material = isTurn ? activeGlowMaterial : inactiveGlowMaterial;
            }
        }
    }

    private void HandleTurnStarted(int turnPlayerIdx)
    {
        UpdateSeatNameplatesVisuals();
        BroadcastCurrentGameState();
    }

    #endregion

    #region B. Local WiFi Colocation Sync Bridge (UDP Port 47777 + Netcode Events)

    private void InitializeLocalWiFiBridge()
    {
        try
        {
            // UDP Broadcaster client
            udpBroadcaster = new UdpClient();
            udpBroadcaster.EnableBroadcast = true;

            // UDP Listener client with port reuse
            udpListener = new UdpClient();
            udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpListener.Client.Bind(new IPEndPoint(IPAddress.Any, broadcastPort));

            isListening = true;
            listenerThread = new Thread(ListenForNetworkPacketsWorker);
            listenerThread.IsBackground = true;
            listenerThread.Start();

            Debug.Log($"<color=#00FF88>[TycoonColocation]</color> Local WiFi Colocation Sync active on UDP port {broadcastPort} (DeviceID: {localDeviceId})");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[TycoonColocation] UDP Socket Note: {ex.Message} (Single device fallback enabled)");
        }
    }

    private void ShutdownWiFiBridge()
    {
        isListening = false;
        try
        {
            if (udpListener != null)
            {
                udpListener.Close();
                udpListener = null;
            }
            if (udpBroadcaster != null)
            {
                udpBroadcaster.Close();
                udpBroadcaster = null;
            }
            if (listenerThread != null && listenerThread.IsAlive)
            {
                listenerThread.Join(150);
                listenerThread = null;
            }
        }
        catch (Exception) { }
    }

    private void ListenForNetworkPacketsWorker()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, broadcastPort);
        while (isListening && udpListener != null)
        {
            try
            {
                byte[] bytes = udpListener.Receive(ref remoteEP);
                if (bytes != null && bytes.Length > 0)
                {
                    string json = Encoding.UTF8.GetString(bytes);
                    incomingPacketsQueue.Enqueue(json);
                }
            }
            catch (SocketException)
            {
                // Socket closed or interrupted
                break;
            }
            catch (Exception)
            {
                // Thread interruption
                break;
            }
        }
    }

    private void HandleLocalGameStateChanged()
    {
        BroadcastCurrentGameState();
    }

    public void BroadcastCurrentGameState()
    {
        if (!enableLocalWiFiColocationSync || udpBroadcaster == null || boardManager == null) return;

        try
        {
            TycoonNetworkPacket packet = new TycoonNetworkPacket
            {
                packetType = "STATE_SYNC",
                senderId = localDeviceId,
                timestamp = DateTime.UtcNow.Ticks,
                currentTurn = boardManager.CurrentTurnPlayer,
                playerCash = boardManager.GetPlayerCashArray(),
                playerPos = boardManager.GetPlayerPosArray(),
                tileOwners = boardManager.GetTileOwnersArray(),
                tileLevels = boardManager.GetTileLevelsArray(),
                anchorUuid = PlayerPrefs.GetString("Tycoon_SpatialAnchor_UUID", "")
            };

            SendNetworkPacket(packet);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[TycoonColocation] Broadcast error: {ex.Message}");
        }
    }

    public void BroadcastDiceThrow(Vector3 velocity, int d1Target, int d2Target)
    {
        if (!enableLocalWiFiColocationSync || udpBroadcaster == null) return;

        try
        {
            TycoonNetworkPacket packet = new TycoonNetworkPacket
            {
                packetType = "DICE_THROW",
                senderId = localDeviceId,
                timestamp = DateTime.UtcNow.Ticks,
                currentTurn = boardManager.CurrentTurnPlayer,
                diceVelocity = velocity,
                die1Target = d1Target,
                die2Target = d2Target
            };

            SendNetworkPacket(packet);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[TycoonColocation] BroadcastDiceThrow error: {ex.Message}");
        }
    }

    public void BroadcastAuctionBid(int bidderIdx, int bidAmount)
    {
        if (!enableLocalWiFiColocationSync || udpBroadcaster == null) return;

        try
        {
            TycoonNetworkPacket packet = new TycoonNetworkPacket
            {
                packetType = "AUCTION_BID",
                senderId = localDeviceId,
                timestamp = DateTime.UtcNow.Ticks,
                currentTurn = boardManager.CurrentTurnPlayer,
                auctionBidder = bidderIdx,
                auctionBidAmount = bidAmount
            };

            SendNetworkPacket(packet);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[TycoonColocation] BroadcastAuctionBid error: {ex.Message}");
        }
    }

    private void SendNetworkPacket(TycoonNetworkPacket packet)
    {
        if (udpBroadcaster == null) return;
        string json = JsonUtility.ToJson(packet);
        byte[] payload = Encoding.UTF8.GetBytes(json);
        IPEndPoint broadcastEP = new IPEndPoint(IPAddress.Broadcast, broadcastPort);
        udpBroadcaster.Send(payload, payload.Length, broadcastEP);
    }

    private void ApplyIncomingNetworkPacket(string json)
    {
        if (string.IsNullOrEmpty(json) || boardManager == null) return;

        try
        {
            TycoonNetworkPacket packet = JsonUtility.FromJson<TycoonNetworkPacket>(json);
            if (packet == null) return;

            // Ignore our own broadcasted packets
            if (packet.senderId == localDeviceId) return;

            if (packet.packetType == "DICE_THROW")
            {
                boardManager.ExecuteRemoteDiceTumble(packet.diceVelocity, packet.die1Target, packet.die2Target);
                return;
            }

            if (packet.packetType == "AUCTION_BID")
            {
                boardManager.ApplyRemoteAuctionBid(packet.auctionBidder, packet.auctionBidAmount);
                return;
            }

            // Sync state into board manager
            boardManager.SyncStateFromNetwork(
                packet.currentTurn,
                packet.playerCash,
                packet.playerPos,
                packet.tileOwners,
                packet.tileLevels,
                packet.anchorUuid
            );

            UpdateSeatNameplatesVisuals();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[TycoonColocation] Failed to parse incoming packet: {ex.Message}");
        }
    }

    #endregion
}
