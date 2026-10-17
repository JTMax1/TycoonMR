using System;
using System.Collections;
using UnityEngine;
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
            Vector3 jitter1 = UnityEngine.Random.insideUnitSphere * 0.004f;
            Vector3 jitter2 = UnityEngine.Random.insideUnitSphere * 0.004f;

            if (boardManager.Die1Transform != null)
                boardManager.Die1Transform.position = currentMidpoint - handRight * 0.022f + jitter1;
            if (boardManager.Die2Transform != null)
                boardManager.Die2Transform.position = currentMidpoint + handRight * 0.022f + jitter2;

            float shakeSpeed = smoothedCuppedVelocity.magnitude;
            if (shakeSpeed > 0.25f)
            {
                // Tumble dice inside palms
                if (boardManager.Die1Transform != null)
                    boardManager.Die1Transform.Rotate(UnityEngine.Random.onUnitSphere * 540f * Time.deltaTime);
                if (boardManager.Die2Transform != null)
                    boardManager.Die2Transform.Rotate(UnityEngine.Random.onUnitSphere * 540f * Time.deltaTime);

                // Rapid spatial clicks and haptics
                if (Time.time - lastRattleSFXTime > 0.09f)
                {
                    lastRattleSFXTime = Time.time;
                    boardManager.PlayDiceRattleSound();
                    boardManager.PlayHaptic(0.35f, 0.06f);
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

        Transform eye = Camera.main != null ? Camera.main.transform : boardManager.transform;
        Vector3 spawnPos = eye.position + eye.forward * 0.48f - eye.up * 0.12f;
        Quaternion spawnRot = Quaternion.LookRotation(eye.forward, Vector3.up);

        activeContractScroll = new GameObject("Executive_Contract_Scroll");
        activeContractScroll.transform.position = spawnPos;
        activeContractScroll.transform.rotation = spawnRot;
        activeContractScroll.transform.localScale = Vector3.one * 0.95f;

        Material parchmentMat = boardManager.CreateColoredMaterial(new Color(0.97f, 0.94f, 0.85f), 0.35f, 0.02f);
        Material goldTrimMat = boardManager.CreateColoredMaterial(new Color(0.85f, 0.68f, 0.24f), 0.85f, 0.8f);
        Material waxSealMat = boardManager.CreateColoredMaterial(new Color(0.82f, 0.15f, 0.18f), 0.65f, 0.1f);

        // --- LEFT HALF ---
        GameObject leftHalfGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftHalfGO.name = "Contract_LeftHalf";
        leftHalfGO.transform.SetParent(activeContractScroll.transform, false);
        leftHalfGO.transform.localPosition = new Vector3(-0.115f, 0f, 0f);
        leftHalfGO.transform.localScale = new Vector3(0.22f, 0.30f, 0.006f);
        Destroy(leftHalfGO.GetComponent<Collider>());
        leftHalfGO.GetComponent<Renderer>().material = parchmentMat;
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
        rightHalfGO.transform.localPosition = new Vector3(0.115f, 0f, 0f);
        rightHalfGO.transform.localScale = new Vector3(0.22f, 0.30f, 0.006f);
        Destroy(rightHalfGO.GetComponent<Collider>());
        rightHalfGO.GetComponent<Renderer>().material = parchmentMat;
        contractRightHalf = rightHalfGO.transform;

        // Right gold roller rod
        GameObject rightRod = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rightRod.transform.SetParent(contractRightHalf, false);
        rightRod.transform.localPosition = new Vector3(0.52f, 0f, 0f);
        rightRod.transform.localScale = new Vector3(0.06f, 0.58f, 0.06f);
        Destroy(rightRod.GetComponent<Collider>());
        rightRod.GetComponent<Renderer>().material = goldTrimMat;

        // --- GOLD WAX SEAL / STAMP BUTTON (ACCEPT) ---
        GameObject sealObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        sealObj.name = "Contract_Wax_Seal";
        sealObj.transform.SetParent(activeContractScroll.transform, false);
        sealObj.transform.localPosition = new Vector3(0f, -0.09f, -0.008f);
        sealObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        sealObj.transform.localScale = new Vector3(0.055f, 0.006f, 0.055f);
        Destroy(sealObj.GetComponent<Collider>());
        sealObj.GetComponent<Renderer>().material = waxSealMat;
        stampSealButton = sealObj.transform;

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

        // 1. Check Stamp to Sign (Right hand tap or pinch near Wax Seal)
        if (stampSealButton != null && rightTracked)
        {
            float distToSeal = Vector3.Distance(rightPos, stampSealButton.position);
            if (distToSeal < 0.075f)
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
}
