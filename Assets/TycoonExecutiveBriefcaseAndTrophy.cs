using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Phase 17: Physical 3D Executive Cash Briefcase, Live Money Stacks, Interactive Audit Bonus, and Grabbable Victory Trophy Podium
/// </summary>
[RequireComponent(typeof(TycoonBoardManager))]
public class TycoonExecutiveBriefcaseAndTrophy : MonoBehaviour
{
    private TycoonBoardManager boardManager;

    [Header("1. Executive Cash Briefcase")]
    private GameObject briefcaseRoot;
    private Transform briefcaseLidTransform;
    private Quaternion lidClosedRot;
    private Quaternion lidDefaultOpenRot;
    private Quaternion lidPopOpenRot;
    private Coroutine lidPopCoroutine;

    // 6 Internal 3D Dollar Bill Stacks
    private GameObject[] cashStackObjects = new GameObject[6];
    private Transform[] cashStackBands = new Transform[6];
    private GameObject goldBarsHolder;

    private int lastObservedCash = 1500;
    private bool isBriefcaseInitialized = false;

    [Header("2. Executive Audit Bonus Floating Stack")]
    private GameObject floatingAuditBonusStack;
    private Coroutine auditBonusCoroutine;
    private bool isAuditBonusActive = false;

    [Header("3. Golden Victory Trophy Podium")]
    private GameObject victoryPodiumRoot;
    private GameObject victoryTrophyGO;
    private bool isTrophySpawned = false;

    void Awake()
    {
        boardManager = GetComponent<TycoonBoardManager>();
    }

    void Start()
    {
        // Subscribe to Board Manager events
        boardManager.OnRivalPaidRentToPlayer += HandleRivalPaidRent;
        boardManager.OnVictoryTrophyTriggered += HandleVictoryTrophyTriggered;

        // Spawn Executive Cash Briefcase at South-Left corner
        SpawnExecutiveBriefcase();

        lastObservedCash = boardManager.PlayerCash;
        UpdateCashStacksVisuals(lastObservedCash);
    }

    void OnDestroy()
    {
        if (boardManager != null)
        {
            boardManager.OnRivalPaidRentToPlayer -= HandleRivalPaidRent;
            boardManager.OnVictoryTrophyTriggered -= HandleVictoryTrophyTriggered;
        }
    }

    void Update()
    {
        // 1. Monitor live player cash changes every frame
        int currentCash = boardManager.PlayerCash;
        if (currentCash != lastObservedCash)
        {
            if (currentCash > lastObservedCash)
            {
                // Cash increased (passing GO, rent, minigame win, jackpot, etc.)
                AnimateBriefcaseLidPop();
            }

            lastObservedCash = currentCash;
            UpdateCashStacksVisuals(currentCash);
        }

        // 2. Monitor tactile poke on floating audit bonus stack
        if (isAuditBonusActive && floatingAuditBonusStack != null)
        {
            CheckAuditBonusHandPoke();
        }
    }

    #region A. Physical 3D Executive Cash Briefcase & Live Stacks

    private Vector3[] baseStackScales = new Vector3[6];
    private Vector3[] baseStackPositions = new Vector3[6];

    private void SpawnExecutiveBriefcase()
    {
        if (briefcaseRoot != null) Destroy(briefcaseRoot);

        briefcaseRoot = new GameObject("Executive_Cash_Briefcase");
        briefcaseRoot.transform.SetParent(transform, false);

        // Dedicated Executive Side-Tray on outer South-West corner (completely outside property tiles)
        briefcaseRoot.transform.localPosition = new Vector3(-0.62f, 0.020f, -0.42f);
        briefcaseRoot.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);

        // Sleek dark-marble mini coaster underneath briefcase
        GameObject coaster = GameObject.CreatePrimitive(PrimitiveType.Cube);
        coaster.name = "Executive_Side_Tray_Coaster";
        coaster.transform.SetParent(briefcaseRoot.transform, false);
        coaster.transform.localPosition = new Vector3(0f, -0.002f, 0f);
        coaster.transform.localRotation = Quaternion.identity;
        coaster.transform.localScale = new Vector3(0.16f, 0.015f, 0.12f);
        Destroy(coaster.GetComponent<Collider>());
        coaster.GetComponent<Renderer>().material = boardManager.CreateColoredMaterial(new Color(0.06f, 0.07f, 0.09f), 0.95f, 0.3f);

