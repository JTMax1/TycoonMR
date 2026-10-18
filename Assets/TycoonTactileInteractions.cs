using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

[RequireComponent(typeof(TycoonBoardManager))]
public class TycoonTactileInteractions : MonoBehaviour
{
    private TycoonBoardManager boardManager;

    // --- 1. VEGAS CUPPED-HANDS DICE RATTLE STATE ---
    private bool isCuppingDice = false;
    private Vector3 lastCuppedMidpoint;
    private Vector3 smoothedCuppedVelocity;
    private float lastRattleSFXTime = 0f;
    private Vector3 die1SpinAxis = new Vector3(1f, 0.6f, 0.3f).normalized;
    private Vector3 die2SpinAxis = new Vector3(-0.4f, 1f, -0.5f).normalized;
    private float currentDiceSpinSpeed = 0f;

    // --- 2. TWO-HANDED GRABBABLE TRADE CONTRACT STATE ---
    private GameObject activeContractScroll;
    private Transform contractLeftHalf;
    private Transform contractRightHalf;
    private Transform stampSealButton;
    private bool isTwoHandTearing = false;
    private float initialTearHandDist = 0.2f;
    private Action onContractSigned;
    private Action onContractTorn;

    void Awake()
    {
        boardManager = GetComponent<TycoonBoardManager>();
    }

    void Update()
    {
        if (boardManager == null) return;

        UpdateVegasCuppedHandsDice();
        UpdateTradeContractInteractions();
        UpdateHologramBlueprintBuilder();
    }

