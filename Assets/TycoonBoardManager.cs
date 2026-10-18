using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

[RequireComponent(typeof(AudioSource))]
public partial class TycoonBoardManager : MonoBehaviour
{
    [Header("1. Mixed Reality References")]
    public OVRPassthroughLayer passthroughLayer;

    [Header("2. Custom Player Vehicle & Rival Avatar Prefabs (Drag & Drop)")]
    public GameObject dicePrefab;
    [Tooltip("Vehicle prefab for Player 0 (You - South)")]
    public GameObject playerTokenPrefab;
    [Tooltip("Vehicle prefab for AI Rival 1 (Mogul Alpha - North)")]
    public GameObject aiRivalTokenPrefab;
    [Tooltip("Vehicle prefab for AI Rival 2 (Baroness Beta - West)")]
    public GameObject rival2TokenPrefab;
    [Tooltip("Vehicle prefab for AI Rival 3 (Titan Gamma - East)")]
    public GameObject rival3TokenPrefab;
    [Tooltip("Optional list to drag & drop player vehicle prefabs (indexed P0..P3)")]
    public List<GameObject> playerTokenPrefabs = new List<GameObject>();

    [Space(8)]
    [Tooltip("3D Avatar prefab for AI Rival 1 (Mogul Alpha - North)")]
    public GameObject rivalAvatarPrefab;
    [Tooltip("3D Avatar prefab for AI Rival 2 (Baroness Beta - West)")]
    public GameObject rival2AvatarPrefab;
    [Tooltip("3D Avatar prefab for AI Rival 3 (Titan Gamma - East)")]
    public GameObject rival3AvatarPrefab;
    [Tooltip("List to drag & drop custom avatar prefabs for all active AI rivals")]
    public List<GameObject> rivalAvatarPrefabs = new List<GameObject>();

    [Header("3. Chronological Building Prefab Holders (Levels 1 to 5)")]
    public GameObject level1HousePrefab;
    public GameObject level2HousePrefab;
    public GameObject level3HousePrefab;
    public GameObject level4HousePrefab;
    public GameObject level5SkyscraperPrefab;

    [Header("4. Courtyard Prefab Holders (Single Drag & Drop Slots)")]
    public GameObject parkTreePrefab;
    public GameObject parkCenterpiecePrefab;
    public GameObject streetlightPrefab;
    public GameObject ambientTrafficCar1Prefab;
    public GameObject ambientTrafficCar2Prefab;
    public GameObject vipHelicopterPrefab;

    [Header("5. Optional Custom Sound Effects")]
    public AudioClip diceRollSound;
    public AudioClip tokenMoveSound;
    public AudioClip cashRegisterSound;
    public AudioClip buildUpgradeSound;

    [Header("6. Table Surface Materials & Dimensions")]
    public Material darkMarbleMaterial;
    public Material brushedGoldMaterial;
    public float boardSize = 1.05f;
    public float currentBoardScale = 0.85f;

    [Header("7. Landmark, Road & Prop Prefab Holders (Drag & Drop - Auto-Filled in Editor)")]
    public bool showCornerLandmarks = true;
    [Tooltip("Landmark placed beside Tile 0 (GO) - e.g. Market")]
    public GameObject goCornerLandmarkPrefab;
    [Tooltip("Landmark placed beside Tile 10 (Jail) - e.g. Police_Station")]
    public GameObject jailCornerLandmarkPrefab;
    [Tooltip("Landmark placed beside Tile 20 (Free Parking) - e.g. Hospital or Fire_Station")]
    public GameObject freeParkingLandmarkPrefab;
    [Tooltip("Landmark placed beside Tile 30 (Go To Jail) - e.g. School or Workshop")]
    public GameObject goToJailLandmarkPrefab;
    [Tooltip("Optional Road Straight prefab for street diorama")]
    public GameObject roadStraightPrefab;
    [Tooltip("Optional Billboard prop prefab")]
    public GameObject billboardPropPrefab;

    [Header("8. Interactive Props & Minigame Prefab Slots (Auto-Searched + Drag & Drop)")]
    public GameObject tradeContractPrefab;
    public GameObject waxSealStampPrefab;
    public GameObject auctionGavelPrefab;
    public GameObject cashBriefcasePrefab;
    public GameObject bankVaultPrefab;
    public GameObject dollarBillPrefab;
    public GameObject cashBundlePrefab;
    public GameObject goldBarPrefab;
    public GameObject padlockPrefab;
    public GameObject steamPipeValvePrefab;
    public GameObject jackpotChestPrefab;
    public GameObject pedestrianPrefab;
    public GameObject crosswalkPropPrefab;
    public GameObject victoryTrophyPrefab;

    private BoardTile[] board = new BoardTile[40];

    private int[] playerCash = new int[4] { 1500, 1500, 1500, 1500 };
    private int[] playerPos = new int[4] { 0, 0, 0, 0 };
    private bool[] inJail = new bool[4] { false, false, false, false };
    private Transform[] playerTokens = new Transform[4];
    private bool[] isBankrupt = new bool[4] { false, false, false, false };
    private int activePlayerCount = 4;
    private int startingCashConfig = 1500;

    public enum AIDifficulty { Tycoon, Billionaire, Casual }
    private AIDifficulty aiDifficulty = AIDifficulty.Tycoon;

    private int buildCycleCursor = 0;

    private int freeParkingJackpot = 100;
    private Transform jackpotGoldPile;

    public enum TurnState { StartMenu, WaitingForRoll, DiceTumbling, Moving, BuyOrPassDecision, AuctionInProgress, AITurn, Handover }
    private TurnState currentState = TurnState.WaitingForRoll;
    private int lastDiceTotal = 0;
    private int consecutiveDoubles = 0;

    public TurnState CurrentState => currentState;
    public bool IsWaitingForRoll => currentState == TurnState.WaitingForRoll;
    public bool IsBuyOrPassDecision => currentState == TurnState.BuyOrPassDecision;

    public bool IsHumanPlayer(int seatIdx)
    {
        if (seatIdx < 0 || seatIdx >= activePlayerCount) return false;
        if (currentMatchMode == MatchMultiplayerMode.PassAndPlay) return true;
        if (currentMatchMode == MatchMultiplayerMode.VsAIMoguls) return seatIdx == 0;
        return seatIdx == 0;
    }

    public int GetRemainingActivePlayerCount()
    {
        int count = 0;
        for (int i = 0; i < activePlayerCount; i++)
        {
            if (isBankrupt != null && !isBankrupt[i]) count++;
        }
        return count;
    }

    public Vector3 LeftHandWorldPos => leftHandWorldPos;
    public Vector3 RightHandWorldPos => rightHandWorldPos;
    public bool IsLeftHandTracked => isLeftHandTracked;
    public bool IsRightHandTracked => isRightHandTracked;
    public bool IsLeftHandGrabbing => isLeftHandGrabbing;
    public bool IsRightHandGrabbing => isRightHandGrabbing;
    public Transform Die1Transform => die1Transform;
    public Transform Die2Transform => die2Transform;
    public PhysicalThrowDie PhysDie1 => physDie1;
    public PhysicalThrowDie PhysDie2 => physDie2;
    public bool IsPlayerHoldingDice { get => isPlayerHoldingDice; set => isPlayerHoldingDice = value; }
    public AudioClip DiceRollSound => diceRollSound != null ? diceRollSound : procDiceClip;
    public AudioClip CashRegisterSound => cashRegisterSound != null ? cashRegisterSound : procCashClip;
    public AudioClip TokenMoveSound => tokenMoveSound != null ? tokenMoveSound : procStepClip;
    public BoardTile CurrentLandingTile => (board != null && playerPos != null && playerPos.Length > 0 && playerPos[0] >= 0 && playerPos[0] < 40) ? board[playerPos[0]] : null;
    public int PlayerCash => playerCash != null && playerCash.Length > 0 ? playerCash[0] : 0;
    private TycoonTactileInteractions tactileInteractions;
    public TycoonTactileInteractions TactileInteractions => tactileInteractions;
    private TycoonMinigameOrchestrator minigameOrchestrator;
    public TycoonMinigameOrchestrator MinigameOrchestrator => minigameOrchestrator;
    private TycoonSpatialAnchorAndDiorama spatialAnchorAndDiorama;
    public TycoonSpatialAnchorAndDiorama SpatialAnchorAndDiorama => spatialAnchorAndDiorama;
    private TycoonExecutiveBriefcaseAndTrophy executiveBriefcaseAndTrophy;
    public TycoonExecutiveBriefcaseAndTrophy ExecutiveBriefcaseAndTrophy => executiveBriefcaseAndTrophy;
    private TycoonColocationAndPolish colocationAndPolish;
    public TycoonColocationAndPolish ColocationAndPolish => colocationAndPolish;
    public AudioClip BuildUpgradeSound => buildUpgradeSound;

    public bool isPassAndPlayMode = false;
    protected int matchSessionGeneration = 0;
    public int MatchSessionGeneration => matchSessionGeneration;
    public Vector3 LockedBoardPosition { get => lockedBoardPosition; set { lockedBoardPosition = value; transform.position = value; OnBoardPoseUpdated?.Invoke(lockedBoardPosition, lockedBoardRotation); } }
    public Quaternion LockedBoardRotation { get => lockedBoardRotation; set { lockedBoardRotation = value; transform.rotation = value; OnBoardPoseUpdated?.Invoke(lockedBoardPosition, lockedBoardRotation); } }
    public void AddPlayerCash(int playerIdx, int delta)
    {
        if (playerCash != null && playerIdx >= 0 && playerIdx < playerCash.Length)
        {
            playerCash[playerIdx] += delta;
            UpdateLiveHUDStats();
            OnGameStateModified?.Invoke();
        }
    }
    public void SetPlayerCash(int playerIdx, int amount)
    {
        if (playerCash != null && playerIdx >= 0 && playerIdx < playerCash.Length)
        {
            playerCash[playerIdx] = amount;
            UpdateLiveHUDStats();
            OnGameStateModified?.Invoke();
        }
    }
    public int CurrentTurnPlayer { get; private set; } = 0;
    public event System.Action<int> OnTurnStarted;
    public event System.Action<int, int> OnRivalPaidRentToPlayer;
    public event System.Action OnVictoryTrophyTriggered;
    public event System.Action<Vector3, Quaternion> OnBoardPoseUpdated;
    public event System.Action OnGameStateModified;

    public bool CheckAllRivalsBankruptPublic() => CheckAllRivalsBankrupt();
    public void TriggerVictoryTrophy() => OnVictoryTrophyTriggered?.Invoke();
    public void NotifyGameStateModified() => OnGameStateModified?.Invoke();

    public void SetCurrentMatchMode(MatchMultiplayerMode mode)
    {
        currentMatchMode = mode;
        isPassAndPlayMode = (mode == MatchMultiplayerMode.PassAndPlay);
    }
    public MatchMultiplayerMode CurrentMatchMode => currentMatchMode;
    public void SetActivePlayerCount(int count) => activePlayerCount = count;
    public int ActivePlayerCount => activePlayerCount;
    public void SetPlayerBankrupt(int seat, bool bankrupt)
    {
        if (isBankrupt != null && seat >= 0 && seat < isBankrupt.Length) isBankrupt[seat] = bankrupt;
    }
    public void SetCurrentState(TurnState state)
    {
        currentState = state;
        isHudDirty = true;
    }
    public void SetCurrentTurnPlayer(int seat)
    {
        CurrentTurnPlayer = seat;
        OnTurnStarted?.Invoke(seat);
    }
    public bool IsNetworkStateSyncAuthorized => enableNetworkStateSync;
    public void CreateStartMenuForTest() => Create3DHolographicStartMenu();

    public int[] GetPlayerCashArray() => (int[])playerCash.Clone();
    public int[] GetPlayerPosArray() => (int[])playerPos.Clone();
    public int[] GetTileOwnersArray()
    {
        int[] owners = new int[40];
        for (int i = 0; i < 40; i++) owners[i] = board != null && board[i] != null ? board[i].owner : -1;
        return owners;
    }
    public int[] GetTileLevelsArray()
    {
        int[] levels = new int[40];
        for (int i = 0; i < 40; i++) levels[i] = board != null && board[i] != null ? board[i].buildingLevel : 0;
        return levels;
    }
    public string GetPlayerNamePublic(int playerIdx) => GetPlayerName(playerIdx);
    public Color GetPlayerColorPublic(int playerIdx) => GetPlayerColor(playerIdx);
    public int CalculatePlayerNetWorthPublic(int playerIdx) => CalculatePlayerNetWorth(playerIdx);

    [Header("Network Authority & State Sync Containment")]
    public bool enableNetworkStateSync = false;

    public void SyncStateFromNetwork(int turnPlayer, int[] incomingCash, int[] incomingPos, int[] incomingOwners, int[] incomingLevels, string anchorUuid)
    {
        if (!enableNetworkStateSync)
        {
            Debug.LogWarning("[TycoonBoardManager] SyncStateFromNetwork ignored: network state synchronization is disabled to preserve authoritative local state.");
            return;
        }

        if (incomingCash != null && incomingCash.Length == 4)
        {
            for (int i = 0; i < 4; i++) playerCash[i] = incomingCash[i];
        }
        if (incomingPos != null && incomingPos.Length == 4)
        {
            for (int i = 0; i < 4; i++)
            {
                if (playerPos[i] != incomingPos[i])
                {
                    playerPos[i] = incomingPos[i];
                    PositionTokenOnTile(i, playerPos[i]);
                }
            }
        }
        if (incomingOwners != null && incomingLevels != null && board != null)
        {
            for (int i = 0; i < 40 && i < incomingOwners.Length && i < incomingLevels.Length; i++)
            {
                if (board[i] == null) continue;
                bool ownerChanged = board[i].owner != incomingOwners[i];
                bool levelChanged = board[i].buildingLevel != incomingLevels[i];
                if (ownerChanged || levelChanged)
                {
                    board[i].owner = incomingOwners[i];
                    board[i].buildingLevel = incomingLevels[i];
                    ApplyTileOwnershipVisuals(board[i], board[i].owner);
                }
            }
            RebuildMonopolySynergyBeams();
        }
        if (!string.IsNullOrEmpty(anchorUuid) && spatialAnchorAndDiorama != null)
        {
            spatialAnchorAndDiorama.AlignToSharedAnchor(anchorUuid);
        }

        CurrentTurnPlayer = turnPlayer;
        UpdateLiveHUDStats();
    }

    public void ExecuteRemoteDiceTumble(Vector3 velocity, int d1Target, int d2Target)
    {
        if (physDie1 != null) physDie1.LaunchWithDirectVelocity(velocity, transform.position);
        if (physDie2 != null) physDie2.LaunchWithDirectVelocity(velocity + Vector3.right * 0.05f, transform.position);
        PlayDiceRattleSound();
        PlaySFX(diceRollSound, procDiceClip, 1.15f);
        RefreshDashboard("REMOTE PLAYER THREW DICE!", "Synchronizing real-time physics dice roll across headsets...");
    }

    public void ApplyRemoteAuctionBid(int bidderIdx, int bidAmount)
    {
        StartCoroutine(AnimateGavelStrike());
        PlaySFX(diceRollSound, procDiceClip, 1.4f);
        RefreshDashboard($"AUCTION BID: ${bidAmount}", $"{GetPlayerName(bidderIdx).ToUpper()} placed bid of ${bidAmount}!");
    }

    private GameObject jailLockpickButton;
    private bool isLockpickingInProgress = false;

    public enum MarketCycle { Standard, BullBoom, BearRecession, TechIPOSurge }
    private MarketCycle currentMarketCycle = MarketCycle.Standard;
    private int totalTurnsCompleted = 0;
    private GameObject marketAuraRing;

    private bool auctionPlayerBidFlag = false;
    private bool auctionPlayerDropFlag = false;

    private Transform die1Transform;
    private Transform die2Transform;
    private PhysicalThrowDie physDie1;
    private PhysicalThrowDie physDie2;
    private bool isPlayerHoldingDice = false;
    private bool isGrabbingDiceWithRight = true;