        GameObject coasterRim = GameObject.CreatePrimitive(PrimitiveType.Cube);
        coasterRim.name = "Coaster_Gold_Trim";
        coasterRim.transform.SetParent(coaster.transform, false);
        coasterRim.transform.localPosition = Vector3.zero;
        coasterRim.transform.localScale = new Vector3(1.04f, 0.6f, 1.04f);
        Destroy(coasterRim.GetComponent<Collider>());
        coasterRim.GetComponent<Renderer>().material = boardManager.CreateColoredMaterial(new Color(0.95f, 0.78f, 0.25f), 0.95f, 0.85f);

        if (boardManager.cashBriefcasePrefab != null)
        {
            GameObject prefabInst = Instantiate(boardManager.cashBriefcasePrefab, briefcaseRoot.transform);
            prefabInst.transform.localPosition = Vector3.zero;
            prefabInst.transform.localRotation = Quaternion.identity;
            boardManager.StripRoguePhysicsFromImportedPrefab(prefabInst);
            boardManager.NormalizePrefabSize(prefabInst, 0.11f, true);

            // Search for lid
            Transform lid = prefabInst.transform.Find("Briefcase_Lid");
            if (lid != null) briefcaseLidTransform = lid;
        }
        else
        {
            ConstructProceduralLeatherBriefcase();
        }