    // =========================================================================
    // 1. VEGAS TWO-HAND CUPPED DICE RATTLE & THROW
    // =========================================================================
    void UpdateVegasCuppedHandsDice()
    {
        // Only active when waiting for player's roll and both hands are tracked
        if (!boardManager.IsWaitingForRoll)
        {
            if (isCuppingDice) isCuppingDice = false;
            return;
        }

        if (!boardManager.IsLeftHandTracked || !boardManager.IsRightHandTracked)
        {
            if (isCuppingDice)
            {
                // Released if tracking lost
                ReleaseAndThrowDice(smoothedCuppedVelocity);
            }
            return;
        }

        Vector3 leftPos = boardManager.LeftHandWorldPos;
        Vector3 rightPos = boardManager.RightHandWorldPos;

        // Verify hands are reasonably positioned in front of player
        if (leftPos.y < -50f || rightPos.y < -50f) return;

        float handDist = Vector3.Distance(leftPos, rightPos);
        Vector3 currentMidpoint = (leftPos + rightPos) * 0.5f;

        if (!isCuppingDice)
        {
            // Enter cupping state when palms are brought together within 14cm
            if (handDist < 0.14f && !boardManager.IsPlayerHoldingDice)
            {
                if (boardManager.PhysDie1 != null && boardManager.PhysDie2 != null)
                {
                    isCuppingDice = true;
                    lastCuppedMidpoint = currentMidpoint;
                    smoothedCuppedVelocity = Vector3.zero;

                    // Make dice kinematic and place them between palms
                    boardManager.PhysDie1.GetComponent<Rigidbody>().isKinematic = true;
                    boardManager.PhysDie2.GetComponent<Rigidbody>().isKinematic = true;

                    Vector3 handRight = (rightPos - leftPos).normalized;
                    boardManager.Die1Transform.position = currentMidpoint - handRight * 0.022f;
                    boardManager.Die2Transform.position = currentMidpoint + handRight * 0.022f;

                    boardManager.PlayDiceRattleSound();
                    boardManager.PlayHaptic(0.4f, 0.08f);
                    boardManager.RefreshDashboard(
                        "VEGAS CUPPED-HANDS DICE RATTLE!",
                        "Shake your hands together to rattle the dice, then OPEN PALMS to throw!"
                    );
                }
            }
        }
        else
        {
            // While cupped: calculate hand shake velocity
            Vector3 frameVel = (currentMidpoint - lastCuppedMidpoint) / Mathf.Max(0.0001f, Time.deltaTime);
            smoothedCuppedVelocity = Vector3.Lerp(smoothedCuppedVelocity, frameVel, Time.deltaTime * 14f);
            lastCuppedMidpoint = currentMidpoint;

            Vector3 handRight = (rightPos - leftPos).normalized;
            float rollAngle = Time.time * 7f;
            Vector3 smoothOrbit = new Vector3(Mathf.Sin(rollAngle), Mathf.Cos(rollAngle) * 0.5f, 0f) * 0.005f;

            if (boardManager.Die1Transform != null)
                boardManager.Die1Transform.position = currentMidpoint - handRight * 0.022f + smoothOrbit;
            if (boardManager.Die2Transform != null)
                boardManager.Die2Transform.position = currentMidpoint + handRight * 0.022f - smoothOrbit;

            float shakeSpeed = smoothedCuppedVelocity.magnitude;
            if (shakeSpeed > 0.18f)
            {
                // Smooth continuous tumbling with evolving 3D rotation axes
                float targetSpinSpeed = Mathf.Clamp(shakeSpeed * 1200f, 360f, 1440f);
                currentDiceSpinSpeed = Mathf.Lerp(currentDiceSpinSpeed, targetSpinSpeed, Time.deltaTime * 12f);

                die1SpinAxis = Vector3.Slerp(die1SpinAxis, new Vector3(Mathf.Sin(Time.time * 3.5f), Mathf.Cos(Time.time * 2.8f), Mathf.Sin(Time.time * 4.2f)).normalized, Time.deltaTime * 2.5f);
                die2SpinAxis = Vector3.Slerp(die2SpinAxis, new Vector3(Mathf.Cos(Time.time * 4.2f), Mathf.Sin(Time.time * 3.1f), Mathf.Cos(Time.time * 2.6f)).normalized, Time.deltaTime * 2.5f);

                if (boardManager.Die1Transform != null)
                    boardManager.Die1Transform.Rotate(die1SpinAxis, currentDiceSpinSpeed * Time.deltaTime, Space.Self);
                if (boardManager.Die2Transform != null)
                    boardManager.Die2Transform.Rotate(die2SpinAxis, currentDiceSpinSpeed * 1.15f * Time.deltaTime, Space.Self);

                // Rapid spatial clicks and haptics
                if (Time.time - lastRattleSFXTime > 0.09f)
                {
                    lastRattleSFXTime = Time.time;
                    boardManager.PlayDiceRattleSound();
                    boardManager.PlayHaptic(0.35f, 0.06f);
                }
            }
            else
            {
                currentDiceSpinSpeed = Mathf.Lerp(currentDiceSpinSpeed, 0f, Time.deltaTime * 8f);
                if (currentDiceSpinSpeed > 5f)
                {
                    if (boardManager.Die1Transform != null) boardManager.Die1Transform.Rotate(die1SpinAxis, currentDiceSpinSpeed * Time.deltaTime, Space.Self);
                    if (boardManager.Die2Transform != null) boardManager.Die2Transform.Rotate(die2SpinAxis, currentDiceSpinSpeed * 1.15f * Time.deltaTime, Space.Self);
                }
            }

            // Release condition: Player spreads palms apart (> 19cm)
            if (handDist > 0.19f)
            {
                ReleaseAndThrowDice(smoothedCuppedVelocity);
            }
        }
    }