    private GameObject grabbableDeedCrystal;
    private bool isHoldingDeedCrystal = false;
    private bool isHoldingDeedWithRight = true;

    // 4-Player AI Avatar Arrays (1 = Alpha [North], 2 = Beta [West], 3 = Gamma [East])
    private Transform[] rivalAvatarRoots = new Transform[4];
    private Transform[] rivalHeadTransforms = new Transform[4];
    private Transform[] rivalLeftHands = new Transform[4];
    private Transform[] rivalRightHands = new Transform[4];
    private Renderer[] rivalVisorRenderers = new Renderer[4];
    private TextMeshProUGUI[] rivalSpeechBubbles = new TextMeshProUGUI[4];
    public enum AvatarMood { Neutral, HappyCheer, AngrySlam, Thinking }
    private AvatarMood[] currentRivalMoods = new AvatarMood[4] { AvatarMood.Neutral, AvatarMood.Neutral, AvatarMood.Neutral, AvatarMood.Neutral };
    private float[] moodResetTimers = new float[4];

    // Spatial AI Voice Synth, District Soundscapes & 3D Laser Jail Cages
    private AudioSource[] rivalVoiceSources = new AudioSource[4];
    private AudioClip procVoiceAlpha;
    private AudioClip procVoiceBeta;
    private AudioClip procVoiceGamma;
    private AudioClip procSirenClip;
    private AudioClip procChopperHumClip;
    private AudioClip procGlassShatterClip;
    private GameObject[] jailCageHolders = new GameObject[4];

    // Legacy single avatar references for backwards compatibility
    private Transform rivalAvatarRoot;
    private Transform rivalHeadTransform;
    private Transform rivalLeftHandTransform;
    private Transform rivalRightHandTransform;
    private Renderer rivalVisorRenderer;
    private TextMeshProUGUI rivalSpeechBubbleTMP;
    private AvatarMood currentRivalMood = AvatarMood.Neutral;
    private float moodResetTimer = 0f;

    // 4 Match Modes (AI, Pass-and-Play, Local WiFi, Online)
    public enum MatchMultiplayerMode { VsAIMoguls, PassAndPlay, LocalWiFiColocation, OnlineMultiplayer }
    public MatchMultiplayerMode currentMatchMode = MatchMultiplayerMode.VsAIMoguls;
    public System.Action<MatchMultiplayerMode> OnMultiplayerMatchRequested;

    // 3D Holographic Start Menu Fields
    private GameObject startMenuHolder;
    private Transform[] startMenuButtonTransforms = new Transform[5];
    private TextMeshProUGUI[] startMenuButtonTexts = new TextMeshProUGUI[5];

    private TextMeshProUGUI dashboardTitleTMP;
    private TextMeshProUGUI dashboardBodyTMP;
    private TextMeshProUGUI dashboardPromptTMP;
    private TextMeshProUGUI dashboardStatsTMP;

    private int selectedTileIndex = -1;
    private float lastTilePokeTime = 0f;
    private Transform mortgageButtonTransform;
    private GameObject mortgageBtnCanvasGO;
    private TextMeshProUGUI mortgageBtnText;
    private Renderer mortgageBtnRenderer;

    private Transform upgradeCardButtonTransform;
    private GameObject upgradeBtnCanvasGO;
    private TextMeshProUGUI upgradeBtnText;
    private Renderer upgradeBtnRenderer;

    private GameObject deedCardHolder;
    private Renderer deedHeaderRenderer;
    private TextMeshProUGUI deedTitleTMP;
    private TextMeshProUGUI deedBodyTMP;

    private List<Transform> ambientCars = new List<Transform>();
    private List<float> ambientCarProgress = new List<float>();
    private List<Light> cityNightLights = new List<Light>();
    private Transform vipChopperTransform;
    private Transform chopperRotorTransform;
    private GameObject monopolyBeamsContainer;

    private GameObject vrSkyDome;
    private GameObject vrExitFloatingButton;
    private GameObject vrRooftopTeleportButton;
    private bool isStandingOnRooftop = false;
    public bool IsStandingOnRooftop => isStandingOnRooftop;

    private AudioSource sfxSource;
    private Material masterURPMaterial;

    private Transform[] buttonTransforms = new Transform[10];
    private float lastButtonTouchTime = 0f;
    private float newGameArmTime = 0f;
    private float deskSnapPinchTimer = 0f;

    private Transform offerDealButtonTransform;
    private GameObject offerDealBtnCanvasGO;
    private TextMeshProUGUI offerDealBtnText;
    private Renderer offerDealBtnRenderer;

    // Meta XR Interaction SDK Hand & Poke References
    private PokeInteractor[] scenePokeInteractors;
    private OVRHand[] sceneOVRHands;
    private OVRSkeleton[] sceneSkeletons;
    private Transform activeHandGrabTransform;
    private Vector3 leftHandWorldPos = new Vector3(0f, -100f, 0f);
    private Vector3 rightHandWorldPos = new Vector3(0f, -100f, 0f);
    private bool isLeftHandTracked = false;
    private bool isRightHandTracked = false;
    private bool isLeftHandGrabbing = false;
    private bool isRightHandGrabbing = false;
    private bool spacePressed = false;
    private bool buyPressed = false;
    private bool passPressed = false;
    private bool isHudDirty = true;
    public void MarkHudDirty() => isHudDirty = true;

    private bool isTwoHandManipulating = false;
    private float initialTwoHandDistance = 0.3f;
    private float initialScaleOnGrab = 0.85f;
    private Vector3 initialGrabMidpoint;
    private Vector3 initialBoardPosOnGrab;

    private AudioClip procDiceClip;
    private AudioClip procStepClip;
    private AudioClip procCashClip;
    private AudioClip procBuildClip;

    private bool isMarketNightMode = false;
    private bool isInPenthouseVR = false;
    private Vector3 lockedBoardPosition = new Vector3(0f, -0.34f, 0.88f);
    private Quaternion lockedBoardRotation = Quaternion.identity;
    private bool hasLockedInitialPose = false;

    private bool wasRightPinching = false;
    private bool wasLeftPinching = false;

    private GameObject buildingBlockGrabTemplate;
    private GameObject buildingBlockPokeTemplate;

    public (Vector3 pos, Quaternion rot) GetElevatedBoardFrontPose(float heightAboveTable = 0.34f, float pullTowardPlayer = 0.22f)
    {
        Transform eye = Camera.main != null ? Camera.main.transform : transform;
        Vector3 eyePos = eye.position;
        Vector3 boardPos = transform.position;

        Vector3 toPlayerHoriz = new Vector3(eyePos.x - boardPos.x, 0f, eyePos.z - boardPos.z);
        if (toPlayerHoriz.sqrMagnitude < 0.001f) toPlayerHoriz = -transform.forward;
        else toPlayerHoriz.Normalize();

        float frontDist = (boardSize * 0.5f * currentBoardScale) - (pullTowardPlayer * currentBoardScale);
        Vector3 targetHorizPos = boardPos + toPlayerHoriz * frontDist;

        float targetY = Mathf.Max(boardPos.y + (heightAboveTable * currentBoardScale), eyePos.y - 0.18f);
        Vector3 targetPos = new Vector3(targetHorizPos.x, targetY, targetHorizPos.z);

        float yaw = Mathf.Atan2(-toPlayerHoriz.x, -toPlayerHoriz.z) * Mathf.Rad2Deg;
        Quaternion targetRot = Quaternion.Euler(15f, yaw, 0f);

        return (targetPos, targetRot);
    }

    void Awake()
    {
        // 1. Disable Meta sample scripts on [BuildingBlock] GameObjects so they never call legacy Input.GetButtonDown in Update()
        MonoBehaviour[] allMBs = GameObject.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
        foreach (MonoBehaviour mb in allMBs)
        {
            if (mb == null) continue;
            GameObject go = mb.gameObject;
            if (go != null && go.name.StartsWith("[BuildingBlock]"))
            {
                string typeName = mb.GetType().Name;
                if (typeName.IndexOf("Sample", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    typeName.IndexOf("ButtonMapper", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    typeName.IndexOf("ControllerButtonsMapper", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    typeName.IndexOf("AnchorUIManager", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    typeName.IndexOf("Debug", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    mb.enabled = false;
                }
            }
        }

        // 1b. Disable any legacy StandaloneInputModule to eliminate InvalidOperationException spam
        UnityEngine.EventSystems.StandaloneInputModule[] legacyModules = GameObject.FindObjectsByType<UnityEngine.EventSystems.StandaloneInputModule>(FindObjectsInactive.Include);
        foreach (var sim in legacyModules)
        {
            if (sim != null) sim.enabled = false;
        }

        // 1c. Deactivate rogue matchmaking/colocation building blocks that cause startup errors
        string[] blocksToDeactivate = new string[]
        {
            "[BuildingBlock] Auto Matchmaking",
            "[BuildingBlock] Custom Matchmaking",
            "[BuildingBlock] Local Matchmaking",
            "[BuildingBlock] Colocation",
            "[BuildingBlock] MR Utility Kit"
        };
        foreach (string blockName in blocksToDeactivate)
        {
            GameObject bGo = GameObject.Find(blockName);
            if (bGo != null) bGo.SetActive(false);
        }

        // 2. Prefab safety guards against rogue Meta SDK assets
        if (tradeContractPrefab != null && (tradeContractPrefab.name.Contains("Microgesture") || tradeContractPrefab.name.Contains("Spawner") || tradeContractPrefab.name.Contains("NGO")))
            tradeContractPrefab = null;
        if (pedestrianPrefab != null && (pedestrianPrefab.name.Contains("Spawner") || pedestrianPrefab.name.Contains("Microgesture") || pedestrianPrefab.name.Contains("NGO")))
            pedestrianPrefab = null;

        GameObject locomotorObj = GameObject.Find("Locomotor");
        if (locomotorObj != null) locomotorObj.SetActive(false);

        GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (GameObject go in allObjects)
        {
            if (go == null) continue;
            if (go.name == "[BuildingBlock] Cube")
            {
                if (buildingBlockGrabTemplate == null) buildingBlockGrabTemplate = go;
                go.SetActive(false);
            }
            else if (go.name == "[BuildingBlock] Auto Matchmaking" ||
                     go.name == "[BuildingBlock] Custom Matchmaking" ||
                     go.name == "[BuildingBlock] Local Matchmaking" ||
                     go.name == "[BuildingBlock] Colocation" ||
                     go.name == "[BuildingBlock] MR Utility Kit")
            {
                go.SetActive(false);
            }
            else if (go.name.Contains("PokeInteraction") || go.name.Contains("Poke Interaction") || go.name == "Poke Interaction")
            {
                if (buildingBlockPokeTemplate == null) buildingBlockPokeTemplate = go;
                // Keep poke interactors active so poking functions natively!
            }
        }

        FixMicrogestureEventSources();

        Rigidbody boardRb = GetComponent<Rigidbody>();
        if (boardRb != null) SafeDestroy(boardRb);

        EnforceUnderlayPassthrough();
    }

    void FixMicrogestureEventSources()
    {
        OVRMicrogestureEventSource[] sources = FindObjectsByType<OVRMicrogestureEventSource>(FindObjectsInactive.Include);
        foreach (var src in sources)
        {
            if (src == null) continue;
            if (src.Hand == null)
            {
                OVRHand hand = src.GetComponentInParent<OVRHand>();
                if (hand == null) hand = src.GetComponentInChildren<OVRHand>();
                if (hand != null)
                {
                    src.Hand = hand;
                }
                else
                {
                    src.enabled = false;
                }
            }
        }
    }

    public void AttachNativeBuildingBlockGrab(GameObject targetObj, Rigidbody targetRb = null)
    {
        if (targetObj == null) return;
        if (targetRb == null)
        {
            targetRb = targetObj.GetComponent<Rigidbody>();
            if (targetRb == null) targetRb = targetObj.AddComponent<Rigidbody>();
        }

        if (!targetObj.name.StartsWith("Die"))
        {
            targetRb.useGravity = false;
            targetRb.isKinematic = true;
        }

        Grabbable targetGrabbable = targetObj.GetComponent<Grabbable>();
        if (targetGrabbable == null) targetGrabbable = targetObj.AddComponent<Grabbable>();
        targetGrabbable.InjectOptionalRigidbody(targetRb);

        if (buildingBlockGrabTemplate != null && buildingBlockGrabTemplate.transform.childCount > 0)
        {
            Transform existingChild = targetObj.transform.Find("[BuildingBlock] DistanceHandGrab");
            if (existingChild == null)
            {
                Transform templateChild = buildingBlockGrabTemplate.transform.GetChild(0);
                GameObject clonedChild = Instantiate(templateChild.gameObject, targetObj.transform);
                clonedChild.name = "[BuildingBlock] DistanceHandGrab";
                clonedChild.transform.localPosition = Vector3.zero;
                clonedChild.transform.localRotation = Quaternion.identity;
                clonedChild.transform.localScale = Vector3.one;
                clonedChild.SetActive(true);

                HandGrabInteractable hgi = clonedChild.GetComponent<HandGrabInteractable>();
                if (hgi != null)
                {
                    hgi.InjectRigidbody(targetRb);
                    hgi.InjectOptionalPointableElement(targetGrabbable);
                }

                DistanceHandGrabInteractable dhgi = clonedChild.GetComponent<DistanceHandGrabInteractable>();
                if (dhgi != null)
                {
                    dhgi.InjectRigidbody(targetRb);
                    dhgi.InjectOptionalPointableElement(targetGrabbable);
                }
            }
        }
        else
        {
            if (targetObj.GetComponent<HandGrabInteractable>() == null && targetObj.GetComponentInChildren<HandGrabInteractable>() == null)
            {
                HandGrabInteractable hgi = targetObj.AddComponent<HandGrabInteractable>();
                hgi.InjectRigidbody(targetRb);
                hgi.InjectOptionalPointableElement(targetGrabbable);
            }
            if (targetObj.GetComponent<DistanceHandGrabInteractable>() == null && targetObj.GetComponentInChildren<DistanceHandGrabInteractable>() == null)
            {
                DistanceHandGrabInteractable dhgi = targetObj.AddComponent<DistanceHandGrabInteractable>();
                dhgi.InjectRigidbody(targetRb);
                dhgi.InjectOptionalPointableElement(targetGrabbable);
            }
        }
    }

    void EnforceUnderlayPassthrough()
    {
#pragma warning disable CS0618
        OVRPassthroughLayer[] layers = Object.FindObjectsByType<OVRPassthroughLayer>(FindObjectsSortMode.None);
#pragma warning restore CS0618
        if (passthroughLayer == null && layers.Length > 0)
        {
            passthroughLayer = layers[0];
        }

        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] == null) continue;
            if (layers[i] == passthroughLayer)
            {
                layers[i].enabled = true;
            }
            else
            {
                layers[i].enabled = false;
            }
        }
    }

    public static void SafeDestroy(UnityEngine.Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }

    public void StripRoguePhysicsFromImportedPrefab(GameObject spawnedInstance)
    {
        if (spawnedInstance == null) return;
        Rigidbody[] rbs = spawnedInstance.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rbs.Length; i++) SafeDestroy(rbs[i]);

        Collider[] cols = spawnedInstance.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++) SafeDestroy(cols[i]);
    }

