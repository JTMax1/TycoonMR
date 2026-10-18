using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

[RequireComponent(typeof(TycoonBoardManager))]
public class TycoonMinigameOrchestrator : MonoBehaviour
{
    private TycoonBoardManager boardManager;
    private GameObject activeMinigameRoot;
    private bool isMinigameRunning = false;
    private Coroutine activeMinigameCoroutine;

    public bool IsMinigameRunning => isMinigameRunning;

    void Awake()
    {
        boardManager = GetComponent<TycoonBoardManager>();
    }

    void OnDisable()
    {
        CancelActiveMinigame();
    }

    void OnDestroy()
    {
        CancelActiveMinigame();
    }

    // =========================================================================
    // HELPER: GET SPAWN TRANSFORM IN FRONT OF PLAYER & ABOVE BOARD
    // =========================================================================
    private (Vector3 pos, Quaternion rot) GetMinigameSpawnPose()
    {
        if (boardManager != null)
        {
            return boardManager.GetElevatedBoardFrontPose(0.36f, 0.22f);
        }

        Transform eye = Camera.main != null ? Camera.main.transform : transform;
        Vector3 pos = eye.position + eye.forward * 0.45f - eye.up * 0.10f;
        Quaternion rot = Quaternion.LookRotation(eye.forward, Vector3.up);
        return (pos, rot);
    }

    private TextMeshProUGUI CreateHoloText(Transform parent, string name, Vector3 localPos, string text, float fontSize, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        GameObject canvasGO = new GameObject($"TMP_{name}");
        canvasGO.transform.SetParent(parent, false);
        canvasGO.transform.localPosition = localPos;
        canvasGO.transform.localScale = Vector3.one * 0.00055f;

        Canvas c = canvasGO.AddComponent<Canvas>();
        c.renderMode = RenderMode.WorldSpace;
        RectTransform rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(900f, 200f);

        TextMeshProUGUI tmp = canvasGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = align;
        tmp.fontStyle = FontStyles.Bold;
        return tmp;
    }