    void ReleaseAndThrowDice(Vector3 throwVelocity)
    {
        if (!isCuppingDice) return;
        isCuppingDice = false;

        Vector3 boardCenter = boardManager.transform.position;
        Vector3 forwardArc = (boardCenter - lastCuppedMidpoint).normalized * 0.85f + Vector3.up * 0.6f;
        Vector3 finalThrow = throwVelocity;
        if (finalThrow.magnitude < 0.4f)
        {
            finalThrow = forwardArc;
        }

        if (boardManager.PhysDie1 != null)
        {
            boardManager.PhysDie1.LaunchWithDirectVelocity(finalThrow + UnityEngine.Random.insideUnitSphere * 0.08f, boardCenter);
        }
        if (boardManager.PhysDie2 != null)
        {
            boardManager.PhysDie2.LaunchWithDirectVelocity(finalThrow + UnityEngine.Random.insideUnitSphere * 0.08f, boardCenter);
        }

        boardManager.PlayDiceRattleSound();
        boardManager.PlayHaptic(0.6f, 0.15f);
        boardManager.ColocationAndPolish?.BroadcastDiceThrow(finalThrow, -1, -1);
        boardManager.TriggerPhysicsDiceRoll(useImpulseToss: false);
    }

    // =========================================================================
    // 2. TWO-HANDED GRABBABLE TRADE CONTRACT (STAMP TO ACCEPT / RIP TO REJECT)
    // =========================================================================
    public void SpawnExecutiveTradeContract(string titleText, string detailsText, Action onAccept, Action onReject)
    {
        DismissTradeContract();

        onContractSigned = onAccept;
        onContractTorn = onReject;

        (Vector3 spawnPos, Quaternion spawnRot) = boardManager.GetElevatedBoardFrontPose(0.36f, 0.24f);

        activeContractScroll = new GameObject("Executive_Contract_Scroll");
        activeContractScroll.transform.position = spawnPos;
        activeContractScroll.transform.rotation = spawnRot;
        activeContractScroll.transform.localScale = Vector3.one * 0.62f;

        Material parchmentMat = boardManager.CreateColoredMaterial(new Color(0.97f, 0.94f, 0.85f), 0.35f, 0.02f);
        Material goldTrimMat = boardManager.CreateColoredMaterial(new Color(0.85f, 0.68f, 0.24f), 0.85f, 0.8f);
        Material waxSealMat = boardManager.CreateColoredMaterial(new Color(0.82f, 0.15f, 0.18f), 0.65f, 0.1f);

        // --- LEFT HALF ---
        GameObject leftHalfGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftHalfGO.name = "Contract_LeftHalf";
        leftHalfGO.transform.SetParent(activeContractScroll.transform, false);
        leftHalfGO.transform.localPosition = new Vector3(-0.11f, 0f, 0f);
        leftHalfGO.transform.localScale = new Vector3(0.22f, 0.30f, 0.006f);
        leftHalfGO.GetComponent<Renderer>().material = parchmentMat;

        Rigidbody leftRb = leftHalfGO.GetComponent<Rigidbody>();
        if (leftRb == null) leftRb = leftHalfGO.AddComponent<Rigidbody>();
        leftRb.useGravity = false;
        leftRb.isKinematic = true;
        boardManager.AttachNativeBuildingBlockGrab(leftHalfGO, leftRb);
        contractLeftHalf = leftHalfGO.transform;

        // Left gold roller rod
        GameObject leftRod = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        leftRod.transform.SetParent(contractLeftHalf, false);
        leftRod.transform.localPosition = new Vector3(-0.52f, 0f, 0f);
        leftRod.transform.localScale = new Vector3(0.06f, 0.58f, 0.06f);
        Destroy(leftRod.GetComponent<Collider>());
        leftRod.GetComponent<Renderer>().material = goldTrimMat;

        // --- RIGHT HALF ---
        GameObject rightHalfGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightHalfGO.name = "Contract_RightHalf";
        rightHalfGO.transform.SetParent(activeContractScroll.transform, false);
        rightHalfGO.transform.localPosition = new Vector3(0.11f, 0f, 0f);
        rightHalfGO.transform.localScale = new Vector3(0.22f, 0.30f, 0.006f);
        rightHalfGO.GetComponent<Renderer>().material = parchmentMat;

        Rigidbody rightRb = rightHalfGO.GetComponent<Rigidbody>();
        if (rightRb == null) rightRb = rightHalfGO.AddComponent<Rigidbody>();
        rightRb.useGravity = false;
        rightRb.isKinematic = true;
        boardManager.AttachNativeBuildingBlockGrab(rightHalfGO, rightRb);
        contractRightHalf = rightHalfGO.transform;

        // Right gold roller rod
        GameObject rightRod = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rightRod.transform.SetParent(contractRightHalf, false);
        rightRod.transform.localPosition = new Vector3(0.52f, 0f, 0f);
        rightRod.transform.localScale = new Vector3(0.06f, 0.58f, 0.06f);
        Destroy(rightRod.GetComponent<Collider>());
        rightRod.GetComponent<Renderer>().material = goldTrimMat;

        // --- GOLD WAX SEAL / STAMP BUTTON (ACCEPT) ---
        if (boardManager.waxSealStampPrefab != null)
        {
            GameObject sealObj = Instantiate(boardManager.waxSealStampPrefab, activeContractScroll.transform, false);
            sealObj.name = "Contract_Wax_Seal";
            boardManager.StripRoguePhysicsFromImportedPrefab(sealObj);
            boardManager.NormalizePrefabSize(sealObj, 0.06f);
            sealObj.transform.localPosition = new Vector3(0f, -0.09f, -0.008f);
            stampSealButton = sealObj.transform;
        }
        else
        {
            GameObject sealObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            sealObj.name = "Contract_Wax_Seal";
            sealObj.transform.SetParent(activeContractScroll.transform, false);
            sealObj.transform.localPosition = new Vector3(0f, -0.09f, -0.008f);
            sealObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            sealObj.transform.localScale = new Vector3(0.055f, 0.006f, 0.055f);
            Destroy(sealObj.GetComponent<Collider>());
            sealObj.GetComponent<Renderer>().material = waxSealMat;
            stampSealButton = sealObj.transform;
        }

        // --- CONTRACT TEXT CANVAS ---
        GameObject canvasGO = new GameObject("Contract_TMP_Canvas");
        canvasGO.transform.SetParent(activeContractScroll.transform, false);
        canvasGO.transform.localPosition = new Vector3(0f, 0f, -0.012f);
        canvasGO.transform.localScale = Vector3.one * 0.00045f;

        Canvas c = canvasGO.AddComponent<Canvas>();
        c.renderMode = RenderMode.WorldSpace;
        canvasGO.GetComponent<RectTransform>().sizeDelta = new Vector2(850f, 600f);

        TextMeshProUGUI contractTitleTMP = CreateContractText(canvasGO.transform, "Title", new Vector2(0, 220), 32, new Color(0.12f, 0.14f, 0.18f), FontStyles.Bold);
        contractTitleTMP.text = titleText;

        TextMeshProUGUI contractDetailsTMP = CreateContractText(canvasGO.transform, "Details", new Vector2(0, 45), 23, new Color(0.18f, 0.20f, 0.24f), FontStyles.Normal);
        contractDetailsTMP.text = detailsText;

        TextMeshProUGUI instructionsTMP = CreateContractText(canvasGO.transform, "Instructions", new Vector2(0, -220), 19, new Color(0.65f, 0.18f, 0.18f), FontStyles.Italic);
        instructionsTMP.text = "<b>[STAMP WAX SEAL]</b> to Sign & Execute\n<b>[GRAB BOTH SIDES & PULL APART]</b> to Rip & Reject Deal";

        isTwoHandTearing = false;
        boardManager.PlaySFX(boardManager.TokenMoveSound, null, 1.15f);
        boardManager.PlayHaptic(0.4f, 0.1f);
    }