    void Start()
    {
        boardSize = 1.05f;
        currentBoardScale = 0.85f;
        transform.localScale = Vector3.one * currentBoardScale;
        lastButtonTouchTime = Time.time + 1.5f;

        sfxSource = GetComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0.5f;
        GenerateProceduralSFX();

        InitializeMasterURPMaterial();

        tactileInteractions = GetComponent<TycoonTactileInteractions>();
        if (tactileInteractions == null)
        {
            tactileInteractions = gameObject.AddComponent<TycoonTactileInteractions>();
        }

        minigameOrchestrator = GetComponent<TycoonMinigameOrchestrator>();
        if (minigameOrchestrator == null)
        {
            minigameOrchestrator = gameObject.AddComponent<TycoonMinigameOrchestrator>();
        }

        spatialAnchorAndDiorama = GetComponent<TycoonSpatialAnchorAndDiorama>();
        if (spatialAnchorAndDiorama == null)
        {
            spatialAnchorAndDiorama = gameObject.AddComponent<TycoonSpatialAnchorAndDiorama>();
        }

        executiveBriefcaseAndTrophy = GetComponent<TycoonExecutiveBriefcaseAndTrophy>();
        if (executiveBriefcaseAndTrophy == null)
        {
            executiveBriefcaseAndTrophy = gameObject.AddComponent<TycoonExecutiveBriefcaseAndTrophy>();
        }

        colocationAndPolish = GetComponent<TycoonColocationAndPolish>();
        if (colocationAndPolish == null)
        {
            colocationAndPolish = gameObject.AddComponent<TycoonColocationAndPolish>();
        }

        CacheMetaInteractionSDKHands();
        InitializeOfficialMonopolyMap();
        Build40TileClockwiseTable();
        BuildLivingCenterCourtyard();
        Create3DRivalMogulAvatar();
        Create3DDiceAndHighDashboard();
        Create3DDeedCardPopup();
        CreateGrabbable3DDeedCrystal();
        Create3DTouchButtonsOnTable();
        Create3DHolographicStartMenu();
        CreateFloatingVRControls();
        CreatePlayerTokens();
        CreateVRSkyDome();

        monopolyBeamsContainer = new GameObject("Monopoly_Synergy_Beams");
        monopolyBeamsContainer.transform.SetParent(transform, false);

        if (OVRManager.display != null)
        {
            OVRManager.display.RecenteredPose += OnSystemRecenterPose;
        }

        StartCoroutine(LockBoardInFrontOfPlayerOnStart());

        if (PlayerPrefs.GetInt("Tycoon_HasSave", 0) == 1)
        {
            LoadGameState();
        }
        else
        {
            ShowStartMenu();
        }
    }

    void OnDestroy()
    {
        if (OVRManager.display != null)
        {
            OVRManager.display.RecenteredPose -= OnSystemRecenterPose;
        }
    }

    void LateUpdate()
    {
        if (hasLockedInitialPose && !isInPenthouseVR && !isTwoHandManipulating)
        {
            transform.position = lockedBoardPosition;
            transform.rotation = lockedBoardRotation;
            transform.localScale = Vector3.one * currentBoardScale;
        }
    }

    void OnSystemRecenterPose()
    {
        lastButtonTouchTime = Time.time + 1.0f;
        StartCoroutine(DelayedRecenterCoroutine());
    }

    IEnumerator DelayedRecenterCoroutine()
    {
        yield return new WaitForSeconds(0.15f);
        RecenterBoardInFrontOfPlayer();
    }

    IEnumerator LockBoardInFrontOfPlayerOnStart()
    {
        lockedBoardPosition = new Vector3(0f, -0.34f, 0.88f);
        lockedBoardRotation = Quaternion.identity;
        transform.position = lockedBoardPosition;
        transform.rotation = lockedBoardRotation;
        hasLockedInitialPose = true;

        yield return new WaitForSeconds(0.45f);
        EnforceUnderlayPassthrough();
        CacheMetaInteractionSDKHands();
        RecenterBoardInFrontOfPlayer();
    }

    public Color GetPlayerColor(int p)
    {
        switch (p)
        {
            case 0: return new Color(0.18f, 0.85f, 0.45f); // Emerald Green (YOU)
            case 1: return new Color(0.85f, 0.30f, 0.55f); // Crimson Purple (MOGUL ALPHA)
            case 2: return new Color(0.15f, 0.78f, 0.96f); // Cyber Cyan (BARONESS BETA)
            case 3: return new Color(0.95f, 0.65f, 0.12f); // Royal Gold/Amber (TITAN GAMMA)
            default: return Color.white;
        }
    }

    public string GetPlayerName(int p)
    {
        switch (p)
        {
            case 0: return "YOU";
            case 1: return "MOGUL ALPHA";
            case 2: return "BARONESS BETA";
            case 3: return "TITAN GAMMA";
            default: return $"PLAYER {p}";
        }
    }

    void UpdateActivePlayerVisibilities()
    {
        for (int p = 0; p < 4; p++)
        {
            bool isActive = (p < activePlayerCount) && !isBankrupt[p];
            if (p > 0 && rivalAvatarRoots[p] != null)
            {
                rivalAvatarRoots[p].gameObject.SetActive(isActive);
            }
            if (playerTokens[p] != null)
            {
                playerTokens[p].gameObject.SetActive(isActive);
            }
        }
    }

    void Update()
    {
        if (Time.frameCount % 90 == 0) FixMicrogestureEventSources();
        UpdateAmbientCourtyardTrafficAndChopper();
        AnimateRivalMogulAvatar();
        AnimateFloatingDeedCrystal();

        spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        buyPressed = Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame;
        passPressed = Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame;
        bool upgradePressed = Keyboard.current != null && Keyboard.current.uKey.wasPressedThisFrame;
        bool takeoverPressed = Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame;
        bool moodPressed = Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame;
        bool vrViewPressed = Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame;
        bool recenterPressed = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        bool shrinkPressed = Keyboard.current != null && Keyboard.current.minusKey.wasPressedThisFrame;
        bool growPressed = Keyboard.current != null && Keyboard.current.equalsKey.wasPressedThisFrame;
        bool kPressed = Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame;
        bool nPressed = Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame;
        bool lPressed = Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame;
        bool gPressed = Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame;
        bool dPressed = Keyboard.current != null && Keyboard.current.dKey.wasPressedThisFrame;

        if (nPressed || lPressed) ResetMatch();
        if (gPressed)
        {
            float deskHeight = (isRightHandTracked && isLeftHandTracked)
                ? (leftHandWorldPos.y + rightHandWorldPos.y) * 0.5f
                : (isRightHandTracked ? rightHandWorldPos.y : (isLeftHandTracked ? leftHandWorldPos.y : (Camera.main != null ? Camera.main.transform.position.y - 0.42f : lockedBoardPosition.y)));
            SnapBoardToDeskHeight(deskHeight);
        }
        if (recenterPressed) RecenterBoardInFrontOfPlayer();
        if (shrinkPressed) AdjustBoardScale(-0.12f);
        if (growPressed) AdjustBoardScale(0.12f);

        bool rightPinchDown = false;
        bool leftPinchDown = false;
        bool rightPinchHeld = false;
        bool leftPinchHeld = false;
        ReadMetaISDKHands(out rightPinchDown, out leftPinchDown, out rightPinchHeld, out leftPinchHeld);

        if (currentState == TurnState.StartMenu)
        {
            int startBtn = CheckStartMenuButtonPoke();
            bool spaceOrEnter = Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame);
            bool modeKey = Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame;
            bool rKey = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
            bool diffKey = Keyboard.current != null && Keyboard.current.dKey.wasPressedThisFrame;
            bool cashKey = Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame;

            if (startBtn == 0 || modeKey)
            {
                if (currentMatchMode == MatchMultiplayerMode.VsAIMoguls) currentMatchMode = MatchMultiplayerMode.PassAndPlay;
                else if (currentMatchMode == MatchMultiplayerMode.PassAndPlay) currentMatchMode = MatchMultiplayerMode.LocalWiFiColocation;
                else if (currentMatchMode == MatchMultiplayerMode.LocalWiFiColocation) currentMatchMode = MatchMultiplayerMode.OnlineMultiplayer;
                else currentMatchMode = MatchMultiplayerMode.VsAIMoguls;

                UpdateStartMenuUI();
                PlaySFX(tokenMoveSound, procStepClip, 1.25f);
                PlayHaptic(0.2f, 0.1f);
            }
            else if (startBtn == 1 || rKey)
            {
                activePlayerCount++;
                if (activePlayerCount > 4) activePlayerCount = 2;
                UpdateActivePlayerVisibilities();
                UpdateStartMenuUI();
                PlaySFX(tokenMoveSound, procStepClip, 1.2f);
                PlayHaptic(0.2f, 0.1f);
            }
            else if (startBtn == 2 || diffKey)
            {
                if (aiDifficulty == AIDifficulty.Tycoon) aiDifficulty = AIDifficulty.Billionaire;
                else if (aiDifficulty == AIDifficulty.Billionaire) aiDifficulty = AIDifficulty.Casual;
                else aiDifficulty = AIDifficulty.Tycoon;
                UpdateStartMenuUI();
                PlaySFX(tokenMoveSound, procStepClip, 1.3f);
                PlayHaptic(0.2f, 0.1f);
            }
            else if (startBtn == 3 || cashKey)
            {
                if (startingCashConfig == 1500) startingCashConfig = 2500;
                else if (startingCashConfig == 2500) startingCashConfig = 1000;
                else startingCashConfig = 1500;
                UpdateStartMenuUI();
                PlaySFX(procCashClip, procCashClip, 1.1f);
                PlayHaptic(0.2f, 0.1f);
            }
            else if (startBtn == 4 || spaceOrEnter)
            {
                LaunchEmpireMatch();
            }

            return;
        }

        if (isInPenthouseVR)
        {
            KeepVRControlsInFrontOfPlayer();
            if (Time.time > lastButtonTouchTime)
            {
                if (vrExitFloatingButton != null)
                {
                    float dR = isRightHandTracked ? Vector3.Distance(rightHandWorldPos, vrExitFloatingButton.transform.position) : 99f;
                    float dL = isLeftHandTracked ? Vector3.Distance(leftHandWorldPos, vrExitFloatingButton.transform.position) : 99f;
                    if (dR < 0.13f || dL < 0.13f)
                    {
                        lastButtonTouchTime = Time.time + 0.65f;
                        TogglePenthouseScaleShift();
                        return;
                    }
                }

                if (vrRooftopTeleportButton != null)
                {
                    float dR = isRightHandTracked ? Vector3.Distance(rightHandWorldPos, vrRooftopTeleportButton.transform.position) : 99f;
                    float dL = isLeftHandTracked ? Vector3.Distance(leftHandWorldPos, vrRooftopTeleportButton.transform.position) : 99f;
                    if (dR < 0.13f || dL < 0.13f || leftPinchDown)
                    {
                        lastButtonTouchTime = Time.time + 0.65f;
                        ToggleRooftopVantagePoint();
                        return;
                    }
                }
            }

            if (vrViewPressed || rightPinchDown)
            {
                TogglePenthouseScaleShift();
            }
            return;
        }

        if (isTwoHandManipulating) return;

        int touchedButton = CheckPhysicalButtonTouch();
        if (touchedButton == 0) spacePressed = true;
        else if (touchedButton == 1) buyPressed = true;
        else if (touchedButton == 2) passPressed = true;
        else if (touchedButton == 3) upgradePressed = true;
        else if (touchedButton == 4) takeoverPressed = true;
        else if (touchedButton == 5) vrViewPressed = true;
        else if (touchedButton == 6) AdjustBoardScale(-0.12f);
        else if (touchedButton == 7) AdjustBoardScale(0.12f);
        else if (touchedButton == 8) RecenterBoardInFrontOfPlayer();
        else if (touchedButton == 9)
        {
            if (Time.time - newGameArmTime < 4.0f)
            {
                newGameArmTime = 0f;
                ResetMatch();
            }
            else
            {
                newGameArmTime = Time.time;
                RefreshDashboard("CONFIRM RESET", "<color=#FF4444>POKE [NEW GAME] AGAIN TO CONFIRM RESET (4s)</color>");
                PlayHaptic(0.4f, 0.15f);
                PlaySFX(diceRollSound, procDiceClip, 1.1f);
            }
        }

        bool mortgagePoked = CheckMortgageButtonPoke();
        if (kPressed || mortgagePoked)
        {
            if (selectedTileIndex >= 0 && selectedTileIndex < 40)
            {
                ExecuteMortgageOrUnmortgage(board[selectedTileIndex]);
            }
            else if (board[playerPos[0]].owner == 0)
            {
                ExecuteMortgageOrUnmortgage(board[playerPos[0]]);
            }
        }

        bool upgradeCardPoked = CheckUpgradeButtonPoke();
        if (upgradeCardPoked)
        {
            TryBuildChronologically(0);
        }

        bool dealPoked = CheckOfferDealButtonPoke();
        if (dPressed || dealPoked)
        {
            BoardTile target = (selectedTileIndex >= 0 && selectedTileIndex < 40) ? board[selectedTileIndex] : null;
            if (target == null && board[playerPos[0]].owner == 1) target = board[playerPos[0]];
            if (target != null && target.owner == 1)
            {
                ExecuteTradeDeal(target);
            }
            else
            {
                RefreshDashboard("NO AI PROPERTY SELECTED", "Poke an AI-owned tile on the board to propose a trade or buyout!");
            }
        }

        if (touchedButton == -1 && !mortgagePoked && !upgradeCardPoked && !dealPoked)
        {
            HandlePhysicalDiceGrabAndThrow(rightPinchDown, leftPinchDown, rightPinchHeld, leftPinchHeld);
            HandlePhysicalDeedCrystalGrab(rightPinchDown, leftPinchDown, rightPinchHeld, leftPinchHeld);

            if (!isPlayerHoldingDice && !isHoldingDeedCrystal)
            {
                int pokedTile = CheckPhysicalTilePoke();
                if (pokedTile != -1)
                {
                    if (inJail[0] && pokedTile == 10)
                    {
                        TriggerJailLockpickMinigame();
                    }
                    else
                    {
                        selectedTileIndex = pokedTile;
                        ShowDeedCardForTile(board[selectedTileIndex]);
                        PlaySFX(tokenMoveSound, procStepClip, 1.4f);
                        StartCoroutine(AnimateButtonPress(board[selectedTileIndex].tileTransform));
                    }
                }
            }
        }

        int curP = CurrentTurnPlayer;
        if (inJail[curP] && jailLockpickButton != null && jailLockpickButton.activeInHierarchy && !isLockpickingInProgress)
        {
            Vector3 btnPos = jailLockpickButton.transform.position;
            float distLeft = Vector3.Distance(leftHandWorldPos, btnPos);
            float distRight = Vector3.Distance(rightHandWorldPos, btnPos);
            if (distLeft < 0.075f || distRight < 0.075f || (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame))
            {
                TriggerJailLockpickMinigame();
            }
        }

        if (takeoverPressed && (currentState == TurnState.WaitingForRoll || currentState == TurnState.BuyOrPassDecision))
        {
            TryExecuteHostileTakeover();
        }

        if (currentState == TurnState.WaitingForRoll && !isPlayerHoldingDice)
        {
            if (inJail[curP] && (buyPressed || (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)))
            {
                playerCash[curP] -= 50;
                freeParkingJackpot += 50;
                inJail[curP] = false;
                HideJailLockpickButton();
                ShatterJailCage(curP);
                PlaySFX(cashRegisterSound, procCashClip, 1.2f);
                PlayHaptic(0.5f, 0.2f);
                RefreshDashboard("PAID $50 BAIL!", $"Player {curP + 1} paid bail! Laser Cage shattered! Free from Jail! Distance-Grab & Throw the dice!");
                UpdateLiveHUDStats();
            }
            else if (spacePressed)
            {
                StartCoroutine(ExecutePhysicsDiceRollTurn(true));
            }
            else if (upgradePressed)
            {
                TryBuildChronologically(curP);
            }
        }
        else if (currentState == TurnState.BuyOrPassDecision && !isHoldingDeedCrystal)
        {
            if (buyPressed || spacePressed)
            {
                PurchaseCurrentTile(curP);
            }
            else if (passPressed || leftPinchDown)
            {
                BoardTile curTile = board[playerPos[curP]];
                if (curTile != null && curTile.owner == -1 && (curTile.type == TileType.Property || curTile.type == TileType.Railroad || curTile.type == TileType.Utility))
                {
                    StartCoroutine(RunPropertyAuctionCoroutine(curTile));
                }
                else
                {
                    HideDeedCard();
                    RefreshDashboard("PASSED ON DEED", "Passing turn to next Mogul...");
                    StartCoroutine(EndPlayerTurn(false));
                }
            }
            else if (upgradePressed)
            {
                TryBuildChronologically(curP);
            }
        }
        else if (currentState == TurnState.AuctionInProgress)
        {
            if (buyPressed || spacePressed || rightPinchDown)
            {
                auctionPlayerBidFlag = true;
            }
            if (passPressed || leftPinchDown)
            {
                auctionPlayerDropFlag = true;
            }
        }

