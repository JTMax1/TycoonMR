using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

[RequireComponent(typeof(AudioSource))]
public class TycoonBoardManager : MonoBehaviour
{
    [Header("1. Mixed Reality References")]
    public OVRPassthroughLayer passthroughLayer;

    [Header("2. Custom Player Vehicle & Rival Avatar Prefabs (Drag & Drop)")]
    public GameObject dicePrefab;
    public GameObject playerTokenPrefab;
    public GameObject aiRivalTokenPrefab;
    public GameObject rivalAvatarPrefab;

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

    public enum TileType { Go, Property, Railroad, Utility, Tax, Chance, Chest, Jail, FreeParking, GoToJail }

    public class BoardTile
    {
        public int index;
        public string name;
        public TileType type;
        public int colorGroup;
        public int price;
        public int buildCost;
        public int[] rentByLevel;
        public int owner = -1;
        public int buildingLevel = 0;
        public bool isMortgaged = false;
        public Transform tileTransform;
        public Renderer tileRenderer;
        public GameObject ownershipBadge;
        public GameObject spawnedBuilding;
    }

    private BoardTile[] board = new BoardTile[40];

    private int[] playerCash = new int[2] { 1500, 1500 };
    private int[] playerPos = new int[2] { 0, 0 };
    private bool[] inJail = new bool[2] { false, false };
    private Transform[] playerTokens = new Transform[2];
    private int buildCycleCursor = 0;

    private int freeParkingJackpot = 100;
    private Transform jackpotGoldPile;

    private enum TurnState { WaitingForRoll, DiceTumbling, Moving, BuyOrPassDecision, AuctionInProgress, AITurn }
    private TurnState currentState = TurnState.WaitingForRoll;
    private int lastDiceTotal = 0;
    private int consecutiveDoubles = 0;

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

    private Transform rivalAvatarRoot;
    private Transform rivalHeadTransform;
    private Transform rivalLeftHandTransform;
    private Transform rivalRightHandTransform;
    private Renderer rivalVisorRenderer;
    private TextMeshProUGUI rivalSpeechBubbleTMP;
    private enum AvatarMood { Neutral, HappyCheer, AngrySlam, Thinking }
    private AvatarMood currentRivalMood = AvatarMood.Neutral;
    private float moodResetTimer = 0f;

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

    private AudioSource sfxSource;
    private Material masterURPMaterial;

    private Transform[] buttonTransforms = new Transform[9];
    private float lastButtonTouchTime = 0f;

    // Meta XR Interaction SDK Hand & Poke References
    private PokeInteractor[] scenePokeInteractors;
    private OVRHand[] sceneOVRHands;
    private Transform activeHandGrabTransform;
    private Vector3 leftHandWorldPos = new Vector3(0f, -100f, 0f);
    private Vector3 rightHandWorldPos = new Vector3(0f, -100f, 0f);
    private bool isLeftHandTracked = false;
    private bool isRightHandTracked = false;

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

    // 1. In Awake(), keep a reference to [BuildingBlock] Cube before hiding it so we can clone its native DistanceHandGrab setup!
    private GameObject buildingBlockGrabTemplate;

    void Awake()
    {
        GameObject locomotorObj = GameObject.Find("Locomotor");
        if (locomotorObj != null) locomotorObj.SetActive(false);

        GameObject sampleCube = GameObject.Find("[BuildingBlock] Cube");
        if (sampleCube != null)
        {
            buildingBlockGrabTemplate = sampleCube;
            sampleCube.SetActive(false);
        }

        Rigidbody boardRb = GetComponent<Rigidbody>();
        if (boardRb != null) Destroy(boardRb);

        EnforceUnderlayPassthrough();
    }