        SpawnInternalCashStacks();
        isBriefcaseInitialized = true;
    }

    private void ConstructProceduralLeatherBriefcase()
    {
        Material darkLeatherMat = boardManager.CreateColoredMaterial(new Color(0.10f, 0.07f, 0.05f), 0.75f, 0.1f);
        Material goldTrimMat = boardManager.CreateColoredMaterial(new Color(0.95f, 0.78f, 0.25f), 0.95f, 0.85f);
        Material interiorFeltMat = boardManager.CreateColoredMaterial(new Color(0.06f, 0.14f, 0.08f), 0.5f);

        // Lower Briefcase Shell
        GameObject baseBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseBox.name = "Briefcase_Base";
        baseBox.transform.SetParent(briefcaseRoot.transform, false);
        baseBox.transform.localPosition = new Vector3(0f, 0.010f, 0f);
        baseBox.transform.localScale = new Vector3(0.125f, 0.020f, 0.085f);
        baseBox.GetComponent<Renderer>().material = darkLeatherMat;

        // Interior Velvet Felt Lining
        GameObject felt = GameObject.CreatePrimitive(PrimitiveType.Cube);
        felt.name = "Interior_Felt";
        felt.transform.SetParent(briefcaseRoot.transform, false);
        felt.transform.localPosition = new Vector3(0f, 0.016f, 0f);
        felt.transform.localScale = new Vector3(0.118f, 0.009f, 0.078f);
        Destroy(felt.GetComponent<Collider>());
        felt.GetComponent<Renderer>().material = interiorFeltMat;

        // 4 Gold Corner Protectors
        Vector3[] corners = new Vector3[]
        {
            new Vector3(0.058f, 0.010f, 0.038f),
            new Vector3(-0.058f, 0.010f, 0.038f),
            new Vector3(0.058f, 0.010f, -0.038f),
            new Vector3(-0.058f, 0.010f, -0.038f)
        };
        for (int i = 0; i < 4; i++)
        {
            GameObject corner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            corner.name = $"Gold_Corner_{i}";
            corner.transform.SetParent(briefcaseRoot.transform, false);
            corner.transform.localPosition = corners[i];
            corner.transform.localScale = new Vector3(0.014f, 0.022f, 0.014f);
            Destroy(corner.GetComponent<Collider>());
            corner.GetComponent<Renderer>().material = goldTrimMat;
        }

        // Hinged Top Lid (Pivot at back edge Z = 0.0425f, Y = 0.020f)
        GameObject lidPivot = new GameObject("Briefcase_Lid_Pivot");
        lidPivot.transform.SetParent(briefcaseRoot.transform, false);
        lidPivot.transform.localPosition = new Vector3(0f, 0.020f, 0.0425f);
        lidPivot.transform.localRotation = Quaternion.Euler(-50f, 0f, 0f); // 50° open towards player
        briefcaseLidTransform = lidPivot.transform;

        lidClosedRot = Quaternion.Euler(0f, 0f, 0f);
        lidDefaultOpenRot = Quaternion.Euler(-50f, 0f, 0f);
        lidPopOpenRot = Quaternion.Euler(-75f, 0f, 0f);

        GameObject lidPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lidPlate.name = "Briefcase_Lid";
        lidPlate.transform.SetParent(lidPivot.transform, false);
        lidPlate.transform.localPosition = new Vector3(0f, 0.003f, -0.0425f);
        lidPlate.transform.localScale = new Vector3(0.125f, 0.006f, 0.085f);
        lidPlate.GetComponent<Renderer>().material = darkLeatherMat;

        // Gold Latch on Lid
        GameObject latch = GameObject.CreatePrimitive(PrimitiveType.Cube);
        latch.name = "Gold_Latch";
        latch.transform.SetParent(lidPlate.transform, false);
        latch.transform.localPosition = new Vector3(0f, 0f, -0.044f);
        latch.transform.localScale = new Vector3(0.022f, 0.008f, 0.006f);
        Destroy(latch.GetComponent<Collider>());
        latch.GetComponent<Renderer>().material = goldTrimMat;
    }

    private void SpawnInternalCashStacks()
    {
        Material billMat = boardManager.CreateColoredMaterial(new Color(0.12f, 0.65f, 0.28f), 0.65f, 0.05f); // Emerald Green
        Material bandMat = boardManager.CreateColoredMaterial(new Color(0.96f, 0.96f, 0.94f), 0.9f); // Paper Band
        Material goldBarMat = boardManager.CreateColoredMaterial(new Color(1f, 0.84f, 0.22f), 0.95f, 0.9f); // Gold Ingot

        float[] xCols = new float[] { -0.034f, 0f, 0.034f };
        float[] zRows = new float[] { -0.018f, 0.018f };

        GameObject bundlePrefab = boardManager.cashBundlePrefab != null ? boardManager.cashBundlePrefab : boardManager.dollarBillPrefab;

        int idx = 0;
        for (int r = 0; r < 2; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                GameObject stack;
                if (bundlePrefab != null)
                {
                    stack = Instantiate(bundlePrefab, briefcaseRoot.transform);
                    stack.name = $"Cash_Bundle_{idx}";
                    boardManager.StripRoguePhysicsFromImportedPrefab(stack);
                    boardManager.NormalizePrefabSize(stack, 0.032f, true);
                }
                else
                {
                    stack = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    stack.name = $"Cash_Stack_{idx}";
                    stack.transform.SetParent(briefcaseRoot.transform, false);
                    stack.transform.localScale = new Vector3(0.028f, 0.012f, 0.016f);
                    Destroy(stack.GetComponent<Collider>());
                    stack.GetComponent<Renderer>().material = billMat;

                    GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    band.name = "Stack_Band";
                    band.transform.SetParent(stack.transform, false);
                    band.transform.localPosition = Vector3.zero;
                    band.transform.localScale = new Vector3(1.02f, 0.45f, 0.45f);
                    Destroy(band.GetComponent<Collider>());
                    band.GetComponent<Renderer>().material = bandMat;
                }

                stack.transform.localPosition = new Vector3(xCols[c], 0.018f, zRows[r]);
                stack.transform.localRotation = Quaternion.Euler(0f, (idx % 2 == 0 ? 3f : -3f), 0f);

                baseStackScales[idx] = stack.transform.localScale;
                baseStackPositions[idx] = stack.transform.localPosition;

                cashStackObjects[idx] = stack;
                idx++;
            }
        }

        // Gold Bullion Bars (visible when PlayerCash >= $3000)
        goldBarsHolder = new GameObject("Overflow_Gold_Bars");
        goldBarsHolder.transform.SetParent(briefcaseRoot.transform, false);
        goldBarsHolder.transform.localPosition = new Vector3(0f, 0.035f, 0f);

        for (int b = -1; b <= 1; b += 2)
        {
            GameObject bar;
            if (boardManager.goldBarPrefab != null)
            {
                bar = Instantiate(boardManager.goldBarPrefab, goldBarsHolder.transform);
                bar.name = $"Gold_Bar_{b}";
                boardManager.StripRoguePhysicsFromImportedPrefab(bar);
                boardManager.NormalizePrefabSize(bar, 0.030f, true);
            }
            else
            {
                bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bar.name = $"Gold_Bar_{b}";
                bar.transform.SetParent(goldBarsHolder.transform, false);
                bar.transform.localScale = new Vector3(0.024f, 0.009f, 0.012f);
                Destroy(bar.GetComponent<Collider>());
                bar.GetComponent<Renderer>().material = goldBarMat;
            }

            bar.transform.localPosition = new Vector3(b * 0.018f, 0f, 0f);
            bar.transform.localRotation = Quaternion.Euler(0f, 15f * b, 0f);
        }

        goldBarsHolder.SetActive(false);
    }

    private void UpdateCashStacksVisuals(int cash)
    {
        if (!isBriefcaseInitialized) return;

        // Determine active count and smooth Y multiplier (1.0f to 1.8f of normalized base scale)
        int activeCount = 0;
        float yMultiplier = 1.0f;

        if (cash <= 0)
        {
            activeCount = 0;
            yMultiplier = 0.001f;
        }
        else if (cash < 500)
        {
            activeCount = 2;
            float t = (float)cash / 500f;
            yMultiplier = Mathf.Lerp(1.0f, 1.25f, t);
        }
        else if (cash < 1500)
        {
            activeCount = 4;
            float t = (float)(cash - 500) / 1000f;
            yMultiplier = Mathf.Lerp(1.2f, 1.55f, t);
        }
        else if (cash < 3000)
        {
            activeCount = 6;
            float t = (float)(cash - 1500) / 1500f;
            yMultiplier = Mathf.Lerp(1.4f, 1.8f, t);
        }
        else
        {
            activeCount = 6;
            yMultiplier = 1.8f;
        }

        for (int i = 0; i < 6; i++)
        {
            if (cashStackObjects[i] == null) continue;

            if (i < activeCount)
            {
                cashStackObjects[i].SetActive(true);
                Vector3 baseS = baseStackScales[i];
                cashStackObjects[i].transform.localScale = new Vector3(baseS.x, baseS.y * yMultiplier, baseS.z);
            }
            else
            {
                cashStackObjects[i].SetActive(false);
            }
        }

        if (goldBarsHolder != null)
        {
            goldBarsHolder.SetActive(cash >= 3000);
            if (cash >= 3000)
            {
                goldBarsHolder.transform.localPosition = new Vector3(0f, 0.038f, 0f);
            }
        }
    }

    public void AnimateBriefcaseLidPop()
    {
        if (briefcaseLidTransform == null) return;
        if (lidPopCoroutine != null) StopCoroutine(lidPopCoroutine);
        lidPopCoroutine = StartCoroutine(BriefcaseLidPopCoroutine());
    }

    private IEnumerator BriefcaseLidPopCoroutine()
    {
        // Burst gold sparkle celebration
        if (briefcaseRoot != null)
        {
            boardManager.SpawnCelebrationFireworks(briefcaseRoot.transform.position + Vector3.up * 0.08f);
        }

        // Animate lid snapping open to -75°
        float popDuration = 0.22f;
        float elapsed = 0f;
        Quaternion startRot = briefcaseLidTransform.localRotation;

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / popDuration);
            briefcaseLidTransform.localRotation = Quaternion.Slerp(startRot, lidPopOpenRot, t);
            yield return null;
        }

        briefcaseLidTransform.localRotation = lidPopOpenRot;
        yield return new WaitForSeconds(0.35f);

        // Ease smoothly back to default 50° open view
        float returnDuration = 0.45f;
        elapsed = 0f;
        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / returnDuration);
            briefcaseLidTransform.localRotation = Quaternion.Slerp(lidPopOpenRot, lidDefaultOpenRot, t);
            yield return null;
        }

        briefcaseLidTransform.localRotation = lidDefaultOpenRot;
        lidPopCoroutine = null;
    }

    #endregion

    #region B. Interactive "Executive Audit Bonus" Briefcase Tap (+15% Rent Bonus)

    private void HandleRivalPaidRent(int rivalIdx, int rent)
    {
        // Whenever an AI Rival lands on a property owned by Player 0, pop glowing floating cash stack
        if (auditBonusCoroutine != null) StopCoroutine(auditBonusCoroutine);
        auditBonusCoroutine = StartCoroutine(SpawnAuditBonusStackCoroutine());
    }

    [ContextMenu("Test Audit Bonus")]
    public void TestAuditBonusManual()
    {
        if (auditBonusCoroutine != null) StopCoroutine(auditBonusCoroutine);
        auditBonusCoroutine = StartCoroutine(SpawnAuditBonusStackCoroutine());
    }

    private IEnumerator SpawnAuditBonusStackCoroutine()
    {
        if (briefcaseRoot == null) yield break;

        if (floatingAuditBonusStack != null) Destroy(floatingAuditBonusStack);

        floatingAuditBonusStack = new GameObject("Floating_Audit_Bonus_Stack");
        floatingAuditBonusStack.transform.SetParent(briefcaseRoot.transform, false);

        Vector3 targetPos = new Vector3(0f, 0.12f, 0f);
        floatingAuditBonusStack.transform.localPosition = targetPos;

        Material goldGlowMat = boardManager.CreateColoredMaterial(new Color(1f, 0.88f, 0.28f), 0.95f, 0.9f);
        Material darkBandMat = boardManager.CreateColoredMaterial(new Color(0.12f, 0.14f, 0.18f), 0.9f);

        // 3D Floating Gold Cash Stack
        GameObject stackBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stackBox.name = "Bonus_Cash_Mesh";
        stackBox.transform.SetParent(floatingAuditBonusStack.transform, false);
        stackBox.transform.localPosition = Vector3.zero;
        stackBox.transform.localScale = new Vector3(0.045f, 0.020f, 0.026f);
        Destroy(stackBox.GetComponent<Collider>());
        stackBox.GetComponent<Renderer>().material = goldGlowMat;

        GameObject bandBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bandBox.name = "Bonus_Cash_Band";
        bandBox.transform.SetParent(stackBox.transform, false);
        bandBox.transform.localPosition = Vector3.zero;
        bandBox.transform.localScale = new Vector3(1.04f, 0.5f, 0.5f);
        Destroy(bandBox.GetComponent<Collider>());
        bandBox.GetComponent<Renderer>().material = darkBandMat;

        // Glowing Aura Ring
        GameObject aura = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        aura.name = "Bonus_Aura";
        aura.transform.SetParent(floatingAuditBonusStack.transform, false);
        aura.transform.localPosition = new Vector3(0f, -0.012f, 0f);
        aura.transform.localScale = new Vector3(0.065f, 0.001f, 0.065f);
        Destroy(aura.GetComponent<Collider>());
        aura.GetComponent<Renderer>().material = goldGlowMat;

        // Floating Text Canvas
        GameObject canvasGO = new GameObject("BonusCanvas");
        canvasGO.transform.SetParent(floatingAuditBonusStack.transform, false);
        canvasGO.transform.localPosition = new Vector3(0f, 0.032f, 0f);
        canvasGO.transform.localRotation = Quaternion.Euler(0f, -20f, 0f); // Face user South
        canvasGO.transform.localScale = Vector3.one * 0.00065f;

        Canvas c = canvasGO.AddComponent<Canvas>();
        c.renderMode = RenderMode.WorldSpace;
        RectTransform rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(350f, 100f);

        GameObject txtGO = new GameObject("BonusText");
        txtGO.transform.SetParent(canvasGO.transform, false);
        TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "<color=#FFD700><b>AUDIT BONUS +$25</b></color>\n<size=18><color=#FFFFFF>[POKE TO CLAIM]</color></size>";
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.rectTransform.sizeDelta = new Vector2(350f, 100f);

        isAuditBonusActive = true;
        boardManager.PlaySFX(boardManager.CashRegisterSound, null, 1.4f);

        float timer = 2.5f;
        while (timer > 0f && isAuditBonusActive)
        {
            timer -= Time.deltaTime;

            if (floatingAuditBonusStack != null)
            {
                // Gentle floating bob & slow spin
                float bob = Mathf.Sin(Time.time * 8f) * 0.006f;
                floatingAuditBonusStack.transform.localPosition = targetPos + Vector3.up * bob;
                stackBox.transform.Rotate(Vector3.up, 65f * Time.deltaTime);
            }

            yield return null;
        }

        // Timer expired without poke: fade & cleanup
        if (floatingAuditBonusStack != null)
        {
            Destroy(floatingAuditBonusStack);
        }
        isAuditBonusActive = false;
        auditBonusCoroutine = null;
    }

    private void CheckAuditBonusHandPoke()
    {
        if (floatingAuditBonusStack == null) return;

        Vector3 bonusWorldPos = floatingAuditBonusStack.transform.position;

        bool pokedWithRight = boardManager.IsRightHandTracked &&
            Vector3.Distance(boardManager.RightHandWorldPos, bonusWorldPos) < 0.095f;
        bool pokedWithLeft = boardManager.IsLeftHandTracked &&
            Vector3.Distance(boardManager.LeftHandWorldPos, bonusWorldPos) < 0.095f;
        bool spaceTriggered = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

        if (pokedWithRight || pokedWithLeft || spaceTriggered)
        {
            ClaimExecutiveAuditBonus(bonusWorldPos);
        }
    }

    private void ClaimExecutiveAuditBonus(Vector3 bonusPos)
    {
        isAuditBonusActive = false;

        // Award +$25 instant cash bonus
        boardManager.AddPlayerCash(0, 25);
        boardManager.PlaySFX(boardManager.CashRegisterSound, null, 1.25f);
        boardManager.PlayHaptic(0.7f, 0.25f);
        boardManager.SpawnCelebrationFireworks(bonusPos);

        boardManager.RefreshDashboard(
            "EXECUTIVE AUDIT BONUS +$25!",
            "Tactile audit bonus collected! +$25 instant profit deposited into your briefcase!"
        );

        if (floatingAuditBonusStack != null)
        {
            Destroy(floatingAuditBonusStack);
        }
    }

    #endregion

    #region C. Grabbable 3D Golden Victory Trophy Podium

    private void HandleVictoryTrophyTriggered()
    {
        if (isTrophySpawned) return;
        StartCoroutine(RiseVictoryPodiumAndTrophyCoroutine());
    }

    [ContextMenu("Spawn Victory Trophy")]
    public void SpawnVictoryTrophyManual()
    {
        StartCoroutine(RiseVictoryPodiumAndTrophyCoroutine());
    }

    private IEnumerator RiseVictoryPodiumAndTrophyCoroutine()
    {
        isTrophySpawned = true;

        if (victoryPodiumRoot != null) Destroy(victoryPodiumRoot);

        victoryPodiumRoot = new GameObject("CentralPark_Victory_Podium");
        victoryPodiumRoot.transform.SetParent(transform, false);

        Material darkMarbleMat = boardManager.CreateColoredMaterial(new Color(0.08f, 0.09f, 0.12f), 0.92f, 0.4f);
        Material goldTrimMat = boardManager.CreateColoredMaterial(new Color(1f, 0.85f, 0.22f), 0.95f, 0.88f);

        // 1. Rising Marble Pedestal
        GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pedestal.name = "Victory_Marble_Pedestal";
        pedestal.transform.SetParent(victoryPodiumRoot.transform, false);
        pedestal.transform.localPosition = new Vector3(0f, 0.024f, 0f);
        pedestal.transform.localScale = new Vector3(0.18f, 0.012f, 0.18f);
        pedestal.GetComponent<Renderer>().material = darkMarbleMat;

        // Gold base ring
        GameObject baseRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseRing.name = "Pedestal_Gold_Ring";
        baseRing.transform.SetParent(pedestal.transform, false);
        baseRing.transform.localPosition = new Vector3(0f, -0.45f, 0f);
        baseRing.transform.localScale = new Vector3(1.08f, 0.25f, 1.08f);
        Destroy(baseRing.GetComponent<Collider>());
        baseRing.GetComponent<Renderer>().material = goldTrimMat;

        // 2. Victory Trophy
        if (boardManager.victoryTrophyPrefab != null)
        {
            victoryTrophyGO = Instantiate(boardManager.victoryTrophyPrefab, victoryPodiumRoot.transform);
            victoryTrophyGO.name = "Golden_Tycoon_Victory_Trophy";
            victoryTrophyGO.transform.localPosition = new Vector3(0f, 0.035f, 0f);
            victoryTrophyGO.transform.localScale = Vector3.one * 0.12f;
        }
        else
        {
            victoryTrophyGO = ConstructProceduralGoldenTrophy(victoryPodiumRoot.transform, goldTrimMat, darkMarbleMat);
            victoryTrophyGO.transform.localPosition = new Vector3(0f, 0.035f, 0f);
        }

        // Attach Meta Interaction SDK Distance Hand Grab building block (useGravity = false, isKinematic = true)
        Rigidbody trophyRb = victoryTrophyGO.GetComponent<Rigidbody>();
        if (trophyRb == null) trophyRb = victoryTrophyGO.AddComponent<Rigidbody>();
        trophyRb.useGravity = false;
        trophyRb.isKinematic = true;

        boardManager.AttachNativeBuildingBlockGrab(victoryTrophyGO, trophyRb);

        // Smoothly rise podium and trophy from the ground over 1.25s
        float riseDuration = 1.25f;
        float elapsed = 0f;
        Vector3 startPos = new Vector3(0f, -0.05f, 0f);
        Vector3 targetPos = new Vector3(0f, 0.045f, 0f);

        boardManager.PlaySFX(boardManager.CashRegisterSound, null, 1.2f);
        boardManager.SpawnCelebrationFireworks(transform.position + Vector3.up * 0.25f);

        while (elapsed < riseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / riseDuration);
            pedestal.transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            victoryTrophyGO.transform.localPosition = Vector3.Lerp(startPos + new Vector3(0f, 0.07f, 0f), new Vector3(0f, 0.14f, 0f), t);
            yield return null;
        }

        victoryTrophyGO.transform.localPosition = new Vector3(0f, 0.14f, 0f);

        boardManager.RefreshDashboard(
            "CHAMPIONSHIP TROPHY UNLOCKED!",
            "You conquered the metropolis empire! Reach out and grab your 3D Golden Victory Trophy to inspect it!"
        );
    }

    private GameObject ConstructProceduralGoldenTrophy(Transform parent, Material goldMat, Material marbleMat)
    {
        GameObject trophy = new GameObject("Golden_Tycoon_Victory_Trophy");
        trophy.transform.SetParent(parent, false);

        // Trophy Base (Marble & Gold Octagonal block)
        GameObject trophyBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
        trophyBase.name = "Trophy_Base";
        trophyBase.transform.SetParent(trophy.transform, false);
        trophyBase.transform.localPosition = new Vector3(0f, 0.015f, 0f);
        trophyBase.transform.localScale = new Vector3(0.065f, 0.024f, 0.065f);
        trophyBase.GetComponent<Renderer>().material = marbleMat;

        // Pedestal Column (Gold)
        GameObject col = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        col.name = "Trophy_Stem";
        col.transform.SetParent(trophy.transform, false);
        col.transform.localPosition = new Vector3(0f, 0.045f, 0f);
        col.transform.localScale = new Vector3(0.032f, 0.025f, 0.032f);
        col.GetComponent<Renderer>().material = goldMat;

        // Chalice Body
        GameObject chalice = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        chalice.name = "Trophy_Chalice";
        chalice.transform.SetParent(trophy.transform, false);
        chalice.transform.localPosition = new Vector3(0f, 0.082f, 0f);
        chalice.transform.localScale = new Vector3(0.075f, 0.065f, 0.075f);
        chalice.GetComponent<Renderer>().material = goldMat;

        // Golden Lip Rim
        GameObject lip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        lip.name = "Trophy_Rim";
        lip.transform.SetParent(chalice.transform, false);
        lip.transform.localPosition = new Vector3(0f, 0.45f, 0f);
        lip.transform.localScale = new Vector3(1.08f, 0.12f, 1.08f);
        Destroy(lip.GetComponent<Collider>());
        lip.GetComponent<Renderer>().material = goldMat;

        // Left & Right Ornate Handles
        for (int h = -1; h <= 1; h += 2)
        {
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handle.name = $"Handle_{(h < 0 ? "L" : "R")}";
            handle.transform.SetParent(chalice.transform, false);
            handle.transform.localPosition = new Vector3(h * 0.58f, 0.05f, 0f);
            handle.transform.localRotation = Quaternion.Euler(0f, 0f, h * 25f);
            handle.transform.localScale = new Vector3(0.18f, 0.65f, 0.15f);
            Destroy(handle.GetComponent<Collider>());
            handle.GetComponent<Renderer>().material = goldMat;
        }

        // Crown / Star on Top
        GameObject star = GameObject.CreatePrimitive(PrimitiveType.Cube);
        star.name = "Tycoon_Crown_Star";
        star.transform.SetParent(chalice.transform, false);
        star.transform.localPosition = new Vector3(0f, 0.68f, 0f);
        star.transform.localRotation = Quaternion.Euler(45f, 45f, 45f);
        star.transform.localScale = Vector3.one * 0.28f;
        Destroy(star.GetComponent<Collider>());
        star.GetComponent<Renderer>().material = goldMat;

        // Compound BoxCollider on trophy root for grabbing
        BoxCollider boxCol = trophy.AddComponent<BoxCollider>();
        boxCol.center = new Vector3(0f, 0.075f, 0f);
        boxCol.size = new Vector3(0.12f, 0.16f, 0.12f);

        return trophy;
    }

    #endregion

    #region D. Physical Rent Hand-Offs (Grab & Throw Cash Stack From Briefcase)

    private GameObject grabbableRentCashBundle;
    private Coroutine rentHandOffCoroutine;

    public void ShowGrabbableRentCashBundle(int payerIdx, int recipientIdx, int rentDue, Action onFinished)
    {
        if (rentHandOffCoroutine != null) StopCoroutine(rentHandOffCoroutine);
        rentHandOffCoroutine = StartCoroutine(RentCashBundleRoutine(payerIdx, recipientIdx, rentDue, onFinished));
    }

    private IEnumerator RentCashBundleRoutine(int payerIdx, int recipientIdx, int rentDue, Action onFinished)
    {
        if (grabbableRentCashBundle != null) Destroy(grabbableRentCashBundle);

        Vector3 spawnPos = briefcaseRoot != null ? briefcaseRoot.transform.position + Vector3.up * 0.08f : transform.position + Vector3.up * 0.12f;

        GameObject bundlePrefab = boardManager.cashBundlePrefab != null ? boardManager.cashBundlePrefab : boardManager.dollarBillPrefab;
        if (bundlePrefab != null)
        {
            grabbableRentCashBundle = Instantiate(bundlePrefab, transform, false);
            grabbableRentCashBundle.name = "Grabbable_Rent_CashBundle";
            grabbableRentCashBundle.transform.position = spawnPos;
            boardManager.StripRoguePhysicsFromImportedPrefab(grabbableRentCashBundle);
            boardManager.NormalizePrefabSize(grabbableRentCashBundle, 0.055f, true);
        }
        else
        {
            grabbableRentCashBundle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grabbableRentCashBundle.name = "Grabbable_Rent_CashBundle";
            grabbableRentCashBundle.transform.SetParent(transform, false);
            grabbableRentCashBundle.transform.position = spawnPos;
            grabbableRentCashBundle.transform.localScale = new Vector3(0.045f, 0.022f, 0.028f);
            Destroy(grabbableRentCashBundle.GetComponent<Collider>());
            grabbableRentCashBundle.GetComponent<Renderer>().material = boardManager.CreateColoredMaterial(new Color(0.18f, 0.95f, 0.45f), 0.92f);
        }

        // Floating Prompt
        GameObject canvasGO = new GameObject("Rent_Cash_Canvas");
        canvasGO.transform.SetParent(grabbableRentCashBundle.transform, false);
        canvasGO.transform.localPosition = new Vector3(0f, 0.06f, 0f);
        canvasGO.transform.localScale = Vector3.one * 0.00045f;
        Canvas c = canvasGO.AddComponent<Canvas>();
        c.renderMode = RenderMode.WorldSpace;
        canvasGO.GetComponent<RectTransform>().sizeDelta = new Vector2(700f, 200f);

        TextMeshProUGUI promptTMP = canvasGO.AddComponent<TextMeshProUGUI>();
        promptTMP.text = $"<color=#FFD700><b>RENT OWED: ${rentDue}</b></color>\n<size=20><color=#00FF88>FLICK CASH ACROSS TABLE FOR +$15 REBATE! (3s)</color></size>";
        promptTMP.alignment = TextAlignmentOptions.Center;
        promptTMP.fontSize = 24f;
        promptTMP.color = Color.white;

        boardManager.PlaySFX(boardManager.CashRegisterSound, null, 1.1f);
        boardManager.PlayHaptic(0.4f, 0.1f);

        float timer = 3.0f;
        bool wasFlicked = false;
        Vector3 lastHandPos = boardManager.RightHandWorldPos;

        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            promptTMP.text = $"<color=#FFD700><b>RENT OWED: ${rentDue}</b></color>\n<size=20><color=#00FF88>FLICK CASH ACROSS TABLE FOR +$15 REBATE! ({timer:F1}s)</color></size>";

            Vector3 rPos = boardManager.RightHandWorldPos;
            Vector3 lPos = boardManager.LeftHandWorldPos;
            bool rTracked = boardManager.IsRightHandTracked && rPos.y > -50f;
            bool lTracked = boardManager.IsLeftHandTracked && lPos.y > -50f;

            float dR = rTracked ? Vector3.Distance(rPos, grabbableRentCashBundle.transform.position) : 99f;
            float dL = lTracked ? Vector3.Distance(lPos, grabbableRentCashBundle.transform.position) : 99f;

            Vector3 handVel = (rPos - lastHandPos) / Mathf.Max(0.001f, Time.deltaTime);
            lastHandPos = rPos;

            bool spacePressed = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame;
            bool handFlick = (dR < 0.09f || dL < 0.09f) && (handVel.magnitude > 0.45f || boardManager.IsPlayerHoldingDice);

            if (handFlick || spacePressed)
            {
                wasFlicked = true;
                break;
            }

            yield return null;
        }

        if (wasFlicked)
        {
            // Award +$15 Prompt Payer Rebate!
            boardManager.AddPlayerCash(payerIdx, 15);
            boardManager.PlaySFX(boardManager.CashRegisterSound, null, 1.35f);
            boardManager.PlayHaptic(0.8f, 0.2f);
            boardManager.RefreshDashboard(
                "PROMPT PAYER REBATE (+$15)!",
                "Swift hand-off! Boardroom awarded you a +$15 Prompt Payer Rebate!"
            );
        }

        if (grabbableRentCashBundle != null)
        {
            Destroy(grabbableRentCashBundle);
            grabbableRentCashBundle = null;
        }

        onFinished?.Invoke();
        rentHandOffCoroutine = null;
    }

    #endregion
}