        if (marketAuraRing != null && marketAuraRing.activeSelf)
        {
            marketAuraRing.transform.Rotate(Vector3.up, 25f * Time.deltaTime);
        }

        if (moodPressed) ToggleMarketAtmosphere();
        if (vrViewPressed) TogglePenthouseScaleShift();

        if (isHudDirty || currentState != lastContextualState)
        {
            isHudDirty = false;
            lastContextualState = currentState;
            UpdateLiveHUDStats();
            UpdateContextualActionButtons();
        }
    }

    void CreateFloatingVRControls()
    {
        vrExitFloatingButton = CreateFloatingVRBtn("VR_Exit_Btn", "EXIT VR VIEW\n(Right Pinch)", new Color(0.92f, 0.20f, 0.25f));
        vrRooftopTeleportButton = CreateFloatingVRBtn("VR_Rooftop_Btn", "ROOFTOP VIEW\n(Left Pinch)", new Color(0.18f, 0.62f, 0.96f));
    }

    GameObject CreateFloatingVRBtn(string objName, string labelText, Color color)
    {
        GameObject btn = GameObject.CreatePrimitive(PrimitiveType.Cube);
        btn.name = objName;
        btn.transform.localScale = new Vector3(0.24f, 0.075f, 0.035f);
        Destroy(btn.GetComponent<Collider>());
        btn.GetComponent<Renderer>().material = CreateColoredMaterial(color, 0.9f);

        GameObject canvasGO = new GameObject("BtnCanvas");
        canvasGO.transform.SetParent(btn.transform, false);
        canvasGO.transform.localPosition = new Vector3(0f, 0f, -0.55f);
        canvasGO.transform.localScale = new Vector3(0.003f, 0.01f, 0.003f);

        Canvas c = canvasGO.AddComponent<Canvas>();
        c.renderMode = RenderMode.WorldSpace;
        canvasGO.GetComponent<RectTransform>().sizeDelta = new Vector2(280f, 85f);

        TextMeshProUGUI tmp = CreateUGUIText(canvasGO.transform, "Label", Vector2.zero, 26, Color.white);
        tmp.text = labelText;
        tmp.rectTransform.sizeDelta = new Vector2(280f, 85f);

        btn.SetActive(false);
        return btn;
    }

    void KeepVRControlsInFrontOfPlayer()
    {
        Transform eye = Camera.main != null ? Camera.main.transform : null;
        if (eye == null) return;
        Vector3 flatFwd = new Vector3(eye.forward.x, 0f, eye.forward.z).normalized;
        if (flatFwd.sqrMagnitude < 0.01f) flatFwd = Vector3.forward;
        Vector3 rightVec = Vector3.Cross(Vector3.up, flatFwd).normalized;

        if (vrExitFloatingButton != null)
        {
            vrExitFloatingButton.transform.position = eye.position + (flatFwd * 0.55f) - (rightVec * 0.15f) + new Vector3(0f, -0.26f, 0f);
            vrExitFloatingButton.transform.rotation = Quaternion.LookRotation(flatFwd);
        }

        if (vrRooftopTeleportButton != null)
        {
            vrRooftopTeleportButton.transform.position = eye.position + (flatFwd * 0.55f) + (rightVec * 0.15f) + new Vector3(0f, -0.26f, 0f);
            vrRooftopTeleportButton.transform.rotation = Quaternion.LookRotation(flatFwd);
        }
    }

    void UpdateVRButtonLabel(GameObject btn, string newText)
    {
        if (btn == null) return;
        TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = newText;
    }

    void CreateVRSkyDome()
    {
        vrSkyDome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        vrSkyDome.name = "VR_Metropolis_SkyDome";
        vrSkyDome.transform.position = transform.position;
        vrSkyDome.transform.localScale = new Vector3(120f, 120f, 120f);
        Destroy(vrSkyDome.GetComponent<Collider>());

        Material skyMat = CreateColoredMaterial(new Color(0.03f, 0.05f, 0.10f), 0.1f);
        if (skyMat.HasProperty("_Cull")) skyMat.SetFloat("_Cull", 1f);
        vrSkyDome.GetComponent<Renderer>().material = skyMat;
        vrSkyDome.SetActive(false);
    }

    GameObject GetTokenPrefabForPlayer(int p)
    {
        if (p == 0)
        {
            if (playerTokenPrefab != null) return playerTokenPrefab;
            if (playerTokenPrefabs != null && playerTokenPrefabs.Count > 0 && playerTokenPrefabs[0] != null) return playerTokenPrefabs[0];
            return null;
        }
        if (p == 1)
        {
            if (aiRivalTokenPrefab != null) return aiRivalTokenPrefab;
            if (playerTokenPrefabs != null && playerTokenPrefabs.Count > 1 && playerTokenPrefabs[1] != null) return playerTokenPrefabs[1];
            return null;
        }
        if (p == 2)
        {
            if (rival2TokenPrefab != null) return rival2TokenPrefab;
            if (playerTokenPrefabs != null && playerTokenPrefabs.Count > 2 && playerTokenPrefabs[2] != null) return playerTokenPrefabs[2];
            if (ambientTrafficCar1Prefab != null) return ambientTrafficCar1Prefab;
            return aiRivalTokenPrefab;
        }
        if (p == 3)
        {
            if (rival3TokenPrefab != null) return rival3TokenPrefab;
            if (playerTokenPrefabs != null && playerTokenPrefabs.Count > 3 && playerTokenPrefabs[3] != null) return playerTokenPrefabs[3];
            if (ambientTrafficCar2Prefab != null) return ambientTrafficCar2Prefab;
            return playerTokenPrefab;
        }
        return null;
    }

    void CreatePlayerTokens()
    {
        playerTokens[0] = SpawnPlayerTokenInstance(0, GetTokenPrefabForPlayer(0), "Player_Vehicle_Token", CreateColoredMaterial(GetPlayerColor(0), 0.9f));
        playerTokens[1] = SpawnPlayerTokenInstance(1, GetTokenPrefabForPlayer(1), "AIRival1_Alpha_Token", CreateColoredMaterial(GetPlayerColor(1), 0.9f));
        playerTokens[2] = SpawnPlayerTokenInstance(2, GetTokenPrefabForPlayer(2), "AIRival2_Beta_Token", CreateColoredMaterial(GetPlayerColor(2), 0.9f));
        playerTokens[3] = SpawnPlayerTokenInstance(3, GetTokenPrefabForPlayer(3), "AIRival3_Gamma_Token", CreateColoredMaterial(GetPlayerColor(3), 0.9f));

        for (int p = 0; p < 4; p++)
        {
            PositionTokenOnTile(p, 0);
        }
        UpdateActivePlayerVisibilities();
    }

    Transform SpawnPlayerTokenInstance(int playerIdx, GameObject customPrefab, string name, Material fallbackMat)
    {
        GameObject holder = new GameObject(name);
        holder.transform.SetParent(transform, false);

        if (customPrefab != null && customPrefab.GetComponent<OVRCameraRig>() == null)
        {
            GameObject model = Instantiate(customPrefab, holder.transform);
            StripRoguePhysicsFromImportedPrefab(model);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            NormalizePrefabSize(model, 0.048f);
        }
        else
        {
            GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cyl.transform.SetParent(holder.transform, false);
            cyl.transform.localScale = new Vector3(0.026f, 0.020f, 0.026f);
            Destroy(cyl.GetComponent<Collider>());
            cyl.GetComponent<Renderer>().material = fallbackMat;
        }
        return holder.transform;
    }

    Quaternion GetClockwiseTravelRotation(int tileIdx)
    {
        int side = tileIdx / 10;
        switch (side)
        {
            case 0: return Quaternion.Euler(0f, -90f, 0f);
            case 1: return Quaternion.Euler(0f, 0f, 0f);
            case 2: return Quaternion.Euler(0f, 90f, 0f);
            default: return Quaternion.Euler(0f, 180f, 0f);
        }
    }

    void PositionTokenOnTile(int playerIdx, int tileIdx)
    {
        if (playerIdx < 0 || playerIdx >= 4 || playerTokens[playerIdx] == null || board[tileIdx] == null || board[tileIdx].tileTransform == null) return;
        Vector3 laneOffset;
        switch (playerIdx)
        {
            case 0: laneOffset = new Vector3(-0.015f, 0.018f, -0.012f); break;
            case 1: laneOffset = new Vector3( 0.015f, 0.018f, -0.012f); break;
            case 2: laneOffset = new Vector3(-0.015f, 0.018f,  0.012f); break;
            case 3: laneOffset = new Vector3( 0.015f, 0.018f,  0.012f); break;
            default: laneOffset = Vector3.up * 0.018f; break;
        }
        playerTokens[playerIdx].localPosition = board[tileIdx].tileTransform.localPosition + laneOffset;
        playerTokens[playerIdx].localRotation = GetClockwiseTravelRotation(tileIdx);
    }

    void CreateGrabbable3DDeedCrystal()
    {
        grabbableDeedCrystal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        grabbableDeedCrystal.name = "Grabbable_3D_Deed_Crystal";
        grabbableDeedCrystal.transform.SetParent(transform, false);
        grabbableDeedCrystal.transform.localPosition = new Vector3(0.24f, 0.24f, -0.18f);
        grabbableDeedCrystal.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
        grabbableDeedCrystal.transform.localScale = Vector3.one * 0.055f;

        BoxCollider crystalBox = grabbableDeedCrystal.GetComponent<BoxCollider>();
        if (crystalBox != null) crystalBox.isTrigger = false;

        Rigidbody crystalRb = grabbableDeedCrystal.GetComponent<Rigidbody>();
        if (crystalRb == null) crystalRb = grabbableDeedCrystal.AddComponent<Rigidbody>();
        crystalRb.isKinematic = true;
        crystalRb.useGravity = false;

        AttachNativeBuildingBlockGrab(grabbableDeedCrystal, crystalRb);

        grabbableDeedCrystal.GetComponent<Renderer>().material = CreateColoredMaterial(new Color(1f, 0.84f, 0.22f), 0.95f, 0.85f);
        grabbableDeedCrystal.SetActive(false);
    }

    void AnimateFloatingDeedCrystal()
    {
        if (grabbableDeedCrystal == null || !grabbableDeedCrystal.activeSelf || isHoldingDeedCrystal) return;
        grabbableDeedCrystal.transform.Rotate(Vector3.up, 65f * Time.deltaTime, Space.World);
        grabbableDeedCrystal.transform.localPosition = new Vector3(0.24f, 0.24f + Mathf.Sin(Time.time * 3f) * 0.015f, -0.18f);
    }

    void Create3DDeedCardPopup()
    {
        deedCardHolder = new GameObject("Executive_Deed_Card_Popup");
        deedCardHolder.transform.SetParent(transform, false);
        deedCardHolder.transform.localPosition = new Vector3(0f, 0.38f, 0.08f);
        deedCardHolder.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);

        GameObject cardBack = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cardBack.name = "Deed_Card_Plate";
        cardBack.transform.SetParent(deedCardHolder.transform, false);
        cardBack.transform.localScale = new Vector3(0.22f, 0.28f, 0.006f);
        Destroy(cardBack.GetComponent<Collider>());
        cardBack.GetComponent<Renderer>().material = CreateColoredMaterial(new Color(0.96f, 0.95f, 0.92f), 0.9f);

        GameObject cardHeader = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cardHeader.name = "Deed_Color_Header";
        cardHeader.transform.SetParent(deedCardHolder.transform, false);
        cardHeader.transform.localPosition = new Vector3(0f, 0.105f, -0.002f);
        cardHeader.transform.localScale = new Vector3(0.205f, 0.055f, 0.008f);
        Destroy(cardHeader.GetComponent<Collider>());
        deedHeaderRenderer = cardHeader.GetComponent<Renderer>();
        deedHeaderRenderer.material = CreateColoredMaterial(new Color(0.14f, 0.30f, 0.92f), 0.85f);

        GameObject canvasGO = new GameObject("Deed_TMP_Canvas");
        canvasGO.transform.SetParent(deedCardHolder.transform, false);
        canvasGO.transform.localPosition = new Vector3(0f, 0f, -0.008f);
        canvasGO.transform.localScale = Vector3.one * 0.00045f;

        Canvas c = canvasGO.AddComponent<Canvas>();
        c.renderMode = RenderMode.WorldSpace;
        canvasGO.GetComponent<RectTransform>().sizeDelta = new Vector2(460f, 580f);

        deedTitleTMP = CreateUGUIText(canvasGO.transform, "DeedTitle", new Vector2(0, 230), 30, Color.white);
        deedBodyTMP = CreateUGUIText(canvasGO.transform, "DeedBody", new Vector2(0, -35), 23, new Color(0.10f, 0.12f, 0.16f));
        deedBodyTMP.rectTransform.sizeDelta = new Vector2(420f, 440f);

        mortgageButtonTransform = CreateMortgageButtonOnDeedCard(deedCardHolder.transform);

        deedCardHolder.SetActive(false);
    }

    Transform CreateMortgageButtonOnDeedCard(Transform parent)
    {
        // 1. Mortgage / Unmortgage Button (Left bottom lip of deed card)
        GameObject btn = GameObject.CreatePrimitive(PrimitiveType.Cube);
        btn.name = "TouchButton_MORTGAGE";
        btn.transform.SetParent(parent, false);
        btn.transform.localPosition = new Vector3(-0.11f, -0.175f, -0.005f);
        btn.transform.localScale = new Vector3(0.18f, 0.038f, 0.014f);

        BoxCollider box = btn.GetComponent<BoxCollider>();
        if (box != null) box.isTrigger = true;

        mortgageBtnRenderer = btn.GetComponent<Renderer>();
        mortgageBtnRenderer.material = CreateColoredMaterial(new Color(0.85f, 0.25f, 0.20f), 0.85f);

        mortgageBtnCanvasGO = new GameObject("BtnLabel_Mortgage");
        mortgageBtnCanvasGO.transform.SetParent(parent, false);
        mortgageBtnCanvasGO.transform.localPosition = new Vector3(-0.11f, -0.175f, -0.015f);
        mortgageBtnCanvasGO.transform.localRotation = Quaternion.identity;
        mortgageBtnCanvasGO.transform.localScale = Vector3.one * 0.00065f;

        Canvas bc = mortgageBtnCanvasGO.AddComponent<Canvas>();
        bc.renderMode = RenderMode.WorldSpace;
        mortgageBtnCanvasGO.GetComponent<RectTransform>().sizeDelta = new Vector2(250f, 55f);

        GameObject txtGO = new GameObject("Label");
        txtGO.transform.SetParent(mortgageBtnCanvasGO.transform, false);
        mortgageBtnText = txtGO.AddComponent<TextMeshProUGUI>();
        mortgageBtnText.text = "[MORTGAGE (K)]";
        mortgageBtnText.fontSize = 22;
        mortgageBtnText.color = Color.white;
        mortgageBtnText.alignment = TextAlignmentOptions.Center;
        mortgageBtnText.rectTransform.sizeDelta = new Vector2(250f, 55f);

        btn.SetActive(false);
        mortgageBtnCanvasGO.SetActive(false);

        // 2. Targeted Upgrade Button [+ BUILD ($Cost)] (Right bottom lip of deed card)
        GameObject btnUp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        btnUp.name = "TouchButton_UPGRADE_DEED";
        btnUp.transform.SetParent(parent, false);
        btnUp.transform.localPosition = new Vector3(0.11f, -0.175f, -0.005f);
        btnUp.transform.localScale = new Vector3(0.18f, 0.038f, 0.014f);

        BoxCollider boxUp = btnUp.GetComponent<BoxCollider>();
        if (boxUp != null) boxUp.isTrigger = true;

        upgradeBtnRenderer = btnUp.GetComponent<Renderer>();
        upgradeBtnRenderer.material = CreateColoredMaterial(new Color(0.18f, 0.65f, 0.35f), 0.85f);

        upgradeBtnCanvasGO = new GameObject("BtnLabel_UpgradeDeed");
        upgradeBtnCanvasGO.transform.SetParent(parent, false);
        upgradeBtnCanvasGO.transform.localPosition = new Vector3(0.11f, -0.175f, -0.015f);
        upgradeBtnCanvasGO.transform.localRotation = Quaternion.identity;
        upgradeBtnCanvasGO.transform.localScale = Vector3.one * 0.00065f;

        Canvas bcUp = upgradeBtnCanvasGO.AddComponent<Canvas>();
        bcUp.renderMode = RenderMode.WorldSpace;
        upgradeBtnCanvasGO.GetComponent<RectTransform>().sizeDelta = new Vector2(250f, 55f);

        GameObject txtUpGO = new GameObject("Label");
        txtUpGO.transform.SetParent(upgradeBtnCanvasGO.transform, false);
        upgradeBtnText = txtUpGO.AddComponent<TextMeshProUGUI>();
        upgradeBtnText.text = "[+ BUILD (U)]";
        upgradeBtnText.fontSize = 22;
        upgradeBtnText.color = Color.white;
        upgradeBtnText.alignment = TextAlignmentOptions.Center;
        upgradeBtnText.rectTransform.sizeDelta = new Vector2(250f, 55f);

        btnUp.SetActive(false);
        upgradeBtnCanvasGO.SetActive(false);
        upgradeCardButtonTransform = btnUp.transform;

        // 3. Negotiated Trade Button [OFFER DEAL (D)] (for AI-owned tiles)
        GameObject btnDeal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        btnDeal.name = "TouchButton_OFFER_DEAL";
        btnDeal.transform.SetParent(parent, false);
        btnDeal.transform.localPosition = new Vector3(0f, -0.175f, -0.005f);
        btnDeal.transform.localScale = new Vector3(0.20f, 0.038f, 0.014f);

        BoxCollider boxDeal = btnDeal.GetComponent<BoxCollider>();
        if (boxDeal != null) boxDeal.isTrigger = true;

        offerDealBtnRenderer = btnDeal.GetComponent<Renderer>();
        offerDealBtnRenderer.material = CreateColoredMaterial(new Color(0.95f, 0.70f, 0.15f), 0.85f);

        offerDealBtnCanvasGO = new GameObject("BtnLabel_OfferDeal");
        offerDealBtnCanvasGO.transform.SetParent(parent, false);
        offerDealBtnCanvasGO.transform.localPosition = new Vector3(0f, -0.175f, -0.015f);
        offerDealBtnCanvasGO.transform.localRotation = Quaternion.identity;
        offerDealBtnCanvasGO.transform.localScale = Vector3.one * 0.00065f;

        Canvas bcDeal = offerDealBtnCanvasGO.AddComponent<Canvas>();
        bcDeal.renderMode = RenderMode.WorldSpace;
        offerDealBtnCanvasGO.GetComponent<RectTransform>().sizeDelta = new Vector2(270f, 55f);

        GameObject txtDealGO = new GameObject("Label");
        txtDealGO.transform.SetParent(offerDealBtnCanvasGO.transform, false);
        offerDealBtnText = txtDealGO.AddComponent<TextMeshProUGUI>();
        offerDealBtnText.text = "[OFFER DEAL (D)]";
        offerDealBtnText.fontSize = 22;
        offerDealBtnText.color = Color.white;
        offerDealBtnText.alignment = TextAlignmentOptions.Center;
        offerDealBtnText.rectTransform.sizeDelta = new Vector2(270f, 55f);

        btnDeal.SetActive(false);
        offerDealBtnCanvasGO.SetActive(false);
        offerDealButtonTransform = btnDeal.transform;

        return btn.transform;
    }

    void ShowDeedCardForTile(BoardTile tile)
    {
        if (deedCardHolder == null || tile == null) return;
        deedCardHolder.SetActive(true);
        deedHeaderRenderer.material = CreateColoredMaterial(GetOfficialColor(tile), 0.85f);
        deedTitleTMP.text = tile.name;

        if (grabbableDeedCrystal != null)
        {
            grabbableDeedCrystal.SetActive(false);
        }

        string ownerLabel = tile.owner == -1 ? "UNOWNED" : (tile.owner == 0 ? "YOU (PLAYER 1)" : "RIVAL MOGUL");
        string mortgageStatus = tile.isMortgaged ? "<color=#FF5252>MORTGAGED (RENT: $0)</color>" : "<color=#69F0AE>ACTIVE</color>";

        if (tile.type == TileType.Property && tile.rentByLevel != null)
        {
            deedBodyTMP.text =
                $"<b>PRICE: ${tile.price}  |  {ownerLabel}</b>\n" +
                $"STATUS: {mortgageStatus}\n" +
                $"BASE RENT: ${tile.rentByLevel[0]}\n" +
                $"1 HOUSE: ${tile.rentByLevel[1]}\n" +
                $"2 HOUSES: ${tile.rentByLevel[2]}\n" +
                $"3 HOUSES: ${tile.rentByLevel[3]}\n" +
                $"4 HOUSES: ${tile.rentByLevel[4]}\n" +
                $"<b>SKYSCRAPER: ${tile.rentByLevel[5]}</b>\n" +
                $"BUILD COST: ${tile.buildCost} | LVL: {tile.buildingLevel}/5";
        }
        else if (tile.type == TileType.Railroad)
        {
            deedBodyTMP.text =
                $"<b>TRANSIT HUB: ${tile.price}  |  {ownerLabel}</b>\n" +
                $"STATUS: {mortgageStatus}\n\n" +
                "1 OWNED: $25\n2 OWNED: $50\n3 OWNED: $100\n4 OWNED: $200";
        }
        else if (tile.type == TileType.Utility)
        {
            deedBodyTMP.text =
                $"<b>UTILITY DEED: ${tile.price}  |  {ownerLabel}</b>\n" +
                $"STATUS: {mortgageStatus}\n\n" +
                "1 UTILITY: 4x DICE ROLL\nBOTH UTILITIES: 10x DICE ROLL";
        }
        else
        {
            deedBodyTMP.text = $"<b>{tile.name}</b>\n\n{tile.type}";
        }

        if (mortgageButtonTransform != null)
        {
            if (tile.owner == 0)
            {
                mortgageButtonTransform.gameObject.SetActive(true);
                if (mortgageBtnCanvasGO != null) mortgageBtnCanvasGO.SetActive(true);

                if (tile.isMortgaged)
                {
                    int unmortgageCost = (int)(tile.price * 0.55f);
                    mortgageBtnText.text = $"[UNMORTGAGE (-${unmortgageCost})]";
                    mortgageBtnRenderer.material = CreateColoredMaterial(new Color(0.18f, 0.65f, 0.35f), 0.85f);
                }
                else
                {
                    if (tile.buildingLevel > 0)
                    {
                        int sellPrice = tile.buildCost / 2;
                        mortgageBtnText.text = $"[SELL BLDG (+${sellPrice})]";
                        mortgageBtnRenderer.material = CreateColoredMaterial(new Color(0.92f, 0.55f, 0.15f), 0.85f);
                    }
                    else
                    {
                        int mortgageVal = tile.price / 2;
                        mortgageBtnText.text = $"[MORTGAGE (+${mortgageVal})]";
                        mortgageBtnRenderer.material = CreateColoredMaterial(new Color(0.85f, 0.25f, 0.20f), 0.85f);
                    }
                }

                if (upgradeCardButtonTransform != null)
                {
                    if (tile.type == TileType.Property && !tile.isMortgaged && tile.buildingLevel < 5)
                    {
                        upgradeCardButtonTransform.gameObject.SetActive(true);
                        if (upgradeBtnCanvasGO != null) upgradeBtnCanvasGO.SetActive(true);
                        int actualCost = (currentMarketCycle == MarketCycle.BearRecession) ? (int)(tile.buildCost * 0.7f) : tile.buildCost;
                        upgradeBtnText.text = $"[+ BUILD (${actualCost})]";
                    }
                    else
                    {
                        upgradeCardButtonTransform.gameObject.SetActive(false);
                        if (upgradeBtnCanvasGO != null) upgradeBtnCanvasGO.SetActive(false);
                    }
                }

                if (offerDealButtonTransform != null) offerDealButtonTransform.gameObject.SetActive(false);
                if (offerDealBtnCanvasGO != null) offerDealBtnCanvasGO.SetActive(false);
            }
            else if (tile.owner > 0 && tile.owner < activePlayerCount)
            {
                mortgageButtonTransform.gameObject.SetActive(false);
                if (mortgageBtnCanvasGO != null) mortgageBtnCanvasGO.SetActive(false);
                if (upgradeCardButtonTransform != null) upgradeCardButtonTransform.gameObject.SetActive(false);
                if (upgradeBtnCanvasGO != null) upgradeBtnCanvasGO.SetActive(false);

                if (offerDealButtonTransform != null)
                {
                    offerDealButtonTransform.gameObject.SetActive(true);
                    if (offerDealBtnCanvasGO != null) offerDealBtnCanvasGO.SetActive(true);
                }
            }
            else
            {
                mortgageButtonTransform.gameObject.SetActive(false);
                if (mortgageBtnCanvasGO != null) mortgageBtnCanvasGO.SetActive(false);
                if (upgradeCardButtonTransform != null) upgradeCardButtonTransform.gameObject.SetActive(false);
                if (upgradeBtnCanvasGO != null) upgradeBtnCanvasGO.SetActive(false);
                if (offerDealButtonTransform != null) offerDealButtonTransform.gameObject.SetActive(false);
                if (offerDealBtnCanvasGO != null) offerDealBtnCanvasGO.SetActive(false);
            }
        }
    }

    void ShowSpecialEventCard(string headerTitle, string cardDescription, Color headerColor)
    {
        if (deedCardHolder == null) return;
        deedCardHolder.SetActive(true);
        deedHeaderRenderer.material = CreateColoredMaterial(headerColor, 0.9f);
        deedTitleTMP.text = headerTitle;
        deedBodyTMP.text = $"<b>EXECUTIVE DIRECTIVE</b>\n\n{cardDescription}";
        if (mortgageButtonTransform != null) mortgageButtonTransform.gameObject.SetActive(false);
        if (mortgageBtnCanvasGO != null) mortgageBtnCanvasGO.SetActive(false);
        if (upgradeCardButtonTransform != null) upgradeCardButtonTransform.gameObject.SetActive(false);
        if (upgradeBtnCanvasGO != null) upgradeBtnCanvasGO.SetActive(false);
        if (offerDealButtonTransform != null) offerDealButtonTransform.gameObject.SetActive(false);
        if (offerDealBtnCanvasGO != null) offerDealBtnCanvasGO.SetActive(false);
    }

    void HideDeedCard()
    {
        if (deedCardHolder != null) deedCardHolder.SetActive(false);
        if (grabbableDeedCrystal != null) grabbableDeedCrystal.SetActive(false);
        if (mortgageButtonTransform != null) mortgageButtonTransform.gameObject.SetActive(false);
        if (mortgageBtnCanvasGO != null) mortgageBtnCanvasGO.SetActive(false);
        if (upgradeCardButtonTransform != null) upgradeCardButtonTransform.gameObject.SetActive(false);
        if (upgradeBtnCanvasGO != null) upgradeBtnCanvasGO.SetActive(false);
        if (offerDealButtonTransform != null) offerDealButtonTransform.gameObject.SetActive(false);
        if (offerDealBtnCanvasGO != null) offerDealBtnCanvasGO.SetActive(false);
        selectedTileIndex = -1;
    }

    Quaternion GetFlatDieRotationForValue(int faceValue)
    {
        switch (faceValue)
        {
            case 1: return Quaternion.Euler(-90f, 0f, 0f);
            case 2: return Quaternion.Euler(0f, 0f, 0f);
            case 3: return Quaternion.Euler(0f, 0f, -90f);
            case 4: return Quaternion.Euler(0f, 0f, 90f);
            case 5: return Quaternion.Euler(180f, 0f, 0f);
            default: return Quaternion.Euler(90f, 0f, 0f); // 6
        }
    }

    Transform CreateExact3DPipsDie(string name, Vector3 localPos, out PhysicalThrowDie physDieComp)
    {
        GameObject dieRoot = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dieRoot.name = name;
        dieRoot.transform.SetParent(transform, false);
        dieRoot.transform.localPosition = localPos;
        dieRoot.transform.localScale = Vector3.one * 0.045f;

        if (dicePrefab != null)
        {
            Renderer rootRend = dieRoot.GetComponent<Renderer>();
            if (rootRend != null) rootRend.enabled = false;

            GameObject visualDie = Instantiate(dicePrefab, dieRoot.transform);
            visualDie.name = "Dice_Visual";
            visualDie.transform.localPosition = Vector3.zero;
            visualDie.transform.localRotation = Quaternion.identity;
            StripRoguePhysicsFromImportedPrefab(visualDie);
            NormalizePrefabSize(visualDie, 0.045f);
        }
        else
        {
            Material ivoryMat = CreateColoredMaterial(new Color(0.97f, 0.96f, 0.92f), 0.85f);
            Material pipMat = CreateColoredMaterial(new Color(0.06f, 0.06f, 0.08f), 0.9f);
            dieRoot.GetComponent<Renderer>().material = ivoryMat;

            float o = 0.505f;
            float s = 0.25f;

            AddPip(dieRoot.transform, pipMat, new Vector3(0, 0, o));

            AddPip(dieRoot.transform, pipMat, new Vector3(-s, o, s));
            AddPip(dieRoot.transform, pipMat, new Vector3(s, o, -s));

            AddPip(dieRoot.transform, pipMat, new Vector3(o, 0, 0));
            AddPip(dieRoot.transform, pipMat, new Vector3(o, s, s));
            AddPip(dieRoot.transform, pipMat, new Vector3(o, -s, -s));

            AddPip(dieRoot.transform, pipMat, new Vector3(-o, -s, -s));
            AddPip(dieRoot.transform, pipMat, new Vector3(-o, -s, s));
            AddPip(dieRoot.transform, pipMat, new Vector3(-o, s, -s));
            AddPip(dieRoot.transform, pipMat, new Vector3(-o, s, s));

            AddPip(dieRoot.transform, pipMat, new Vector3(0, -o, 0));
            AddPip(dieRoot.transform, pipMat, new Vector3(-s, -o, -s));
            AddPip(dieRoot.transform, pipMat, new Vector3(-s, -o, s));
            AddPip(dieRoot.transform, pipMat, new Vector3(s, -o, -s));
            AddPip(dieRoot.transform, pipMat, new Vector3(s, -o, s));

            AddPip(dieRoot.transform, pipMat, new Vector3(-s, -s, -o));
            AddPip(dieRoot.transform, pipMat, new Vector3(-s, 0, -o));
            AddPip(dieRoot.transform, pipMat, new Vector3(-s, s, -o));
            AddPip(dieRoot.transform, pipMat, new Vector3(s, -s, -o));
            AddPip(dieRoot.transform, pipMat, new Vector3(s, 0, -o));
            AddPip(dieRoot.transform, pipMat, new Vector3(s, s, -o));
        }

        physDieComp = dieRoot.AddComponent<PhysicalThrowDie>();
        physDieComp.OnDieBounceImpact += (impactStrength) =>
        {
            PlaySFX(diceRollSound, procDiceClip, Random.Range(0.92f, 1.18f));
        };
        return dieRoot.transform;
    }

    void AddPip(Transform dieParent, Material pipMat, Vector3 localPos)
    {
        GameObject pip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pip.name = "Pip";
        pip.transform.SetParent(dieParent, false);
        pip.transform.localPosition = localPos;
        pip.transform.localScale = Vector3.one * 0.16f;
        Destroy(pip.GetComponent<Collider>());
        pip.GetComponent<Renderer>().material = pipMat;
    }

    void Create3DDiceAndHighDashboard()
    {
        die1Transform = CreateExact3DPipsDie("Die_1", new Vector3(-0.055f, 0.052f, -0.05f), out physDie1);
        die2Transform = CreateExact3DPipsDie("Die_2", new Vector3(0.055f, 0.052f, -0.05f), out physDie2);
        die1Transform.localRotation = GetFlatDieRotationForValue(6);
        die2Transform.localRotation = GetFlatDieRotationForValue(6);

        AttachNativeBuildingBlockGrab(die1Transform.gameObject, die1Transform.GetComponent<Rigidbody>());
        AttachNativeBuildingBlockGrab(die2Transform.gameObject, die2Transform.GetComponent<Rigidbody>());
        if (physDie1 != null) physDie1.InjectMetaGrabbable(die1Transform.GetComponent<Grabbable>());
        if (physDie2 != null) physDie2.InjectMetaGrabbable(die2Transform.GetComponent<Grabbable>());

        GameObject dashHolder = new GameObject("Executive_Holo_Dashboard");
        dashHolder.transform.SetParent(transform, false);
        dashHolder.transform.localPosition = new Vector3(0f, 0.58f, 0.20f);
        dashHolder.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);

        GameObject bgBoard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bgBoard.name = "Dashboard_Glass_Plate";
        bgBoard.transform.SetParent(dashHolder.transform, false);
        bgBoard.transform.localScale = new Vector3(0.74f, 0.30f, 0.01f);
        Destroy(bgBoard.GetComponent<Collider>());
        bgBoard.GetComponent<Renderer>().material = CreateColoredMaterial(new Color(0.05f, 0.07f, 0.11f), 0.9f);

        GameObject canvasGO = new GameObject("Dashboard_TMP_Canvas");
        canvasGO.transform.SetParent(dashHolder.transform, false);
        canvasGO.transform.localPosition = new Vector3(0f, 0f, -0.012f);
        canvasGO.transform.localScale = Vector3.one * 0.00065f;

        Canvas c = canvasGO.AddComponent<Canvas>();
        c.renderMode = RenderMode.WorldSpace;
        canvasGO.GetComponent<RectTransform>().sizeDelta = new Vector2(1040f, 400f);

        dashboardStatsTMP = CreateUGUIText(canvasGO.transform, "LiveStats", new Vector2(0, 145), 20, new Color(0.35f, 0.95f, 0.60f));
        dashboardStatsTMP.rectTransform.sizeDelta = new Vector2(980f, 55f);
        dashboardStatsTMP.textWrappingMode = TextWrappingModes.Normal;

        dashboardTitleTMP = CreateUGUIText(canvasGO.transform, "Title", new Vector2(0, 82), 28, new Color(1f, 0.84f, 0.32f));
        dashboardTitleTMP.rectTransform.sizeDelta = new Vector2(980f, 55f);
        dashboardTitleTMP.textWrappingMode = TextWrappingModes.Normal;

        dashboardBodyTMP = CreateUGUIText(canvasGO.transform, "Body", new Vector2(0, 8), 25, Color.white);
        dashboardBodyTMP.rectTransform.sizeDelta = new Vector2(960f, 75f);
        dashboardBodyTMP.textWrappingMode = TextWrappingModes.Normal;

        dashboardPromptTMP = CreateUGUIText(canvasGO.transform, "Prompt", new Vector2(0, -85), 21, new Color(0.45f, 0.90f, 1f));
        dashboardPromptTMP.rectTransform.sizeDelta = new Vector2(960f, 65f);
        dashboardPromptTMP.textWrappingMode = TextWrappingModes.Normal;

        UpdateLiveHUDStats();
    }

    TextMeshProUGUI CreateUGUIText(Transform canvasParent, string name, Vector2 anchoredPos, float fontSize, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(canvasParent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        RectTransform rt = tmp.rectTransform;
        rt.sizeDelta = new Vector2(940f, 95f);
        rt.anchoredPosition = anchoredPos;
        return tmp;
    }

    void Create3DTouchButtonsOnTable()
    {
        string[] labels = { "ROLL DICE", "BUY DEED", "PASS TURN", "BUILD (U)", "TAKEOVER", "VR VIEW" };
        Color[] colors = {
            new Color(0.12f, 0.78f, 0.40f),
            new Color(0.95f, 0.74f, 0.15f),
            new Color(0.88f, 0.22f, 0.24f),
            new Color(0.18f, 0.62f, 0.96f),
            new Color(0.95f, 0.45f, 0.10f),
            new Color(0.68f, 0.28f, 0.94f)
        };

        float spacing = 0.168f;
        float startX = -spacing * 2.5f;
        float frontZ = -(boardSize * 0.5f) - 0.045f;

        for (int i = 0; i < 6; i++)
        {
            buttonTransforms[i] = CreateSingle3DButton(
                labels[i],
                colors[i],
                new Vector3(startX + i * spacing, 0.035f, frontZ),
                new Vector3(0.15f, 0.032f, 0.072f)
            );
        }

        string[] utilLabels = { "- SHRINK", "+ GROW", "RECENTER", "NEW GAME" };
        Color[] utilColors = {
            new Color(0.28f, 0.32f, 0.40f),
            new Color(0.28f, 0.32f, 0.40f),
            new Color(0.85f, 0.52f, 0.15f),
            new Color(0.85f, 0.22f, 0.25f)
        };

        float utilSpacing = 0.185f;
        float startUtilX = -utilSpacing * 1.5f;

        for (int j = 0; j < 4; j++)
        {
            buttonTransforms[6 + j] = CreateSingle3DButton(
                utilLabels[j],
                utilColors[j],
                new Vector3(startUtilX + j * utilSpacing, 0.020f, frontZ - 0.075f),
                new Vector3(0.15f, 0.025f, 0.055f)
            );
        }
    }

    Transform CreateSingle3DButton(string label, Color color, Vector3 localPos, Vector3 localScale)
    {
        GameObject btn = GameObject.CreatePrimitive(PrimitiveType.Cube);
        btn.name = $"TouchButton_{label}";
        btn.transform.SetParent(transform, false);
        btn.transform.localPosition = localPos;
        btn.transform.localScale = localScale;

        BoxCollider box = btn.GetComponent<BoxCollider>();
        if (box != null) box.isTrigger = true;

        btn.GetComponent<Renderer>().material = CreateColoredMaterial(color, 0.85f);

        GameObject btnCanvasGO = new GameObject($"BtnLabel_{label}");
        btnCanvasGO.transform.SetParent(btn.transform, false);
        btnCanvasGO.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        btnCanvasGO.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        btnCanvasGO.transform.localScale = new Vector3(0.0035f, 0.008f, 0.0035f);

        Canvas bc = btnCanvasGO.AddComponent<Canvas>();
        bc.renderMode = RenderMode.WorldSpace;
        btnCanvasGO.GetComponent<RectTransform>().sizeDelta = new Vector2(260f, 80f);

        GameObject txtGO = new GameObject("Label");
        txtGO.transform.SetParent(btnCanvasGO.transform, false);
        TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 30;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.rectTransform.sizeDelta = new Vector2(260f, 80f);

        return btn.transform;
    }

    IEnumerator AnimateButtonPress(Transform btn)
    {
        Vector3 origPos = btn.localPosition;
        btn.localPosition = origPos - new Vector3(0f, 0.012f, 0f);
        yield return new WaitForSeconds(0.18f);
        btn.localPosition = origPos;
    }

    void Create3DHolographicStartMenu()
    {
        if (startMenuHolder != null) SafeDestroy(startMenuHolder);

        startMenuHolder = new GameObject("Executive_Start_Menu");
        startMenuHolder.transform.SetParent(transform, false);
        startMenuHolder.transform.localPosition = new Vector3(0f, 0.08f, -(boardSize * 0.5f) - 0.08f);
        startMenuHolder.transform.localRotation = Quaternion.Euler(38f, 0f, 0f);

        GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bg.name = "StartMenu_Backdrop";
        bg.transform.SetParent(startMenuHolder.transform, false);
        bg.transform.localPosition = Vector3.zero;
        bg.transform.localRotation = Quaternion.identity;
        bg.transform.localScale = new Vector3(0.38f, 0.28f, 0.010f);
        SafeDestroy(bg.GetComponent<Collider>());
        bg.GetComponent<Renderer>().material = CreateColoredMaterial(new Color(0.04f, 0.07f, 0.14f), 0.92f);

        GameObject border = GameObject.CreatePrimitive(PrimitiveType.Cube);
        border.name = "StartMenu_Border";
        border.transform.SetParent(startMenuHolder.transform, false);
        border.transform.localPosition = new Vector3(0f, 0f, 0.002f);
        border.transform.localRotation = Quaternion.identity;
        border.transform.localScale = new Vector3(0.386f, 0.286f, 0.008f);
        SafeDestroy(border.GetComponent<Collider>());
        border.GetComponent<Renderer>().material = CreateColoredMaterial(new Color(0.18f, 0.85f, 0.45f), 0.45f);

        GameObject headerCanvasGO = new GameObject("StartMenu_HeaderCanvas");
        headerCanvasGO.transform.SetParent(startMenuHolder.transform, false);
        headerCanvasGO.transform.localPosition = new Vector3(0f, 0.108f, -0.015f);
        headerCanvasGO.transform.localRotation = Quaternion.identity;
        headerCanvasGO.transform.localScale = Vector3.one * 0.00045f;
        Canvas hc = headerCanvasGO.AddComponent<Canvas>();
        hc.renderMode = RenderMode.WorldSpace;
        RectTransform hrt = headerCanvasGO.GetComponent<RectTransform>();
        hrt.sizeDelta = new Vector2(760f, 100f);

        GameObject headerTxtGO = new GameObject("HeaderTitle");
        headerTxtGO.transform.SetParent(headerCanvasGO.transform, false);
        TextMeshProUGUI headerTMP = headerTxtGO.AddComponent<TextMeshProUGUI>();
        headerTMP.text = "<color=#FFD700><b>TYCOON METROPOLIS</b></color>\n<size=20><color=#66DDFF>EXECUTIVE MATCH SETUP</color></size>";
        headerTMP.alignment = TextAlignmentOptions.Center;
        headerTMP.rectTransform.sizeDelta = new Vector2(760f, 100f);

        float[] yPositions = { 0.062f, 0.022f, -0.018f, -0.058f, -0.102f };
        Color[] btnColors = {
            new Color(0.12f, 0.65f, 0.85f), // Cyan Blue (Mode)
            new Color(0.18f, 0.45f, 0.85f), // Slate Blue (Rivals)
            new Color(0.85f, 0.55f, 0.15f), // Royal Amber (Difficulty)
            new Color(0.55f, 0.25f, 0.85f), // Purple (Cash)
            new Color(0.18f, 0.85f, 0.45f)  // Emerald Green (Launch)
        };

        for (int i = 0; i < 5; i++)
        {
            GameObject btnCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            btnCube.name = $"StartMenu_Btn_{i}";
            btnCube.transform.SetParent(startMenuHolder.transform, false);
            btnCube.transform.localPosition = new Vector3(0f, yPositions[i], -0.010f);
            btnCube.transform.localScale = new Vector3(0.34f, 0.034f, 0.015f);

            BoxCollider col = btnCube.GetComponent<BoxCollider>();
            if (col != null) col.isTrigger = true;
            btnCube.GetComponent<Renderer>().material = CreateColoredMaterial(btnColors[i], 0.90f);
            startMenuButtonTransforms[i] = btnCube.transform;

            GameObject btnCanvasGO = new GameObject($"StartMenu_Btn_Canvas_{i}");
            btnCanvasGO.transform.SetParent(startMenuHolder.transform, false);
            btnCanvasGO.transform.localPosition = new Vector3(0f, yPositions[i], -0.020f);
            btnCanvasGO.transform.localRotation = Quaternion.identity;
            btnCanvasGO.transform.localScale = Vector3.one * 0.00045f;

            Canvas bc = btnCanvasGO.AddComponent<Canvas>();
            bc.renderMode = RenderMode.WorldSpace;
            RectTransform brt = btnCanvasGO.GetComponent<RectTransform>();
            brt.sizeDelta = new Vector2(680f, 65f);

            GameObject txtGO = new GameObject("Text");
            txtGO.transform.SetParent(btnCanvasGO.transform, false);
            TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 24;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(680f, 65f);
            startMenuButtonTexts[i] = tmp;
        }

        UpdateStartMenuUI();
        startMenuHolder.SetActive(false);
    }

    void UpdateStartMenuUI()
    {
        if (startMenuButtonTexts[0] != null)
        {
            switch (currentMatchMode)
            {
                case MatchMultiplayerMode.VsAIMoguls:
                    startMenuButtonTexts[0].text = "MODE: SOLO VS AI MOGULS";
                    break;
                case MatchMultiplayerMode.PassAndPlay:
                    startMenuButtonTexts[0].text = "MODE: PASS-AND-PLAY (1 HEADSET)";
                    break;
                case MatchMultiplayerMode.LocalWiFiColocation:
                    startMenuButtonTexts[0].text = "MODE: LOCAL WIFI (PHASE 7 NGO)";
                    break;
                case MatchMultiplayerMode.OnlineMultiplayer:
                    startMenuButtonTexts[0].text = "MODE: ONLINE (PHASE 7 NGO)";
                    break;
            }
        }

        if (startMenuButtonTexts[1] != null)
        {
            if (activePlayerCount == 2)
                startMenuButtonTexts[1].text = "RIVALS: 1 AI (1v1 DUEL)";
            else if (activePlayerCount == 3)
                startMenuButtonTexts[1].text = "RIVALS: 2 AI (3-PLAYER MATCH)";
            else
                startMenuButtonTexts[1].text = "RIVALS: 3 AI (4-PLAYER FULL TABLE)";
        }

        if (startMenuButtonTexts[2] != null)
        {
            if (aiDifficulty == AIDifficulty.Tycoon)
                startMenuButtonTexts[2].text = "DIFFICULTY: TYCOON (NORMAL)";
            else if (aiDifficulty == AIDifficulty.Billionaire)
                startMenuButtonTexts[2].text = "DIFFICULTY: BILLIONAIRE (HARD)";
            else
                startMenuButtonTexts[2].text = "DIFFICULTY: CASUAL (EASY)";
        }

        if (startMenuButtonTexts[3] != null)
        {
            if (startingCashConfig == 1500)
                startMenuButtonTexts[3].text = "START CASH: $1500 (STANDARD)";
            else if (startingCashConfig == 2500)
                startMenuButtonTexts[3].text = "START CASH: $2500 (EXECUTIVE)";
            else
                startMenuButtonTexts[3].text = "START CASH: $1000 (HARDCORE)";
        }

        if (startMenuButtonTexts[4] != null)
        {
            startMenuButtonTexts[4].text = "LAUNCH EMPIRE MATCH";
        }
    }

    public void ShowStartMenu()
    {
        currentState = TurnState.StartMenu;
        if (startMenuHolder != null)
        {
            startMenuHolder.SetActive(true);
        }
        UpdateActivePlayerVisibilities();
        UpdateStartMenuUI();
        UpdateContextualActionButtons();
        RefreshDashboard("EXECUTIVE START MENU", "Configure Mode, Rivals, Difficulty, & Starting Capital, then poke [LAUNCH]!");
        SetRivalReaction(1, AvatarMood.Thinking, "Configure the boardroom match! Will you dare face 3 AI Moguls?");
    }

    public void LaunchEmpireMatch()
    {
        if (currentMatchMode == MatchMultiplayerMode.LocalWiFiColocation || currentMatchMode == MatchMultiplayerMode.OnlineMultiplayer)
        {
            RefreshDashboard("MULTI-HEADSET MODE REQUIRES PHASE 7", "Please select SOLO VS AI or PASS-AND-PLAY for current standalone build.");
            return;
        }

        CurrentTurnPlayer = 0;
        if (currentMatchMode == MatchMultiplayerMode.PassAndPlay)
        {
            isPassAndPlayMode = true;
            if (spatialAnchorAndDiorama != null) spatialAnchorAndDiorama.RotateBoardToSeat(0);
        }
        else
        {
            isPassAndPlayMode = false;
        }

        for (int i = 0; i < 4; i++)
        {
            playerCash[i] = startingCashConfig;
            playerPos[i] = 0;
            isBankrupt[i] = false;
            inJail[i] = false;
            if (jailCageHolders[i] != null)
            {
                SafeDestroy(jailCageHolders[i]);
                jailCageHolders[i] = null;
            }
            PositionTokenOnTile(i, 0);
        }

        UpdateActivePlayerVisibilities();
        if (startMenuHolder != null)
        {
            startMenuHolder.SetActive(false);
        }

        PlaySFX(cashRegisterSound, procCashClip, 1.25f);
        SpawnCelebrationFireworks(transform.position + Vector3.up * 0.35f);

        currentState = TurnState.WaitingForRoll;
        CurrentTurnPlayer = 0;
        OnTurnStarted?.Invoke(0);
        UpdateLiveHUDStats();
        UpdateContextualActionButtons();
        RefreshDashboard(
            "MATCH LAUNCHED — YOUR TURN!",
            "Point & Pinch to Magnet-Pull the 3D Dice into your palm, shake, and release to Throw!"
        );
        SetRivalReaction(1, AvatarMood.HappyCheer, $"Let the games begin! ${startingCashConfig} starting capital.");
        SaveGameState();
    }

    public void ShowJailLockpickButton()
    {
        if (jailLockpickButton == null)
        {
            if (board != null && board.Length > 10 && board[10] != null && board[10].tileTransform != null)
            {
                jailLockpickButton = new GameObject("Jail_Lockpick_Button");
                jailLockpickButton.transform.SetParent(board[10].tileTransform, false);
                jailLockpickButton.transform.localPosition = new Vector3(0f, 0.09f, 0f);
                jailLockpickButton.transform.localRotation = Quaternion.identity;

                GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                plate.name = "ButtonPlate";
                plate.transform.SetParent(jailLockpickButton.transform, false);
                plate.transform.localScale = new Vector3(0.065f, 0.008f, 0.065f);
                Destroy(plate.GetComponent<Collider>());
                plate.GetComponent<Renderer>().material = CreateColoredMaterial(new Color(1f, 0.82f, 0.15f), 0.95f, 0.1f);

                GameObject textGO = new GameObject("ButtonText");
                textGO.transform.SetParent(jailLockpickButton.transform, false);
                textGO.transform.localPosition = new Vector3(0f, 0.006f, 0f);
                textGO.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                textGO.transform.localScale = Vector3.one * 0.00045f;

                Canvas c = textGO.AddComponent<Canvas>();
                c.renderMode = RenderMode.WorldSpace;
                TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
                tmp.text = "<b>PICK LOCK</b>\n<size=18>(FREE)</size>";
                tmp.fontSize = 24;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.black;
            }
        }
        if (jailLockpickButton != null) jailLockpickButton.SetActive(true);
    }

    public void HideJailLockpickButton()
    {
        if (jailLockpickButton != null) jailLockpickButton.SetActive(false);
    }

    public void TriggerJailLockpickMinigame()
    {
        if (isLockpickingInProgress || !inJail[0] || minigameOrchestrator == null) return;
        isLockpickingInProgress = true;
        HideJailLockpickButton();
        RefreshDashboard("MECHANICAL LOCKPICK INITIATED!", "Poke 3 Tumbler Pins at the green shear line to escape!");
        minigameOrchestrator.RunJailLockpickMinigame((escaped) =>
        {
            isLockpickingInProgress = false;
            if (escaped)
            {
                inJail[0] = false;
                ShatterJailCage(0);
                PlaySFX(cashRegisterSound, procCashClip, 1.3f);
                RefreshDashboard("LOCKPICK SUCCESSFUL! ESCAPED!", "You cracked the mechanical padlock and shattered the Laser Cage! Free to roll!");
            }
            else
            {
                RefreshDashboard("LOCKPICK FAILED!", "Timer expired before all pins locked! Pay $50 bail or roll for doubles.");
                if (inJail[0]) ShowJailLockpickButton();
            }
            UpdateLiveHUDStats();
        });
    }

    public void TriggerPhysicsDiceRoll(bool useImpulseToss)
    {
        StartCoroutine(ExecutePhysicsDiceRollTurn(useImpulseToss));
    }

    public void SaveGameState()
    {
        PlayerPrefs.SetInt("Tycoon_HasSave", 1);
        PlayerPrefs.SetInt("Tycoon_ActivePlayers", activePlayerCount);
        PlayerPrefs.SetInt("Tycoon_AIDifficulty", (int)aiDifficulty);
        PlayerPrefs.SetInt("Tycoon_StartCashConfig", startingCashConfig);

        for (int p = 0; p < 4; p++)
        {
            PlayerPrefs.SetInt($"Tycoon_Cash_P{p}", playerCash[p]);
            PlayerPrefs.SetInt($"Tycoon_Pos_P{p}", playerPos[p]);
            PlayerPrefs.SetInt($"Tycoon_Bankrupt_P{p}", isBankrupt[p] ? 1 : 0);
            PlayerPrefs.SetInt($"Tycoon_InJail_P{p}", inJail[p] ? 1 : 0);
        }

        PlayerPrefs.SetInt("Tycoon_Jackpot", freeParkingJackpot);
        PlayerPrefs.SetInt("Tycoon_MarketCycle", (int)currentMarketCycle);
        PlayerPrefs.SetInt("Tycoon_TotalTurns", totalTurnsCompleted);
        PlayerPrefs.SetFloat("Tycoon_BoardScale", currentBoardScale);

        for (int i = 0; i < 40; i++)
        {
            if (board[i] != null)
            {
                PlayerPrefs.SetInt($"Tycoon_Tile_{i}_Owner", board[i].owner);
                PlayerPrefs.SetInt($"Tycoon_Tile_{i}_Lvl", board[i].buildingLevel);
                PlayerPrefs.SetInt($"Tycoon_Tile_{i}_Mortgaged", board[i].isMortgaged ? 1 : 0);
            }
        }
        PlayerPrefs.Save();
    }

    public void LoadGameState()
    {
        if (PlayerPrefs.GetInt("Tycoon_HasSave", 0) != 1) return;

        activePlayerCount = PlayerPrefs.GetInt("Tycoon_ActivePlayers", 4);
        aiDifficulty = (AIDifficulty)PlayerPrefs.GetInt("Tycoon_AIDifficulty", 0);
        startingCashConfig = PlayerPrefs.GetInt("Tycoon_StartCashConfig", 1500);

        for (int p = 0; p < 4; p++)
        {
            playerCash[p] = PlayerPrefs.GetInt($"Tycoon_Cash_P{p}", startingCashConfig);
            playerPos[p] = PlayerPrefs.GetInt($"Tycoon_Pos_P{p}", 0);
            isBankrupt[p] = (PlayerPrefs.GetInt($"Tycoon_Bankrupt_P{p}", 0) == 1);
            inJail[p] = (PlayerPrefs.GetInt($"Tycoon_InJail_P{p}", 0) == 1);
            PositionTokenOnTile(p, playerPos[p]);
            if (inJail[p])
            {
                SpawnJailLaserCage(p);
            }
        }

        UpdateActivePlayerVisibilities();

        if (startMenuHolder != null)
        {
            startMenuHolder.SetActive(false);
        }

        freeParkingJackpot = PlayerPrefs.GetInt("Tycoon_Jackpot", 100);
        currentMarketCycle = (MarketCycle)PlayerPrefs.GetInt("Tycoon_MarketCycle", 0);
        totalTurnsCompleted = PlayerPrefs.GetInt("Tycoon_TotalTurns", 0);
        currentBoardScale = PlayerPrefs.GetFloat("Tycoon_BoardScale", 0.85f);
        transform.localScale = Vector3.one * currentBoardScale;

        for (int i = 0; i < 40; i++)
        {
            if (board[i] != null)
            {
                board[i].owner = PlayerPrefs.GetInt($"Tycoon_Tile_{i}_Owner", -1);
                board[i].buildingLevel = PlayerPrefs.GetInt($"Tycoon_Tile_{i}_Lvl", 0);
                board[i].isMortgaged = (PlayerPrefs.GetInt($"Tycoon_Tile_{i}_Mortgaged", 0) == 1);

                if (board[i].owner != -1)
                {
                    ApplyTileOwnershipVisuals(board[i], board[i].owner);
                }
            }
        }

        if (marketAuraRing != null)
        {
            marketAuraRing.SetActive(currentMarketCycle == MarketCycle.BullBoom);
        }

        RebuildMonopolySynergyBeams();
        UpdateLiveHUDStats();
        if (inJail[0])
        {
            RefreshDashboard("IN JAIL!", "IN JAIL! POKE [BUY DEED] TO PAY $50 BAIL NOW, OR THROW DICE FOR DOUBLES!");
        }
        else
        {
            RefreshDashboard("SAVED EMPIRE LOADED", "Restored your metropolis, net worth, and active market cycle!");
        }
    }

    public static void ClearMatchSave()
    {
        PlayerPrefs.DeleteKey("Tycoon_HasSave");
        PlayerPrefs.DeleteKey("Tycoon_ActivePlayers");
        PlayerPrefs.DeleteKey("Tycoon_ActivePlayerCount");
        PlayerPrefs.DeleteKey("Tycoon_AIDifficulty");
        PlayerPrefs.DeleteKey("Tycoon_StartCashConfig");
        PlayerPrefs.DeleteKey("Tycoon_CurrentTurn");
        PlayerPrefs.DeleteKey("Tycoon_TurnPlayer");

        for (int p = 0; p < 4; p++)
        {
            PlayerPrefs.DeleteKey($"Tycoon_Cash_P{p}");
            PlayerPrefs.DeleteKey($"Tycoon_Pos_P{p}");
            PlayerPrefs.DeleteKey($"Tycoon_Bankrupt_P{p}");
            PlayerPrefs.DeleteKey($"Tycoon_InJail_P{p}");

            PlayerPrefs.DeleteKey($"Tycoon_Player_{p}_Cash");
            PlayerPrefs.DeleteKey($"Tycoon_Player_{p}_Pos");
            PlayerPrefs.DeleteKey($"Tycoon_Player_{p}_Bankrupt");
            PlayerPrefs.DeleteKey($"Tycoon_Player_{p}_InJail");
        }

        PlayerPrefs.DeleteKey("Tycoon_Jackpot");
        PlayerPrefs.DeleteKey("Tycoon_MarketCycle");
        PlayerPrefs.DeleteKey("Tycoon_TotalTurns");
        PlayerPrefs.DeleteKey("Tycoon_BoardScale");

        for (int i = 0; i < 40; i++)
        {
            PlayerPrefs.DeleteKey($"Tycoon_Tile_{i}_Owner");
            PlayerPrefs.DeleteKey($"Tycoon_Tile_{i}_Lvl");
            PlayerPrefs.DeleteKey($"Tycoon_Tile_{i}_Mortgaged");
        }
        PlayerPrefs.Save();
    }

    public void ResetMatch()
    {
        ClearMatchSave();
        matchSessionGeneration++;

        if (minigameOrchestrator != null)
        {
            minigameOrchestrator.CancelActiveMinigame();
        }
        ClearActiveTransientVisuals();
        isLockpickingInProgress = false;
        HideJailLockpickButton();

        for (int p = 0; p < 4; p++)
        {
            playerCash[p] = startingCashConfig;
            playerPos[p] = 0;
            isBankrupt[p] = false;
            inJail[p] = false;
            if (jailCageHolders[p] != null)
            {
                SafeDestroy(jailCageHolders[p]);
                jailCageHolders[p] = null;
            }
            PositionTokenOnTile(p, 0);
        }

        freeParkingJackpot = 100;
        currentMarketCycle = MarketCycle.Standard;
        totalTurnsCompleted = 0;

        for (int i = 0; i < 40; i++)
        {
            if (board[i] != null)
            {
                board[i].owner = -1;
                board[i].buildingLevel = 0;
                board[i].isMortgaged = false;
                if (board[i].ownershipBadge != null) SafeDestroy(board[i].ownershipBadge);
                if (board[i].spawnedBuilding != null) SafeDestroy(board[i].spawnedBuilding);
                if (board[i].tileRenderer != null)
                {
                    board[i].tileRenderer.material = CreateColoredMaterial(GetOfficialColor(board[i]), 0.85f);
                }
            }
        }

        if (marketAuraRing != null) marketAuraRing.SetActive(false);

        RebuildMonopolySynergyBeams();
        HideDeedCard();
        UpdateLiveHUDStats();
        PlaySFX(cashRegisterSound, procCashClip, 1.0f);
        ShowStartMenu();
    }

    int CalculatePlayerNetWorth(int playerIdx)
    {
        if (playerIdx < 0 || playerIdx >= 4) return 0;
        int net = playerCash[playerIdx];
        for (int i = 0; i < 40; i++)
        {
            if (board[i] != null && board[i].owner == playerIdx)
            {
                net += board[i].isMortgaged ? (board[i].price / 2) : board[i].price;
                net += (board[i].buildingLevel * board[i].buildCost);
            }
        }
        return net;
    }

    string GetMonopolyProgressSummary(int playerIdx)
    {
        string[] groupNames = { "Brown", "Light Blue", "Pink", "Orange", "Red", "Yellow", "Green", "Dark Blue" };
        int monopolies = 0;
        List<string> nearSets = new List<string>();

        for (int g = 0; g < 8; g++)
        {
            int totalInGroup = 0;
            int ownedInGroup = 0;
            for (int i = 0; i < 40; i++)
            {
                if (board[i] != null && board[i].colorGroup == g)
                {
                    totalInGroup++;
                    if (board[i].owner == playerIdx)
                        ownedInGroup++;
                }
            }

            if (totalInGroup > 0)
            {
                if (ownedInGroup == totalInGroup)
                {
                    monopolies++;
                }
                else if (ownedInGroup == totalInGroup - 1 && ownedInGroup > 0)
                {
                    nearSets.Add($"{groupNames[g]} ({ownedInGroup}/{totalInGroup})");
                }
            }
        }

        string nearSetText = nearSets.Count > 0 ? string.Join(", ", nearSets) : "None";
        return $"MONOPOLIES: {monopolies} | NEAR SET: {nearSetText}";
    }

    private TurnState lastContextualState = (TurnState)(-1);

    void UpdateContextualActionButtons()
    {
        if (buttonTransforms == null || buttonTransforms.Length < 10) return;

        if (currentState == TurnState.StartMenu)
        {
            for (int i = 0; i < 10; i++)
            {
                if (buttonTransforms[i] != null) buttonTransforms[i].gameObject.SetActive(false);
            }
            return;
        }

        for (int i = 5; i < 10; i++)
        {
            if (buttonTransforms[i] != null) buttonTransforms[i].gameObject.SetActive(true);
        }

        bool canRoll = (currentState == TurnState.WaitingForRoll);
        bool canBuyPass = (currentState == TurnState.BuyOrPassDecision);
        bool canAuction = (currentState == TurnState.AuctionInProgress);
        bool canBuild = (currentState == TurnState.WaitingForRoll || currentState == TurnState.BuyOrPassDecision);

        if (buttonTransforms[0] != null) buttonTransforms[0].gameObject.SetActive(canRoll || canAuction);
        if (buttonTransforms[1] != null) buttonTransforms[1].gameObject.SetActive(canBuyPass || canAuction);
        if (buttonTransforms[2] != null) buttonTransforms[2].gameObject.SetActive(canBuyPass || canAuction);
        if (buttonTransforms[3] != null) buttonTransforms[3].gameObject.SetActive(canBuild);
        if (buttonTransforms[4] != null) buttonTransforms[4].gameObject.SetActive(canBuild);
    }

    void UpdateLiveHUDStats()
    {
        if (dashboardStatsTMP == null) return;
        string monopolySummary = GetMonopolyProgressSummary(CurrentTurnPlayer);
        string cycleBadge = "";
        switch (currentMarketCycle)
        {
            case MarketCycle.BullBoom: cycleBadge = "MARKET: BULL BOOM (+50% RENT)"; break;
            case MarketCycle.BearRecession: cycleBadge = "MARKET: BEAR RECESSION (-30% BUILD)"; break;
            case MarketCycle.TechIPOSurge: cycleBadge = "MARKET: TECH IPO (2.5x TRANSIT)"; break;
            default: cycleBadge = "MARKET: STANDARD"; break;
        }

        dashboardStatsTMP.text = $"ROUND {totalTurnsCompleted + 1}  |  POT: ${freeParkingJackpot}  |  {cycleBadge}\n{monopolySummary}";
    }

    public void RefreshDashboard(string eventHeadline, string promptLine)
    {
        if (dashboardTitleTMP != null)
        {
            string activeName = (!isPassAndPlayMode && CurrentTurnPlayer == 0) ? "YOU" : GetPlayerName(CurrentTurnPlayer).ToUpper();
            int currentCash = (playerCash != null && CurrentTurnPlayer >= 0 && CurrentTurnPlayer < playerCash.Length) ? playerCash[CurrentTurnPlayer] : 0;
            dashboardTitleTMP.text = $"TURN: {activeName} (CASH: ${currentCash} | NET: ${CalculatePlayerNetWorth(CurrentTurnPlayer)})";
        }
        if (dashboardBodyTMP != null) dashboardBodyTMP.text = eventHeadline;
        if (dashboardPromptTMP != null) dashboardPromptTMP.text = promptLine;
        isHudDirty = true;
        UpdateLiveHUDStats();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        AutoPopulateEmptyPrefabs();
    }

    [ContextMenu("Auto-Populate City Asset Prefabs")]
    public void AutoPopulateEmptyPrefabs()
    {
        // 1. Safety sanitization: Reset any slots currently referencing Meta / SDK / non-mesh prefabs
        System.Func<GameObject, bool> isInvalidPrefab = (go) =>
        {
            if (go == null) return false;
            string n = go.name;
            if (n.Contains("Microgesture") || n.Contains("Spawner") || n.Contains("BuildingBlock") || n.Contains("NGO")) return true;
            if (go.GetComponentInChildren<MeshRenderer>(true) == null && go.GetComponentInChildren<SkinnedMeshRenderer>(true) == null) return true;
            return false;
        };

        if (isInvalidPrefab(dicePrefab)) dicePrefab = null;
        if (isInvalidPrefab(playerTokenPrefab)) playerTokenPrefab = null;
        if (isInvalidPrefab(aiRivalTokenPrefab)) aiRivalTokenPrefab = null;
        if (isInvalidPrefab(rival2TokenPrefab)) rival2TokenPrefab = null;
        if (isInvalidPrefab(rival3TokenPrefab)) rival3TokenPrefab = null;
        if (isInvalidPrefab(rivalAvatarPrefab)) rivalAvatarPrefab = null;
        if (isInvalidPrefab(rival2AvatarPrefab)) rival2AvatarPrefab = null;
        if (isInvalidPrefab(rival3AvatarPrefab)) rival3AvatarPrefab = null;
        if (isInvalidPrefab(level1HousePrefab)) level1HousePrefab = null;
        if (isInvalidPrefab(level2HousePrefab)) level2HousePrefab = null;
        if (isInvalidPrefab(level3HousePrefab)) level3HousePrefab = null;
        if (isInvalidPrefab(level4HousePrefab)) level4HousePrefab = null;
        if (isInvalidPrefab(level5SkyscraperPrefab)) level5SkyscraperPrefab = null;
        if (isInvalidPrefab(parkTreePrefab)) parkTreePrefab = null;
        if (isInvalidPrefab(parkCenterpiecePrefab)) parkCenterpiecePrefab = null;
        if (isInvalidPrefab(streetlightPrefab)) streetlightPrefab = null;
        if (isInvalidPrefab(ambientTrafficCar1Prefab)) ambientTrafficCar1Prefab = null;
        if (isInvalidPrefab(ambientTrafficCar2Prefab)) ambientTrafficCar2Prefab = null;
        if (isInvalidPrefab(vipHelicopterPrefab)) vipHelicopterPrefab = null;
        if (isInvalidPrefab(goCornerLandmarkPrefab)) goCornerLandmarkPrefab = null;
        if (isInvalidPrefab(jailCornerLandmarkPrefab)) jailCornerLandmarkPrefab = null;
        if (isInvalidPrefab(freeParkingLandmarkPrefab)) freeParkingLandmarkPrefab = null;
        if (isInvalidPrefab(goToJailLandmarkPrefab)) goToJailLandmarkPrefab = null;
        if (isInvalidPrefab(roadStraightPrefab)) roadStraightPrefab = null;
        if (isInvalidPrefab(billboardPropPrefab)) billboardPropPrefab = null;
        if (isInvalidPrefab(tradeContractPrefab)) tradeContractPrefab = null;
        if (isInvalidPrefab(waxSealStampPrefab)) waxSealStampPrefab = null;
        if (isInvalidPrefab(auctionGavelPrefab)) auctionGavelPrefab = null;
        if (isInvalidPrefab(cashBriefcasePrefab)) cashBriefcasePrefab = null;
        if (isInvalidPrefab(bankVaultPrefab)) bankVaultPrefab = null;
        if (isInvalidPrefab(dollarBillPrefab)) dollarBillPrefab = null;
        if (isInvalidPrefab(cashBundlePrefab)) cashBundlePrefab = null;
        if (isInvalidPrefab(goldBarPrefab)) goldBarPrefab = null;
        if (isInvalidPrefab(padlockPrefab)) padlockPrefab = null;
        if (isInvalidPrefab(steamPipeValvePrefab)) steamPipeValvePrefab = null;
        if (isInvalidPrefab(jackpotChestPrefab)) jackpotChestPrefab = null;
        if (isInvalidPrefab(pedestrianPrefab)) pedestrianPrefab = null;
        if (isInvalidPrefab(crosswalkPropPrefab)) crosswalkPropPrefab = null;
        if (isInvalidPrefab(victoryTrophyPrefab)) victoryTrophyPrefab = null;

        string[] artFolders = new string[] {
            "Assets/ithappy",
            "Assets/SimplePoly City - Low Poly Assets",
            "Assets/Quaint Game Studio",
            "Assets/Pandazole_Lowpoly_Asset_Bundle",
            "Assets/Studio Horizon",
            "Assets/Dice_6"
        };

        // 1. Dice
        if (dicePrefab == null)
            dicePrefab = FindPrefabByNameOrFilter(artFolders, "Dice_d6_Plastic Glossy Pure write", "Dice_d6");

        // 2. Levels 1 to 5 Houses & Skyscraper
        if (level1HousePrefab == null)
            level1HousePrefab = FindPrefabByNameOrFilter(artFolders, "House_01", "Env_ResidentBuilding_04", "Building_House_01_color01");
        if (level2HousePrefab == null)
            level2HousePrefab = FindPrefabByNameOrFilter(artFolders, "Building_House_03_color01", "House_02", "Env_ResidentBuilding_02");
        if (level3HousePrefab == null)
            level3HousePrefab = FindPrefabByNameOrFilter(artFolders, "Corner Building", "House_04", "Building_House_04_color01");
        if (level4HousePrefab == null)
            level4HousePrefab = FindPrefabByNameOrFilter(artFolders, "Eco_Building_Slope", "Red Building", "Building_Residential_color01");
        if (level5SkyscraperPrefab == null)
            level5SkyscraperPrefab = FindPrefabByNameOrFilter(artFolders, "Eco_Building_Grid", "Regular_Building_TwistedTower_Large", "Building Sky_big_color01");

        // 3. Vehicles & Player Tokens
        if (playerTokenPrefab == null)
            playerTokenPrefab = FindPrefabByNameOrFilter(artFolders, "Car_13", "Vehicle_Police Car_separate");
        if (aiRivalTokenPrefab == null)
            aiRivalTokenPrefab = FindPrefabByNameOrFilter(artFolders, "Vehicle_Police Car_separate", "Car_06");
        if (rival2TokenPrefab == null)
            rival2TokenPrefab = FindPrefabByNameOrFilter(artFolders, "Van", "Car_16");
        if (rival3TokenPrefab == null)
            rival3TokenPrefab = FindPrefabByNameOrFilter(artFolders, "Pickup", "Car_10");

        if (ambientTrafficCar1Prefab == null)
            ambientTrafficCar1Prefab = FindPrefabByNameOrFilter(artFolders, "Car_11", "Car_04");
        if (ambientTrafficCar2Prefab == null)
            ambientTrafficCar2Prefab = FindPrefabByNameOrFilter(artFolders, "Car_15", "Car_08");
        if (vipHelicopterPrefab == null)
            vipHelicopterPrefab = FindPrefabByNameOrFilter(artFolders, "Helicopter", "Chopper");

        // 4. Landmarks (Header 7)
        if (goCornerLandmarkPrefab == null)
            goCornerLandmarkPrefab = FindPrefabByNameOrFilter(artFolders, "Market", "Building_Super Market");
        if (jailCornerLandmarkPrefab == null)
            jailCornerLandmarkPrefab = FindPrefabByNameOrFilter(artFolders, "Police_Station");
        if (freeParkingLandmarkPrefab == null)
            freeParkingLandmarkPrefab = FindPrefabByNameOrFilter(artFolders, "Hospital", "Fire Station", "Building_Gas Station");
        if (goToJailLandmarkPrefab == null)
            goToJailLandmarkPrefab = FindPrefabByNameOrFilter(artFolders, "School", "Building_Factory");

        // 5. Roads & Billboards
        if (roadStraightPrefab == null)
            roadStraightPrefab = FindPrefabByNameOrFilter(artFolders, "Road Lane_01", "road_001", "Env_Road_Straight_01");
        if (billboardPropPrefab == null)
            billboardPropPrefab = FindPrefabByNameOrFilter(artFolders, "Billboard_4x1_03", "Billboard_2x1_03", "Signboard_01");

        // 6. Courtyard Elements
        if (parkTreePrefab == null)
            parkTreePrefab = FindPrefabByNameOrFilter(artFolders, "Natures_Big Tree", "Prop_Tree_01", "Palm_03");
        if (parkCenterpiecePrefab == null)
            parkCenterpiecePrefab = FindPrefabByNameOrFilter(artFolders, "Fountain_03", "Natures_Pot Bush_big");
        if (streetlightPrefab == null)
            streetlightPrefab = FindPrefabByNameOrFilter(artFolders, "Spotlight_01", "traffic_light_001");

        // 7. Interactive Props & Minigame Prefabs (Header 8)
        tradeContractPrefab = null; // Clean procedural parchment scroll with zero obstruction
        if (waxSealStampPrefab == null)
            waxSealStampPrefab = FindPrefabByNameOrFilter(artFolders, "WaxSeal", "Stamp", "Seal");
        if (auctionGavelPrefab == null)
            auctionGavelPrefab = FindPrefabByNameOrFilter(artFolders, "Gavel", "AuctionGavel", "Hammer", "Prop_ThrowingStick");

        string[] casinoFolders = new string[] { "Assets/ithappy/Casino_Free", "Assets/ithappy" };
        string[] chessFolders = new string[] { "Assets/ithappy/Chess_Pack_Free", "Assets/ithappy/Casino_Free", "Assets/ithappy" };

        if (cashBriefcasePrefab == null)
            cashBriefcasePrefab = FindPrefabByNameOrFilter(casinoFolders, "Briefcase", "CashBriefcase", "Suitcase", "Case", "Prop_SmallStorageBox_02");
        if (bankVaultPrefab == null)
            bankVaultPrefab = FindPrefabByNameOrFilter(casinoFolders, "SafeBox_01", "BankVault", "Vault", "Safe", "Prop_SmallStorageBox_04");
        if (dollarBillPrefab == null)
            dollarBillPrefab = FindPrefabByNameOrFilter(casinoFolders, "Cash_01", "DollarBill", "Cash", "Money", "Dollar");
        if (cashBundlePrefab == null)
            cashBundlePrefab = FindPrefabByNameOrFilter(casinoFolders, "Cash_07", "Cash_11", "Cash_01", "Money", "Banknote", "Bill", "Stack", "Bundle", "Chip");
        if (goldBarPrefab == null)
            goldBarPrefab = FindPrefabByNameOrFilter(casinoFolders, "Gold_Ingot_01", "Gold", "Ingot", "Bar", "Chip");
        if (padlockPrefab == null)
            padlockPrefab = FindPrefabByNameOrFilter(artFolders, "Padlock", "Lock", "Key");
        if (steamPipeValvePrefab == null)
            steamPipeValvePrefab = FindPrefabByNameOrFilter(artFolders, "SteamValve", "Valve", "Pipe", "Faucet", "Prop_Faucet_06", "Props_Hydrant");
        if (jackpotChestPrefab == null)
            jackpotChestPrefab = FindPrefabByNameOrFilter(artFolders, "Prop_WoodenChest_02", "Chest", "TreasureChest", "Box_01", "Crate_01", "Prop_SmallStorageBox_01");
        if (pedestrianPrefab == null)
            pedestrianPrefab = FindPrefabByNameOrFilter(chessFolders, "Adult_Base", "ChessPlayer_01", "Basic_Character_Adult", "Character", "Person", "Pedestrian", "Citizen");
        if (crosswalkPropPrefab == null)
            crosswalkPropPrefab = FindPrefabByNameOrFilter(artFolders, "Crosswalk", "Road_Crosswalk");
        if (victoryTrophyPrefab == null)
            victoryTrophyPrefab = FindPrefabByNameOrFilter(chessFolders, "Chess_White_King_01", "Chess_Black_King_01", "Cup_02", "Cup_01", "King", "Queen", "Trophy", "Cup", "Crown", "Award");

        UnityEditor.EditorUtility.SetDirty(this);
    }

    private GameObject FindPrefabByNameOrFilter(string[] searchFolders, params string[] targetNames)
    {
        string[] folders = searchFolders != null && searchFolders.Length > 0 ? searchFolders : new string[] {
            "Assets/ithappy",
            "Assets/SimplePoly City - Low Poly Assets",
            "Assets/Quaint Game Studio",
            "Assets/Pandazole_Lowpoly_Asset_Bundle",
            "Assets/Studio Horizon",
            "Assets/Dice_6"
        };

        foreach (string name in targetNames)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"{name} t:Prefab", folders);
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (path.IndexOf("Oculus", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    path.IndexOf("Meta", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    path.IndexOf("BuildingBlock", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    path.IndexOf("TextMesh Pro", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    path.IndexOf("Packages", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                if (path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
                {
                    GameObject go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (go != null && (go.GetComponentInChildren<MeshRenderer>(true) != null || go.GetComponentInChildren<SkinnedMeshRenderer>(true) != null))
                    {
                        if (go.name.Contains("Microgesture") || go.name.Contains("Spawner") || go.name.Contains("BuildingBlock") || go.name.Contains("NGO"))
                            continue;
                        return go;
                    }
                }
            }
        }
        return null;
    }

    private GameObject FindPrefabByNameOrFilter(params string[] targetNames)
    {
        return FindPrefabByNameOrFilter(null, targetNames);
    }
#endif
}