    void AttachNativeBuildingBlockGrab(GameObject targetObj, Rigidbody targetRb)
    {
        if (targetObj == null) return;
        if (targetRb == null)
        {
            targetRb = targetObj.GetComponent<Rigidbody>();
            if (targetRb == null) targetRb = targetObj.AddComponent<Rigidbody>();
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
        OVRPassthroughLayer[] layers = Object.FindObjectsByType<OVRPassthroughLayer>(FindObjectsSortMode.None);
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] == null) continue;
            // overlayType and compositionDepth were removed in recent Meta XR SDKs.
            // The [BuildingBlock] Passthrough handles the background underlay automatically.
            if (passthroughLayer == null) passthroughLayer = layers[i];
        }
    }

    void StripRoguePhysicsFromImportedPrefab(GameObject spawnedInstance)
    {
        if (spawnedInstance == null) return;
        Rigidbody[] rbs = spawnedInstance.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rbs.Length; i++) Destroy(rbs[i]);

        Collider[] cols = spawnedInstance.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++) Destroy(cols[i]);
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
        CacheMetaInteractionSDKHands();
        InitializeOfficialMonopolyMap();
        Build40TileClockwiseTable();
        BuildLivingCenterCourtyard();
        Create3DRivalMogulAvatar();
        Create3DDiceAndHighDashboard();
        Create3DDeedCardPopup();
        CreateGrabbable3DDeedCrystal();
        Create3DTouchButtonsOnTable();
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
            RefreshDashboard(
                "YOUR TURN — DISTANCE-GRAB & THROW DICE!",
                "Point & Pinch with your hand to Magnet-Pull the 3D Dice into your palm, shake, and release to Throw!"
            );
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

    public void RecenterBoardInFrontOfPlayer()
    {
        if (isInPenthouseVR) return;
        lastButtonTouchTime = Time.time + 0.8f;

        Transform eye = Camera.main != null ? Camera.main.transform : null;
        if (eye == null)
        {
            OVRCameraRig rig = Object.FindAnyObjectByType<OVRCameraRig>();
            if (rig != null) eye = rig.centerEyeAnchor;
        }
        if (eye == null) return;

        Vector3 eyePos = eye.position;
        if (float.IsNaN(eyePos.x) || float.IsNaN(eyePos.y) || float.IsNaN(eyePos.z) || eyePos.magnitude > 50f)
        {
            eyePos = Vector3.zero;
        }

        Vector3 forwardFlat = new Vector3(eye.forward.x, 0f, eye.forward.z);
        if (forwardFlat.sqrMagnitude < 0.001f) forwardFlat = Vector3.forward;
        else forwardFlat.Normalize();

        float halfBoardWorld = (boardSize * currentBoardScale) * 0.5f;
        float forwardDistance = halfBoardWorld + 0.42f;

        transform.localScale = Vector3.one * currentBoardScale;
        lockedBoardPosition = eyePos + (forwardFlat * forwardDistance) + new Vector3(0f, -0.34f, 0f);
        lockedBoardRotation = Quaternion.LookRotation(forwardFlat, Vector3.up);

        transform.position = lockedBoardPosition;
        transform.rotation = lockedBoardRotation;
        hasLockedInitialPose = true;
    }

    public void AdjustBoardScale(float delta)
    {
        if (isInPenthouseVR) return;
        currentBoardScale = Mathf.Clamp(currentBoardScale + delta, 0.45f, 1.55f);
        transform.localScale = Vector3.one * currentBoardScale;
        SaveGameState();
        PlaySFX(buildUpgradeSound, procStepClip, delta > 0 ? 1.15f : 0.85f);
        RefreshDashboard($"BOARD SIZE: {(int)(currentBoardScale * 100)}%", "Touch [- SHRINK] / [+ GROW] or Pinch BOTH hands to move & resize!");
    }

    void InitializeMasterURPMaterial()
    {
        if (darkMarbleMaterial != null)
        {
            masterURPMaterial = new Material(darkMarbleMaterial);
        }
        else if (brushedGoldMaterial != null)
        {
            masterURPMaterial = new Material(brushedGoldMaterial);
        }
        else
        {
            if (level1HousePrefab != null)
            {
                Renderer r = level1HousePrefab.GetComponentInChildren<Renderer>();
                if (r != null && r.sharedMaterial != null) masterURPMaterial = new Material(r.sharedMaterial);
            }
            if (masterURPMaterial == null)
            {
                masterURPMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            }
        }
    }

    Material CreateColoredMaterial(Color color, float smoothness = 0.85f, float metallic = 0.05f)
    {
        Material m = new Material(masterURPMaterial);
        m.mainTexture = null;
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", null);
        m.color = color;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
        return m;
    }

    void CacheMetaInteractionSDKHands()
    {
        scenePokeInteractors = Object.FindObjectsByType<PokeInteractor>();
        sceneOVRHands = Object.FindObjectsByType<OVRHand>();

        if (activeHandGrabTransform == null)
        {
            GameObject holder = new GameObject("ISDK_Active_Hand_GrabPoint");
            activeHandGrabTransform = holder.transform;
        }
    }

    // Determines if a transform belongs to the Left hand (-1), Right hand (1), or Unknown (0)
    // by inspecting GameObject names up the entire parent hierarchy and checking for OVRHand.
    private int DetermineHierarchyHandedness(Transform targetTransform)
    {
        if (targetTransform == null) return 0;

        Transform curr = targetTransform;
        while (curr != null)
        {
            string n = curr.name.ToLowerInvariant();
            bool hasLeft = n.Contains("left") || n.Contains("lhand") || n.Contains("_l_") || n.EndsWith("_l") || n.StartsWith("l_") || n.Contains("hand_l");
            bool hasRight = n.Contains("right") || n.Contains("rhand") || n.Contains("_r_") || n.EndsWith("_r") || n.StartsWith("r_") || n.Contains("hand_r");

            if (hasLeft && !hasRight) return -1;
            if (hasRight && !hasLeft) return 1;

            curr = curr.parent;
        }

        OVRHand ovr = targetTransform.GetComponentInParent<OVRHand>();
        if (ovr != null)
        {
            if (ovr.GetHand() == OVRPlugin.Hand.HandLeft) return -1;
            if (ovr.GetHand() == OVRPlugin.Hand.HandRight) return 1;
        }

        return 0;
    }

    // --- READS BOTH META INTERACTIONS RIG (`OVRHand` / `PokeInteractor`) AND HARDWARE `OVRPlugin` ---
    void ReadMetaISDKHands(out bool rightPinchDown, out bool leftPinchDown, out bool isRightPinching, out bool isLeftPinching)
    {
        rightPinchDown = false;
        leftPinchDown = false;
        isRightPinching = false;
        isLeftPinching = false;
        isLeftHandTracked = false;
        isRightHandTracked = false;
        leftHandWorldPos = new Vector3(0f, -100f, 0f);
        rightHandWorldPos = new Vector3(0f, -100f, 0f);

        // 1. Read Index Fingertip Poke Positions directly from Meta's PokeInteractor (Interactions Rig)!
        if (scenePokeInteractors == null || scenePokeInteractors.Length == 0)
        {
            scenePokeInteractors = Object.FindObjectsByType<PokeInteractor>();
        }

        if (scenePokeInteractors != null)
        {
            for (int i = 0; i < scenePokeInteractors.Length; i++)
            {
                PokeInteractor pi = scenePokeInteractors[i];
                if (pi == null || !pi.gameObject.activeInHierarchy || !pi.enabled) continue;

                // PokeInteractor.Origin is the exact index fingertip point computed by Meta XR Interaction SDK!
                Vector3 tipPos = pi.Origin != Vector3.zero ? pi.Origin : pi.transform.position;
                if (tipPos.sqrMagnitude < 0.0001f) continue;

                int handedness = DetermineHierarchyHandedness(pi.transform);
                if (handedness == -1)
                {
                    leftHandWorldPos = tipPos;
                    isLeftHandTracked = true;
                }
                else if (handedness == 1)
                {
                    rightHandWorldPos = tipPos;
                    isRightHandTracked = true;
                }
                else
                {
                    // Fallback to camera relative horizontal position if hierarchy name is indeterminate
                    Vector3 camLocal = Camera.main != null ? Camera.main.transform.InverseTransformPoint(tipPos) : tipPos;
                    if (camLocal.x < 0f)
                    {
                        leftHandWorldPos = tipPos;
                        isLeftHandTracked = true;
                    }
                    else
                    {
                        rightHandWorldPos = tipPos;
                        isRightHandTracked = true;
                    }
                }
            }
        }

        // 2. Read Pinch Gestures & Hand Positions from Meta's OVRHand (BuildingBlock Hand Tracking / Real Hands)
        if (sceneOVRHands == null || sceneOVRHands.Length == 0)
        {
            sceneOVRHands = Object.FindObjectsByType<OVRHand>();
        }

        if (sceneOVRHands != null)
        {
            foreach (OVRHand hand in sceneOVRHands)
            {
                if (hand == null || !hand.IsTracked) continue;

                bool pinching = hand.GetFingerIsPinching(OVRHand.HandFinger.Index) ||
                                hand.GetFingerPinchStrength(OVRHand.HandFinger.Index) > 0.65f;

                Vector3 ptrPos = hand.PointerPose != null ? hand.PointerPose.position : hand.transform.position;
                int handedness = DetermineHierarchyHandedness(hand.transform);
                if (handedness == 0)
                {
                    if (hand.GetHand() == OVRPlugin.Hand.HandLeft) handedness = -1;
                    else if (hand.GetHand() == OVRPlugin.Hand.HandRight) handedness = 1;
                }

                if (handedness == -1)
                {
                    isLeftHandTracked = true;
                    isLeftPinching |= pinching;
                    if (leftHandWorldPos.y < -50f) leftHandWorldPos = ptrPos;
                }
                else if (handedness == 1)
                {
                    isRightHandTracked = true;
                    isRightPinching |= pinching;
                    if (rightHandWorldPos.y < -50f) rightHandWorldPos = ptrPos;
                }
            }
        }

        // 3. Fallback to OVRPlugin if either hand wasn't tracked by ISDK / OVRHand
        Transform refSpace = Camera.main != null && Camera.main.transform.parent != null ? Camera.main.transform.parent : transform;
        if (!isRightHandTracked)
        {
            try
            {
                OVRPlugin.HandState rightState = new OVRPlugin.HandState();
                if (OVRPlugin.GetHandState(OVRPlugin.Step.Render, OVRPlugin.Hand.HandRight, ref rightState) &&
                    (rightState.Status & OVRPlugin.HandStatus.HandTracked) != 0)
                {
                    isRightHandTracked = true;
                    Vector3 wristLocal = new Vector3(rightState.RootPose.Position.x, rightState.RootPose.Position.y, -rightState.RootPose.Position.z);
                    rightHandWorldPos = refSpace.TransformPoint(wristLocal);
                    isRightPinching = (rightState.Pinches & OVRPlugin.HandFingerPinch.Index) != 0;
                }
            }
            catch { }
        }

        if (!isLeftHandTracked)
        {
            try
            {
                OVRPlugin.HandState leftState = new OVRPlugin.HandState();
                if (OVRPlugin.GetHandState(OVRPlugin.Step.Render, OVRPlugin.Hand.HandLeft, ref leftState) &&
                    (leftState.Status & OVRPlugin.HandStatus.HandTracked) != 0)
                {
                    isLeftHandTracked = true;
                    Vector3 wristLocal = new Vector3(leftState.RootPose.Position.x, leftState.RootPose.Position.y, -leftState.RootPose.Position.z);
                    leftHandWorldPos = refSpace.TransformPoint(wristLocal);
                    isLeftPinching = (leftState.Pinches & OVRPlugin.HandFingerPinch.Index) != 0;
                }
            }
            catch { }
        }

        // Two-Hand Pinch Move & Scale
        if (!isInPenthouseVR && !isPlayerHoldingDice && isRightHandTracked && isLeftHandTracked && isRightPinching && isLeftPinching)
        {
            Vector3 midPoint = (leftHandWorldPos + rightHandWorldPos) * 0.5f;
            float handDist = Vector3.Distance(leftHandWorldPos, rightHandWorldPos);

            if (!isTwoHandManipulating)
            {
                isTwoHandManipulating = true;
                initialTwoHandDistance = Mathf.Max(0.08f, handDist);
                initialScaleOnGrab = currentBoardScale;
                initialGrabMidpoint = midPoint;
                initialBoardPosOnGrab = lockedBoardPosition;
            }
            else
            {
                float scaleFactor = handDist / initialTwoHandDistance;
                currentBoardScale = Mathf.Clamp(initialScaleOnGrab * scaleFactor, 0.40f, 1.65f);
                transform.localScale = Vector3.one * currentBoardScale;

                lockedBoardPosition = initialBoardPosOnGrab + (midPoint - initialGrabMidpoint);
                transform.position = lockedBoardPosition;
            }

            wasRightPinching = true;
            wasLeftPinching = true;
            return;
        }
        else
        {
            if (isTwoHandManipulating)
            {
                isTwoHandManipulating = false;
                SaveGameState();
            }
        }

        rightPinchDown = isRightPinching && !wasRightPinching;
        leftPinchDown = isLeftPinching && !wasLeftPinching;
        wasRightPinching = isRightPinching;
        wasLeftPinching = isLeftPinching;
    }

    void Update()
    {
        UpdateAmbientCourtyardTrafficAndChopper();
        AnimateRivalMogulAvatar();
        AnimateFloatingDeedCrystal();

        bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool buyPressed = Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame;
        bool passPressed = Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame;
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

        if (nPressed || lPressed) ResetMatch();
        if (recenterPressed) RecenterBoardInFrontOfPlayer();
        if (shrinkPressed) AdjustBoardScale(-0.12f);
        if (growPressed) AdjustBoardScale(0.12f);

        bool rightPinchDown = false;
        bool leftPinchDown = false;
        bool rightPinchHeld = false;
        bool leftPinchHeld = false;
        ReadMetaISDKHands(out rightPinchDown, out leftPinchDown, out rightPinchHeld, out leftPinchHeld);

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

        // Check contextual [MORTGAGE] and [+ BUILD] button pokes on Deed Card
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

        // Only trigger Distance Grab / Hand Grab / Tile Poke if player didn't just poke a UI button!
        if (touchedButton == -1 && !mortgagePoked && !upgradeCardPoked)
        {
            HandlePhysicalDiceGrabAndThrow(rightPinchDown, leftPinchDown, rightPinchHeld, leftPinchHeld);
            HandlePhysicalDeedCrystalGrab(rightPinchDown, leftPinchDown, rightPinchHeld, leftPinchHeld);

            // 40-Tile fingertip poke detection
            if (!isPlayerHoldingDice && !isHoldingDeedCrystal)
            {
                int pokedTile = CheckPhysicalTilePoke();
                if (pokedTile != -1)
                {
                    selectedTileIndex = pokedTile;
                    ShowDeedCardForTile(board[selectedTileIndex]);
                    PlaySFX(tokenMoveSound, procStepClip, 1.4f);
                    StartCoroutine(AnimateButtonPress(board[selectedTileIndex].tileTransform));
                }
            }
        }

        if (takeoverPressed && (currentState == TurnState.WaitingForRoll || currentState == TurnState.BuyOrPassDecision))
        {
            TryExecuteHostileTakeover();
        }

        if (currentState == TurnState.WaitingForRoll && !isPlayerHoldingDice)
        {
            if (spacePressed)
            {
                StartCoroutine(ExecutePhysicsDiceRollTurn(true));
            }
            else if (upgradePressed)
            {
                TryBuildChronologically(0);
            }
        }
        else if (currentState == TurnState.BuyOrPassDecision && !isHoldingDeedCrystal)
        {
            if (buyPressed || spacePressed)
            {
                PurchaseCurrentTile(0);
            }
            else if (passPressed || leftPinchDown)
            {
                BoardTile curTile = board[playerPos[0]];
                if (curTile != null && curTile.owner == -1 && (curTile.type == TileType.Property || curTile.type == TileType.Railroad || curTile.type == TileType.Utility))
                {
                    StartCoroutine(RunPropertyAuctionCoroutine(curTile));
                }
                else
                {
                    HideDeedCard();
                    RefreshDashboard("PASSED ON DEED", "Passing turn to Rival Mogul...");
                    StartCoroutine(EndPlayerTurn(false));
                }
            }
            else if (upgradePressed)
            {
                TryBuildChronologically(0);
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

        UpdateLiveHUDStats();
    }

    // --- DISTANCE GRAB (PULL INTERACTABLE TO HAND) & HAND GRAB FOR DICE ---
    void HandlePhysicalDiceGrabAndThrow(bool rPinchDown, bool lPinchDown, bool rPinchHeld, bool lPinchHeld)
    {
        if (currentState != TurnState.WaitingForRoll || physDie1 == null || physDie2 == null) return;

        // 1. When not currently holding dice, check if a grab has been initiated
        if (!isPlayerHoldingDice)
        {
            // Direct touch / native Grabbable grab check
            if (physDie1.isHeldByPlayer || physDie2.isHeldByPlayer)
            {
                isPlayerHoldingDice = true;
                float distR = (isRightHandTracked && rightHandWorldPos.y > -50f) ? Vector3.Distance(rightHandWorldPos, die1Transform.position) : 99f;
                float distL = (isLeftHandTracked && leftHandWorldPos.y > -50f) ? Vector3.Distance(leftHandWorldPos, die1Transform.position) : 99f;
                isGrabbingDiceWithRight = (distR <= distL);

                Vector3 grabPos = isGrabbingDiceWithRight ? rightHandWorldPos : leftHandWorldPos;
                if (activeHandGrabTransform != null && grabPos.y > -50f)
                {
                    activeHandGrabTransform.position = grabPos;
                }

                physDie1.BeginHandGrab(activeHandGrabTransform, new Vector3(-0.028f, 0f, 0f));
                physDie2.BeginHandGrab(activeHandGrabTransform, new Vector3(0.028f, 0f, 0f));

                PlaySFX(diceRollSound, procDiceClip, 1.1f);
                RefreshDashboard("HOLDING 3D DICE IN " + (isGrabbingDiceWithRight ? "RIGHT" : "LEFT") + " HAND!", "Shake your hand and release your pinch to THROW the dice onto the board!");
                return;
            }

            // Distance grab pinch check
            if (rPinchDown || lPinchDown)
            {
                float distR = (isRightHandTracked && rightHandWorldPos.y > -50f) ? Vector3.Distance(rightHandWorldPos, die1Transform.position) : 99f;
                float distL = (isLeftHandTracked && leftHandWorldPos.y > -50f) ? Vector3.Distance(leftHandWorldPos, die1Transform.position) : 99f;

                bool grabRight;
                if (rPinchDown && lPinchDown)
                {
                    grabRight = (distR <= distL);
                }
                else if (rPinchDown)
                {
                    grabRight = true;
                }
                else
                {
                    grabRight = false;
                }

                float candidateDist = grabRight ? distR : distL;
                Vector3 candidatePos = grabRight ? rightHandWorldPos : leftHandWorldPos;

                if (candidateDist < 1.35f && activeHandGrabTransform != null && candidatePos.y > -50f)
                {
                    isPlayerHoldingDice = true;
                    isGrabbingDiceWithRight = grabRight; // LOCK ONTO THE GRABBING HAND!
                    activeHandGrabTransform.position = candidatePos;

                    physDie1.BeginHandGrab(activeHandGrabTransform, new Vector3(-0.028f, 0f, 0f));
                    physDie2.BeginHandGrab(activeHandGrabTransform, new Vector3(0.028f, 0f, 0f));

                    PlaySFX(diceRollSound, procDiceClip, 1.15f);
                    RefreshDashboard("PULLED 3D DICE TO " + (isGrabbingDiceWithRight ? "RIGHT" : "LEFT") + " HAND!", "Shake your hand and release your pinch to THROW the dice onto Central Park!");
                }
            }
        }
        else // isPlayerHoldingDice is true -> stay LOCKED onto the grabbing hand
        {
            Vector3 lockedHandPos = isGrabbingDiceWithRight ? rightHandWorldPos : leftHandWorldPos;
            if (activeHandGrabTransform != null && lockedHandPos.y > -50f)
            {
                activeHandGrabTransform.position = lockedHandPos;
            }

            bool grabbingHandPinching = isGrabbingDiceWithRight ? rPinchHeld : lPinchHeld;
            bool grabbingHandTracked = isGrabbingDiceWithRight ? isRightHandTracked : isLeftHandTracked;

            // Throw when the specific grabbing hand releases pinch
            if (!grabbingHandPinching || (!grabbingHandTracked && !rPinchHeld && !lPinchHeld))
            {
                isPlayerHoldingDice = false;
                StartCoroutine(ExecutePhysicsDiceRollTurn(false));
            }
        }
    }

    // --- DISTANCE GRAB & HAND GRAB FOR 3D TITLE DEED CRYSTAL ---
    void HandlePhysicalDeedCrystalGrab(bool rPinchDown, bool lPinchDown, bool rPinchHeld, bool lPinchHeld)
    {
        if (currentState != TurnState.BuyOrPassDecision || grabbableDeedCrystal == null || !grabbableDeedCrystal.activeSelf) return;

        Grabbable crystalGrabbable = grabbableDeedCrystal.GetComponent<Grabbable>();

        if (!isHoldingDeedCrystal)
        {
            // Direct touch / native ISDK selection check
            if (crystalGrabbable != null && crystalGrabbable.SelectingPointsCount > 0)
            {
                isHoldingDeedCrystal = true;
                float distR = (isRightHandTracked && rightHandWorldPos.y > -50f) ? Vector3.Distance(rightHandWorldPos, grabbableDeedCrystal.transform.position) : 99f;
                float distL = (isLeftHandTracked && leftHandWorldPos.y > -50f) ? Vector3.Distance(leftHandWorldPos, grabbableDeedCrystal.transform.position) : 99f;
                isHoldingDeedWithRight = (distR <= distL);
                PlaySFX(tokenMoveSound, procStepClip, 1.2f);
                RefreshDashboard("HOLDING TITLE DEED CRYSTAL IN " + (isHoldingDeedWithRight ? "RIGHT" : "LEFT") + " HAND!", "Release your pinch (or slam onto the board) to purchase this property!");
                return;
            }

            // Distance grab pinch check
            if (rPinchDown || lPinchDown)
            {
                float distR = (isRightHandTracked && rightHandWorldPos.y > -50f) ? Vector3.Distance(rightHandWorldPos, grabbableDeedCrystal.transform.position) : 99f;
                float distL = (isLeftHandTracked && leftHandWorldPos.y > -50f) ? Vector3.Distance(leftHandWorldPos, grabbableDeedCrystal.transform.position) : 99f;

                bool grabRight = (rPinchDown && lPinchDown) ? (distR <= distL) : rPinchDown;
                float candidateDist = grabRight ? distR : distL;

                if (candidateDist < 1.25f)
                {
                    isHoldingDeedCrystal = true;
                    isHoldingDeedWithRight = grabRight;
                    PlaySFX(tokenMoveSound, procStepClip, 1.2f);
                    RefreshDashboard("PULLED TITLE DEED CRYSTAL TO " + (isHoldingDeedWithRight ? "RIGHT" : "LEFT") + " HAND!", "Release your pinch (or slam onto the board) to purchase this property!");
                }
            }
        }
        else
        {
            Vector3 targetHandPos = isHoldingDeedWithRight ? rightHandWorldPos : leftHandWorldPos;
            if (targetHandPos.y > -50f)
            {
                grabbableDeedCrystal.transform.position = Vector3.Lerp(grabbableDeedCrystal.transform.position, targetHandPos, Time.deltaTime * 24f);
            }

            bool heldPinch = isHoldingDeedWithRight ? rPinchHeld : lPinchHeld;
            if (!heldPinch || grabbableDeedCrystal.transform.localPosition.y < 0.08f)
            {
                isHoldingDeedCrystal = false;
                PurchaseCurrentTile(0);
            }
        }
    }

    void CreateGrabbable3DDeedCrystal()
    {
        grabbableDeedCrystal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        grabbableDeedCrystal.name = "Grabbable_3D_Deed_Crystal";
        grabbableDeedCrystal.transform.SetParent(transform, false);
        grabbableDeedCrystal.transform.localPosition = new Vector3(0.24f, 0.24f, -0.18f);
        grabbableDeedCrystal.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
        grabbableDeedCrystal.transform.localScale = Vector3.one * 0.055f;

        // Keep BoxCollider on crystal so Meta's ISDK can grab it natively
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

    void Create3DRivalMogulAvatar()
    {
        GameObject avatarHolder = new GameObject("Rival_Mogul_3D_Avatar");
        avatarHolder.transform.SetParent(transform, false);
        avatarHolder.transform.localPosition = new Vector3(0f, 0.14f, (boardSize * 0.5f) + 0.22f);
        avatarHolder.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        rivalAvatarRoot = avatarHolder.transform;

        if (rivalAvatarPrefab != null && rivalAvatarPrefab.GetComponent<OVRCameraRig>() == null)
        {
            GameObject customModel = Instantiate(rivalAvatarPrefab, avatarHolder.transform);
            StripRoguePhysicsFromImportedPrefab(customModel);
            customModel.transform.localPosition = Vector3.zero;
            NormalizePrefabSize(customModel, 0.35f);
            rivalHeadTransform = customModel.transform;
        }
        else
        {
            Material suitMat = CreateColoredMaterial(new Color(0.10f, 0.12f, 0.18f), 0.85f, 0.4f);
            Material goldLapelMat = CreateColoredMaterial(new Color(0.92f, 0.74f, 0.22f), 0.9f, 0.85f);
            Material headMat = CreateColoredMaterial(new Color(0.82f, 0.86f, 0.94f), 0.9f, 0.7f);
            Material visorMat = CreateColoredMaterial(new Color(0.15f, 0.88f, 1f), 0.95f);

            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            torso.name = "Mogul_Torso";
            torso.transform.SetParent(avatarHolder.transform, false);
            torso.transform.localPosition = new Vector3(0f, -0.04f, 0f);
            torso.transform.localScale = new Vector3(0.18f, 0.14f, 0.11f);
            Destroy(torso.GetComponent<Collider>());
            torso.GetComponent<Renderer>().material = suitMat;

            GameObject tie = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tie.name = "Mogul_Gold_Tie";
            tie.transform.SetParent(torso.transform, false);
            tie.transform.localPosition = new Vector3(0f, 0.25f, 0.48f);
            tie.transform.localScale = new Vector3(0.18f, 0.55f, 0.12f);
            Destroy(tie.GetComponent<Collider>());
            tie.GetComponent<Renderer>().material = goldLapelMat;

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Mogul_Head";
            head.transform.SetParent(avatarHolder.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.16f, 0f);
            head.transform.localScale = new Vector3(0.12f, 0.135f, 0.12f);
            Destroy(head.GetComponent<Collider>());
            head.GetComponent<Renderer>().material = headMat;
            rivalHeadTransform = head.transform;

            GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "Mogul_Visor";
            visor.transform.SetParent(head.transform, false);
            visor.transform.localPosition = new Vector3(0f, 0.08f, 0.44f);
            visor.transform.localScale = new Vector3(0.72f, 0.22f, 0.22f);
            Destroy(visor.GetComponent<Collider>());
            rivalVisorRenderer = visor.GetComponent<Renderer>();
            rivalVisorRenderer.material = visorMat;

            GameObject lHand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lHand.name = "Mogul_Left_Hand";
            lHand.transform.SetParent(avatarHolder.transform, false);
            lHand.transform.localPosition = new Vector3(-0.16f, 0.02f, 0.10f);
            lHand.transform.localScale = new Vector3(0.045f, 0.035f, 0.055f);
            Destroy(lHand.GetComponent<Collider>());
            lHand.GetComponent<Renderer>().material = goldLapelMat;
            rivalLeftHandTransform = lHand.transform;

            GameObject rHand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rHand.name = "Mogul_Right_Hand";
            rHand.transform.SetParent(avatarHolder.transform, false);
            rHand.transform.localPosition = new Vector3(0.16f, 0.02f, 0.10f);
            rHand.transform.localScale = new Vector3(0.045f, 0.035f, 0.055f);
            Destroy(rHand.GetComponent<Collider>());
            rHand.GetComponent<Renderer>().material = goldLapelMat;
            rivalRightHandTransform = rHand.transform;
        }

        GameObject bubbleCanvas = new GameObject("Mogul_Speech_Bubble");
        bubbleCanvas.transform.SetParent(avatarHolder.transform, false);
        bubbleCanvas.transform.localPosition = new Vector3(0f, 0.29f, 0f);
        bubbleCanvas.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        bubbleCanvas.transform.localScale = Vector3.one * 0.0005f;

        Canvas c = bubbleCanvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.WorldSpace;
        bubbleCanvas.GetComponent<RectTransform>().sizeDelta = new Vector2(620f, 110f);

        rivalSpeechBubbleTMP = CreateUGUIText(bubbleCanvas.transform, "BubbleText", Vector2.zero, 30, new Color(1f, 0.90f, 0.45f));
        rivalSpeechBubbleTMP.text = "MOGUL AI: \"Let's see what you've got!\"";
    }

    void SetRivalReaction(AvatarMood mood, string speechLine, float duration = 3.0f)
    {
        currentRivalMood = mood;
        moodResetTimer = duration;
        if (rivalSpeechBubbleTMP != null)
        {
            rivalSpeechBubbleTMP.text = $"MOGUL AI: \"{speechLine}\"";
        }

        if (rivalVisorRenderer != null)
        {
            Color visorCol = new Color(0.15f, 0.88f, 1f);
            if (mood == AvatarMood.HappyCheer) visorCol = new Color(0.20f, 1f, 0.45f);
            else if (mood == AvatarMood.AngrySlam) visorCol = new Color(1f, 0.18f, 0.20f);
            else if (mood == AvatarMood.Thinking) visorCol = new Color(1f, 0.84f, 0.20f);
            rivalVisorRenderer.material = CreateColoredMaterial(visorCol, 0.95f);
        }
    }

    void AnimateRivalMogulAvatar()
    {
        if (rivalAvatarRoot == null) return;

        if (moodResetTimer > 0f)
        {
            moodResetTimer -= Time.deltaTime;
            if (moodResetTimer <= 0f)
            {
                currentRivalMood = AvatarMood.Neutral;
                if (rivalVisorRenderer != null)
                    rivalVisorRenderer.material = CreateColoredMaterial(new Color(0.15f, 0.88f, 1f), 0.95f);
            }
        }

        float t = Time.time;
        rivalAvatarRoot.localPosition = new Vector3(0f, 0.14f + Mathf.Sin(t * 2.2f) * 0.008f, (boardSize * 0.5f) + 0.22f);

        if (rivalHeadTransform != null && rivalLeftHandTransform != null && rivalRightHandTransform != null)
        {
            switch (currentRivalMood)
            {
                case AvatarMood.HappyCheer:
                    rivalHeadTransform.localRotation = Quaternion.Euler(Mathf.Sin(t * 12f) * 10f, 0f, 0f);
                    rivalLeftHandTransform.localPosition = new Vector3(-0.16f, 0.12f + Mathf.Sin(t * 14f) * 0.045f, 0.10f);
                    rivalRightHandTransform.localPosition = new Vector3(0.16f, 0.12f + Mathf.Cos(t * 14f) * 0.045f, 0.10f);
                    break;

                case AvatarMood.AngrySlam:
                    rivalHeadTransform.localRotation = Quaternion.Euler(8f, Mathf.Sin(t * 22f) * 18f, 0f);
                    float slamY = 0.01f + Mathf.Abs(Mathf.Sin(t * 16f)) * 0.06f;
                    rivalLeftHandTransform.localPosition = new Vector3(-0.15f, slamY, 0.14f);
                    rivalRightHandTransform.localPosition = new Vector3(0.15f, slamY, 0.14f);
                    break;

                default:
                    rivalHeadTransform.localRotation = Quaternion.Euler(Mathf.Sin(t * 1.5f) * 4f, Mathf.Cos(t * 1.1f) * 8f, 0f);
                    rivalLeftHandTransform.localPosition = new Vector3(-0.16f, 0.02f + Mathf.Sin(t * 2.5f) * 0.01f, 0.10f);
                    rivalRightHandTransform.localPosition = new Vector3(0.16f, 0.02f + Mathf.Cos(t * 2.5f) * 0.01f, 0.10f);
                    break;
            }
        }
    }

    public void TryExecuteHostileTakeover()
    {
        BoardTile currentTile = board[playerPos[0]];
        BoardTile targetTile = null;

        if (currentTile.owner == 1 && (currentTile.type == TileType.Property || currentTile.type == TileType.Railroad || currentTile.type == TileType.Utility))
        {
            targetTile = currentTile;
        }
        else
        {
            for (int i = 0; i < 40; i++)
            {
                if (board[i].owner == 1 && board[i].colorGroup >= 0 && PlayerOwnsAnyInGroup(0, board[i].colorGroup))
                {
                    targetTile = board[i];
                    break;
                }
            }
            if (targetTile == null)
            {
                for (int i = 0; i < 40; i++)
                {
                    if (board[i].owner == 1)
                    {
                        targetTile = board[i];
                        break;
                    }
                }
            }
        }

        if (targetTile == null)
        {
            RefreshDashboard("NO RIVAL PROPERTY FOR TAKEOVER", "Mogul AI doesn't own any properties yet! Roll and play on.");
            return;
        }

        int buyoutPrice = (targetTile.price * 2) + (targetTile.buildingLevel * targetTile.buildCost);
        if (playerCash[0] < buyoutPrice)
        {
            RefreshDashboard($"HOSTILE TAKEOVER NEEDS ${buyoutPrice}", $"You need ${buyoutPrice} to force Mogul AI to sell {targetTile.name}!");
            SetRivalReaction(AvatarMood.HappyCheer, $"Haha! You can't afford to buy out {targetTile.name}!");
            return;
        }

        playerCash[0] -= buyoutPrice;
        playerCash[1] += buyoutPrice;
        targetTile.owner = 0;

        PlaySFX(cashRegisterSound, procCashClip, 1.3f);
        PlaySFX(buildUpgradeSound, procBuildClip, 1.15f);
        StartCoroutine(SpawnFlyingCoinsStream(playerTokens[0].position, playerTokens[1].position, 10));

        Color playerColor = new Color(0.18f, 0.85f, 0.45f);
        if (targetTile.tileRenderer != null)
        {
            targetTile.tileRenderer.material = CreateColoredMaterial(playerColor, 0.85f);
        }
        if (targetTile.ownershipBadge != null)
        {
            targetTile.ownershipBadge.GetComponent<Renderer>().material = CreateColoredMaterial(playerColor, 0.95f);
        }

        RebuildMonopolySynergyBeams();
        SetRivalReaction(AvatarMood.AngrySlam, $"NO! You seized {targetTile.name} in a Hostile Takeover!");
        ShowSpecialEventCard("HOSTILE TAKEOVER!", $"You paid ${buyoutPrice} to seize\n<b>{targetTile.name}</b>\nfrom Mogul AI!", new Color(0.95f, 0.74f, 0.15f));
        RefreshDashboard($"HOSTILE TAKEOVER: SEIZED {targetTile.name}!", $"Paid ${buyoutPrice} buyout to Mogul AI! Property is now YOURS!");
    }

    bool PlayerOwnsAnyInGroup(int playerIdx, int colorGroup)
    {
        if (colorGroup < 0) return false;
        for (int i = 0; i < 40; i++)
        {
            if (board[i].colorGroup == colorGroup && board[i].owner == playerIdx) return true;
        }
        return false;
    }

    IEnumerator SpawnFlyingCoinsStream(Vector3 startWorldPos, Vector3 endWorldPos, int coinCount = 8)
    {
        Material goldCoinMat = CreateColoredMaterial(new Color(1f, 0.84f, 0.20f), 0.95f, 0.85f);
        List<Transform> coins = new List<Transform>();

        for (int i = 0; i < coinCount; i++)
        {
            GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coin.name = "Flying_Gold_Coin";
            coin.transform.localScale = new Vector3(0.022f, 0.004f, 0.022f) * currentBoardScale;
            coin.transform.position = startWorldPos;
            Destroy(coin.GetComponent<Collider>());
            coin.GetComponent<Renderer>().material = goldCoinMat;
            coins.Add(coin.transform);
        }

        float duration = 0.55f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            for (int i = 0; i < coins.Count; i++)
            {
                if (coins[i] == null) continue;
                float delay = i * 0.04f;
                float t = Mathf.Clamp01((elapsed - delay) / (duration - delay));
                float arc = Mathf.Sin(t * Mathf.PI) * (0.14f * currentBoardScale);
                coins[i].position = Vector3.Lerp(startWorldPos, endWorldPos, t) + new Vector3(0f, arc, 0f);
                coins[i].Rotate(new Vector3(360f, 540f, 180f) * Time.deltaTime);
            }
            yield return null;
        }

        for (int i = 0; i < coins.Count; i++)
        {
            if (coins[i] != null) Destroy(coins[i].gameObject);
        }
    }

    void RebuildMonopolySynergyBeams()
    {
        if (monopolyBeamsContainer == null) return;
        foreach (Transform child in monopolyBeamsContainer.transform)
        {
            Destroy(child.gameObject);
        }

        for (int group = 0; group < 8; group++)
        {
            int groupOwner = -1;
            if (OwnsFullColorGroup(0, group)) groupOwner = 0;
            else if (OwnsFullColorGroup(1, group)) groupOwner = 1;

            if (groupOwner != -1)
            {
                List<Transform> groupTiles = new List<Transform>();
                for (int i = 0; i < 40; i++)
                {
                    if (board[i].colorGroup == group) groupTiles.Add(board[i].tileTransform);
                }

                Color beamColor = (groupOwner == 0) ? new Color(1f, 0.86f, 0.22f) : new Color(0.95f, 0.25f, 0.65f);
                Material beamMat = CreateColoredMaterial(beamColor, 0.95f);

                for (int j = 0; j < groupTiles.Count - 1; j++)
                {
                    Vector3 p1 = groupTiles[j].localPosition + new Vector3(0f, 0.028f, 0f);
                    Vector3 p2 = groupTiles[j + 1].localPosition + new Vector3(0f, 0.028f, 0f);
                    Vector3 mid = (p1 + p2) * 0.5f;
                    float dist = Vector3.Distance(p1, p2);

                    GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    beam.name = $"Monopoly_Synergy_Beam_G{group}";
                    beam.transform.SetParent(monopolyBeamsContainer.transform, false);
                    beam.transform.localPosition = mid;
                    beam.transform.localRotation = Quaternion.LookRotation(p2 - p1);
                    beam.transform.localScale = new Vector3(0.012f, 0.008f, dist);
                    Destroy(beam.GetComponent<Collider>());
                    beam.GetComponent<Renderer>().material = beamMat;
                }
            }
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

    void ToggleRooftopVantagePoint()
    {
        Transform eye = Camera.main != null ? Camera.main.transform : null;
        if (!isInPenthouseVR || eye == null) return;
        isStandingOnRooftop = !isStandingOnRooftop;

        if (isStandingOnRooftop)
        {
            BoardTile bestTile = board[39];
            int bestLvl = -1;
            for (int i = 0; i < 40; i++)
            {
                if (board[i].owner == 0 && board[i].buildingLevel > bestLvl)
                {
                    bestLvl = board[i].buildingLevel;
                    bestTile = board[i];
                }
            }

            Vector3 tileLocal = bestTile.tileTransform.localPosition;
            float rooftopHeightOffset = 2.8f + Mathf.Max(1, bestTile.buildingLevel) * 0.65f;
            transform.position = eye.position - (transform.rotation * (tileLocal * 14f)) - new Vector3(0f, rooftopHeightOffset, 0f);
            PlaySFX(buildUpgradeSound, procBuildClip, 1.25f);
            RefreshDashboard($"PENTHOUSE ROOFTOP: {bestTile.name}", "Look down over your skyline! Left Pinch for Plaza | Right Pinch to Exit.");
        }
        else
        {
            transform.position = new Vector3(eye.position.x, eye.position.y - 1.65f, eye.position.z);
            PlaySFX(tokenMoveSound, procStepClip, 0.9f);
            RefreshDashboard("CENTRAL PARK STREET PLAZA", "Left Pinch for Rooftop Terrace | Right Pinch to Exit VR View.");
        }
    }

    void BuildLivingCenterCourtyard()
    {
        GameObject courtyardRoot = new GameObject("Inner_Metropolis_Courtyard");
        courtyardRoot.transform.SetParent(transform, false);

        Material asphaltMat = CreateColoredMaterial(new Color(0.14f, 0.16f, 0.19f), 0.45f);
        Material laneLineMat = CreateColoredMaterial(new Color(0.95f, 0.80f, 0.25f), 0.7f);
        Material parkLawnMat = CreateColoredMaterial(new Color(0.14f, 0.48f, 0.24f), 0.35f);
        Material helipadMat = CreateColoredMaterial(new Color(0.20f, 0.23f, 0.28f), 0.75f);

        GameObject roadPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roadPlate.name = "Inner_Ring_Road_Asphalt";
        roadPlate.transform.SetParent(courtyardRoot.transform, false);
        roadPlate.transform.localPosition = new Vector3(0f, 0.021f, 0f);
        roadPlate.transform.localScale = new Vector3(0.76f, 0.004f, 0.76f);
        // Keep BoxCollider on roadPlate and parkIsland so PhysicalThrowDie bounces on the courtyard!
        roadPlate.GetComponent<Renderer>().material = asphaltMat;

        GameObject parkIsland = GameObject.CreatePrimitive(PrimitiveType.Cube);
        parkIsland.name = "Central_Park_Plaza";
        parkIsland.transform.SetParent(courtyardRoot.transform, false);
        parkIsland.transform.localPosition = new Vector3(0f, 0.024f, 0f);
        parkIsland.transform.localScale = new Vector3(0.56f, 0.008f, 0.56f);
        parkIsland.GetComponent<Renderer>().material = parkLawnMat;

        GameObject parkCurb = GameObject.CreatePrimitive(PrimitiveType.Cube);
        parkCurb.name = "Central_Park_Gold_Curb";
        parkCurb.transform.SetParent(courtyardRoot.transform, false);
        parkCurb.transform.localPosition = new Vector3(0f, 0.023f, 0f);
        parkCurb.transform.localScale = new Vector3(0.58f, 0.006f, 0.58f);
        Destroy(parkCurb.GetComponent<Collider>());
        parkCurb.GetComponent<Renderer>().material = laneLineMat;

        GameObject helipad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        helipad.name = "VIP_Dice_Helipad";
        helipad.transform.SetParent(courtyardRoot.transform, false);
        helipad.transform.localPosition = new Vector3(0f, 0.029f, -0.05f);
        helipad.transform.localScale = new Vector3(0.24f, 0.003f, 0.24f);
        Destroy(helipad.GetComponent<Collider>());
        helipad.GetComponent<Renderer>().material = helipadMat;

        GameObject jackpotObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        jackpotObj.name = "Free_Parking_Gold_Vault";
        jackpotObj.transform.SetParent(courtyardRoot.transform, false);
        jackpotObj.transform.localPosition = new Vector3(-0.22f, 0.038f, 0.22f);
        jackpotObj.transform.localScale = new Vector3(0.055f, 0.018f, 0.055f);
        Destroy(jackpotObj.GetComponent<Collider>());
        jackpotObj.GetComponent<Renderer>().material = CreateColoredMaterial(new Color(1f, 0.85f, 0.20f), 0.95f, 0.9f);
        jackpotGoldPile = jackpotObj.transform;

        SpawnVIPHelicopter(courtyardRoot.transform);

        if (parkCenterpiecePrefab != null && parkCenterpiecePrefab.GetComponent<OVRCameraRig>() == null)
        {
            GameObject cp = Instantiate(parkCenterpiecePrefab, courtyardRoot.transform);
            StripRoguePhysicsFromImportedPrefab(cp);
            cp.transform.localPosition = new Vector3(0f, 0.03f, 0.14f);
            NormalizePrefabSize(cp, 0.085f);
        }

        GameObject auraRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        auraRing.name = "MarketAuraRing";
        auraRing.transform.SetParent(courtyardRoot.transform, false);
        auraRing.transform.localPosition = new Vector3(0f, 0.040f, 0f);
        auraRing.transform.localScale = new Vector3(0.55f, 0.002f, 0.55f);
        Destroy(auraRing.GetComponent<Collider>());
        auraRing.GetComponent<Renderer>().material = CreateColoredMaterial(new Color(1f, 0.82f, 0.15f), 0.95f, 0.9f);
        marketAuraRing = auraRing;
        marketAuraRing.SetActive(currentMarketCycle == MarketCycle.BullBoom);

        Vector3[] treeSpots = new Vector3[]
        {
            new Vector3(-0.22f, 0.028f, -0.22f),
            new Vector3( 0.22f, 0.028f, -0.22f),
            new Vector3( 0.22f, 0.028f,  0.22f),
            new Vector3(-0.22f, 0.028f,  0.00f),
            new Vector3( 0.22f, 0.028f,  0.00f),
            new Vector3( 0.00f, 0.028f, -0.22f),
            new Vector3( 0.00f, 0.028f,  0.22f)
        };

        for (int i = 0; i < treeSpots.Length; i++)
        {
            SpawnParkTree(courtyardRoot.transform, treeSpots[i], i);
        }

        Vector3[] lampSpots = new Vector3[]
        {
            new Vector3(-0.27f, 0.028f, -0.27f),
            new Vector3( 0.27f, 0.028f, -0.27f),
            new Vector3(-0.27f, 0.028f,  0.27f),
            new Vector3( 0.27f, 0.028f,  0.27f)
        };

        for (int i = 0; i < lampSpots.Length; i++)
        {
            SpawnStreetlight(courtyardRoot.transform, lampSpots[i], i);
        }

        for (int i = 0; i < 4; i++)
        {
            SpawnAmbientTrafficCar(courtyardRoot.transform, i, i * 0.25f);
        }
    }

    void SpawnVIPHelicopter(Transform parent)
    {
        GameObject chopperHolder = new GameObject("VIP_Executive_Chopper");
        chopperHolder.transform.SetParent(parent, false);
        chopperHolder.transform.localPosition = new Vector3(0f, 0.20f, 0f);
        vipChopperTransform = chopperHolder.transform;

        if (vipHelicopterPrefab != null && vipHelicopterPrefab.GetComponent<OVRCameraRig>() == null)
        {
            GameObject model = Instantiate(vipHelicopterPrefab, chopperHolder.transform);
            StripRoguePhysicsFromImportedPrefab(model);
            model.transform.localPosition = Vector3.zero;
            NormalizePrefabSize(model, 0.055f);
            return;
        }

        GameObject fuselage = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        fuselage.transform.SetParent(chopperHolder.transform, false);
        fuselage.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        fuselage.transform.localScale = new Vector3(0.018f, 0.026f, 0.018f);
        Destroy(fuselage.GetComponent<Collider>());
        fuselage.GetComponent<Renderer>().material = CreateColoredMaterial(new Color(0.10f, 0.12f, 0.16f), 0.92f, 0.6f);

        GameObject tailBoom = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tailBoom.transform.SetParent(chopperHolder.transform, false);
        tailBoom.transform.localPosition = new Vector3(0f, 0.002f, -0.025f);
        tailBoom.transform.localScale = new Vector3(0.004f, 0.005f, 0.028f);
        Destroy(tailBoom.GetComponent<Collider>());
        tailBoom.GetComponent<Renderer>().material = brushedGoldMaterial;

        GameObject rotor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rotor.name = "Main_Rotor";
        rotor.transform.SetParent(chopperHolder.transform, false);
        rotor.transform.localPosition = new Vector3(0f, 0.012f, 0f);
        rotor.transform.localScale = new Vector3(0.055f, 0.0015f, 0.005f);
        Destroy(rotor.GetComponent<Collider>());
        rotor.GetComponent<Renderer>().material = brushedGoldMaterial;
        chopperRotorTransform = rotor.transform;
    }

    void SpawnParkTree(Transform parent, Vector3 localPos, int index)
    {
        GameObject holder = new GameObject($"Park_Tree_{index}");
        holder.transform.SetParent(parent, false);
        holder.transform.localPosition = localPos;

        if (parkTreePrefab != null && parkTreePrefab.GetComponent<OVRCameraRig>() == null)
        {
            GameObject tree = Instantiate(parkTreePrefab, holder.transform);
            StripRoguePhysicsFromImportedPrefab(tree);
            tree.transform.localPosition = Vector3.zero;
            NormalizePrefabSize(tree, 0.065f);
            return;
        }

        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.transform.SetParent(holder.transform, false);
        trunk.transform.localPosition = new Vector3(0f, 0.015f, 0f);
        trunk.transform.localScale = new Vector3(0.008f, 0.015f, 0.008f);
        Destroy(trunk.GetComponent<Collider>());
        trunk.GetComponent<Renderer>().material = CreateColoredMaterial(new Color(0.42f, 0.26f, 0.14f), 0.4f);

        GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        foliage.transform.SetParent(holder.transform, false);
        foliage.transform.localPosition = new Vector3(0f, 0.042f, 0f);
        foliage.transform.localScale = new Vector3(0.032f, 0.048f, 0.032f);
        Destroy(foliage.GetComponent<Collider>());
        foliage.GetComponent<Renderer>().material = CreateColoredMaterial(new Color(0.18f, 0.68f, 0.32f), 0.5f);
    }

    void SpawnStreetlight(Transform parent, Vector3 localPos, int index)
    {
        GameObject holder = new GameObject($"Streetlight_{index}");
        holder.transform.SetParent(parent, false);
        holder.transform.localPosition = localPos;

        if (streetlightPrefab != null && streetlightPrefab.GetComponent<OVRCameraRig>() == null)
        {
            GameObject lamp = Instantiate(streetlightPrefab, holder.transform);
            StripRoguePhysicsFromImportedPrefab(lamp);
            lamp.transform.localPosition = Vector3.zero;
            NormalizePrefabSize(lamp, 0.06f);
        }
        else
        {
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.transform.SetParent(holder.transform, false);
            pole.transform.localPosition = new Vector3(0f, 0.028f, 0f);
            pole.transform.localScale = new Vector3(0.004f, 0.028f, 0.004f);
            Destroy(pole.GetComponent<Collider>());
            pole.GetComponent<Renderer>().material = brushedGoldMaterial;

            GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.transform.SetParent(holder.transform, false);
            bulb.transform.localPosition = new Vector3(0f, 0.058f, 0f);
            bulb.transform.localScale = Vector3.one * 0.012f;
            Destroy(bulb.GetComponent<Collider>());
            bulb.GetComponent<Renderer>().material = CreateColoredMaterial(new Color(1f, 0.92f, 0.65f), 0.95f);
        }

        GameObject lightGO = new GameObject("Warm_City_Light");
        lightGO.transform.SetParent(holder.transform, false);
        lightGO.transform.localPosition = new Vector3(0f, 0.065f, 0f);
        Light ptLight = lightGO.AddComponent<Light>();
        ptLight.type = LightType.Point;
        ptLight.color = new Color(1f, 0.85f, 0.55f);
        ptLight.range = 0.45f;
        ptLight.intensity = 0.06f;
        cityNightLights.Add(ptLight);
    }

    void SpawnAmbientTrafficCar(Transform parent, int index, float startProgress)
    {
        GameObject holder = new GameObject($"Ambient_City_Car_{index}");
        holder.transform.SetParent(parent, false);

        GameObject prefabToUse = (index % 2 == 0) ? ambientTrafficCar1Prefab : ambientTrafficCar2Prefab;
        if (prefabToUse == null) prefabToUse = ambientTrafficCar1Prefab != null ? ambientTrafficCar1Prefab : playerTokenPrefab;

        if (prefabToUse != null && prefabToUse.GetComponent<OVRCameraRig>() == null)
        {
            GameObject model = Instantiate(prefabToUse, holder.transform);
            StripRoguePhysicsFromImportedPrefab(model);
            model.transform.localPosition = Vector3.zero;
            NormalizePrefabSize(model, 0.032f);
        }
        else
        {
            GameObject carBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            carBody.transform.SetParent(holder.transform, false);
            carBody.transform.localScale = new Vector3(0.014f, 0.008f, 0.026f);
            Destroy(carBody.GetComponent<Collider>());
            carBody.GetComponent<Renderer>().material = CreateColoredMaterial(new Color(0.9f, 0.75f, 0.2f), 0.9f);
        }

        ambientCars.Add(holder.transform);
        ambientCarProgress.Add(startProgress);
    }

    void UpdateAmbientCourtyardTrafficAndChopper()
    {
        if (vipChopperTransform != null)
        {
            float angle = Time.time * 0.65f;
            float radius = 0.20f;
            Vector3 nextPos = new Vector3(Mathf.Cos(angle) * radius, 0.20f + Mathf.Sin(Time.time * 2f) * 0.012f, Mathf.Sin(angle) * radius);
            Vector3 tangent = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle)).normalized;
            vipChopperTransform.localPosition = nextPos;
            vipChopperTransform.localRotation = Quaternion.LookRotation(tangent);
        }
        if (chopperRotorTransform != null)
        {
            chopperRotorTransform.Rotate(Vector3.up, 1080f * Time.deltaTime, Space.Self);
        }

        float roadHalf = 0.33f;
        for (int i = 0; i < ambientCars.Count; i++)
        {
            if (ambientCars[i] == null) continue;
            ambientCarProgress[i] = (ambientCarProgress[i] + Time.deltaTime * 0.055f) % 1f;
            float p = ambientCarProgress[i] * 4f;
            int side = Mathf.FloorToInt(p);
            float t = p - side;

            Vector3 pos;
            Quaternion rot;
            switch (side)
            {
                case 0:
                    pos = new Vector3(Mathf.Lerp(-roadHalf, roadHalf, t), 0.026f, -roadHalf);
                    rot = Quaternion.Euler(0f, 90f, 0f);
                    break;
                case 1:
                    pos = new Vector3(roadHalf, 0.026f, Mathf.Lerp(-roadHalf, roadHalf, t));
                    rot = Quaternion.Euler(0f, 0f, 0f);
                    break;
                case 2:
                    pos = new Vector3(Mathf.Lerp(roadHalf, -roadHalf, t), 0.026f, roadHalf);
                    rot = Quaternion.Euler(0f, -90f, 0f);
                    break;
                default:
                    pos = new Vector3(-roadHalf, 0.026f, Mathf.Lerp(roadHalf, -roadHalf, t));
                    rot = Quaternion.Euler(0f, 180f, 0f);
                    break;
            }
            ambientCars[i].localPosition = pos;
            ambientCars[i].localRotation = rot;
        }
    }

    void Create3DDeedCardPopup()
    {
        deedCardHolder = new GameObject("3D_Title_Deed_Card");
        deedCardHolder.transform.SetParent(transform, false);
        deedCardHolder.transform.localPosition = new Vector3(0.38f, 0.46f, 0.15f);
        deedCardHolder.transform.localRotation = Quaternion.Euler(16f, -15f, 0f);

        GameObject cardBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cardBody.name = "Deed_Card_Slab";
        cardBody.transform.SetParent(deedCardHolder.transform, false);
        cardBody.transform.localScale = new Vector3(0.22f, 0.28f, 0.008f);
        Destroy(cardBody.GetComponent<Collider>());
        cardBody.GetComponent<Renderer>().material = CreateColoredMaterial(new Color(0.96f, 0.95f, 0.92f), 0.8f);

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

        // Parent Canvas directly to parent (deedCardHolder) at Z = -0.015f to avoid non-uniform aspect distortion!
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

        return btn.transform;
    }

    bool CheckMortgageButtonPoke()
    {
        if (mortgageButtonTransform == null || !mortgageButtonTransform.gameObject.activeInHierarchy) return false;
        if (Time.time < lastButtonTouchTime) return false;
        if (!isRightHandTracked && !isLeftHandTracked) return false;

        Collider col = mortgageButtonTransform.GetComponent<Collider>();
        if (col == null) return false;

        bool rightPoked = isRightHandTracked && rightHandWorldPos.y > -50f && Vector3.Distance(col.ClosestPoint(rightHandWorldPos), rightHandWorldPos) < 0.018f;
        bool leftPoked = isLeftHandTracked && leftHandWorldPos.y > -50f && Vector3.Distance(col.ClosestPoint(leftHandWorldPos), leftHandWorldPos) < 0.018f;

        if (rightPoked || leftPoked)
        {
            lastButtonTouchTime = Time.time + 0.50f;
            StartCoroutine(AnimateButtonPress(mortgageButtonTransform));
            return true;
        }
        return false;
    }

    bool CheckUpgradeButtonPoke()
    {
        if (upgradeCardButtonTransform == null || !upgradeCardButtonTransform.gameObject.activeInHierarchy) return false;
        if (Time.time < lastButtonTouchTime) return false;
        if (!isRightHandTracked && !isLeftHandTracked) return false;

        Collider col = upgradeCardButtonTransform.GetComponent<Collider>();
        if (col == null) return false;

        bool rightPoked = isRightHandTracked && rightHandWorldPos.y > -50f && Vector3.Distance(col.ClosestPoint(rightHandWorldPos), rightHandWorldPos) < 0.018f;
        bool leftPoked = isLeftHandTracked && leftHandWorldPos.y > -50f && Vector3.Distance(col.ClosestPoint(leftHandWorldPos), leftHandWorldPos) < 0.018f;

        if (rightPoked || leftPoked)
        {
            lastButtonTouchTime = Time.time + 0.50f;
            StartCoroutine(AnimateButtonPress(upgradeCardButtonTransform));
            return true;
        }
        return false;
    }

    int CheckPhysicalTilePoke()
    {
        if (Time.time < lastTilePokeTime) return -1;
        if (!isRightHandTracked && !isLeftHandTracked) return -1;

        for (int i = 0; i < 40; i++)
        {
            if (board[i] == null || board[i].tileTransform == null) continue;
            Collider col = board[i].tileTransform.GetComponent<Collider>();
            if (col == null) continue;

            bool rightPoked = isRightHandTracked && rightHandWorldPos.y > -50f && Vector3.Distance(col.ClosestPoint(rightHandWorldPos), rightHandWorldPos) < 0.018f;
            bool leftPoked = isLeftHandTracked && leftHandWorldPos.y > -50f && Vector3.Distance(col.ClosestPoint(leftHandWorldPos), leftHandWorldPos) < 0.018f;

            if (rightPoked || leftPoked)
            {
                lastTilePokeTime = Time.time + 0.50f;
                return i;
            }
        }
        return -1;
    }

    void ExecuteMortgageOrUnmortgage(BoardTile tile)
    {
        if (tile == null || tile.owner != 0) return;

        if (!tile.isMortgaged)
        {
            // Sell building level back to bank for 50% buildCost
            if (tile.buildingLevel > 0)
            {
                int refund = tile.buildCost / 2;
                playerCash[0] += refund;
                tile.buildingLevel--;
                if (tile.spawnedBuilding != null) Destroy(tile.spawnedBuilding);
                if (tile.buildingLevel > 0) ConstructChronological3DBuilding(tile);
                PlaySFX(cashRegisterSound, procBuildClip, 1.1f);
                RefreshDashboard($"SOLD BUILDING ON {tile.name} (+${refund})", $"Building level reduced to {tile.buildingLevel}/5. Cash: ${playerCash[0]}");
            }
            else
            {
                // Mortgage deed for +50% of price
                int mortgageValue = tile.price / 2;
                playerCash[0] += mortgageValue;
                tile.isMortgaged = true;
                if (tile.ownershipBadge != null)
                {
                    Renderer r = tile.ownershipBadge.GetComponent<Renderer>();
                    if (r != null) r.material = CreateColoredMaterial(new Color(0.48f, 0.12f, 0.16f), 0.95f);
                }
                PlaySFX(cashRegisterSound, procDiceClip, 0.95f);
                RefreshDashboard($"MORTGAGED {tile.name} (+${mortgageValue})", $"Property mortgaged! Rent is now $0. Cash: ${playerCash[0]}");
            }
        }
        else
        {
            // Unmortgage: deduct 50% principal + 10% interest = 55% of price
            int unmortgageCost = (int)(tile.price * 0.55f);
            if (playerCash[0] < unmortgageCost)
            {
                RefreshDashboard($"NEED ${unmortgageCost} TO UNMORTGAGE", $"Insufficient cash to unmortgage {tile.name}.");
                return;
            }

            playerCash[0] -= unmortgageCost;
            tile.isMortgaged = false;
            if (tile.ownershipBadge != null)
            {
                Renderer r = tile.ownershipBadge.GetComponent<Renderer>();
                if (r != null) r.material = CreateColoredMaterial(new Color(0.18f, 0.85f, 0.45f), 0.95f);
            }
            PlaySFX(cashRegisterSound, procStepClip, 1.15f);
            RefreshDashboard($"UNMORTGAGED {tile.name} (-${unmortgageCost})", $"Property active! Full rent restored. Cash: ${playerCash[0]}");
        }

        ShowDeedCardForTile(tile);
        UpdateLiveHUDStats();
        SaveGameState();
    }

    void ShowDeedCardForTile(BoardTile tile)
    {
        if (deedCardHolder == null || tile == null) return;
        deedCardHolder.SetActive(true);
        deedHeaderRenderer.material = CreateColoredMaterial(GetOfficialColor(tile), 0.85f);
        deedTitleTMP.text = tile.name;

        if (grabbableDeedCrystal != null && tile.owner == -1)
        {
            grabbableDeedCrystal.SetActive(true);
            grabbableDeedCrystal.transform.localPosition = new Vector3(0.24f, 0.24f, -0.18f);
            grabbableDeedCrystal.GetComponent<Renderer>().material = CreateColoredMaterial(GetOfficialColor(tile), 0.95f, 0.7f);
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
            }
            else
            {
                mortgageButtonTransform.gameObject.SetActive(false);
                if (mortgageBtnCanvasGO != null) mortgageBtnCanvasGO.SetActive(false);
                if (upgradeCardButtonTransform != null) upgradeCardButtonTransform.gameObject.SetActive(false);
                if (upgradeBtnCanvasGO != null) upgradeBtnCanvasGO.SetActive(false);
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
    }

    void HideDeedCard()
    {
        if (deedCardHolder != null) deedCardHolder.SetActive(false);
        if (grabbableDeedCrystal != null) grabbableDeedCrystal.SetActive(false);
        if (mortgageButtonTransform != null) mortgageButtonTransform.gameObject.SetActive(false);
        if (mortgageBtnCanvasGO != null) mortgageBtnCanvasGO.SetActive(false);
        if (upgradeCardButtonTransform != null) upgradeCardButtonTransform.gameObject.SetActive(false);
        if (upgradeBtnCanvasGO != null) upgradeBtnCanvasGO.SetActive(false);
        selectedTileIndex = -1;
    }

    Quaternion GetFlatDieRotationForValue(int faceValue)
    {
        switch (faceValue)
        {
            case 1: return Quaternion.Euler(0f, 0f, 0f);
            case 2: return Quaternion.Euler(-90f, 0f, 0f);
            case 3: return Quaternion.Euler(0f, 0f, 90f);
            case 4: return Quaternion.Euler(0f, 0f, -90f);
            case 5: return Quaternion.Euler(90f, 0f, 0f);
            default: return Quaternion.Euler(180f, 0f, 0f);
        }
    }

    Transform CreateExact3DPipsDie(string name, Vector3 localPos, out PhysicalThrowDie physDieComp)
    {
        Material ivoryMat = CreateColoredMaterial(new Color(0.97f, 0.96f, 0.92f), 0.85f);
        Material pipMat = CreateColoredMaterial(new Color(0.06f, 0.06f, 0.08f), 0.9f);

        GameObject dieRoot = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dieRoot.name = name;
        dieRoot.transform.SetParent(transform, false);
        dieRoot.transform.localPosition = localPos;
        dieRoot.transform.localScale = Vector3.one * 0.045f;
        dieRoot.GetComponent<Renderer>().material = ivoryMat;

        float o = 0.505f;
        float s = 0.25f;

        AddPip(dieRoot.transform, pipMat, new Vector3(0, o, 0));

        AddPip(dieRoot.transform, pipMat, new Vector3(-s, -o, -s));
        AddPip(dieRoot.transform, pipMat, new Vector3(-s, -o, 0));
        AddPip(dieRoot.transform, pipMat, new Vector3(-s, -o, s));
        AddPip(dieRoot.transform, pipMat, new Vector3(s, -o, -s));
        AddPip(dieRoot.transform, pipMat, new Vector3(s, -o, 0));
        AddPip(dieRoot.transform, pipMat, new Vector3(s, -o, s));

        AddPip(dieRoot.transform, pipMat, new Vector3(-s, s, o));
        AddPip(dieRoot.transform, pipMat, new Vector3(s, -s, o));

        AddPip(dieRoot.transform, pipMat, new Vector3(0, 0, -o));
        AddPip(dieRoot.transform, pipMat, new Vector3(-s, -s, -o));
        AddPip(dieRoot.transform, pipMat, new Vector3(-s, s, -o));
        AddPip(dieRoot.transform, pipMat, new Vector3(s, -s, -o));
        AddPip(dieRoot.transform, pipMat, new Vector3(s, s, -o));

        AddPip(dieRoot.transform, pipMat, new Vector3(o, 0, 0));
        AddPip(dieRoot.transform, pipMat, new Vector3(o, s, s));
        AddPip(dieRoot.transform, pipMat, new Vector3(o, -s, -s));

        AddPip(dieRoot.transform, pipMat, new Vector3(-o, -s, -s));
        AddPip(dieRoot.transform, pipMat, new Vector3(-o, -s, s));
        AddPip(dieRoot.transform, pipMat, new Vector3(-o, s, -s));
        AddPip(dieRoot.transform, pipMat, new Vector3(-o, s, s));

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

        // Attach native building block grab setup to dice
        AttachNativeBuildingBlockGrab(die1Transform.gameObject, die1Transform.GetComponent<Rigidbody>());
        AttachNativeBuildingBlockGrab(die2Transform.gameObject, die2Transform.GetComponent<Rigidbody>());
        if (physDie1 != null) physDie1.InjectMetaGrabbable(die1Transform.GetComponent<Grabbable>());
        if (physDie2 != null) physDie2.InjectMetaGrabbable(die2Transform.GetComponent<Grabbable>());

        GameObject dashHolder = new GameObject("Executive_Holo_Dashboard");
        dashHolder.transform.SetParent(transform, false);
        dashHolder.transform.localPosition = new Vector3(0f, 0.48f, 0.22f);
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

        dashboardStatsTMP = CreateUGUIText(canvasGO.transform, "LiveStats", new Vector2(0, 130), 22, new Color(0.35f, 0.95f, 0.60f));
        dashboardStatsTMP.rectTransform.sizeDelta = new Vector2(1000f, 60f);

        dashboardTitleTMP = CreateUGUIText(canvasGO.transform, "Title", new Vector2(0, 65), 32, new Color(1f, 0.84f, 0.32f));
        dashboardBodyTMP = CreateUGUIText(canvasGO.transform, "Body", new Vector2(0, -10), 27, Color.white);
        dashboardPromptTMP = CreateUGUIText(canvasGO.transform, "Prompt", new Vector2(0, -90), 22, new Color(0.45f, 0.90f, 1f));

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

        string[] utilLabels = { "- SHRINK", "+ GROW", "RECENTER" };
        Color[] utilColors = {
            new Color(0.28f, 0.32f, 0.40f),
            new Color(0.28f, 0.32f, 0.40f),
            new Color(0.85f, 0.52f, 0.15f)
        };

        for (int j = 0; j < 3; j++)
        {
            buttonTransforms[6 + j] = CreateSingle3DButton(
                utilLabels[j],
                utilColors[j],
                new Vector3((j - 1) * 0.20f, 0.020f, frontZ - 0.075f),
                new Vector3(0.16f, 0.025f, 0.055f)
            );
        }
    }

// 2. Replace CreateSingle3DButton() and CheckPhysicalButtonTouch() with Collider-Based Native Fingertip Poking:
    Transform CreateSingle3DButton(string label, Color color, Vector3 localPos, Vector3 localScale)
    {
        GameObject btn = GameObject.CreatePrimitive(PrimitiveType.Cube);
        btn.name = $"TouchButton_{label}";
        btn.transform.SetParent(transform, false);
        btn.transform.localPosition = localPos;
        btn.transform.localScale = localScale;

        // Keep BoxCollider as a Trigger so Meta's PokeInteractor fingertip collider intersects the exact 3D button box!
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

    int CheckPhysicalButtonTouch()
    {
        if (Time.time < lastButtonTouchTime) return -1;
        if (!isRightHandTracked && !isLeftHandTracked) return -1;

        for (int i = 0; i < buttonTransforms.Length; i++)
        {
            if (buttonTransforms[i] == null) continue;
            Collider btnCol = buttonTransforms[i].GetComponent<Collider>();
            if (btnCol == null) continue;

            // Exact 3D BoxCollider surface poke check using Meta's PokeInteractor fingertip origin!
            bool rightPoked = isRightHandTracked && Vector3.Distance(btnCol.ClosestPoint(rightHandWorldPos), rightHandWorldPos) < 0.018f;
            bool leftPoked = isLeftHandTracked && Vector3.Distance(btnCol.ClosestPoint(leftHandWorldPos), leftHandWorldPos) < 0.018f;

            if (rightPoked || leftPoked)
            {
                lastButtonTouchTime = Time.time + 0.50f;
                StartCoroutine(AnimateButtonPress(buttonTransforms[i]));
                return i;
            }
        }
        return -1;
    }


    IEnumerator AnimateButtonPress(Transform btn)
    {
        Vector3 origPos = btn.localPosition;
        btn.localPosition = origPos - new Vector3(0f, 0.012f, 0f);
        yield return new WaitForSeconds(0.18f);
        btn.localPosition = origPos;
    }

    void GenerateProceduralSFX()
    {
        int sampleRate = 44100;

        float[] diceSamples = new float[sampleRate / 8];
        for (int i = 0; i < diceSamples.Length; i++)
        {
            float env = 1f - ((float)i / diceSamples.Length);
            diceSamples[i] = (Random.Range(-1f, 1f) * 0.4f + Mathf.Sin(i * 0.18f) * 0.6f) * env * env;
        }
        procDiceClip = AudioClip.Create("ProcDice", diceSamples.Length, 1, sampleRate, false);
        procDiceClip.SetData(diceSamples, 0);

        float[] stepSamples = new float[sampleRate / 12];
        for (int i = 0; i < stepSamples.Length; i++)
        {
            float t = (float)i / sampleRate;
            float env = 1f - ((float)i / stepSamples.Length);
            stepSamples[i] = Mathf.Sin(2f * Mathf.PI * (320f + i * 0.08f) * t) * env * 0.5f;
        }
        procStepClip = AudioClip.Create("ProcStep", stepSamples.Length, 1, sampleRate, false);
        procStepClip.SetData(stepSamples, 0);

        float[] cashSamples = new float[sampleRate / 3];
        for (int i = 0; i < cashSamples.Length; i++)
        {
            float t = (float)i / sampleRate;
            float freq = (i < cashSamples.Length / 2) ? 880f : 1318.5f;
            float env = 1f - ((float)i / cashSamples.Length);
            cashSamples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.45f;
        }
        procCashClip = AudioClip.Create("ProcCash", cashSamples.Length, 1, sampleRate, false);
        procCashClip.SetData(cashSamples, 0);

        float[] buildSamples = new float[sampleRate / 2];
        for (int i = 0; i < buildSamples.Length; i++)
        {
            float t = (float)i / sampleRate;
            float env = 1f - ((float)i / buildSamples.Length);
            float f1 = Mathf.Lerp(261.6f, 523.2f, (float)i / buildSamples.Length);
            buildSamples[i] = (Mathf.Sin(2f * Mathf.PI * f1 * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * f1 * 1.5f * t)) * env * 0.35f;
        }
        procBuildClip = AudioClip.Create("ProcBuild", buildSamples.Length, 1, sampleRate, false);
        procBuildClip.SetData(buildSamples, 0);
    }

    void PlaySFX(AudioClip customClip, AudioClip fallbackClip, float pitch = 1f)
    {
        if (sfxSource == null) return;
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(customClip != null ? customClip : fallbackClip);
    }

    void InitializeOfficialMonopolyMap()
    {
        SetTile(0, "GO (COLLECT $200)", TileType.Go, -1, 0, 0, null);
        SetTile(1, "MEDITERRANEAN AVE", TileType.Property, 0, 60, 50, new int[] { 2, 10, 30, 90, 160, 250 });
        SetTile(2, "COMMUNITY CHEST", TileType.Chest, -1, 0, 0, null);
        SetTile(3, "BALTIC AVE", TileType.Property, 0, 60, 50, new int[] { 4, 20, 60, 180, 320, 450 });
        SetTile(4, "INCOME TAX ($200)", TileType.Tax, -1, 200, 0, null);
        SetTile(5, "READING RAILROAD", TileType.Railroad, -1, 200, 0, null);
        SetTile(6, "ORIENTAL AVE", TileType.Property, 1, 100, 50, new int[] { 6, 30, 90, 270, 400, 550 });
        SetTile(7, "CHANCE", TileType.Chance, -1, 0, 0, null);
        SetTile(8, "VERMONT AVE", TileType.Property, 1, 100, 50, new int[] { 6, 30, 90, 270, 400, 550 });
        SetTile(9, "CONNECTICUT AVE", TileType.Property, 1, 120, 50, new int[] { 8, 40, 100, 300, 450, 600 });

        SetTile(10, "JAIL / JUST VISITING", TileType.Jail, -1, 0, 0, null);
        SetTile(11, "ST. CHARLES PLACE", TileType.Property, 2, 140, 100, new int[] { 10, 50, 150, 450, 625, 750 });
        SetTile(12, "ELECTRIC COMPANY", TileType.Utility, -1, 150, 0, null);
        SetTile(13, "STATES AVE", TileType.Property, 2, 140, 100, new int[] { 10, 50, 150, 450, 625, 750 });
        SetTile(14, "VIRGINIA AVE", TileType.Property, 2, 160, 100, new int[] { 12, 60, 180, 500, 700, 900 });
        SetTile(15, "PENNSYLVANIA RR", TileType.Railroad, -1, 200, 0, null);
        SetTile(16, "ST. JAMES PLACE", TileType.Property, 3, 180, 100, new int[] { 14, 70, 200, 550, 750, 950 });
        SetTile(17, "COMMUNITY CHEST", TileType.Chest, -1, 0, 0, null);
        SetTile(18, "TENNESSEE AVE", TileType.Property, 3, 180, 100, new int[] { 14, 70, 200, 550, 750, 950 });
        SetTile(19, "NEW YORK AVE", TileType.Property, 3, 200, 100, new int[] { 16, 80, 220, 600, 800, 1000 });

        SetTile(20, "FREE PARKING", TileType.FreeParking, -1, 0, 0, null);
        SetTile(21, "KENTUCKY AVE", TileType.Property, 4, 220, 150, new int[] { 18, 90, 250, 700, 875, 1050 });
        SetTile(22, "CHANCE", TileType.Chance, -1, 0, 0, null);
        SetTile(23, "INDIANA AVE", TileType.Property, 4, 220, 150, new int[] { 18, 90, 250, 700, 875, 1050 });
        SetTile(24, "ILLINOIS AVE", TileType.Property, 4, 240, 150, new int[] { 20, 100, 300, 750, 925, 1100 });
        SetTile(25, "B. & O. RAILROAD", TileType.Railroad, -1, 200, 0, null);
        SetTile(26, "ATLANTIC AVE", TileType.Property, 5, 260, 150, new int[] { 22, 110, 330, 800, 975, 1150 });
        SetTile(27, "VENTNOR AVE", TileType.Property, 5, 260, 150, new int[] { 22, 110, 330, 800, 975, 1150 });
        SetTile(28, "WATER WORKS", TileType.Utility, -1, 150, 0, null);
        SetTile(29, "MARVIN GARDENS", TileType.Property, 5, 280, 150, new int[] { 24, 120, 360, 850, 1025, 1200 });

        SetTile(30, "GO TO JAIL", TileType.GoToJail, -1, 0, 0, null);
        SetTile(31, "PACIFIC AVE", TileType.Property, 6, 300, 200, new int[] { 26, 130, 390, 900, 1100, 1275 });
        SetTile(32, "NORTH CAROLINA AVE", TileType.Property, 6, 300, 200, new int[] { 26, 130, 390, 900, 1100, 1275 });
        SetTile(33, "COMMUNITY CHEST", TileType.Chest, -1, 0, 0, null);
        SetTile(34, "PENNSYLVANIA AVE", TileType.Property, 6, 320, 200, new int[] { 28, 150, 450, 1000, 1200, 1400 });
        SetTile(35, "SHORT LINE RR", TileType.Railroad, -1, 200, 0, null);
        SetTile(36, "CHANCE", TileType.Chance, -1, 0, 0, null);
        SetTile(37, "PARK PLACE", TileType.Property, 7, 350, 200, new int[] { 35, 175, 500, 1100, 1300, 1500 });
        SetTile(38, "LUXURY TAX ($100)", TileType.Tax, -1, 100, 0, null);
        SetTile(39, "BOARDWALK", TileType.Property, 7, 400, 200, new int[] { 50, 200, 600, 1400, 1700, 2000 });
    }

    void SetTile(int index, string name, TileType type, int colorGroup, int price, int buildCost, int[] rents)
    {
        board[index] = new BoardTile
        {
            index = index,
            name = name,
            type = type,
            colorGroup = colorGroup,
            price = price,
            buildCost = buildCost,
            rentByLevel = rents
        };
    }

    void Build40TileClockwiseTable()
    {
        if (darkMarbleMaterial == null)
            darkMarbleMaterial = CreateColoredMaterial(new Color(0.06f, 0.07f, 0.09f), 0.9f);

        if (brushedGoldMaterial == null)
            brushedGoldMaterial = CreateColoredMaterial(new Color(0.82f, 0.65f, 0.26f), 0.78f, 0.88f);

        GameObject tableBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tableBase.name = "Tycoon_Marble_Base";
        tableBase.transform.SetParent(transform, false);
        tableBase.transform.localScale = new Vector3(boardSize, 0.04f, boardSize);
        tableBase.GetComponent<Renderer>().material = darkMarbleMaterial;

        GameObject goldRim = GameObject.CreatePrimitive(PrimitiveType.Cube);
        goldRim.name = "Brushed_Gold_Bezel";
        goldRim.transform.SetParent(transform, false);
        goldRim.transform.localPosition = new Vector3(0, -0.005f, 0);
        goldRim.transform.localScale = new Vector3(boardSize + 0.04f, 0.03f, boardSize + 0.04f);
        Destroy(goldRim.GetComponent<Collider>());
        goldRim.GetComponent<Renderer>().material = brushedGoldMaterial;

        int gridCount = 11;
        float span = boardSize * 0.88f;
        float step = span / (gridCount - 1);
        float half = span / 2f;

        List<Vector3> tilePositions = new List<Vector3>();
        for (int i = 0; i < gridCount; i++) tilePositions.Add(new Vector3(half - i * step, 0.025f, -half));
        for (int i = 1; i < gridCount; i++) tilePositions.Add(new Vector3(-half, 0.025f, -half + i * step));
        for (int i = 1; i < gridCount; i++) tilePositions.Add(new Vector3(-half + i * step, 0.025f, half));
        for (int i = 1; i < gridCount - 1; i++) tilePositions.Add(new Vector3(half, 0.025f, half - i * step));

        for (int i = 0; i < 40; i++)
        {
            bool isCorner = (i % 10 == 0);
            GameObject plot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plot.name = $"Tile_{i}_{board[i].name}";
            plot.transform.SetParent(transform, false);
            plot.transform.localPosition = tilePositions[i];

            int side = i / 10;
            plot.transform.localRotation = Quaternion.Euler(0f, side * 90f, 0f);

            float width = isCorner ? step * 0.92f : step * 0.80f;
            float depth = isCorner ? step * 0.92f : step * 0.96f;
            plot.transform.localScale = new Vector3(width, 0.014f, depth);

            // Configure each tile's BoxCollider as a Trigger so fingertip poking detects it!
            BoxCollider tileCol = plot.GetComponent<BoxCollider>();
            if (tileCol != null) tileCol.isTrigger = true;

            Renderer plotRenderer = plot.GetComponent<Renderer>();
            plotRenderer.material = CreateColoredMaterial(new Color(0.82f, 0.65f, 0.26f), 0.78f, 0.5f);
            board[i].tileRenderer = plotRenderer;

            if (!isCorner && (board[i].colorGroup >= 0 || board[i].type == TileType.Railroad || board[i].type == TileType.Utility))
            {
                GameObject colorStrip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                colorStrip.name = "District_Color_Band";
                colorStrip.transform.SetParent(plot.transform, false);
                colorStrip.transform.localPosition = new Vector3(0f, 0.75f, 0.32f);
                colorStrip.transform.localScale = new Vector3(0.94f, 0.45f, 0.28f);
                Destroy(colorStrip.GetComponent<Collider>());
                colorStrip.GetComponent<Renderer>().material = CreateColoredMaterial(GetOfficialColor(board[i]), 0.9f);
            }

            board[i].tileTransform = plot.transform;
        }
    }

    Color GetOfficialColor(BoardTile tile)
    {
        if (tile.type == TileType.Railroad) return new Color(0.15f, 0.17f, 0.22f);
        if (tile.type == TileType.Utility) return new Color(0.88f, 0.92f, 0.98f);
        switch (tile.colorGroup)
        {
            case 0: return new Color(0.55f, 0.28f, 0.14f);
            case 1: return new Color(0.35f, 0.78f, 0.98f);
            case 2: return new Color(0.92f, 0.20f, 0.68f);
            case 3: return new Color(0.98f, 0.55f, 0.08f);
            case 4: return new Color(0.92f, 0.14f, 0.18f);
            case 5: return new Color(0.98f, 0.88f, 0.12f);
            case 6: return new Color(0.12f, 0.76f, 0.32f);
            case 7: return new Color(0.14f, 0.30f, 0.92f);
            default: return Color.gray;
        }
    }

    void CreatePlayerTokens()
    {
        playerTokens[0] = SpawnPlayerTokenInstance(0, playerTokenPrefab, "Player_Vehicle_Token", brushedGoldMaterial);
        Material silverMat = CreateColoredMaterial(new Color(0.80f, 0.85f, 0.92f), 0.85f, 0.9f);
        playerTokens[1] = SpawnPlayerTokenInstance(1, aiRivalTokenPrefab, "AIRival_Vehicle_Token", silverMat);

        PositionTokenOnTile(0, 0);
        PositionTokenOnTile(1, 0);
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

    void NormalizePrefabSize(GameObject obj, float targetMaxSize)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

        float maxDim = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
        if (maxDim > 0.0001f)
        {
            float scaleRatio = (targetMaxSize * transform.lossyScale.x) / maxDim;
            obj.transform.localScale *= scaleRatio;
        }
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
        if (playerTokens[playerIdx] == null || board[tileIdx] == null || board[tileIdx].tileTransform == null) return;
        Vector3 laneOffset = (playerIdx == 0) ? new Vector3(-0.012f, 0.018f, 0f) : new Vector3(0.012f, 0.018f, 0f);
        playerTokens[playerIdx].localPosition = board[tileIdx].tileTransform.localPosition + laneOffset;
        playerTokens[playerIdx].localRotation = GetClockwiseTravelRotation(tileIdx);
    }

    IEnumerator ExecutePhysicsDiceRollTurn(bool useImpulseToss)
    {
        currentState = TurnState.DiceTumbling;
        isPlayerHoldingDice = false;
        HideDeedCard();

        if (physDie1 != null && physDie2 != null)
        {
            if (useImpulseToss)
            {
                physDie1.TossWithPhysicsImpulse();
                physDie2.TossWithPhysicsImpulse();
            }
            else
            {
                physDie1.EndHandGrabAndThrow(transform.position);
                physDie2.EndHandGrabAndThrow(transform.position);
            }

            PlaySFX(diceRollSound, procDiceClip, 1.0f);
            RefreshDashboard("3D PHYSICS DICE TUMBLING...", "Watching where the dice settle on Central Park!");

            float waitElapsed = 0f;
            while ((physDie1.isRolling || physDie2.isRolling) && waitElapsed < 2.6f)
            {
                waitElapsed += Time.deltaTime;
                yield return null;
            }
            PlaySFX(diceRollSound, procDiceClip, 0.85f);
        }

        int d1 = physDie1 != null ? physDie1.ReadTopFaceFromOrientation() : Random.Range(1, 7);
        int d2 = physDie2 != null ? physDie2.ReadTopFaceFromOrientation() : Random.Range(1, 7);

        die1Transform.localPosition = new Vector3(-0.055f, 0.048f, -0.05f);
        die2Transform.localPosition = new Vector3(0.055f, 0.048f, -0.05f);
        die1Transform.localRotation = GetFlatDieRotationForValue(d1);
        die2Transform.localRotation = GetFlatDieRotationForValue(d2);

        lastDiceTotal = d1 + d2;
        bool isDoubles = (d1 == d2);
        currentState = TurnState.Moving;

        if (inJail[0])
        {
            if (isDoubles)
            {
                inJail[0] = false;
                RefreshDashboard($"ROLLED DOUBLES [{d1}+{d2}]!", "Released from Jail!");
                yield return new WaitForSeconds(0.8f);
            }
            else
            {
                playerCash[0] -= 50;
                freeParkingJackpot += 50;
                inJail[0] = false;
                RefreshDashboard($"PAID $50 BAIL [{d1}+{d2}]", "Bail added to Free Parking Jackpot!");
                yield return new WaitForSeconds(0.8f);
            }
        }

        if (isDoubles) consecutiveDoubles++;
        else consecutiveDoubles = 0;

        if (consecutiveDoubles >= 3)
        {
            consecutiveDoubles = 0;
            SendToJail(0);
            SetRivalReaction(AvatarMood.HappyCheer, "Haha! Caught speeding 3 times—enjoy Jail!");
            RefreshDashboard("3X DOUBLES SPEEDING!", "Sent directly to JAIL!");
            yield return new WaitForSeconds(1.2f);
            StartCoroutine(EndPlayerTurn(false));
            yield break;
        }

        RefreshDashboard($"YOU ROLLED [{d1} + {d2} = {lastDiceTotal}]" + (isDoubles ? " (DOUBLES!)" : ""), $"Moving {lastDiceTotal} steps clockwise...");
        yield return StartCoroutine(MoveTokenSteps(0, lastDiceTotal));
        yield return StartCoroutine(EvaluateTileLanding(0, isDoubles));
    }

    IEnumerator MoveTokenSteps(int playerIdx, int steps)
    {
        if (playerTokens[playerIdx] == null) yield break;

        for (int i = 0; i < steps; i++)
        {
            int nextTile = (playerPos[playerIdx] + 1) % 40;
            Vector3 startLocal = playerTokens[playerIdx].localPosition;
            Vector3 laneOffset = (playerIdx == 0) ? new Vector3(-0.012f, 0.018f, 0f) : new Vector3(0.012f, 0.018f, 0f);
            Vector3 endLocal = board[nextTile].tileTransform.localPosition + laneOffset;

            Quaternion startRot = playerTokens[playerIdx].localRotation;
            Quaternion endRot = GetClockwiseTravelRotation(nextTile);

            PlaySFX(tokenMoveSound, procStepClip, 0.95f + (i * 0.03f));

            float hopDuration = 0.15f;
            float elapsed = 0f;
            while (elapsed < hopDuration)
            {
                elapsed += Time.deltaTime;
                float p = elapsed / hopDuration;
                float arc = Mathf.Sin(p * Mathf.PI) * 0.025f;
                playerTokens[playerIdx].localPosition = Vector3.Lerp(startLocal, endLocal, p) + new Vector3(0, arc, 0);
                playerTokens[playerIdx].localRotation = Quaternion.Slerp(startRot, endRot, p);
                yield return null;
            }

            playerPos[playerIdx] = nextTile;
            playerTokens[playerIdx].localRotation = endRot;

            if (nextTile == 0)
            {
                int goSalary = (currentMarketCycle == MarketCycle.BullBoom) ? 300 : 200;
                playerCash[playerIdx] += goSalary;
                PlaySFX(cashRegisterSound, procCashClip, 1.1f);
                StartCoroutine(SpawnFlyingCoinsStream(board[0].tileTransform.position, playerTokens[playerIdx].position, 6));
                RefreshDashboard($"PASSED GO! +${goSalary} SALARY", $"{(playerIdx == 0 ? "You" : "Mogul AI")} collected ${goSalary}.");
            }
        }
    }

    IEnumerator EvaluateTileLanding(int playerIdx, bool rolledDoubles)
    {
        BoardTile tile = board[playerPos[playerIdx]];
        string who = (playerIdx == 0) ? "YOU" : "MOGUL AI";

        switch (tile.type)
        {
            case TileType.Property:
            case TileType.Railroad:
            case TileType.Utility:
                if (tile.owner == -1)
                {
                    if (playerIdx == 0)
                    {
                        ShowDeedCardForTile(tile);
                        if (playerCash[0] >= tile.price)
                        {
                            currentState = TurnState.BuyOrPassDecision;
                            SetRivalReaction(AvatarMood.Thinking, $"Thinking about buying {tile.name}?");
                            RefreshDashboard(
                                $"LANDED ON {tile.name} (${tile.price})",
                                "Right Pinch to Distance-Pull the 3D Deed Crystal (or touch [BUY DEED]) | Left Pinch to Pass"
                            );
                            yield break;
                        }
                        else
                        {
                            RefreshDashboard($"{tile.name} (${tile.price})", "Insufficient funds to purchase deed.");
                        }
                    }
                    else
                    {
                        if (playerCash[1] >= tile.price + 100)
                        {
                            PurchaseCurrentTile(1);
                            yield return new WaitForSeconds(0.9f);
                        }
                    }
                }
                else if (tile.owner != playerIdx)
                {
                    int rent = CalculateAuthenticRent(tile, lastDiceTotal);
                    playerCash[playerIdx] -= rent;
                    playerCash[tile.owner] += rent;
                    PlaySFX(cashRegisterSound, procCashClip, 0.9f);
                    StartCoroutine(SpawnFlyingCoinsStream(playerTokens[playerIdx].position, playerTokens[tile.owner].position, 8));

                    if (playerIdx == 0)
                        SetRivalReaction(AvatarMood.HappyCheer, $"Pay up! Thanks for the ${rent} rent on {tile.name}!");
                    else
                        SetRivalReaction(AvatarMood.AngrySlam, $"Ugh! Paid you ${rent} rent on {tile.name}!");

                    RefreshDashboard(
                        $"{who} PAID ${rent} RENT ON {tile.name}!",
                        $"Paid to {(tile.owner == 0 ? "YOU" : "MOGUL AI")}. (Touch [TAKEOVER] on your turn to buy out AI properties!)"
                    );
                    yield return new WaitForSeconds(1.1f);
                }
                else
                {
                    ShowDeedCardForTile(tile);
                    RefreshDashboard($"{who} ON OWN PROPERTY ({tile.name})", "Touch [BUILD (U)] to upgrade buildings!");
                    yield return new WaitForSeconds(0.8f);
                }
                break;

            case TileType.Tax:
                playerCash[playerIdx] -= tile.price;
                freeParkingJackpot += tile.price;
                if (jackpotGoldPile != null)
                {
                    StartCoroutine(SpawnFlyingCoinsStream(playerTokens[playerIdx].position, jackpotGoldPile.position, 6));
                }
                RefreshDashboard($"{who} PAID {tile.name}", $"${tile.price} added to Free Parking Jackpot (Now ${freeParkingJackpot})!");
                yield return new WaitForSeconds(1.1f);
                break;

            case TileType.FreeParking:
                if (freeParkingJackpot > 0)
                {
                    int won = freeParkingJackpot;
                    playerCash[playerIdx] += won;
                    freeParkingJackpot = 50;
                    PlaySFX(cashRegisterSound, procCashClip, 1.25f);
                    if (jackpotGoldPile != null)
                    {
                        StartCoroutine(SpawnFlyingCoinsStream(jackpotGoldPile.position, playerTokens[playerIdx].position, 12));
                    }
                    if (playerIdx == 0) SetRivalReaction(AvatarMood.AngrySlam, $"You swept the ${won} Free Parking Jackpot?!");
                    else SetRivalReaction(AvatarMood.HappyCheer, $"Jackpot! +${won} is all mine!");
                    RefreshDashboard($"{who} WON FREE PARKING JACKPOT!", $"Collected +${won} from the central gold vault!");
                }
                else
                {
                    RefreshDashboard($"{who} ON FREE PARKING", "Resting safely on corner plaza.");
                }
                yield return new WaitForSeconds(1.2f);
                break;

            case TileType.Chance:
            case TileType.Chest:
                if (currentMarketCycle == MarketCycle.TechIPOSurge)
                {
                    playerCash[playerIdx] += 50;
                    PlaySFX(cashRegisterSound, procCashClip, 1.15f);
                    RefreshDashboard($"{who} TECH DIVIDEND (+$50)", "Tech IPO Surge pays +$50 innovation dividend!");
                }
                yield return StartCoroutine(ExecuteAuthenticCardDraw(playerIdx, tile.type == TileType.Chance));
                break;

            case TileType.GoToJail:
                SendToJail(playerIdx);
                RefreshDashboard($"{who} SENT TO JAIL!", "Moved directly to Tile 10 (Jail). Do not collect $200.");
                yield return new WaitForSeconds(1.2f);
                rolledDoubles = false;
                break;

            default:
                RefreshDashboard($"{who} ON {tile.name}", "Resting safely on corner plaza.");
                yield return new WaitForSeconds(0.8f);
                break;
        }

        if (playerIdx == 0)
        {
            StartCoroutine(EndPlayerTurn(rolledDoubles));
        }
    }

    IEnumerator ExecuteAuthenticCardDraw(int playerIdx, bool isChance)
    {
        string who = (playerIdx == 0) ? "YOU" : "MOGUL AI";
        string deckName = isChance ? "CHANCE DIRECTIVE" : "COMMUNITY CHEST";
        Color deckColor = isChance ? new Color(0.95f, 0.50f, 0.12f) : new Color(0.18f, 0.62f, 0.95f);

        int pick = Random.Range(0, 5);
        switch (pick)
        {
            case 0:
                int goSalary = (currentMarketCycle == MarketCycle.BullBoom) ? 300 : 200;
                playerCash[playerIdx] += goSalary;
                playerPos[playerIdx] = 0;
                PositionTokenOnTile(playerIdx, 0);
                PlaySFX(cashRegisterSound, procCashClip, 1.15f);
                ShowSpecialEventCard(deckName, $"ADVANCE TO GO!\nCollect ${goSalary} Dividend Salary immediately.", deckColor);
                RefreshDashboard($"{who}: ADVANCE TO GO (+${goSalary})!", $"Teleported to GO and collected ${goSalary}.");
                break;

            case 1:
                playerCash[playerIdx] += 150;
                PlaySFX(cashRegisterSound, procCashClip, 1.1f);
                if (jackpotGoldPile != null)
                    StartCoroutine(SpawnFlyingCoinsStream(jackpotGoldPile.position, playerTokens[playerIdx].position, 7));
                ShowSpecialEventCard(deckName, "BUILDING & LOAN MATURES!\nCollect $150 from the Bank.", deckColor);
                RefreshDashboard($"{who} COLLECTED $150 DIVIDEND!", "Building & Loan matured (+$150).");
                break;

            case 2:
                int other = 1 - playerIdx;
                playerCash[playerIdx] -= 50;
                playerCash[other] += 50;
                PlaySFX(cashRegisterSound, procCashClip, 0.9f);
                StartCoroutine(SpawnFlyingCoinsStream(playerTokens[playerIdx].position, playerTokens[other].position, 5));
                ShowSpecialEventCard(deckName, "ELECTED CHAIRMAN OF THE BOARD!\nPay rival mogul $50.", deckColor);
                RefreshDashboard($"{who}: CHAIRMAN OF THE BOARD", $"Paid $50 to {(other == 0 ? "YOU" : "MOGUL AI")}.");
                break;

            case 3:
                playerCash[playerIdx] -= 100;
                freeParkingJackpot += 100;
                if (jackpotGoldPile != null)
                    StartCoroutine(SpawnFlyingCoinsStream(playerTokens[playerIdx].position, jackpotGoldPile.position, 6));
                ShowSpecialEventCard(deckName, "CITY STREET REPAIRS!\nPay $100 into the Free Parking Jackpot.", deckColor);
                RefreshDashboard($"{who} PAID $100 STREET REPAIRS", $"Added $100 to Free Parking Jackpot (${freeParkingJackpot}).");
                break;

            default:
                playerPos[playerIdx] = 39;
                PositionTokenOnTile(playerIdx, 39);
                ShowSpecialEventCard(deckName, "TAKE A WALK ON BOARDWALK!\nAdvance token directly to Boardwalk (Tile 39).", deckColor);
                RefreshDashboard($"{who} ADVANCED TO BOARDWALK!", "Moved directly to Tile 39 (Boardwalk).");
                break;
        }
        yield return new WaitForSeconds(1.3f);
    }

    void PurchaseCurrentTile(int playerIdx)
    {
        BoardTile tile = board[playerPos[playerIdx]];
        if (playerCash[playerIdx] < tile.price) return;

        playerCash[playerIdx] -= tile.price;
        tile.owner = playerIdx;
        PlaySFX(cashRegisterSound, procCashClip, 1.0f);
        HideDeedCard();

        StartCoroutine(SpawnFlyingCoinsStream(playerTokens[playerIdx].position, tile.tileTransform.position, 6));

        if (tile.type == TileType.Property && tile.buildingLevel == 0)
        {
            tile.buildingLevel = 1;
        }

        ApplyTileOwnershipVisuals(tile, playerIdx);
        RebuildMonopolySynergyBeams();

        if (playerIdx == 0)
        {
            if (OwnsFullColorGroup(0, tile.colorGroup))
                SetRivalReaction(AvatarMood.AngrySlam, $"Uh oh! You completed a Monopoly on {tile.name}!");
            else
                SetRivalReaction(AvatarMood.Neutral, $"Nice pickup on {tile.name}...");
        }
        else
        {
            SetRivalReaction(AvatarMood.HappyCheer, $"I just acquired {tile.name}!");
        }

        RefreshDashboard(
            $"{(playerIdx == 0 ? "YOU" : "MOGUL AI")} BOUGHT {tile.name} (${tile.price})!",
            "Tile claimed & Level 1 structure erected! Touch [BUILD (U)] on your turn to upgrade."
        );

        SaveGameState();

        if (playerIdx == 0)
        {
            StartCoroutine(EndPlayerTurn(false));
        }
    }

    public void TryBuildChronologically(int playerIdx)
    {
        BoardTile standingTile = board[playerPos[playerIdx]];
        BoardTile targetTile = null;

        // Targeted Upgrade: if inspecting an owned, unmortgaged property via tile poke, upgrade that specific tile first!
        if (playerIdx == 0 && selectedTileIndex >= 0 && selectedTileIndex < 40 &&
            board[selectedTileIndex].type == TileType.Property &&
            board[selectedTileIndex].owner == playerIdx &&
            !board[selectedTileIndex].isMortgaged &&
            board[selectedTileIndex].buildingLevel < 5)
        {
            targetTile = board[selectedTileIndex];
        }
        else if (standingTile.type == TileType.Property && standingTile.owner == playerIdx && !standingTile.isMortgaged && standingTile.buildingLevel < 5)
        {
            targetTile = standingTile;
        }
        else
        {
            int lowestLevelFound = 99;
            for (int offset = 0; offset < 40; offset++)
            {
                int idx = (buildCycleCursor + offset) % 40;
                if (board[idx].type == TileType.Property && board[idx].owner == playerIdx && !board[idx].isMortgaged && board[idx].buildingLevel < 5)
                {
                    if (board[idx].buildingLevel < lowestLevelFound)
                    {
                        lowestLevelFound = board[idx].buildingLevel;
                        targetTile = board[idx];
                    }
                }
            }
        }

        if (targetTile == null)
        {
            RefreshDashboard("NO OWNED PROPERTY TO UPGRADE", "Buy a Property Deed first, then touch [BUILD (U)].");
            return;
        }

        int actualBuildCost = (currentMarketCycle == MarketCycle.BearRecession) ? (int)(targetTile.buildCost * 0.7f) : targetTile.buildCost;

        if (playerCash[playerIdx] < actualBuildCost)
        {
            RefreshDashboard($"NEED ${actualBuildCost} TO BUILD", $"Insufficient cash to upgrade {targetTile.name}.");
            return;
        }

        playerCash[playerIdx] -= actualBuildCost;
        targetTile.buildingLevel++;
        buildCycleCursor = (targetTile.index + 1) % 40;

        PlaySFX(buildUpgradeSound, procBuildClip, 0.9f + (targetTile.buildingLevel * 0.08f));
        StartCoroutine(SpawnFlyingCoinsStream(playerTokens[playerIdx].position, targetTile.tileTransform.position, 5));
        ConstructChronological3DBuilding(targetTile);

        string tierLabel = (targetTile.buildingLevel == 5)
            ? "LVL 5 SKYSCRAPER HOTEL"
            : $"HOUSE LEVEL {targetTile.buildingLevel} OF 4";

        if (playerIdx == 0 && targetTile.buildingLevel == 5)
        {
            SetRivalReaction(AvatarMood.AngrySlam, $"A Skyscraper Hotel on {targetTile.name}?! Dangerous!");
        }

        RefreshDashboard(
            $"BUILT {tierLabel} ON {targetTile.name}!",
            $"Paid ${actualBuildCost} | New Rent: ${CalculateAuthenticRent(targetTile, 7)}"
        );

        if (selectedTileIndex == targetTile.index)
        {
            ShowDeedCardForTile(targetTile);
        }
        UpdateLiveHUDStats();
        SaveGameState();
    }

    GameObject GetPrefabForChronologicalLevel(int level)
    {
        switch (level)
        {
            case 1: return level1HousePrefab != null ? level1HousePrefab : level2HousePrefab;
            case 2: return level2HousePrefab != null ? level2HousePrefab : level1HousePrefab;
            case 3: return level3HousePrefab != null ? level3HousePrefab : (level2HousePrefab != null ? level2HousePrefab : level1HousePrefab);
            case 4: return level4HousePrefab != null ? level4HousePrefab : (level3HousePrefab != null ? level3HousePrefab : level2HousePrefab);
            case 5: return level5SkyscraperPrefab != null ? level5SkyscraperPrefab : level4HousePrefab;
            default: return level1HousePrefab;
        }
    }

    void ConstructChronological3DBuilding(BoardTile tile)
    {
        if (tile.spawnedBuilding != null) Destroy(tile.spawnedBuilding);

        GameObject prefabToUse = GetPrefabForChronologicalLevel(tile.buildingLevel);

        GameObject holder = new GameObject($"Building_Tile{tile.index}_Lvl{tile.buildingLevel}");
        holder.transform.SetParent(transform, false);
        holder.transform.localPosition = tile.tileTransform.localPosition + new Vector3(0f, 0.006f, 0f);
        holder.transform.localRotation = tile.tileTransform.localRotation;

        GameObject model;
        if (prefabToUse != null && prefabToUse.GetComponent<OVRCameraRig>() == null)
        {
            model = Instantiate(prefabToUse, holder.transform);
            StripRoguePhysicsFromImportedPrefab(model);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
        }
        else
        {
            model = GameObject.CreatePrimitive(PrimitiveType.Cube);
            model.transform.SetParent(holder.transform, false);
            Destroy(model.GetComponent<Collider>());
            model.GetComponent<Renderer>().material = CreateColoredMaterial(GetOfficialColor(tile), 0.85f);
        }

        tile.spawnedBuilding = holder;
        StartCoroutine(FitAndRiseBuildingChronological(holder.transform, model, tile.buildingLevel));
    }

    IEnumerator FitAndRiseBuildingChronological(Transform holder, GameObject model, int level)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        float progressiveHeight = 0.55f + (level * 0.22f);

        if (renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

            float maxFootprint = Mathf.Max(b.size.x, b.size.z);
            if (maxFootprint > 0.0001f)
            {
                float targetFootprint = 0.058f * transform.lossyScale.x;
                float s = targetFootprint / maxFootprint;
                model.transform.localScale = new Vector3(
                    model.transform.localScale.x * s,
                    model.transform.localScale.y * s * progressiveHeight,
                    model.transform.localScale.z * s
                );

                b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
                float bottomOffsetWorld = holder.position.y - b.min.y;
                model.transform.position += new Vector3(0f, bottomOffsetWorld, 0f);
            }
        }

        Vector3 targetScale = holder.localScale;
        holder.localScale = new Vector3(targetScale.x, 0.01f, targetScale.z);

        float duration = 0.45f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float ease = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / duration), 3f);
            holder.localScale = new Vector3(targetScale.x, Mathf.Lerp(0.01f, targetScale.y, ease), targetScale.z);
            yield return null;
        }
        holder.localScale = targetScale;
    }

    int CalculateAuthenticRent(BoardTile tile, int diceRoll)
    {
        if (tile.isMortgaged) return 0;

        if (tile.type == TileType.Railroad)
        {
            int rrCount = 0;
            int[] rrIndices = { 5, 15, 25, 35 };
            foreach (int idx in rrIndices) if (board[idx].owner == tile.owner) rrCount++;
            int baseRent = 25 * (int)Mathf.Pow(2, Mathf.Max(0, rrCount - 1));
            if (currentMarketCycle == MarketCycle.TechIPOSurge)
            {
                baseRent = (int)(baseRent * 2.5f);
            }
            return baseRent;
        }

        if (tile.type == TileType.Utility)
        {
            bool ownsBoth = (board[12].owner == tile.owner && board[28].owner == tile.owner);
            int baseRent = diceRoll * (ownsBoth ? 10 : 4);
            if (currentMarketCycle == MarketCycle.TechIPOSurge)
            {
                baseRent = (int)(baseRent * 2.5f);
            }
            return baseRent;
        }

        int propRent = tile.rentByLevel[Mathf.Clamp(tile.buildingLevel, 0, 5)];
        if (OwnsFullColorGroup(tile.owner, tile.colorGroup) && tile.buildingLevel <= 1)
        {
            propRent = propRent * 2;
        }

        if (currentMarketCycle == MarketCycle.BullBoom)
        {
            propRent = (int)(propRent * 1.5f);
        }
        else if (currentMarketCycle == MarketCycle.BearRecession)
        {
            propRent = (int)(propRent * 0.7f);
        }

        return propRent;
    }

    bool OwnsFullColorGroup(int playerIdx, int colorGroup)
    {
        if (colorGroup < 0) return false;
        for (int i = 0; i < 40; i++)
        {
            if (board[i].colorGroup == colorGroup && board[i].owner != playerIdx)
                return false;
        }
        return true;
    }

    void SendToJail(int playerIdx)
    {
        playerPos[playerIdx] = 10;
        inJail[playerIdx] = true;
        PositionTokenOnTile(playerIdx, 10);
    }

    IEnumerator EndPlayerTurn(bool playerRolledDoubles)
    {
        if (playerCash[1] < 0)
        {
            SetRivalReaction(AvatarMood.AngrySlam, "I'm bankrupt! You win this empire!");
            RefreshDashboard("VICTORY! MOGUL AI IS BANKRUPT!", "You conquered the 40-Tile Metropolis Empire!");
            currentState = TurnState.WaitingForRoll;
            yield break;
        }
        if (playerCash[0] < 0)
        {
            SetRivalReaction(AvatarMood.HappyCheer, "Look who's bankrupt! My empire reigns supreme!");
            RefreshDashboard("BANKRUPTCY! MOGUL AI WINS!", "Touch [ROLL DICE] to start a fresh comeback round!");
            playerCash[0] = 1500;
            playerCash[1] = 1500;
        }

        if (playerRolledDoubles && !inJail[0])
        {
            currentState = TurnState.WaitingForRoll;
            RefreshDashboard("DOUBLES BONUS ROLL!", "Pinch to Distance-Pull & Throw the 3D Dice again!");
            yield break;
        }

        currentState = TurnState.AITurn;
        SetRivalReaction(AvatarMood.Thinking, "My turn! Let's roll...");
        yield return new WaitForSeconds(0.75f);

        if (playerCash[1] > 500)
        {
            TryBuildChronologically(1);
            yield return new WaitForSeconds(0.55f);
        }

        int aiD1 = Random.Range(1, 7);
        int aiD2 = Random.Range(1, 7);
        lastDiceTotal = aiD1 + aiD2;
        if (die1Transform != null) die1Transform.localRotation = GetFlatDieRotationForValue(aiD1);
        if (die2Transform != null) die2Transform.localRotation = GetFlatDieRotationForValue(aiD2);
        PlaySFX(diceRollSound, procDiceClip, 0.95f);

        if (inJail[1])
        {
            inJail[1] = false;
            playerCash[1] -= 50;
            freeParkingJackpot += 50;
        }

        RefreshDashboard($"MOGUL AI ROLLED [{aiD1}+{aiD2}={lastDiceTotal}]", $"AI Rival is moving {lastDiceTotal} steps clockwise...");
        yield return StartCoroutine(MoveTokenSteps(1, lastDiceTotal));
        yield return StartCoroutine(EvaluateTileLanding(1, false));

        totalTurnsCompleted++;
        if (totalTurnsCompleted % 5 == 0)
        {
            ShiftMarketCycle();
        }

        SaveGameState();

        currentState = TurnState.WaitingForRoll;
        RefreshDashboard("YOUR TURN — DISTANCE-GRAB & THROW DICE", "Pinch with your hand to Pull & Throw the 3D Dice | Touch [TAKEOVER] to buy out AI!");
    }

    void ShiftMarketCycle()
    {
        List<MarketCycle> options = new List<MarketCycle>();
        foreach (MarketCycle mc in System.Enum.GetValues(typeof(MarketCycle)))
        {
            if (mc != currentMarketCycle) options.Add(mc);
        }
        currentMarketCycle = options[Random.Range(0, options.Count)];

        if (marketAuraRing != null)
        {
            marketAuraRing.SetActive(currentMarketCycle == MarketCycle.BullBoom);
        }

        string cycleName = "";
        string desc = "";
        string aiLine = "";
        switch (currentMarketCycle)
        {
            case MarketCycle.BullBoom:
                cycleName = "BULL MARKET BOOM (+50% RENT)";
                desc = "Macro-economy surges! All rents +50%, passing GO pays $300!";
                aiLine = "A raging Bull Market! My portfolio is heading straight to the moon!";
                break;
            case MarketCycle.BearRecession:
                cycleName = "BEAR MARKET CRUNCH (-30% BUILD)";
                desc = "Economic crunch! Rents 0.7x, but building upgrades are 30% off!";
                aiLine = "Recession hits... time to buy low and construct skyscrapers on discount!";
                break;
            case MarketCycle.TechIPOSurge:
                cycleName = "TECH IPO SURGE (2.5x TRANSIT)";
                desc = "Tech IPO boom! Railroads & Utilities pay 2.5x rent, +$50 tech dividends!";
                aiLine = "Tech stocks surging! Our transit and utility monopolies dominate!";
                break;
            default:
                cycleName = "STANDARD EQUILIBRIUM";
                desc = "Market returns to steady equilibrium.";
                aiLine = "Back to business fundamentals.";
                break;
        }

        PlaySFX(cashRegisterSound, procCashClip, 1.25f);
        SetRivalReaction(AvatarMood.Thinking, aiLine);
        RefreshDashboard($"ECONOMIC SHIFT: {cycleName}", desc);
        UpdateLiveHUDStats();
    }

    GameObject Create3DGavel(Transform parent)
    {
        GameObject gavelRoot = new GameObject("3D_Auction_Gavel");
        gavelRoot.transform.SetParent(parent, false);
        gavelRoot.transform.localPosition = new Vector3(0f, 0.28f, -0.02f);
        gavelRoot.transform.localScale = Vector3.one * 0.75f;

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        head.name = "GavelHead";
        head.transform.SetParent(gavelRoot.transform, false);
        head.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        head.transform.localScale = new Vector3(0.045f, 0.055f, 0.045f);
        Destroy(head.GetComponent<Collider>());
        head.GetComponent<Renderer>().material = CreateColoredMaterial(new Color(0.48f, 0.26f, 0.12f), 0.7f, 0.1f);

        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        handle.name = "GavelHandle";
        handle.transform.SetParent(gavelRoot.transform, false);
        handle.transform.localPosition = new Vector3(0f, -0.055f, 0f);
        handle.transform.localScale = new Vector3(0.014f, 0.06f, 0.014f);
        Destroy(handle.GetComponent<Collider>());
        handle.GetComponent<Renderer>().material = CreateColoredMaterial(new Color(0.72f, 0.45f, 0.20f), 0.7f, 0.1f);

        return gavelRoot;
    }

    IEnumerator AnimateGavelStrike(GameObject gavel)
    {
        if (gavel == null) yield break;
        Vector3 origPos = gavel.transform.localPosition;
        gavel.transform.localPosition = origPos + new Vector3(0f, -0.025f, 0f);
        yield return new WaitForSeconds(0.12f);
        if (gavel != null) gavel.transform.localPosition = origPos;
    }

    IEnumerator RunPropertyAuctionCoroutine(BoardTile tile)
    {
        currentState = TurnState.AuctionInProgress;
        ShowDeedCardForTile(tile);

        GameObject gavel = Create3DGavel(deedCardHolder != null ? deedCardHolder.transform : transform);

        int currentBid = Mathf.Max(10, tile.price / 2);
        int highestBidder = -1; // -1 = opening/no bidder yet, 0 = Player, 1 = AI
        int aiMaxBid = tile.price + Random.Range(-30, 61);
        aiMaxBid = Mathf.Min(aiMaxBid, playerCash[1]);

        float timer = 6.0f;
        float nextAiBidTime = Time.time + 1.1f;
        bool playerDroppedOut = false;
        bool aiDroppedOut = false;
        auctionPlayerBidFlag = false;
        auctionPlayerDropFlag = false;

        PlaySFX(cashRegisterSound, procCashClip, 1.3f);
        SetRivalReaction(AvatarMood.Thinking, $"An auction for {tile.name}! Opening bid: ${currentBid}. Let's bid!");

        while (timer > 0f)
        {
            timer -= Time.deltaTime;

            if (!playerDroppedOut)
            {
                if (auctionPlayerDropFlag)
                {
                    auctionPlayerDropFlag = false;
                    playerDroppedOut = true;
                    RefreshDashboard("AUCTION: YOU DROPPED OUT!", "You conceded the auction to Rival Mogul.");
                    PlaySFX(tokenMoveSound, procStepClip, 0.8f);
                    break;
                }
                if (auctionPlayerBidFlag)
                {
                    auctionPlayerBidFlag = false;
                    int nextPlayerBid = (highestBidder == -1) ? currentBid : currentBid + 20;
                    if (playerCash[0] >= nextPlayerBid)
                    {
                        currentBid = nextPlayerBid;
                        highestBidder = 0;
                        timer = Mathf.Max(timer, 2.5f);
                        PlaySFX(cashRegisterSound, procCashClip, 1.2f);
                        StartCoroutine(AnimateGavelStrike(gavel));
                        SetRivalReaction(AvatarMood.Neutral, $"You raised to ${currentBid}?! Let me calculate...");
                    }
                    else
                    {
                        RefreshDashboard("INSUFFICIENT FUNDS TO BID", $"Need ${nextPlayerBid} to place bid.");
                    }
                }
            }

            if (!aiDroppedOut && Time.time >= nextAiBidTime && highestBidder != 1)
            {
                int nextAiBid = (highestBidder == -1) ? currentBid : currentBid + 20;
                if (nextAiBid <= aiMaxBid && playerCash[1] >= nextAiBid)
                {
                    currentBid = nextAiBid;
                    highestBidder = 1;
                    nextAiBidTime = Time.time + 1.1f + Random.Range(0f, 0.4f);
                    timer = Mathf.Max(timer, 2.5f);
                    PlaySFX(diceRollSound, procDiceClip, 1.2f);
                    StartCoroutine(AnimateGavelStrike(gavel));
                    string taunt = (Random.value > 0.5f) ? $"I counter-bid ${currentBid}!" : $"Outbid! It's mine for ${currentBid}!";
                    SetRivalReaction(AvatarMood.HappyCheer, taunt);
                }
                else
                {
                    aiDroppedOut = true;
                    SetRivalReaction(AvatarMood.AngrySlam, $"Too steep for my spreadsheet! Take it at ${currentBid}.");
                }
            }

            string bidderName = (highestBidder == 0) ? "YOU" : (highestBidder == 1 ? "MOGUL AI" : "NONE");
            RefreshDashboard(
                $"AUCTION: {tile.name.ToUpper()} — HIGH BID: ${currentBid} ({bidderName})",
                $"Timer: {timer:F1}s | Right-Pinch/B: +$20 Raise | Left-Pinch/P: Drop Out"
            );

            if (deedBodyTMP != null)
            {
                deedBodyTMP.text =
                    $"<b>PROPERTY AUCTION IN PROGRESS</b>\n\n" +
                    $"Current Bid: <color=#FFD700><b>${currentBid}</b></color>\n" +
                    $"High Bidder: <b>{bidderName}</b>\n" +
                    $"Countdown: <b>{timer:F1}s</b>\n\n" +
                    $"[BUY / B]: Raise +$20\n[PASS / P]: Drop Out";
            }

            if (gavel != null)
            {
                gavel.transform.Rotate(Vector3.up, 35f * Time.deltaTime);
            }

            yield return null;
        }

        if (gavel != null) Destroy(gavel);

        if (highestBidder == 0)
        {
            playerCash[0] -= currentBid;
            tile.owner = 0;
            if (tile.type == TileType.Property && tile.buildingLevel == 0) tile.buildingLevel = 1;
            ApplyTileOwnershipVisuals(tile, 0);
            RebuildMonopolySynergyBeams();
            PlaySFX(cashRegisterSound, procCashClip, 1.25f);
            StartCoroutine(SpawnFlyingCoinsStream(playerTokens[0].position, tile.tileTransform.position, 6));
            RefreshDashboard($"AUCTION WON BY YOU FOR ${currentBid}!", $"Acquired {tile.name} with Level 1 Building at auction!");
            SetRivalReaction(AvatarMood.AngrySlam, $"You sniped {tile.name} for ${currentBid}... well played.");
        }
        else if (highestBidder == 1)
        {
            playerCash[1] -= currentBid;
            tile.owner = 1;
            if (tile.type == TileType.Property && tile.buildingLevel == 0) tile.buildingLevel = 1;
            ApplyTileOwnershipVisuals(tile, 1);
            RebuildMonopolySynergyBeams();
            PlaySFX(cashRegisterSound, procCashClip, 1.25f);
            StartCoroutine(SpawnFlyingCoinsStream(playerTokens[1].position, tile.tileTransform.position, 6));
            RefreshDashboard($"AUCTION WON BY MOGUL AI FOR ${currentBid}!", $"Mogul AI claimed {tile.name} at auction!");
            SetRivalReaction(AvatarMood.HappyCheer, $"A brilliant asset acquisition for ${currentBid}!");
        }
        else
        {
            RefreshDashboard("AUCTION PASSED WITH NO BIDS", $"No bids placed for {tile.name}. Returned to bank.");
        }

        SaveGameState();
        HideDeedCard();
        yield return new WaitForSeconds(1.8f);
        StartCoroutine(EndPlayerTurn(false));
    }

    void ApplyTileOwnershipVisuals(BoardTile tile, int playerIdx)
    {
        if (tile == null || tile.tileTransform == null) return;
        Color tileTint = (playerIdx == 0) ? new Color(0.18f, 0.85f, 0.45f) : new Color(0.85f, 0.30f, 0.55f);
        if (tile.isMortgaged)
        {
            tileTint = new Color(0.48f, 0.12f, 0.16f);
        }

        if (tile.tileRenderer != null)
        {
            tile.tileRenderer.material = CreateColoredMaterial(tileTint, 0.85f);
        }

        if (tile.ownershipBadge != null) Destroy(tile.ownershipBadge);
        GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beacon.name = $"Owned_Beacon_P{playerIdx}";
        beacon.transform.SetParent(tile.tileTransform, false);
        beacon.transform.localPosition = new Vector3(0f, 1.8f, -0.35f);
        beacon.transform.localScale = new Vector3(0.24f, 1.4f, 0.24f);
        Destroy(beacon.GetComponent<Collider>());
        beacon.GetComponent<Renderer>().material = CreateColoredMaterial(tileTint, 0.95f);
        tile.ownershipBadge = beacon;

        if (tile.buildingLevel > 0 && tile.type == TileType.Property && !tile.isMortgaged)
        {
            ConstructChronological3DBuilding(tile);
        }
    }

    public void SaveGameState()
    {
        PlayerPrefs.SetInt("Tycoon_HasSave", 1);
        PlayerPrefs.SetInt("Tycoon_Cash_P0", playerCash[0]);
        PlayerPrefs.SetInt("Tycoon_Cash_P1", playerCash[1]);
        PlayerPrefs.SetInt("Tycoon_Pos_P0", playerPos[0]);
        PlayerPrefs.SetInt("Tycoon_Pos_P1", playerPos[1]);
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

        playerCash[0] = PlayerPrefs.GetInt("Tycoon_Cash_P0", 1500);
        playerCash[1] = PlayerPrefs.GetInt("Tycoon_Cash_P1", 1500);
        playerPos[0] = PlayerPrefs.GetInt("Tycoon_Pos_P0", 0);
        playerPos[1] = PlayerPrefs.GetInt("Tycoon_Pos_P1", 0);
        freeParkingJackpot = PlayerPrefs.GetInt("Tycoon_Jackpot", 100);
        currentMarketCycle = (MarketCycle)PlayerPrefs.GetInt("Tycoon_MarketCycle", 0);
        totalTurnsCompleted = PlayerPrefs.GetInt("Tycoon_TotalTurns", 0);
        currentBoardScale = PlayerPrefs.GetFloat("Tycoon_BoardScale", 0.85f);
        transform.localScale = Vector3.one * currentBoardScale;

        PositionTokenOnTile(0, playerPos[0]);
        PositionTokenOnTile(1, playerPos[1]);

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
        RefreshDashboard("SAVED EMPIRE LOADED", "Restored your metropolis, net worth, and active market cycle!");
    }

    public void ResetMatch()
    {
        PlayerPrefs.DeleteKey("Tycoon_HasSave");
        PlayerPrefs.Save();

        playerCash[0] = 1500;
        playerCash[1] = 1500;
        playerPos[0] = 0;
        playerPos[1] = 0;
        freeParkingJackpot = 100;
        currentMarketCycle = MarketCycle.Standard;
        totalTurnsCompleted = 0;

        PositionTokenOnTile(0, 0);
        PositionTokenOnTile(1, 0);

        for (int i = 0; i < 40; i++)
        {
            if (board[i] != null)
            {
                board[i].owner = -1;
                board[i].buildingLevel = 0;
                board[i].isMortgaged = false;
                if (board[i].ownershipBadge != null) Destroy(board[i].ownershipBadge);
                if (board[i].spawnedBuilding != null) Destroy(board[i].spawnedBuilding);
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
        RefreshDashboard("MATCH RESET (NEW GAME)", "Fresh $1500 match started! Distance-grab dice to roll.");
        SetRivalReaction(AvatarMood.Thinking, "A fresh board! Let's see who builds the empire this time.");
    }

    int CalculatePlayerNetWorth(int playerIdx)
    {
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

    void UpdateLiveHUDStats()
    {
        if (dashboardStatsTMP == null) return;
        int net0 = CalculatePlayerNetWorth(0);
        int net1 = CalculatePlayerNetWorth(1);
        string monopolySummary = GetMonopolyProgressSummary(0);
        string cycleBadge = "";
        switch (currentMarketCycle)
        {
            case MarketCycle.BullBoom: cycleBadge = "MARKET: BULL BOOM (+50% RENT)"; break;
            case MarketCycle.BearRecession: cycleBadge = "MARKET: BEAR RECESSION (-30% BUILD)"; break;
            case MarketCycle.TechIPOSurge: cycleBadge = "MARKET: TECH IPO (2.5x TRANSIT)"; break;
            default: cycleBadge = "MARKET: STANDARD"; break;
        }
        dashboardStatsTMP.text = $"YOU: ${playerCash[0]} (NET: ${net0})  |  AI: ${playerCash[1]} (NET: ${net1})  |  POT: ${freeParkingJackpot}  |  {cycleBadge}\n{monopolySummary}";
    }

    void RefreshDashboard(string eventHeadline, string promptLine)
    {
        if (dashboardTitleTMP == null) return;
        dashboardTitleTMP.text = $"YOU: ${playerCash[0]}  |  AI: ${playerCash[1]}  |  JACKPOT: ${freeParkingJackpot}";
        dashboardBodyTMP.text = eventHeadline;
        dashboardPromptTMP.text = promptLine;
        UpdateLiveHUDStats();
    }

    // 3. Replace ToggleMarketAtmosphere() so streetlight intensity stays gentle (0.15f Night / 0.06f Day):
    public void ToggleMarketAtmosphere()
    {
        isMarketNightMode = !isMarketNightMode;
        foreach (Light l in cityNightLights)
        {
            if (l != null) l.intensity = isMarketNightMode ? 0.15f : 0.06f;
        }
        if (passthroughLayer != null)
        {
            StopCoroutine("FadePassthroughOpacity");
            StartCoroutine(FadePassthroughOpacity(isMarketNightMode ? 0.22f : 1.0f));
        }
    }

    public void TogglePenthouseScaleShift()
    {
        isInPenthouseVR = !isInPenthouseVR;
        isStandingOnRooftop = false;
        Transform eye = Camera.main != null ? Camera.main.transform : transform;

        if (isInPenthouseVR)
        {
            transform.localScale = Vector3.one * 14f;
            transform.rotation = lockedBoardRotation;
            transform.position = new Vector3(eye.position.x, eye.position.y - 1.65f, eye.position.z);

            if (passthroughLayer != null) passthroughLayer.textureOpacity = 0f;
            if (vrSkyDome != null)
            {
                vrSkyDome.transform.position = eye.position;
                vrSkyDome.SetActive(true);
            }
            if (vrExitFloatingButton != null) vrExitFloatingButton.SetActive(true);
            if (vrRooftopTeleportButton != null) vrRooftopTeleportButton.SetActive(true);
            RefreshDashboard("METROPOLIS PLAZA FULL-VR VIEW", "Left Pinch for Rooftop Terrace | Right Pinch (or Red Button) to Exit VR!");
        }
        else
        {
            transform.localScale = Vector3.one * currentBoardScale;
            transform.position = lockedBoardPosition;
            transform.rotation = lockedBoardRotation;

            if (passthroughLayer != null) passthroughLayer.textureOpacity = isMarketNightMode ? 0.22f : 1.0f;
            if (vrSkyDome != null) vrSkyDome.SetActive(false);
            if (vrExitFloatingButton != null) vrExitFloatingButton.SetActive(false);
            if (vrRooftopTeleportButton != null) vrRooftopTeleportButton.SetActive(false);
            RefreshDashboard("RETURNED TO MR EXECUTIVE TABLE", "Pinch to Pull & Throw the 3D Dice or touch [ROLL DICE]!");
        }
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

    IEnumerator FadePassthroughOpacity(float targetOpacity)
    {
        float startOpacity = passthroughLayer.textureOpacity;
        float duration = 0.8f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            passthroughLayer.textureOpacity = Mathf.Lerp(startOpacity, targetOpacity, elapsed / duration);
            yield return null;
        }
        passthroughLayer.textureOpacity = targetOpacity;
    }
}