using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TycoonBoardManager))]
public class TycoonMinigameOrchestrator : MonoBehaviour
{
    private TycoonBoardManager boardManager;
    private GameObject activeMinigameRoot;
    private bool isMinigameRunning = false;

    public bool IsMinigameRunning => isMinigameRunning;

    void Awake()
    {
        boardManager = GetComponent<TycoonBoardManager>();
    }

    // =========================================================================
    // HELPER: GET SPAWN TRANSFORM IN FRONT OF PLAYER
    // =========================================================================
    private (Vector3 pos, Quaternion rot) GetMinigameSpawnPose()
    {
        Transform eye = Camera.main != null ? Camera.main.transform : boardManager.transform;
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

    // =========================================================================
    // 1. MINIGAME 1: "BANK ERROR IN YOUR FAVOR — VAULT CASH SWAT"
    // =========================================================================
    public void RunVaultCashSwatMinigame(Action<int> onComplete)
    {
        if (isMinigameRunning) CancelActiveMinigame();
        StartCoroutine(VaultCashSwatRoutine(onComplete));
    }

    private class FloatingCashBill
    {
        public GameObject billGO;
        public Vector3 velocity;
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
        Material darkSteelMat = boardManager.CreateColoredMaterial(new Color(0.12f, 0.14f, 0.18f), 0.7f, 0.3f);
        Material emeraldCashMat = boardManager.CreateColoredMaterial(new Color(0.15f, 0.75f, 0.35f), 0.5f, 0.05f);
        Material goldTrimMat = boardManager.CreateColoredMaterial(new Color(0.95f, 0.85f, 0.40f), 0.85f, 0.7f);

        // 3D Vault Frame
        GameObject vaultFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vaultFrame.name = "Vault_Frame";
        vaultFrame.transform.SetParent(activeMinigameRoot.transform, false);
        vaultFrame.transform.localPosition = new Vector3(0f, 0f, 0.04f);
        vaultFrame.transform.localScale = new Vector3(0.38f, 0.38f, 0.06f);
        Destroy(vaultFrame.GetComponent<Collider>());
        vaultFrame.GetComponent<Renderer>().material = darkSteelMat;

        // Vault Door
        GameObject vaultDoor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        vaultDoor.name = "Vault_Door";
        vaultDoor.transform.SetParent(activeMinigameRoot.transform, false);
        vaultDoor.transform.localPosition = new Vector3(-0.06f, 0f, 0f);
        vaultDoor.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        vaultDoor.transform.localScale = new Vector3(0.28f, 0.02f, 0.28f);
        Destroy(vaultDoor.GetComponent<Collider>());
        vaultDoor.GetComponent<Renderer>().material = goldVaultMat;

        // HUD Text
        TextMeshProUGUI titleTMP = CreateHoloText(activeMinigameRoot.transform, "Title", new Vector3(0f, 0.24f, 0f), "BANK ERROR IN YOUR FAVOR!", 34, new Color(1f, 0.85f, 0.30f));
        TextMeshProUGUI counterTMP = CreateHoloText(activeMinigameRoot.transform, "Counter", new Vector3(0f, 0.18f, 0f), "SWAT FLYING CASH! Swatted: 0 | Time: 8.0s", 25, Color.white);
        TextMeshProUGUI payoutTMP = CreateHoloText(activeMinigameRoot.transform, "Payout", new Vector3(0f, -0.22f, 0f), "Guaranteed Base: $100 (+$25 / bill swatted)", 22, new Color(0.4f, 0.95f, 0.6f));

        boardManager.PlaySFX(boardManager.CashRegisterSound, null, 1.2f);
        boardManager.PlayHaptic(0.5f, 0.15f);

        // Animate door swinging open
        float doorOpenTime = 0.5f;
        float doorElapsed = 0f;
        while (doorElapsed < doorOpenTime)
        {
            doorElapsed += Time.deltaTime;
            float t = doorElapsed / doorOpenTime;
            vaultDoor.transform.localPosition = Vector3.Lerp(new Vector3(-0.06f, 0f, 0f), new Vector3(-0.20f, 0f, -0.05f), t);
            vaultDoor.transform.localRotation = Quaternion.Euler(90f, -65f * t, 0f);
            yield return null;
        }

        // Spawn 12 Flying Cash Bills
        List<FloatingCashBill> bills = new List<FloatingCashBill>();
        int totalBills = 12;
        int swattedCount = 0;

        for (int i = 0; i < totalBills; i++)
        {
            GameObject bill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bill.name = $"DollarBill_{i}";
            bill.transform.SetParent(activeMinigameRoot.transform, false);
            bill.transform.localPosition = new Vector3(UnityEngine.Random.Range(-0.06f, 0.06f), UnityEngine.Random.Range(-0.04f, 0.04f), 0.02f);
            bill.transform.localScale = new Vector3(0.08f, 0.04f, 0.004f);
            Destroy(bill.GetComponent<Collider>());
            bill.GetComponent<Renderer>().material = emeraldCashMat;

            // Gold band
            GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cube);
            band.transform.SetParent(bill.transform, false);
            band.transform.localPosition = Vector3.zero;
            band.transform.localScale = new Vector3(0.35f, 1.02f, 1.15f);
            Destroy(band.GetComponent<Collider>());
            band.GetComponent<Renderer>().material = goldTrimMat;

            Vector3 outwardVel = new Vector3(
                UnityEngine.Random.Range(-0.16f, 0.16f),
                UnityEngine.Random.Range(0.04f, 0.20f),
                UnityEngine.Random.Range(-0.18f, -0.06f)
            );
            Vector3 rot = new Vector3(
                UnityEngine.Random.Range(90f, 220f),
                UnityEngine.Random.Range(60f, 180f),
                UnityEngine.Random.Range(40f, 120f)
            );

            bills.Add(new FloatingCashBill
            {
                billGO = bill,
                velocity = outwardVel,
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

            foreach (var b in bills)
            {
                if (b.isSwatted || b.billGO == null) continue;

                // Move bill
                b.billGO.transform.localPosition += b.velocity * Time.deltaTime;
                b.billGO.transform.Rotate(b.rotSpeed * Time.deltaTime);

                // Gentle fluttering drag
                b.velocity.y += Mathf.Sin((elapsed + b.billGO.transform.position.x) * 4f) * 0.02f * Time.deltaTime;

                // Check Hand Swat Proximity (0.085m = 8.5cm)
                Vector3 billWorldPos = b.billGO.transform.position;
                bool swattedByLeft = leftTracked && Vector3.Distance(leftHand, billWorldPos) < 0.085f;
                bool swattedByRight = rightTracked && Vector3.Distance(rightHand, billWorldPos) < 0.085f;

                // Desktop / Mouse fallback
                bool mouseSwat = (Camera.main != null && Input.GetMouseButton(0)) &&
                    Vector3.Distance(Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0.45f)), billWorldPos) < 0.12f;

                if (swattedByLeft || swattedByRight || mouseSwat)
                {
                    b.isSwatted = true;
                    swattedCount++;
                    boardManager.PlaySFX(boardManager.CashRegisterSound, null, 1.0f + (swattedCount * 0.05f));
                    boardManager.PlayHaptic(0.55f, 0.08f);

                    // Burst into gold sparks / hide bill
                    StartCoroutine(AnimateBillSwatBurst(b.billGO));
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
        StartCoroutine(JailLockpickRoutine(onComplete));
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

        // Padlock Body
        GameObject lockBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
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

        // Center Green Shear Line
        GameObject shearLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shearLine.name = "Shear_Line";
        shearLine.transform.SetParent(lockBody.transform, false);
        shearLine.transform.localPosition = new Vector3(0f, 0f, -0.52f);
        shearLine.transform.localScale = new Vector3(0.92f, 0.04f, 0.06f);
        Destroy(shearLine.GetComponent<Collider>());
        shearLine.GetComponent<Renderer>().material = neonLineMat;

        // HUD Text
        TextMeshProUGUI titleTMP = CreateHoloText(activeMinigameRoot.transform, "Title", new Vector3(0f, 0.25f, 0f), "THE GREAT ESCAPE — LOCKPICK", 34, new Color(1f, 0.85f, 0.30f));
        TextMeshProUGUI promptTMP = CreateHoloText(activeMinigameRoot.transform, "Prompt", new Vector3(0f, -0.19f, 0f), "POKE EACH PIN WHEN AT GREEN LINE!\n(Locks: 0/3 | Time: 10.0s)", 23, Color.white);

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

            for (int i = 0; i < pins.Count; i++)
            {
                var pin = pins[i];
                if (pin.isLocked) continue;

                // Pin oscillation
                float currentY = pin.baseY + Mathf.Sin(elapsed * pin.freq) * pin.amplitude;
                pin.pinTransform.localPosition = new Vector3(pinXOffsets[i], currentY, -0.55f);

                // Check Hand Poke Proximity (< 6.5cm)
                Vector3 pinWorldPos = pin.pinTransform.position;
                bool pokeLeft = leftTracked && Vector3.Distance(leftHand, pinWorldPos) < 0.065f;
                bool pokeRight = rightTracked && Vector3.Distance(rightHand, pinWorldPos) < 0.065f;
                bool mousePoke = Input.GetMouseButtonDown(0);

                if ((pokeLeft || pokeRight || mousePoke) && Time.time > lastPokeAttemptTime)
                {
                    lastPokeAttemptTime = Time.time + 0.35f;

                    // Check if near shear line (|currentY| < 0.065f)
                    if (Mathf.Abs(currentY) < 0.075f)
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
        StartCoroutine(PipeRepairRoutine(onComplete));
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

        // Horizontal Pipe Body
        GameObject mainPipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        mainPipe.name = "Main_Pipe";
        mainPipe.transform.SetParent(activeMinigameRoot.transform, false);
        mainPipe.transform.localPosition = new Vector3(0f, 0f, 0f);
        mainPipe.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        mainPipe.transform.localScale = new Vector3(0.045f, 0.24f, 0.045f);
        Destroy(mainPipe.GetComponent<Collider>());
        mainPipe.GetComponent<Renderer>().material = pipeSteelMat;

        // HUD Text
        TextMeshProUGUI titleTMP = CreateHoloText(activeMinigameRoot.transform, "Title", new Vector3(0f, 0.22f, 0f), "BURST PIPE EMERGENCY!", 34, new Color(1f, 0.45f, 0.35f));
        TextMeshProUGUI promptTMP = CreateHoloText(activeMinigameRoot.transform, "Prompt", new Vector3(0f, -0.19f, 0f), "SHUT OFF 3 PRESSURE VALVES! (Saves $30/valve)\nValves Closed: 0/3 | Time: 7.0s", 23, Color.white);

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

                // Check Hand Proximity (< 7.5cm)
                Vector3 valveWorldPos = v.valveTransform.position;
                bool touchLeft = leftTracked && Vector3.Distance(leftHand, valveWorldPos) < 0.075f;
                bool touchRight = rightTracked && Vector3.Distance(rightHand, valveWorldPos) < 0.075f;
                bool mouseTouch = Input.GetMouseButtonDown(0);

                if ((touchLeft || touchRight || mouseTouch) && Time.time > lastTouchTime)
                {
                    lastTouchTime = Time.time + 0.30f;
                    v.isClosed = true;
                    valvesClosed++;

                    v.valveRenderer.material = valveGreenMat;
                    v.valveTransform.Rotate(Vector3.up * 180f);
                    if (v.steamPlume != null) v.steamPlume.SetActive(false);

                    boardManager.PlaySFX(boardManager.CashRegisterSound, null, 1.15f);
                    boardManager.PlayHaptic(0.7f, 0.10f);
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

    public void CancelActiveMinigame()
    {
        if (activeMinigameRoot != null)
        {
            Destroy(activeMinigameRoot);
            activeMinigameRoot = null;
        }
        isMinigameRunning = false;
    }
}