    private GameObject CreateBar(Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
    {
        GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bar.transform.SetParent(parent, false);
        bar.transform.localPosition = localPos;
        bar.transform.localScale = localScale;
        Destroy(bar.GetComponent<Collider>());
        bar.GetComponent<Renderer>().material = mat;
        return bar;
    }

    // =========================================================================
    // 1. MINIGAME 1: "BANK ERROR IN YOUR FAVOR — VAULT CASH SWAT"
    // =========================================================================
    public void RunVaultCashSwatMinigame(Action<int> onComplete)
    {
        if (isMinigameRunning) CancelActiveMinigame();
        activeMinigameCoroutine = StartCoroutine(VaultCashSwatRoutine(onComplete));
    }

    private class FloatingCashBill
    {
        public GameObject billGO;
        public Vector3 baseLocalPos;
        public float phaseX;
        public float phaseY;
        public float phaseZ;
        public float freq;
        public Vector3 rotSpeed;
        public bool isSwatted;
    }

    private IEnumerator VaultCashSwatRoutine(Action<int> onComplete)
    {
        isMinigameRunning = true;
        var (spawnPos, spawnRot) = GetMinigameSpawnPose();

        activeMinigameRoot = new GameObject("Vault_Cash_Swat_Minigame");
        activeMinigameRoot.transform.position = spawnPos;
        activeMinigameRoot.transform.rotation = spawnRot;

        Material goldVaultMat = boardManager.CreateColoredMaterial(new Color(0.92f, 0.75f, 0.25f), 0.9f, 0.85f);
        Material emeraldCashMat = boardManager.CreateColoredMaterial(new Color(0.15f, 0.75f, 0.35f), 0.5f, 0.05f);
        Material goldTrimMat = boardManager.CreateColoredMaterial(new Color(0.95f, 0.85f, 0.40f), 0.85f, 0.7f);

        // Vault Model: Use bankVaultPrefab if assigned, otherwise sleek open 4-bar golden frame
        if (boardManager.bankVaultPrefab != null)
        {
            GameObject vaultInst = Instantiate(boardManager.bankVaultPrefab, activeMinigameRoot.transform, false);
            vaultInst.name = "Bank_Vault_Visual";
            boardManager.StripRoguePhysicsFromImportedPrefab(vaultInst);
            boardManager.NormalizePrefabSize(vaultInst, 0.14f);
            vaultInst.transform.localPosition = new Vector3(0f, -0.10f, 0.05f);
        }
        else
        {
            // Open 3D Golden Vault Ring (4 thin gold bars framing hollow window so board is visible)
            GameObject ringRoot = new GameObject("Golden_Vault_Ring");
            ringRoot.transform.SetParent(activeMinigameRoot.transform, false);
            ringRoot.transform.localPosition = new Vector3(0f, 0.02f, 0.05f);

            float w = 0.38f, h = 0.26f, barThick = 0.014f, depth = 0.020f;
            CreateBar(ringRoot.transform, new Vector3(0f, h * 0.5f, 0f), new Vector3(w, barThick, depth), goldVaultMat);
            CreateBar(ringRoot.transform, new Vector3(0f, -h * 0.5f, 0f), new Vector3(w, barThick, depth), goldVaultMat);
            CreateBar(ringRoot.transform, new Vector3(-w * 0.5f, 0f, 0f), new Vector3(barThick, h, depth), goldVaultMat);
            CreateBar(ringRoot.transform, new Vector3(w * 0.5f, 0f, 0f), new Vector3(barThick, h, depth), goldVaultMat);
        }

        // HUD Text
        TextMeshProUGUI titleTMP = CreateHoloText(activeMinigameRoot.transform, "Title", new Vector3(0f, 0.22f, -0.02f), "BANK ERROR IN YOUR FAVOR!", 34, new Color(1f, 0.85f, 0.30f));
        TextMeshProUGUI counterTMP = CreateHoloText(activeMinigameRoot.transform, "Counter", new Vector3(0f, 0.16f, -0.02f), "SWAT FLYING CASH! Swatted: 0 | Time: 8.0s", 25, Color.white);
        TextMeshProUGUI payoutTMP = CreateHoloText(activeMinigameRoot.transform, "Payout", new Vector3(0f, -0.18f, -0.02f), "Guaranteed Base: $100 (+$25 / bill swatted)", 22, new Color(0.4f, 0.95f, 0.6f));

        boardManager.PlaySFX(boardManager.CashRegisterSound, null, 1.2f);
        boardManager.PlayHaptic(0.5f, 0.15f);

        // Spawn 12 Flying Cash Bills in reachable zone
        List<FloatingCashBill> bills = new List<FloatingCashBill>();
        int totalBills = 12;
        int swattedCount = 0;

        for (int i = 0; i < totalBills; i++)
        {
            GameObject bill;
            if (boardManager.dollarBillPrefab != null)
            {
                bill = Instantiate(boardManager.dollarBillPrefab, activeMinigameRoot.transform, false);
                boardManager.StripRoguePhysicsFromImportedPrefab(bill);
                boardManager.NormalizePrefabSize(bill, 0.08f);
            }
            else
            {
                bill = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bill.transform.SetParent(activeMinigameRoot.transform, false);
                bill.transform.localScale = new Vector3(0.08f, 0.04f, 0.004f);
                Destroy(bill.GetComponent<Collider>());
                bill.GetComponent<Renderer>().material = emeraldCashMat;

                // Gold center band
                GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cube);
                band.transform.SetParent(bill.transform, false);
                band.transform.localPosition = Vector3.zero;
                band.transform.localScale = new Vector3(0.35f, 1.02f, 1.15f);
                Destroy(band.GetComponent<Collider>());
                band.GetComponent<Renderer>().material = goldTrimMat;
            }
            bill.name = $"DollarBill_{i}";

            Vector3 basePos = new Vector3(
                UnityEngine.Random.Range(-0.16f, 0.16f),
                UnityEngine.Random.Range(-0.06f, 0.12f),
                UnityEngine.Random.Range(-0.05f, 0.05f)
            );
            bill.transform.localPosition = basePos;

            Vector3 rot = new Vector3(
                UnityEngine.Random.Range(40f, 100f),
                UnityEngine.Random.Range(50f, 120f),
                UnityEngine.Random.Range(30f, 80f)
            );

            bills.Add(new FloatingCashBill
            {
                billGO = bill,
                baseLocalPos = basePos,
                phaseX = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                phaseY = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                phaseZ = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                freq = UnityEngine.Random.Range(2.5f, 4.2f),
                rotSpeed = rot,
                isSwatted = false
            });
        }

        float gameDuration = 8.0f;
        float elapsed = 0f;

        while (elapsed < gameDuration && swattedCount < totalBills)
        {
            elapsed += Time.deltaTime;
            float timeLeft = Mathf.Max(0f, gameDuration - elapsed);

            Vector3 leftHand = boardManager.LeftHandWorldPos;
            Vector3 rightHand = boardManager.RightHandWorldPos;
            bool leftTracked = boardManager.IsLeftHandTracked && leftHand.y > -50f;
            bool rightTracked = boardManager.IsRightHandTracked && rightHand.y > -50f;
            bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

            foreach (var b in bills)
            {
                if (b.isSwatted || b.billGO == null) continue;

                // Gentle sinusoidal fluttering inside reachable box
                Vector3 bobbing = new Vector3(
                    Mathf.Sin(elapsed * b.freq + b.phaseX) * 0.035f,
                    Mathf.Sin(elapsed * (b.freq * 1.2f) + b.phaseY) * 0.030f,
                    Mathf.Cos(elapsed * (b.freq * 0.8f) + b.phaseZ) * 0.025f
                );
                b.billGO.transform.localPosition = b.baseLocalPos + bobbing;
                b.billGO.transform.Rotate(b.rotSpeed * Time.deltaTime);

                // Hand Swat Proximity (0.11m = 11cm radius)
                Vector3 billWorldPos = b.billGO.transform.position;
                bool swattedByLeft = leftTracked && Vector3.Distance(leftHand, billWorldPos) < 0.11f;
                bool swattedByRight = rightTracked && Vector3.Distance(rightHand, billWorldPos) < 0.11f;

                if (swattedByLeft || swattedByRight || spacePressed)
                {
                    b.isSwatted = true;
                    swattedCount++;
                    boardManager.PlaySFX(boardManager.CashRegisterSound, null, 1.0f + (swattedCount * 0.05f));
                    boardManager.PlayHaptic(0.55f, 0.08f);

                    StartCoroutine(AnimateBillSwatBurst(b.billGO));
                    if (spacePressed) break; // Swat one bill per space key press
                }
            }

            counterTMP.text = $"SWAT FLYING CASH! Swatted: {swattedCount}/{totalBills} | Time: {timeLeft:F1}s";
            yield return null;
        }

        int finalPayout = 100 + (swattedCount * 25);
        titleTMP.text = $"MINIGAME COMPLETE! +${finalPayout}";
        titleTMP.color = new Color(0.2f, 1f, 0.4f);
        counterTMP.text = $"Swatted {swattedCount} bills! Total payout: ${finalPayout}!";
        boardManager.PlaySFX(boardManager.CashRegisterSound, null, 1.25f);
        boardManager.PlayHaptic(0.8f, 0.25f);

        yield return new WaitForSeconds(1.2f);
        CancelActiveMinigame();

        onComplete?.Invoke(finalPayout);
    }

