using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Phase 16: Meta Spatial Anchor Persistence, Pass-and-Play Table Auto-Rotation, and Living Metropolis Diorama
/// </summary>
[RequireComponent(typeof(TycoonBoardManager))]
public class TycoonSpatialAnchorAndDiorama : MonoBehaviour
{
    private TycoonBoardManager boardManager;

    [Header("1. Meta Spatial Anchor Settings")]
    [Tooltip("Dedicated anchor GameObject that hosts the OVRSpatialAnchor component")]
    private GameObject anchorHolder;
    private const string PREF_ANCHOR_UUID = "Tycoon_SpatialAnchor_UUID";
    private const string PREF_ANCHOR_POS_X = "Tycoon_Anchor_PosX";
    private const string PREF_ANCHOR_POS_Y = "Tycoon_Anchor_PosY";
    private const string PREF_ANCHOR_POS_Z = "Tycoon_Anchor_PosZ";
    private const string PREF_ANCHOR_ROT_Y = "Tycoon_Anchor_RotY";

    [Header("2. Pass-and-Play State")]
    private bool wasUserPresentLastFrame = true;
    private Coroutine rotationCoroutine;
    private Quaternion baseTableRotation = Quaternion.identity;
    private GameObject passAndPlayBadgeGO;
    private TextMeshProUGUI passAndPlayBadgeTMP;
    private Renderer passAndPlayBadgeRenderer;

    [Header("3. Courtyard Pedestrians & Crosswalks")]
    private GameObject dioramaContainer;
    private List<Transform> pedestrianTransforms = new List<Transform>();
    private List<float> pedestrianProgress = new List<float>();
    private List<float> pedestrianSpeeds = new List<float>();
    private const float PERIMETER_EXTENT = 0.255f;
    private const float PERIMETER_LENGTH = PERIMETER_EXTENT * 8f; // 4 sides * 2 * extent

    void Awake()
    {
        boardManager = GetComponent<TycoonBoardManager>();
    }

    void Start()
    {
        baseTableRotation = boardManager.LockedBoardRotation;

        // Subscribe to Board Manager hooks
        boardManager.OnBoardPoseUpdated += HandleBoardPoseUpdated;
        boardManager.OnTurnStarted += HandleTurnStarted;

        // Initialize Spatial Anchor Holder
        InitializeSpatialAnchorHolder();

        // Spawn Diorama Decor (Crosswalks + Living Pedestrians)
        SpawnLivingCourtyardDiorama();

        // Spawn 3D Pass & Play UI Badge
        CreatePassAndPlay3DBadge();
    }

    void OnDestroy()
    {
        if (boardManager != null)
        {
            boardManager.OnBoardPoseUpdated -= HandleBoardPoseUpdated;
            boardManager.OnTurnStarted -= HandleTurnStarted;
        }
    }

    void Update()
    {
        // 0. Hide Pass & Play badge during Start Menu to prevent tabletop clutter
        if (passAndPlayBadgeGO != null && boardManager != null)
        {
            bool isStartMenu = (boardManager.CurrentState == TycoonBoardManager.TurnState.StartMenu);
            if (passAndPlayBadgeGO.activeSelf == isStartMenu)
            {
                passAndPlayBadgeGO.SetActive(!isStartMenu);
            }
        }

        // 1. Keyboard Tab shortcut to toggle Pass & Play mode
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            TogglePassAndPlayMode();
        }

        // 2. Check 3D Touch on Pass & Play badge
        CheckPassAndPlayBadgePoke();

        // 3. Monitor Headset Presence in Pass & Play mode
        MonitorHeadsetPresence();