    TextMeshProUGUI CreateContractText(Transform parent, string name, Vector2 pos, float fontSize, Color color, FontStyles style)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        RectTransform rt = tmp.rectTransform;
        rt.sizeDelta = new Vector2(800f, 150f);
        rt.anchoredPosition = pos;
        return tmp;
    }

    void UpdateTradeContractInteractions()
    {
        if (activeContractScroll == null) return;

        Vector3 leftPos = boardManager.LeftHandWorldPos;
        Vector3 rightPos = boardManager.RightHandWorldPos;
        bool leftTracked = boardManager.IsLeftHandTracked && leftPos.y > -50f;
        bool rightTracked = boardManager.IsRightHandTracked && rightPos.y > -50f;

        // 1. Check Stamp to Sign (Left or Right hand tap near Wax Seal, or Space bar desktop test)
        if (stampSealButton != null)
        {
            float distLeftToSeal = leftTracked ? Vector3.Distance(leftPos, stampSealButton.position) : 999f;
            float distRightToSeal = rightTracked ? Vector3.Distance(rightPos, stampSealButton.position) : 999f;
            bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

            if (distLeftToSeal < 0.085f || distRightToSeal < 0.085f || spacePressed)
            {
                SignAndAcceptContract();
                return;
            }
        }

        // 2. Check Two-Handed Pull Apart to Rip
        if (leftTracked && rightTracked && contractLeftHalf != null && contractRightHalf != null)
        {
            float distLeftHand = Vector3.Distance(leftPos, contractLeftHalf.position);
            float distRightHand = Vector3.Distance(rightPos, contractRightHalf.position);
            float handDist = Vector3.Distance(leftPos, rightPos);

            if (distLeftHand < 0.12f && distRightHand < 0.12f)
            {
                if (!isTwoHandTearing)
                {
                    isTwoHandTearing = true;
                    initialTearHandDist = handDist;
                    boardManager.PlayHaptic(0.3f, 0.05f);
                }
                else
                {
                    // If pulled apart beyond initial grab distance + 18cm: RIP!
                    if (handDist > initialTearHandDist + 0.18f)
                    {
                        RipAndRejectContract();
                        return;
                    }
                }
            }
            else
            {
                isTwoHandTearing = false;
            }
        }
    }

    void SignAndAcceptContract()
    {
        if (activeContractScroll == null) return;

        boardManager.PlaySFX(boardManager.CashRegisterSound, null, 1.25f);
        boardManager.PlayHaptic(0.7f, 0.2f);
        boardManager.RefreshDashboard("CONTRACT SIGNED & SEALED!", "The trade deal has been officially ratified!");

        onContractSigned?.Invoke();
        DismissTradeContract();
    }

    void RipAndRejectContract()
    {
        if (activeContractScroll == null) return;

        boardManager.PlaySFX(boardManager.DiceRollSound, null, 0.65f);
        boardManager.PlayHaptic(0.9f, 0.25f);
        boardManager.SetRivalReaction(TycoonBoardManager.AvatarMood.AngrySlam, "You ripped the contract in half?! Unbelievable!");
        boardManager.RefreshDashboard("CONTRACT TORN TO PIECES!", "Deal rejected and negotiations terminated!");

        // Animate the two halves separating
        if (contractLeftHalf != null && contractRightHalf != null)
        {
            StartCoroutine(AnimateTornContractHalves(contractLeftHalf.gameObject, contractRightHalf.gameObject));
        }

        onContractTorn?.Invoke();
        DismissTradeContract(delayDestroy: true);
    }

    IEnumerator AnimateTornContractHalves(GameObject leftHalf, GameObject rightHalf)
    {
        float duration = 0.65f;
        float elapsed = 0f;
        Vector3 leftStart = leftHalf.transform.position;
        Vector3 rightStart = rightHalf.transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (leftHalf != null)
            {
                leftHalf.transform.position = leftStart - leftHalf.transform.right * (t * 0.18f) - Vector3.up * (t * 0.12f);
                leftHalf.transform.Rotate(new Vector3(0f, 0f, 45f) * Time.deltaTime);
            }
            if (rightHalf != null)
            {
                rightHalf.transform.position = rightStart + rightHalf.transform.right * (t * 0.18f) - Vector3.up * (t * 0.12f);
                rightHalf.transform.Rotate(new Vector3(0f, 0f, -45f) * Time.deltaTime);
            }
            yield return null;
        }

        if (leftHalf != null) Destroy(leftHalf);
        if (rightHalf != null) Destroy(rightHalf);
    }

    public void DismissTradeContract(bool delayDestroy = false)
    {
        if (activeContractScroll != null)
        {
            if (!delayDestroy)
            {
                Destroy(activeContractScroll);
            }
            activeContractScroll = null;
        }
        isTwoHandTearing = false;
        onContractSigned = null;
        onContractTorn = null;
    }

    // =========================================================================
    // 3. THE HOLOGRAM BLUEPRINT BUILDER (PROPERTY UPGRADES)
    // =========================================================================
    private GameObject activeBlueprintRoot;
    private Transform blueprintWireframe;
    private Transform modernOrbTransform;
    private Transform classicOrbTransform;
    private Renderer modernOrbRenderer;
    private Renderer classicOrbRenderer;
    private int selectedArchitectureStyle = 0; // 0: Modern Glass, 1: Classic Executive
    private Action<int> onBlueprintConstructed;
    private Vector3 targetTilePlotWorldPos;
    private float lastBlueprintActionTime = 0f;
    private bool isSlammingBlueprint = false;

    public void SpawnHologramBlueprint(TycoonBoardManager.BoardTile targetTile, Action<int> onConstruct)
    {
        DismissHologramBlueprint();

        if (targetTile == null || targetTile.tileTransform == null) return;
        onBlueprintConstructed = onConstruct;
        targetTilePlotWorldPos = targetTile.tileTransform.position;

        activeBlueprintRoot = new GameObject($"Hologram_Blueprint_{targetTile.name}");
        activeBlueprintRoot.transform.position = targetTilePlotWorldPos + Vector3.up * 0.10f;
        activeBlueprintRoot.transform.rotation = targetTile.tileTransform.rotation;

        Material cyanHoloMat = boardManager.CreateColoredMaterial(new Color(0.15f, 0.85f, 1f, 0.75f), 0.95f, 0.2f);
        Material goldHoloMat = boardManager.CreateColoredMaterial(new Color(1f, 0.84f, 0.25f, 0.75f), 0.95f, 0.5f);

        // Holographic Wireframe Cube Box
        GameObject wireframeGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wireframeGO.name = "Wireframe_Box";
        wireframeGO.transform.SetParent(activeBlueprintRoot.transform, false);
        wireframeGO.transform.localPosition = Vector3.zero;
        wireframeGO.transform.localScale = new Vector3(0.075f, 0.11f, 0.075f);
        Destroy(wireframeGO.GetComponent<Collider>());
        wireframeGO.GetComponent<Renderer>().material = cyanHoloMat;
        blueprintWireframe = wireframeGO.transform;

        // Top Roof Accent
        GameObject roofPyramid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        roofPyramid.transform.SetParent(blueprintWireframe, false);
        roofPyramid.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        roofPyramid.transform.localScale = new Vector3(0.85f, 0.18f, 0.85f);
        Destroy(roofPyramid.GetComponent<Collider>());
        roofPyramid.GetComponent<Renderer>().material = goldHoloMat;

        // Floating Title Canvas
        GameObject canvasGO = new GameObject("Blueprint_Canvas");
        canvasGO.transform.SetParent(activeBlueprintRoot.transform, false);
        canvasGO.transform.localPosition = new Vector3(0f, 0.09f, 0f);
        canvasGO.transform.localScale = Vector3.one * 0.00045f;
        Canvas c = canvasGO.AddComponent<Canvas>();
        c.renderMode = RenderMode.WorldSpace;
        canvasGO.GetComponent<RectTransform>().sizeDelta = new Vector2(750f, 250f);

        TextMeshProUGUI titleTMP = canvasGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text = $"<b>HOLOGRAM BLUEPRINT: {targetTile.name.ToUpper()}</b>\n<size=22><color=#66DDFF>[MODERN GLASS]</color> vs <color=#FFD700>[CLASSIC EXECUTIVE]</color>\n<color=#00FF88>SLAM HAND DOWNWARD TO HAMMER INTO PLOT!</color></size>";
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.fontSize = 26f;
        titleTMP.color = Color.white;

        // 2 Floating Style Selector Orbs
        // Orb 0: Modern Glass (Left)
        GameObject orb0 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb0.name = "Selector_Orb_Modern";
        orb0.transform.SetParent(activeBlueprintRoot.transform, false);
        orb0.transform.localPosition = new Vector3(-0.065f, 0.035f, 0f);
        orb0.transform.localScale = Vector3.one * 0.035f;
        modernOrbRenderer = orb0.GetComponent<Renderer>();
        modernOrbRenderer.material = cyanHoloMat;
        modernOrbTransform = orb0.transform;

        // Orb 1: Classic Executive (Right)
        GameObject orb1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb1.name = "Selector_Orb_Classic";
        orb1.transform.SetParent(activeBlueprintRoot.transform, false);
        orb1.transform.localPosition = new Vector3(0.065f, 0.035f, 0f);
        orb1.transform.localScale = Vector3.one * 0.035f;
        classicOrbRenderer = orb1.GetComponent<Renderer>();
        classicOrbRenderer.material = goldHoloMat;
        classicOrbTransform = orb1.transform;

        selectedArchitectureStyle = 0;
        isSlammingBlueprint = false;

        boardManager.PlaySFX(boardManager.BuildUpgradeSound, null, 1.25f);
        boardManager.PlayHaptic(0.5f, 0.15f);
        boardManager.RefreshDashboard(
            $"BLUEPRINT: {targetTile.name.ToUpper()}",
            "Poke orb to switch architecture style, then SLAM HAND DOWNWARD to construct!"
        );
    }

    void UpdateHologramBlueprintBuilder()
    {
        if (activeBlueprintRoot == null || isSlammingBlueprint) return;

        // Rotate wireframe slightly for holographic float effect
        if (blueprintWireframe != null)
        {
            blueprintWireframe.Rotate(Vector3.up, 35f * Time.deltaTime, Space.Self);
        }

        Vector3 leftPos = boardManager.LeftHandWorldPos;
        Vector3 rightPos = boardManager.RightHandWorldPos;
        bool leftTracked = boardManager.IsLeftHandTracked && leftPos.y > -50f;
        bool rightTracked = boardManager.IsRightHandTracked && rightPos.y > -50f;

        // 1. Check Style Selector Orb Poke
        if (Time.time > lastBlueprintActionTime)
        {
            if (modernOrbTransform != null)
            {
                float dL = leftTracked ? Vector3.Distance(leftPos, modernOrbTransform.position) : 99f;
                float dR = rightTracked ? Vector3.Distance(rightPos, modernOrbTransform.position) : 99f;
                if (dL < 0.035f || dR < 0.035f)
                {
                    selectedArchitectureStyle = 0;
                    lastBlueprintActionTime = Time.time + 0.35f;
                    boardManager.PlayHaptic(0.4f, 0.08f);
                    boardManager.PlaySFX(boardManager.TokenMoveSound, null, 1.4f);
                    if (blueprintWireframe != null)
                    {
                        blueprintWireframe.GetComponent<Renderer>().material = boardManager.CreateColoredMaterial(new Color(0.15f, 0.85f, 1f, 0.75f), 0.95f, 0.2f);
                    }
                    boardManager.RefreshDashboard("SELECTED: MODERN GLASS STYLE", "SLAM HAND DOWNWARD into tile plot to construct!");
                }
            }

            if (classicOrbTransform != null)
            {
                float dL = leftTracked ? Vector3.Distance(leftPos, classicOrbTransform.position) : 99f;
                float dR = rightTracked ? Vector3.Distance(rightPos, classicOrbTransform.position) : 99f;
                if (dL < 0.035f || dR < 0.035f)
                {
                    selectedArchitectureStyle = 1;
                    lastBlueprintActionTime = Time.time + 0.35f;
                    boardManager.PlayHaptic(0.4f, 0.08f);
                    boardManager.PlaySFX(boardManager.TokenMoveSound, null, 1.1f);
                    if (blueprintWireframe != null)
                    {
                        blueprintWireframe.GetComponent<Renderer>().material = boardManager.CreateColoredMaterial(new Color(1f, 0.84f, 0.25f, 0.75f), 0.95f, 0.5f);
                    }
                    boardManager.RefreshDashboard("SELECTED: CLASSIC EXECUTIVE STYLE", "SLAM HAND DOWNWARD into tile plot to construct!");
                }
            }
        }

        // 2. Check Downward Hand Slam / Plunge into the Plot
        Vector3 rootPos = activeBlueprintRoot.transform.position;
        float dLeftPlot = leftTracked ? Vector3.Distance(new Vector3(leftPos.x, rootPos.y, leftPos.z), rootPos) : 99f;
        float dRightPlot = rightTracked ? Vector3.Distance(new Vector3(rightPos.x, rootPos.y, rightPos.z), rootPos) : 99f;

        bool leftPlunged = leftTracked && dLeftPlot < 0.09f && leftPos.y < (rootPos.y - 0.025f);
        bool rightPlunged = rightTracked && dRightPlot < 0.09f && rightPos.y < (rootPos.y - 0.025f);
        bool keyboardSlam = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame;

        if (leftPlunged || rightPlunged || keyboardSlam)
        {
            isSlammingBlueprint = true;
            StartCoroutine(AnimateBlueprintHammerDown());
        }
    }

    IEnumerator AnimateBlueprintHammerDown()
    {
        boardManager.PlayHaptic(0.9f, 0.3f);
        boardManager.PlaySFX(boardManager.BuildUpgradeSound, null, 0.95f);

        float duration = 0.25f;
        float elapsed = 0f;
        Vector3 startPos = activeBlueprintRoot.transform.position;
        Vector3 endPos = targetTilePlotWorldPos + Vector3.up * 0.015f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (activeBlueprintRoot != null)
            {
                activeBlueprintRoot.transform.position = Vector3.Lerp(startPos, endPos, t * t);
                activeBlueprintRoot.transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(1.15f, 0.35f, 1.15f), t);
            }
            yield return null;
        }

        // Expanding golden shockwave ring
        GameObject shockwave = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shockwave.name = "Blueprint_Shockwave";
        shockwave.transform.position = targetTilePlotWorldPos + Vector3.up * 0.02f;
        shockwave.transform.localScale = new Vector3(0.04f, 0.002f, 0.04f);
        Destroy(shockwave.GetComponent<Collider>());
        Material goldRingMat = boardManager.CreateColoredMaterial(new Color(1f, 0.85f, 0.25f), 0.95f, 0.85f);
        shockwave.GetComponent<Renderer>().material = goldRingMat;

        // Finalize building construction callback
        var cb = onBlueprintConstructed;
        int style = selectedArchitectureStyle;
        DismissHologramBlueprint();

        cb?.Invoke(style);

        // Animate expanding shockwave over 0.35s
        elapsed = 0f;
        while (elapsed < 0.35f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.35f;
            if (shockwave != null)
            {
                shockwave.transform.localScale = new Vector3(0.04f + t * 0.22f, 0.002f, 0.04f + t * 0.22f);
            }
            yield return null;
        }
        if (shockwave != null) Destroy(shockwave);
    }

    public void DismissHologramBlueprint()
    {
        if (activeBlueprintRoot != null)
        {
            Destroy(activeBlueprintRoot);
            activeBlueprintRoot = null;
        }
        isSlammingBlueprint = false;
        onBlueprintConstructed = null;
    }
}