    private IEnumerator AnimateBillSwatBurst(GameObject bill)
    {
        if (bill == null) yield break;
        float duration = 0.25f;
        float elapsed = 0f;
        Vector3 origScale = bill.transform.localScale;

        while (elapsed < duration)
        {
            if (bill == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            bill.transform.localScale = Vector3.Lerp(origScale, origScale * 1.6f, t);
            yield return null;
        }
        Destroy(bill);
    }

    // =========================================================================
    // 2. MINIGAME 2: "THE GREAT ESCAPE — 3D MECHANICAL JAIL LOCKPICK"
    // =========================================================================
    public void RunJailLockpickMinigame(Action<bool> onComplete)
    {
        if (isMinigameRunning) CancelActiveMinigame();
        activeMinigameCoroutine = StartCoroutine(JailLockpickRoutine(onComplete));
    }

    private class LockpickPin
    {
        public Transform pinTransform;
        public float baseY;
        public float freq;
        public float amplitude;
        public bool isLocked;
        public Renderer pinRenderer;
    }

    private IEnumerator JailLockpickRoutine(Action<bool> onComplete)
    {
        isMinigameRunning = true;
        var (spawnPos, spawnRot) = GetMinigameSpawnPose();

        activeMinigameRoot = new GameObject("Jail_Lockpick_Minigame");
        activeMinigameRoot.transform.position = spawnPos;
        activeMinigameRoot.transform.rotation = spawnRot;

        Material brassMat = boardManager.CreateColoredMaterial(new Color(0.85f, 0.70f, 0.22f), 0.9f, 0.8f);
        Material steelPinMat = boardManager.CreateColoredMaterial(new Color(0.75f, 0.78f, 0.85f), 0.95f, 0.7f);
        Material greenLockedMat = boardManager.CreateColoredMaterial(new Color(0.18f, 0.95f, 0.45f), 0.95f, 0.6f);
        Material neonLineMat = boardManager.CreateColoredMaterial(new Color(0.15f, 1f, 0.35f), 0.95f, 0.2f);

        GameObject lockBody;
        if (boardManager.padlockPrefab != null)
        {
            lockBody = Instantiate(boardManager.padlockPrefab, activeMinigameRoot.transform, false);
            lockBody.name = "Padlock_Body";
            boardManager.StripRoguePhysicsFromImportedPrefab(lockBody);
            boardManager.NormalizePrefabSize(lockBody, 0.26f);
            lockBody.transform.localPosition = new Vector3(0f, -0.04f, 0f);
        }
        else
        {
            // Padlock Body
            lockBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lockBody.name = "Padlock_Body";
            lockBody.transform.SetParent(activeMinigameRoot.transform, false);
            lockBody.transform.localPosition = new Vector3(0f, -0.04f, 0f);
            lockBody.transform.localScale = new Vector3(0.28f, 0.20f, 0.05f);
            Destroy(lockBody.GetComponent<Collider>());
            lockBody.GetComponent<Renderer>().material = brassMat;

            // Padlock Shackle
            GameObject shackle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shackle.name = "Padlock_Shackle";
            shackle.transform.SetParent(activeMinigameRoot.transform, false);
            shackle.transform.localPosition = new Vector3(0f, 0.10f, 0f);
            shackle.transform.localScale = new Vector3(0.20f, 0.018f, 0.20f);
            Destroy(shackle.GetComponent<Collider>());
            shackle.GetComponent<Renderer>().material = steelPinMat;
        }

        // Center Green Shear Line
        GameObject shearLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shearLine.name = "Shear_Line";
        shearLine.transform.SetParent(lockBody.transform, false);
        shearLine.transform.localPosition = new Vector3(0f, 0f, -0.52f);
        shearLine.transform.localScale = new Vector3(0.92f, 0.04f, 0.06f);
        Destroy(shearLine.GetComponent<Collider>());
        shearLine.GetComponent<Renderer>().material = neonLineMat;

        // HUD Text
        TextMeshProUGUI titleTMP = CreateHoloText(activeMinigameRoot.transform, "Title", new Vector3(0f, 0.25f, -0.02f), "THE GREAT ESCAPE — LOCKPICK", 34, new Color(1f, 0.85f, 0.30f));
        TextMeshProUGUI promptTMP = CreateHoloText(activeMinigameRoot.transform, "Prompt", new Vector3(0f, -0.19f, -0.02f), "POKE EACH PIN WHEN AT GREEN LINE!\n(Locks: 0/3 | Time: 10.0s)", 23, Color.white);

        // 3 Oscillating Tumbler Pins
        List<LockpickPin> pins = new List<LockpickPin>();
        float[] pinXOffsets = new float[] { -0.075f, 0.0f, 0.075f };
        float[] pinFreqs = new float[] { 2.6f, 3.8f, 3.1f };

        for (int i = 0; i < 3; i++)
        {
            GameObject pinGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pinGO.name = $"TumblerPin_{i}";
            pinGO.transform.SetParent(lockBody.transform, false);
            pinGO.transform.localPosition = new Vector3(pinXOffsets[i], 0f, -0.55f);
            pinGO.transform.localScale = new Vector3(0.11f, 0.26f, 0.11f);
            Destroy(pinGO.GetComponent<Collider>());
            Renderer rend = pinGO.GetComponent<Renderer>();
            rend.material = steelPinMat;

            pins.Add(new LockpickPin
            {
                pinTransform = pinGO.transform,
                baseY = 0f,
                freq = pinFreqs[i],
                amplitude = 0.28f,
                isLocked = false,
                pinRenderer = rend
            });
        }

        float timeLimit = 10.0f;
        float elapsed = 0f;
        int lockedCount = 0;
        float lastPokeAttemptTime = 0f;

        while (elapsed < timeLimit && lockedCount < 3)
        {
            elapsed += Time.deltaTime;
            float remaining = Mathf.Max(0f, timeLimit - elapsed);

            Vector3 leftHand = boardManager.LeftHandWorldPos;
            Vector3 rightHand = boardManager.RightHandWorldPos;
            bool leftTracked = boardManager.IsLeftHandTracked && leftHand.y > -50f;
            bool rightTracked = boardManager.IsRightHandTracked && rightHand.y > -50f;
            bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

            for (int i = 0; i < pins.Count; i++)
            {
                var pin = pins[i];
                if (pin.isLocked) continue;

                // Pin oscillation
                float currentY = pin.baseY + Mathf.Sin(elapsed * pin.freq) * pin.amplitude;
                pin.pinTransform.localPosition = new Vector3(pinXOffsets[i], currentY, -0.55f);

                // Check Hand Poke Proximity (< 9.5cm)
                Vector3 pinWorldPos = pin.pinTransform.position;
                bool pokeLeft = leftTracked && Vector3.Distance(leftHand, pinWorldPos) < 0.095f;
                bool pokeRight = rightTracked && Vector3.Distance(rightHand, pinWorldPos) < 0.095f;

                if ((pokeLeft || pokeRight || spacePressed) && Time.time > lastPokeAttemptTime)
                {
                    lastPokeAttemptTime = Time.time + 0.30f;

                    // Check if near shear line tolerance (0.12f) or forced by space key
                    if (Mathf.Abs(currentY) < 0.12f || spacePressed)
                    {
                        pin.isLocked = true;
                        pin.pinTransform.localPosition = new Vector3(pinXOffsets[i], 0f, -0.55f);
                        pin.pinRenderer.material = greenLockedMat;
                        lockedCount++;

                        boardManager.PlaySFX(boardManager.CashRegisterSound, null, 1.4f);
                        boardManager.PlayHaptic(0.65f, 0.08f);
                    }
                    else
                    {
                        boardManager.PlaySFX(boardManager.DiceRollSound, null, 0.7f);
                        boardManager.PlayHaptic(0.3f, 0.05f);
                    }
                    if (spacePressed) break;
                }
            }

            promptTMP.text = $"POKE PIN WHEN AT GREEN LINE!\n(Pins Locked: {lockedCount}/3 | Time: {remaining:F1}s)";
            yield return null;
        }

        bool success = (lockedCount >= 3);
        if (success)
        {
            titleTMP.text = "JAIL LOCKPICKED! ESCAPED FOR FREE!";
            titleTMP.color = new Color(0.2f, 1f, 0.4f);
            promptTMP.text = "Laser Cage shattered! Free escape granted!";
            boardManager.PlaySFX(boardManager.CashRegisterSound, null, 1.35f);
            boardManager.PlayHaptic(0.9f, 0.25f);
            boardManager.ShatterJailCage(0);
        }
        else
        {
            titleTMP.text = "LOCKPICK FAILED! TIME EXPIRED";
            titleTMP.color = new Color(1f, 0.3f, 0.3f);
            promptTMP.text = "Pins reset! Pay $50 bail or roll for doubles.";
            boardManager.PlaySFX(boardManager.DiceRollSound, null, 0.65f);
            boardManager.PlayHaptic(0.4f, 0.15f);
        }

        yield return new WaitForSeconds(1.3f);
        CancelActiveMinigame();

        onComplete?.Invoke(success);
    }

    // =========================================================================
    // 3. MINIGAME 3: "CITY STREET REPAIRS — STEAM PIPE VALVE FIX"
    // =========================================================================
    public void RunPipeRepairMinigame(Action<int> onComplete)
    {
        if (isMinigameRunning) CancelActiveMinigame();
        activeMinigameCoroutine = StartCoroutine(PipeRepairRoutine(onComplete));
    }

    private class PipeValve
    {
        public Transform valveTransform;
        public GameObject steamPlume;
        public bool isClosed;
        public Renderer valveRenderer;
    }

    private IEnumerator PipeRepairRoutine(Action<int> onComplete)
    {
        isMinigameRunning = true;
        var (spawnPos, spawnRot) = GetMinigameSpawnPose();

        activeMinigameRoot = new GameObject("Steam_Pipe_Repair_Minigame");
        activeMinigameRoot.transform.position = spawnPos;
        activeMinigameRoot.transform.rotation = spawnRot;

        Material pipeSteelMat = boardManager.CreateColoredMaterial(new Color(0.22f, 0.25f, 0.28f), 0.75f, 0.5f);
        Material valveRedMat = boardManager.CreateColoredMaterial(new Color(0.88f, 0.18f, 0.18f), 0.85f, 0.3f);
        Material valveGreenMat = boardManager.CreateColoredMaterial(new Color(0.18f, 0.88f, 0.35f), 0.9f, 0.3f);
        Material steamMat = boardManager.CreateColoredMaterial(new Color(0.92f, 0.95f, 1f, 0.65f), 0.1f, 0.0f);

        // Optional Pipe / Valve custom prefab
        if (boardManager.steamPipeValvePrefab != null)
        {
            GameObject pipeObj = Instantiate(boardManager.steamPipeValvePrefab, activeMinigameRoot.transform, false);
            pipeObj.name = "Custom_Steam_Pipe";
            boardManager.StripRoguePhysicsFromImportedPrefab(pipeObj);
            boardManager.NormalizePrefabSize(pipeObj, 0.30f);
            pipeObj.transform.localPosition = new Vector3(0f, 0f, 0f);
        }
        else
        {
            // Horizontal Pipe Body
            GameObject mainPipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mainPipe.name = "Main_Pipe";
            mainPipe.transform.SetParent(activeMinigameRoot.transform, false);
            mainPipe.transform.localPosition = new Vector3(0f, 0f, 0f);
            mainPipe.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            mainPipe.transform.localScale = new Vector3(0.045f, 0.24f, 0.045f);
            Destroy(mainPipe.GetComponent<Collider>());
            mainPipe.GetComponent<Renderer>().material = pipeSteelMat;
        }

        // HUD Text
        TextMeshProUGUI titleTMP = CreateHoloText(activeMinigameRoot.transform, "Title", new Vector3(0f, 0.22f, -0.02f), "BURST PIPE EMERGENCY!", 34, new Color(1f, 0.45f, 0.35f));
        TextMeshProUGUI promptTMP = CreateHoloText(activeMinigameRoot.transform, "Prompt", new Vector3(0f, -0.19f, -0.02f), "SHUT OFF 3 PRESSURE VALVES! (Saves $30/valve)\nValves Closed: 0/3 | Time: 7.0s", 23, Color.white);

        // 3 Pressure Valves & Steam Plumes
        List<PipeValve> valves = new List<PipeValve>();
        float[] valveXOffsets = new float[] { -0.12f, 0.0f, 0.12f };

        for (int i = 0; i < 3; i++)
        {
            // Valve wheel
            GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = $"ValveWheel_{i}";
            wheel.transform.SetParent(activeMinigameRoot.transform, false);
            wheel.transform.localPosition = new Vector3(valveXOffsets[i], 0.065f, 0f);
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            wheel.transform.localScale = new Vector3(0.075f, 0.012f, 0.075f);
            Destroy(wheel.GetComponent<Collider>());
            Renderer wheelRend = wheel.GetComponent<Renderer>();
            wheelRend.material = valveRedMat;

            // Steam Plume
            GameObject steam = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            steam.name = $"Steam_{i}";
            steam.transform.SetParent(wheel.transform, false);
            steam.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            steam.transform.localScale = new Vector3(1.2f, 1.6f, 1.2f);
            Destroy(steam.GetComponent<Collider>());
            steam.GetComponent<Renderer>().material = steamMat;

            valves.Add(new PipeValve
            {
                valveTransform = wheel.transform,
                steamPlume = steam,
                isClosed = false,
                valveRenderer = wheelRend
            });
        }

        float timeLimit = 7.0f;
        float elapsed = 0f;
        int valvesClosed = 0;
        float lastTouchTime = 0f;

        while (elapsed < timeLimit && valvesClosed < 3)
        {
            elapsed += Time.deltaTime;
            float remaining = Mathf.Max(0f, timeLimit - elapsed);

            Vector3 leftHand = boardManager.LeftHandWorldPos;
            Vector3 rightHand = boardManager.RightHandWorldPos;
            bool leftTracked = boardManager.IsLeftHandTracked && leftHand.y > -50f;
            bool rightTracked = boardManager.IsRightHandTracked && rightHand.y > -50f;
            bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

            for (int i = 0; i < valves.Count; i++)
            {
                var v = valves[i];
                if (v.isClosed) continue;

                // Animate steam puffing
                if (v.steamPlume != null)
                {
                    float pulse = 1f + Mathf.Sin((elapsed * 9f) + i) * 0.25f;
                    v.steamPlume.transform.localScale = new Vector3(1.2f * pulse, 1.6f * pulse, 1.2f * pulse);
                }

                // Check Hand Proximity (< 11cm radius)
                Vector3 valveWorldPos = v.valveTransform.position;
                bool touchLeft = leftTracked && Vector3.Distance(leftHand, valveWorldPos) < 0.11f;
                bool touchRight = rightTracked && Vector3.Distance(rightHand, valveWorldPos) < 0.11f;

                if ((touchLeft || touchRight || spacePressed) && Time.time > lastTouchTime)
                {
                    lastTouchTime = Time.time + 0.30f;
                    v.isClosed = true;
                    valvesClosed++;

                    v.valveRenderer.material = valveGreenMat;
                    v.valveTransform.Rotate(Vector3.up * 180f);
                    if (v.steamPlume != null) v.steamPlume.SetActive(false);

                    boardManager.PlaySFX(boardManager.CashRegisterSound, null, 1.15f);
                    boardManager.PlayHaptic(0.7f, 0.10f);

                    if (spacePressed) break;
                }
            }

            promptTMP.text = $"SHUT OFF 3 PRESSURE VALVES! (Saves $30/valve)\nValves Closed: {valvesClosed}/3 | Time: {remaining:F1}s";
            yield return null;
        }

        int totalSavings = valvesClosed * 30;
        titleTMP.text = $"PIPE EMERGENCY RESOLVED! SAVED ${totalSavings}";
        titleTMP.color = new Color(0.2f, 1f, 0.4f);
        promptTMP.text = $"Successfully sealed {valvesClosed}/3 valves! Repair bill slashed by ${totalSavings}!";
        boardManager.PlaySFX(boardManager.CashRegisterSound, null, 1.2f);
        boardManager.PlayHaptic(0.75f, 0.2f);

        yield return new WaitForSeconds(1.3f);
        CancelActiveMinigame();

        onComplete?.Invoke(totalSavings);
    }

    // =========================================================================
    // 4. MINIGAME 4: "DEFENSIVE SABOTAGE — LASER-GRID HEIST"
    // =========================================================================
    public void RunLaserHeistSabotageMinigame(int baseRent, Action<int> onComplete)
    {
        if (isMinigameRunning) CancelActiveMinigame();
        activeMinigameCoroutine = StartCoroutine(LaserHeistRoutine(baseRent, onComplete));
    }

    private IEnumerator LaserHeistRoutine(int baseRent, Action<int> onComplete)
    {
        isMinigameRunning = true;
        var (spawnPos, spawnRot) = GetMinigameSpawnPose();

        activeMinigameRoot = new GameObject("Laser_Heist_Minigame");
        activeMinigameRoot.transform.position = spawnPos;
        activeMinigameRoot.transform.rotation = spawnRot;

        Material frameMat = boardManager.CreateColoredMaterial(new Color(0.12f, 0.14f, 0.18f), 0.9f, 0.3f);
        Material laserMat = boardManager.CreateColoredMaterial(new Color(1f, 0.15f, 0.20f, 0.75f), 0.95f, 0.1f);
        Material nodeGreenMat = boardManager.CreateColoredMaterial(new Color(0.18f, 0.95f, 0.45f), 0.95f, 0.7f);
        Material nodeHackedMat = boardManager.CreateColoredMaterial(new Color(0.20f, 0.85f, 1f), 0.95f, 0.9f);
        Material yellowMat = boardManager.CreateColoredMaterial(new Color(1f, 0.84f, 0.25f), 0.95f, 0.5f);

        // Hologram UI Header
        TextMeshProUGUI titleTMP = CreateHoloText(activeMinigameRoot.transform, "Title", new Vector3(0f, 0.18f, 0f), "DEFENSIVE SABOTAGE: LASER-GRID HEIST", 22f, new Color(1f, 0.35f, 0.35f));
        TextMeshProUGUI timerTMP = CreateHoloText(activeMinigameRoot.transform, "Timer", new Vector3(0f, 0.135f, 0f), "TIME REMAINING: 8.0s", 18f, yellowMat.color);
        TextMeshProUGUI promptTMP = CreateHoloText(activeMinigameRoot.transform, "Prompt", new Vector3(0f, -0.16f, 0f), "POKE 3 GREEN OVERRIDE NODES! AVOID THE SWEEPING RED LASER!", 15f, Color.white);

        // Security Terminal Enclosure Frame
        CreateBar(activeMinigameRoot.transform, new Vector3(0f, 0f, 0.015f), new Vector3(0.42f, 0.24f, 0.012f), frameMat);

        // 3 Green Override Hacking Nodes
        Vector3[] nodeLocalPositions = new Vector3[]
        {
            new Vector3(-0.13f, 0.02f, -0.015f),
            new Vector3(0f, -0.04f, -0.015f),
            new Vector3(0.13f, 0.035f, -0.015f)
        };

        GameObject[] nodes = new GameObject[3];
        bool[] nodeHacked = new bool[3];
        Renderer[] nodeRenderers = new Renderer[3];

        for (int i = 0; i < 3; i++)
        {
            GameObject n = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            n.name = $"Override_Node_{i}";
            n.transform.SetParent(activeMinigameRoot.transform, false);
            n.transform.localPosition = nodeLocalPositions[i];
            n.transform.localScale = Vector3.one * 0.045f;
            nodeRenderers[i] = n.GetComponent<Renderer>();
            nodeRenderers[i].material = nodeGreenMat;
            nodes[i] = n;
        }

        // Sweeping Red Laser Beam
        GameObject laserBeam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        laserBeam.name = "Sweeping_Laser_Beam";
        laserBeam.transform.SetParent(activeMinigameRoot.transform, false);
        laserBeam.transform.localPosition = new Vector3(0f, 0f, -0.018f);
        laserBeam.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        laserBeam.transform.localScale = new Vector3(0.012f, 0.21f, 0.012f);
        laserBeam.GetComponent<Renderer>().material = laserMat;
        Destroy(laserBeam.GetComponent<Collider>());

        boardManager.PlaySFX(boardManager.DiceRollSound, null, 1.4f);
        boardManager.PlayHaptic(0.5f, 0.15f);

        float timeLeft = 8.0f;
        int hackedCount = 0;
        float laserSpeed = 1.35f;

        while (timeLeft > 0f)
        {
            timeLeft -= Time.deltaTime;
            timerTMP.text = $"TIME: {timeLeft:F1}s | HACKED: {hackedCount}/3 (-{hackedCount * 20}% RENT)";

            // Ping-pong sweep laser vertically between -0.08f and +0.08f
            float laserY = Mathf.PingPong(Time.time * laserSpeed, 0.16f) - 0.08f;
            laserBeam.transform.localPosition = new Vector3(0f, laserY, -0.018f);

            Vector3 lPos = boardManager.LeftHandWorldPos;
            Vector3 rPos = boardManager.RightHandWorldPos;
            bool lTracked = boardManager.IsLeftHandTracked && lPos.y > -50f;
            bool rTracked = boardManager.IsRightHandTracked && rPos.y > -50f;

            // Check if hand intersects laser beam
            Vector3 laserWorldPos = laserBeam.transform.position;
            float distLeftToLaser = lTracked ? Mathf.Abs(lPos.y - laserWorldPos.y) : 99f;
            float distRightToLaser = rTracked ? Mathf.Abs(rPos.y - laserWorldPos.y) : 99f;
            bool laserTripped = (distLeftToLaser < 0.022f && Vector3.Distance(lPos, laserWorldPos) < 0.22f) ||
                                (distRightToLaser < 0.022f && Vector3.Distance(rPos, laserWorldPos) < 0.22f);

            if (laserTripped && hackedCount > 0)
            {
                // Trip alarm! Flash red, buzz haptic, reset 1 hacked node
                boardManager.PlaySFX(boardManager.TokenMoveSound, null, 0.65f);
                boardManager.PlayHaptic(0.7f, 0.15f);
                promptTMP.text = "<color=#FF4444>SECURITY ALARM TRIPPED! LASER BEAM BREACHED!</color>";
                promptTMP.color = Color.red;

                for (int i = 2; i >= 0; i--)
                {
                    if (nodeHacked[i])
                    {
                        nodeHacked[i] = false;
                        nodeRenderers[i].material = nodeGreenMat;
                        hackedCount--;
                        break;
                    }
                }
            }

            // Check poke on green nodes
            for (int i = 0; i < 3; i++)
            {
                if (nodeHacked[i]) continue;
                Collider nCol = nodes[i].GetComponent<Collider>();
                if (nCol == null) continue;

                bool lHit = lTracked && Vector3.Distance(nCol.ClosestPoint(lPos), lPos) < 0.028f;
                bool rHit = rTracked && Vector3.Distance(nCol.ClosestPoint(rPos), rPos) < 0.028f;
                bool keyboardTest = UnityEngine.InputSystem.Keyboard.current != null &&
                    ((i == 0 && UnityEngine.InputSystem.Keyboard.current.digit1Key.wasPressedThisFrame) ||
                     (i == 1 && UnityEngine.InputSystem.Keyboard.current.digit2Key.wasPressedThisFrame) ||
                     (i == 2 && UnityEngine.InputSystem.Keyboard.current.digit3Key.wasPressedThisFrame));

                if (lHit || rHit || keyboardTest)
                {
                    nodeHacked[i] = true;
                    nodeRenderers[i].material = nodeHackedMat;
                    hackedCount++;
                    boardManager.PlaySFX(boardManager.CashRegisterSound, null, 1.2f + (hackedCount * 0.15f));
                    boardManager.PlayHaptic(0.6f, 0.1f);
                    promptTMP.text = $"<color=#00FF88>NODE {i + 1} OVERRIDDEN! -{hackedCount * 20}% DISCOUNT!</color>";
                    promptTMP.color = Color.green;
                }
            }

            if (hackedCount >= 3) break;
            yield return null;
        }

        int discountPercent = hackedCount * 20;
        int finalRent = Mathf.Max(10, Mathf.RoundToInt(baseRent * (1f - (discountPercent / 100f))));
        int savedCash = baseRent - finalRent;

        titleTMP.text = $"HEIST COMPLETE! -{discountPercent}% RENT DISCOUNT";
        titleTMP.color = new Color(0.2f, 1f, 0.4f);
        timerTMP.text = $"BASE: ${baseRent} ➔ DISCOUNTED RENT: ${finalRent}";
        promptTMP.text = $"Bypassed {hackedCount}/3 security nodes! Slashed ${savedCash} off rival's rent bill!";

        boardManager.PlaySFX(boardManager.CashRegisterSound, null, 1.35f);
        boardManager.PlayHaptic(0.85f, 0.25f);
        yield return new WaitForSeconds(1.35f);

        CancelActiveMinigame();
        onComplete?.Invoke(finalRent);
    }

    public void CancelActiveMinigame()
    {
        if (activeMinigameCoroutine != null)
        {
            StopCoroutine(activeMinigameCoroutine);
            activeMinigameCoroutine = null;
        }
        if (activeMinigameRoot != null)
        {
            Destroy(activeMinigameRoot);
            activeMinigameRoot = null;
        }
        isMinigameRunning = false;
    }
}