        // 4. Animate Living Courtyard Pedestrians
        AnimateCourtyardPedestrians();
    }

    #region A. Meta Spatial Anchor Table Lock

    private void InitializeSpatialAnchorHolder()
    {
        anchorHolder = GameObject.Find("Tycoon_Table_SpatialAnchor");
        if (anchorHolder == null)
        {
            anchorHolder = new GameObject("Tycoon_Table_SpatialAnchor");
            anchorHolder.transform.position = boardManager.LockedBoardPosition;
            anchorHolder.transform.rotation = boardManager.LockedBoardRotation;
        }

        // Attempt to restore saved coffee table pose if available
        if (PlayerPrefs.HasKey(PREF_ANCHOR_POS_X) && PlayerPrefs.HasKey(PREF_ANCHOR_POS_Y))
        {
            float px = PlayerPrefs.GetFloat(PREF_ANCHOR_POS_X);
            float py = PlayerPrefs.GetFloat(PREF_ANCHOR_POS_Y);
            float pz = PlayerPrefs.GetFloat(PREF_ANCHOR_POS_Z);
            float ry = PlayerPrefs.GetFloat(PREF_ANCHOR_ROT_Y, 0f);

            Vector3 savedPos = new Vector3(px, py, pz);
            Quaternion savedRot = Quaternion.Euler(0f, ry, 0f);

            if (!float.IsNaN(savedPos.x) && savedPos.sqrMagnitude < 400f)
            {
                anchorHolder.transform.position = savedPos;
                anchorHolder.transform.rotation = savedRot;
                baseTableRotation = savedRot;
            }
        }

        if (!OVRManager.isHmdPresent)
        {
            OVRSpatialAnchor existingAnchor = anchorHolder.GetComponent<OVRSpatialAnchor>();
            if (existingAnchor != null) DestroyImmediate(existingAnchor);
            return;
        }

        AttachOrUpdateSpatialAnchor();
    }

    private void HandleBoardPoseUpdated(Vector3 newPos, Quaternion newRot)
    {
        baseTableRotation = newRot;
        if (anchorHolder != null)
        {
            anchorHolder.transform.position = newPos;
            anchorHolder.transform.rotation = newRot;
        }

        SaveAnchorPoseToPlayerPrefs(newPos, newRot);
        AttachOrUpdateSpatialAnchor();
    }

    private void SaveAnchorPoseToPlayerPrefs(Vector3 pos, Quaternion rot)
    {
        PlayerPrefs.SetFloat(PREF_ANCHOR_POS_X, pos.x);
        PlayerPrefs.SetFloat(PREF_ANCHOR_POS_Y, pos.y);
        PlayerPrefs.SetFloat(PREF_ANCHOR_POS_Z, pos.z);
        PlayerPrefs.SetFloat(PREF_ANCHOR_ROT_Y, rot.eulerAngles.y);
        PlayerPrefs.Save();
    }

    private void AttachOrUpdateSpatialAnchor()
    {
        if (anchorHolder == null) return;
        if (!OVRManager.isHmdPresent) return;

        try
        {
            OVRSpatialAnchor sa = anchorHolder.GetComponent<OVRSpatialAnchor>();
            if (sa == null)
            {
                sa = anchorHolder.AddComponent<OVRSpatialAnchor>();
            }

            if (sa != null)
            {
                StartCoroutine(SaveAnchorCoroutine(sa));
            }
        }
        catch (Exception ex)
        {
            // Desktop fallback or Quest non-VR editor mode
            Debug.Log($"[TycoonSpatialAnchor] OVRSpatialAnchor runtime note: {ex.Message}");
        }
    }

    private IEnumerator SaveAnchorCoroutine(OVRSpatialAnchor sa)
    {
        // Wait until anchor creation completes
        float timeout = 2.5f;
        while (sa != null && !sa.Created && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (sa != null && sa.Created)
        {
            PlayerPrefs.SetString(PREF_ANCHOR_UUID, sa.Uuid.ToString());
            PlayerPrefs.Save();
            try
            {
                sa.SaveAnchorAsync();
                Debug.Log($"<color=#00FF88>[TycoonSpatialAnchor]</color> Physical table anchor locked and saved: UUID = {sa.Uuid}");
            }
            catch (Exception ex)
            {
                Debug.Log($"[TycoonSpatialAnchor] SaveAnchorAsync note: {ex.Message}");
            }
        }
    }

    public void AlignToSharedAnchor(string remoteUuid)
    {
        if (string.IsNullOrEmpty(remoteUuid)) return;
        string currentUuid = PlayerPrefs.GetString(PREF_ANCHOR_UUID, "");
        if (currentUuid == remoteUuid) return;

        PlayerPrefs.SetString(PREF_ANCHOR_UUID, remoteUuid);
        PlayerPrefs.Save();

        Debug.Log($"<color=#00FF88>[TycoonSpatialAnchor]</color> Aligning to shared Colocation anchor: {remoteUuid}");
        if (anchorHolder != null && OVRManager.isHmdPresent)
        {
            try
            {
                OVRSpatialAnchor sa = anchorHolder.GetComponent<OVRSpatialAnchor>();
                if (sa == null) sa = anchorHolder.AddComponent<OVRSpatialAnchor>();
            }
            catch { }
        }
    }

    #endregion

    #region B. Pass-and-Play Table Auto-Rotation

    public void TogglePassAndPlayMode()
    {
        boardManager.isPassAndPlayMode = !boardManager.isPassAndPlayMode;
        UpdatePassAndPlayBadgeVisuals();
        boardManager.PlaySFX(boardManager.CashRegisterSound, null, boardManager.isPassAndPlayMode ? 1.25f : 0.85f);
        boardManager.PlayHaptic(0.4f, 0.15f);

        string status = boardManager.isPassAndPlayMode ? "<color=#00FF88>ENABLED</color>" : "<color=#FF6666>DISABLED</color>";
        boardManager.RefreshDashboard(
            $"PASS & PLAY: {status}",
            boardManager.isPassAndPlayMode
                ? "Table will auto-rotate to face active player's seat upon headset handoff or turn switch!"
                : "Standard mode: Board faces your chair locked to South."
        );
    }

    private void MonitorHeadsetPresence()
    {
        if (!boardManager.isPassAndPlayMode) return;

        bool userPresent = OVRPlugin.userPresent;
        if (!wasUserPresentLastFrame && userPresent)
        {
            // Headset was handed over and put on by the active player!
            RotateBoardToSeat(boardManager.CurrentTurnPlayer);
            boardManager.PlaySFX(boardManager.TokenMoveSound, null, 1.2f);
        }
        wasUserPresentLastFrame = userPresent;
    }

    private void HandleTurnStarted(int playerIdx)
    {
        if (boardManager != null && boardManager.isPassAndPlayMode)
        {
            RotateBoardToSeat(playerIdx);
        }
    }

    public void RotateBoardToSeat(int seatPlayerIdx)
    {
        if (rotationCoroutine != null) StopCoroutine(rotationCoroutine);
        rotationCoroutine = StartCoroutine(SmoothRotateBoardCoroutine(seatPlayerIdx));
    }

    private IEnumerator SmoothRotateBoardCoroutine(int seatPlayerIdx)
    {
        // 4-player table mapping to bring active seat to face South (0°):
        // Seat 0: South (0° offset)
        // Seat 1: North (180° offset to bring North to South)
        // Seat 2: West (90° offset to bring West to South)
        // Seat 3: East (-90° / 270° offset to bring East to South)
        float yawOffset = 0f;
        switch (seatPlayerIdx)
        {
            case 0: yawOffset = 0f; break;
            case 1: yawOffset = 180f; break;
            case 2: yawOffset = 90f; break;
            case 3: yawOffset = -90f; break;
        }

        Quaternion startRot = boardManager.LockedBoardRotation;
        Quaternion targetRot = baseTableRotation * Quaternion.Euler(0f, yawOffset, 0f);

        float duration = 0.65f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            boardManager.LockedBoardRotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        boardManager.LockedBoardRotation = targetRot;
        rotationCoroutine = null;
    }

    private class CourtyardPedestrianInfo
    {
        public Transform root;
        public float progress;
        public float speed;
        public Animator animator;
        public Animation legacyAnim;
        public List<Transform> leftLegs = new List<Transform>();
        public List<Transform> rightLegs = new List<Transform>();
        public List<Transform> leftArms = new List<Transform>();
        public List<Transform> rightArms = new List<Transform>();
    }

    private List<CourtyardPedestrianInfo> pedestrians = new List<CourtyardPedestrianInfo>();

    private void CreatePassAndPlay3DBadge()
    {
        if (passAndPlayBadgeGO != null) Destroy(passAndPlayBadgeGO);

        passAndPlayBadgeGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        passAndPlayBadgeGO.name = "PassAndPlay_3D_Toggle_Badge";
        passAndPlayBadgeGO.transform.SetParent(transform, false);

        // Position on the front utility row beside NEW GAME
        passAndPlayBadgeGO.transform.localPosition = new Vector3(0.46f, 0.020f, -(boardManager.boardSize * 0.5f) - 0.12f);
        passAndPlayBadgeGO.transform.localRotation = Quaternion.identity;
        passAndPlayBadgeGO.transform.localScale = new Vector3(0.15f, 0.025f, 0.055f);

        passAndPlayBadgeRenderer = passAndPlayBadgeGO.GetComponent<Renderer>();

        // Text Canvas
        GameObject canvasGO = new GameObject("BadgeCanvas");
        canvasGO.transform.SetParent(passAndPlayBadgeGO.transform, false);
        canvasGO.transform.localPosition = new Vector3(0f, 0.54f, 0f);
        canvasGO.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        canvasGO.transform.localScale = Vector3.one * 0.0016f;

        Canvas c = canvasGO.AddComponent<Canvas>();
        c.renderMode = RenderMode.WorldSpace;
        RectTransform rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(280f, 100f);

        GameObject txtGO = new GameObject("BadgeText");
        txtGO.transform.SetParent(canvasGO.transform, false);
        passAndPlayBadgeTMP = txtGO.AddComponent<TextMeshProUGUI>();
        passAndPlayBadgeTMP.alignment = TextAlignmentOptions.Center;
        passAndPlayBadgeTMP.rectTransform.sizeDelta = new Vector2(280f, 100f);

        UpdatePassAndPlayBadgeVisuals();
    }

    private void UpdatePassAndPlayBadgeVisuals()
    {
        if (passAndPlayBadgeTMP != null)
        {
            passAndPlayBadgeTMP.text = boardManager.isPassAndPlayMode
                ? "<color=#00FF88><b>PASS & PLAY\n[ON]</b></color>"
                : "<color=#AAAAAA><b>PASS & PLAY\n[OFF]</b></color>";
        }

        if (passAndPlayBadgeRenderer != null)
        {
            Color badgeCol = boardManager.isPassAndPlayMode
                ? new Color(0.08f, 0.32f, 0.18f)
                : new Color(0.12f, 0.14f, 0.18f);
            passAndPlayBadgeRenderer.material = boardManager.CreateColoredMaterial(badgeCol, 0.88f);
        }
    }

    private void CheckPassAndPlayBadgePoke()
    {
        if (passAndPlayBadgeGO == null) return;
        Collider badgeCol = passAndPlayBadgeGO.GetComponent<Collider>();
        if (badgeCol == null) return;

        bool rightPoked = boardManager.IsRightHandTracked &&
            Vector3.Distance(badgeCol.ClosestPoint(boardManager.RightHandWorldPos), boardManager.RightHandWorldPos) < 0.024f;
        bool leftPoked = boardManager.IsLeftHandTracked &&
            Vector3.Distance(badgeCol.ClosestPoint(boardManager.LeftHandWorldPos), boardManager.LeftHandWorldPos) < 0.024f;

        if (rightPoked || leftPoked)
        {
            TogglePassAndPlayMode();
        }
    }

    #endregion

    #region C. Living Courtyard Pedestrians & Crosswalks

    private void SpawnLivingCourtyardDiorama()
    {
        if (dioramaContainer != null) Destroy(dioramaContainer);

        dioramaContainer = new GameObject("Courtyard_Diorama_Decor");
        dioramaContainer.transform.SetParent(transform, false);

        SpawnZebraCrosswalks();
        SpawnMiniaturePedestrians();
    }

    private void SpawnZebraCrosswalks()
    {
        // 4 Painted white zebra crosswalk strips crossing Inner_Ring_Road_Asphalt (between ±0.28m and ±0.38m)
        Vector3[] cornerCenters = new Vector3[]
        {
            new Vector3(0.325f, 0.0235f, 0.325f),
            new Vector3(-0.325f, 0.0235f, 0.325f),
            new Vector3(-0.325f, 0.0235f, -0.325f),
            new Vector3(0.325f, 0.0235f, -0.325f)
        };

        float[] cornerAngles = new float[] { -45f, 45f, -45f, 45f };

        Material whiteStripeMat = boardManager.CreateColoredMaterial(new Color(0.96f, 0.96f, 0.96f), 0.95f);

        for (int c = 0; c < 4; c++)
        {
            if (boardManager.crosswalkPropPrefab != null)
            {
                GameObject cw = Instantiate(boardManager.crosswalkPropPrefab, dioramaContainer.transform);
                cw.name = $"Crosswalk_Corner_{c}";
                boardManager.StripRoguePhysicsFromImportedPrefab(cw);
                boardManager.NormalizePrefabSize(cw, 0.055f, true);
                cw.transform.localPosition = cornerCenters[c];
                cw.transform.localRotation = Quaternion.Euler(0f, cornerAngles[c], 0f);
            }
            else
            {
                GameObject cwHolder = new GameObject($"Zebra_Crosswalk_Corner_{c}");
                cwHolder.transform.SetParent(dioramaContainer.transform, false);
                cwHolder.transform.localPosition = cornerCenters[c];
                cwHolder.transform.localRotation = Quaternion.Euler(0f, cornerAngles[c], 0f);

                // 4 crisp white painted zebra stripes per corner
                for (int s = -2; s <= 2; s++)
                {
                    if (s == 0) continue;
                    GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    stripe.name = $"Stripe_{s}";
                    stripe.transform.SetParent(cwHolder.transform, false);
                    stripe.transform.localPosition = new Vector3(s * 0.016f, 0f, 0f);
                    stripe.transform.localRotation = Quaternion.identity;
                    stripe.transform.localScale = new Vector3(0.008f, 0.0015f, 0.065f);
                    Destroy(stripe.GetComponent<Collider>());
                    stripe.GetComponent<Renderer>().material = whiteStripeMat;
                }
            }
        }
    }

    private void SpawnMiniaturePedestrians()
    {
        pedestrians.Clear();
        pedestrianTransforms.Clear();
        pedestrianProgress.Clear();
        pedestrianSpeeds.Clear();

        Color[] attireColors = new Color[]
        {
            new Color(0.12f, 0.45f, 0.85f), // Royal Blue
            new Color(0.85f, 0.22f, 0.22f), // Crimson
            new Color(0.18f, 0.72f, 0.38f), // Emerald
            new Color(0.92f, 0.65f, 0.15f), // Gold Amber
            new Color(0.65f, 0.25f, 0.75f), // Purple
            new Color(0.20f, 0.25f, 0.30f)  // Executive Slate
        };

        int count = 6;
        for (int i = 0; i < count; i++)
        {
            GameObject citizen;
            if (boardManager.pedestrianPrefab != null && boardManager.pedestrianPrefab.GetComponentInChildren<Renderer>(true) != null)
            {
                citizen = Instantiate(boardManager.pedestrianPrefab, dioramaContainer.transform);
                citizen.name = $"Pedestrian_{i}";
                boardManager.StripRoguePhysicsFromImportedPrefab(citizen);
                boardManager.NormalizePrefabSize(citizen, 0.026f, true);
            }
            else
            {
                citizen = CreateCuteProceduralCitizen(i, attireColors[i % attireColors.Length]);
                citizen.transform.SetParent(dioramaContainer.transform, false);
            }

            CourtyardPedestrianInfo pInfo = new CourtyardPedestrianInfo
            {
                root = citizen.transform,
                progress = (float)i / count,
                speed = 0.028f + (i % 3) * 0.004f,
                animator = citizen.GetComponentInChildren<Animator>(),
                legacyAnim = citizen.GetComponentInChildren<Animation>()
            };

            // Start any existing animations
            if (pInfo.animator != null && pInfo.animator.runtimeAnimatorController != null)
            {
                pInfo.animator.enabled = true;
            }
            if (pInfo.legacyAnim != null)
            {
                pInfo.legacyAnim.Play();
            }

            // Find leg/arm child transforms for procedural walk cycle
            DiscoverLimbs(citizen.transform, pInfo);

            pedestrians.Add(pInfo);
            pedestrianTransforms.Add(citizen.transform);
            pedestrianProgress.Add(pInfo.progress);
            pedestrianSpeeds.Add(pInfo.speed);
        }
    }

    private void DiscoverLimbs(Transform root, CourtyardPedestrianInfo info)
    {
        Transform[] allChildren = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in allChildren)
        {
            if (child == root) continue;
            string n = child.name.ToLower();

            if (n.Contains("leftupleg") || n.Contains("leg_l") || n.Contains("thigh_l") || n.Contains("l_thigh") || n.Contains("thigh.l"))
            {
                info.leftLegs.Add(child);
            }
            else if (n.Contains("rightupleg") || n.Contains("leg_r") || n.Contains("thigh_r") || n.Contains("r_thigh") || n.Contains("thigh.r"))
            {
                info.rightLegs.Add(child);
            }
            else if (n.Contains("leftarm") || n.Contains("arm_l") || n.Contains("shoulder_l") || n.Contains("l_arm"))
            {
                info.leftArms.Add(child);
            }
            else if (n.Contains("rightarm") || n.Contains("arm_r") || n.Contains("shoulder_r") || n.Contains("r_arm"))
            {
                info.rightArms.Add(child);
            }
        }
    }

    private GameObject CreateCuteProceduralCitizen(int index, Color suitColor)
    {
        GameObject citizenRoot = new GameObject($"Living_Citizen_{index}");

        // Torso / Suit jacket (scaled to total height ~ 0.018f)
        GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        torso.name = "Torso";
        torso.transform.SetParent(citizenRoot.transform, false);
        torso.transform.localPosition = new Vector3(0f, 0.0085f, 0f);
        torso.transform.localScale = new Vector3(0.006f, 0.0065f, 0.0045f);
        Destroy(torso.GetComponent<Collider>());
        torso.GetComponent<Renderer>().material = boardManager.CreateColoredMaterial(suitColor, 0.85f);

        // Head (Sphere)
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(citizenRoot.transform, false);
        head.transform.localPosition = new Vector3(0f, 0.0155f, 0f);
        head.transform.localScale = Vector3.one * 0.0055f;
        Destroy(head.GetComponent<Collider>());
        head.GetComponent<Renderer>().material = boardManager.CreateColoredMaterial(new Color(0.95f, 0.82f, 0.72f), 0.7f);

        // Hat / Hair
        GameObject hat = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hat.name = "Hat";
        hat.transform.SetParent(citizenRoot.transform, false);
        hat.transform.localPosition = new Vector3(0f, 0.0182f, 0f);
        hat.transform.localScale = new Vector3(0.006f, 0.0018f, 0.006f);
        Destroy(hat.GetComponent<Collider>());
        hat.GetComponent<Renderer>().material = boardManager.CreateColoredMaterial(new Color(0.10f, 0.12f, 0.16f), 0.8f);

        // Mini Briefcase
        GameObject miniBriefcase = GameObject.CreatePrimitive(PrimitiveType.Cube);
        miniBriefcase.name = "Briefcase";
        miniBriefcase.transform.SetParent(citizenRoot.transform, false);
        miniBriefcase.transform.localPosition = new Vector3(0.0045f, 0.006f, 0f);
        miniBriefcase.transform.localScale = new Vector3(0.0018f, 0.0035f, 0.0045f);
        Destroy(miniBriefcase.GetComponent<Collider>());
        miniBriefcase.GetComponent<Renderer>().material = boardManager.CreateColoredMaterial(new Color(0.35f, 0.20f, 0.10f), 0.85f);

        return citizenRoot;
    }

    private void AnimateCourtyardPedestrians()
    {
        for (int i = 0; i < pedestrians.Count; i++)
        {
            CourtyardPedestrianInfo p = pedestrians[i];
            if (p.root == null) continue;

            p.progress += (p.speed / PERIMETER_LENGTH) * Time.deltaTime;
            if (p.progress >= 1f) p.progress -= 1f;

            // Compute perimeter position and tangent direction
            GetPerimeterPose(p.progress, out Vector3 pos, out Vector3 tangent);

            // Procedural walking step-bounce & side-to-side stride sway
            float walkTime = Time.time * 10f + i * 1.5f;
            float stepBounce = Mathf.Abs(Mathf.Sin(walkTime)) * 0.002f;
            float strideSway = Mathf.Sin(walkTime) * 4.0f;

            p.root.localPosition = new Vector3(pos.x, pos.y + stepBounce, pos.z);
            if (tangent.sqrMagnitude > 0.001f)
            {
                p.root.localRotation = Quaternion.LookRotation(tangent, Vector3.up) * Quaternion.Euler(0f, 0f, strideSway);
            }

            // Procedural limb swinging for static or rigged characters without active animator clips
            float limbSwingAngle = Mathf.Sin(walkTime) * 25f;
            foreach (Transform lLeg in p.leftLegs)
            {
                if (lLeg != null) lLeg.localRotation = Quaternion.Euler(limbSwingAngle, 0f, 0f);
            }
            foreach (Transform rLeg in p.rightLegs)
            {
                if (rLeg != null) rLeg.localRotation = Quaternion.Euler(-limbSwingAngle, 0f, 0f);
            }
            foreach (Transform lArm in p.leftArms)
            {
                if (lArm != null) lArm.localRotation = Quaternion.Euler(-limbSwingAngle * 0.8f, 0f, 0f);
            }
            foreach (Transform rArm in p.rightArms)
            {
                if (rArm != null) rArm.localRotation = Quaternion.Euler(limbSwingAngle * 0.8f, 0f, 0f);
            }
        }
    }

    private void GetPerimeterPose(float normalizedT, out Vector3 pos, out Vector3 tangent)
    {
        float total = Mathf.Repeat(normalizedT, 1f) * 4f; // 4 segments: South, East, North, West
        int seg = Mathf.FloorToInt(total);
        float frac = total - seg;
        float e = PERIMETER_EXTENT;
        float y = 0.024f; // Ground level on Central Park lawn

        switch (seg)
        {
            case 0: // South side: (-e, -e) to (e, -e) moving +X
                pos = new Vector3(Mathf.Lerp(-e, e, frac), y, -e);
                tangent = Vector3.right;
                break;
            case 1: // East side: (e, -e) to (e, e) moving +Z
                pos = new Vector3(e, y, Mathf.Lerp(-e, e, frac));
                tangent = Vector3.forward;
                break;
            case 2: // North side: (e, e) to (-e, e) moving -X
                pos = new Vector3(Mathf.Lerp(e, -e, frac), y, e);
                tangent = Vector3.left;
                break;
            default: // West side: (-e, e) to (-e, -e) moving -Z
                pos = new Vector3(-e, y, Mathf.Lerp(e, -e, frac));
                tangent = Vector3.back;
                break;
        }
    }

    #endregion
}
