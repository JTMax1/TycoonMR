using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class TycoonBoardManager : MonoBehaviour
{
    private Dictionary<Color32, Material> sharedMaterialCache = new Dictionary<Color32, Material>();
    private MaterialPropertyBlock sharedPropertyBlock;
    private GameObject comfortVignetteSphere;
    private Material comfortVignetteMaterial;

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

    public Material CreateColoredMaterial(Color color, float smoothness = 0.85f, float metallic = 0.05f)
    {
        Color32 c32 = (Color32)color;
        if (sharedMaterialCache.TryGetValue(c32, out Material cachedMat) && cachedMat != null)
        {
            return cachedMat;
        }

        if (masterURPMaterial == null) InitializeMasterURPMaterial();

        Material m = new Material(masterURPMaterial);
        m.mainTexture = null;
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", null);
        m.color = color;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
        m.enableInstancing = true;
        sharedMaterialCache[c32] = m;
        return m;
    }

    public void SetRendererDynamicColor(Renderer rend, Color color)
    {
        if (rend == null) return;
        if (sharedPropertyBlock == null) sharedPropertyBlock = new MaterialPropertyBlock();
        rend.GetPropertyBlock(sharedPropertyBlock);
        sharedPropertyBlock.SetColor("_BaseColor", color);
        sharedPropertyBlock.SetColor("_Color", color);
        rend.SetPropertyBlock(sharedPropertyBlock);
    }

    public void FlashRendererDynamicColor(Renderer rend, Color flashColor, float duration = 0.35f)
    {
        if (rend != null) StartCoroutine(AnimateDynamicColorFlash(rend, flashColor, duration));
    }

    private IEnumerator AnimateDynamicColorFlash(Renderer rend, Color flashColor, float duration)
    {
        if (rend == null) yield break;
        if (sharedPropertyBlock == null) sharedPropertyBlock = new MaterialPropertyBlock();

        Color originalColor = rend.sharedMaterial != null ? rend.sharedMaterial.color : Color.white;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (rend == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.PingPong(elapsed * 4f, 1f);
            Color c = Color.Lerp(originalColor, flashColor, t);
            rend.GetPropertyBlock(sharedPropertyBlock);
            sharedPropertyBlock.SetColor("_BaseColor", c);
            sharedPropertyBlock.SetColor("_Color", c);
            rend.SetPropertyBlock(sharedPropertyBlock);
            yield return null;
        }

        if (rend != null)
        {
            rend.GetPropertyBlock(sharedPropertyBlock);
            sharedPropertyBlock.SetColor("_BaseColor", originalColor);
            sharedPropertyBlock.SetColor("_Color", originalColor);
            rend.SetPropertyBlock(sharedPropertyBlock);
        }
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

        // Player 1 (MOGUL ALPHA - North): Deep baritone executive synth (210 Hz - 280 Hz formant)
        int alphaLen = sampleRate * 9 / 100;
        float[] alphaSamples = new float[alphaLen];
        for (int i = 0; i < alphaLen; i++)
        {
            float t = (float)i / sampleRate;
            float env = Mathf.Sin(Mathf.PI * (float)i / alphaLen);
            float f0 = Mathf.Lerp(210f, 280f, (float)i / alphaLen);
            alphaSamples[i] = (Mathf.Sin(2f * Mathf.PI * f0 * t) * 0.65f +
                               Mathf.Sin(2f * Mathf.PI * (f0 * 2f) * t) * 0.25f +
                               Mathf.Sin(2f * Mathf.PI * (f0 * 0.5f) * t) * 0.15f) * env * 0.5f;
        }
        procVoiceAlpha = AudioClip.Create("ProcVoiceAlpha", alphaLen, 1, sampleRate, false);
        procVoiceAlpha.SetData(alphaSamples, 0);

        // Player 2 (BARONESS BETA - West): Crisp, high-tech melodic synth (360 Hz - 480 Hz formant)
        int betaLen = sampleRate * 8 / 100;
        float[] betaSamples = new float[betaLen];
        for (int i = 0; i < betaLen; i++)
        {
            float t = (float)i / sampleRate;
            float env = Mathf.Sin(Mathf.PI * (float)i / betaLen);
            float f0 = Mathf.Lerp(360f, 480f, (float)i / betaLen);
            float fm = Mathf.Sin(2f * Mathf.PI * (f0 * 2f) * t) * 0.8f;
            betaSamples[i] = (Mathf.Sin(2f * Mathf.PI * f0 * t + fm) * 0.7f +
                              Mathf.Sin(2f * Mathf.PI * (f0 * 1.5f) * t) * 0.3f) * env * 0.45f;
        }
        procVoiceBeta = AudioClip.Create("ProcVoiceBeta", betaLen, 1, sampleRate, false);
        procVoiceBeta.SetData(betaSamples, 0);

        // Player 3 (TITAN GAMMA - East): Warm, measured bass-baritone cadence (165 Hz - 220 Hz formant)
        int gammaLen = sampleRate * 10 / 100;
        float[] gammaSamples = new float[gammaLen];
        for (int i = 0; i < gammaLen; i++)
        {
            float t = (float)i / sampleRate;
            float env = Mathf.Sin(Mathf.PI * (float)i / gammaLen);
            float f0 = Mathf.Lerp(165f, 220f, (float)i / gammaLen);
            gammaSamples[i] = (Mathf.Sin(2f * Mathf.PI * f0 * t) * 0.7f +
                               Mathf.Sin(2f * Mathf.PI * (f0 * 2f) * t) * 0.2f +
                               Mathf.Sin(2f * Mathf.PI * (f0 * 3f) * t) * 0.1f) * env * 0.55f;
        }
        procVoiceGamma = AudioClip.Create("ProcVoiceGamma", gammaLen, 1, sampleRate, false);
        procVoiceGamma.SetData(gammaSamples, 0);

        // Spatial Police Siren (600 Hz <-> 920 Hz sine sweep over 0.65s)
        int sirenLen = Mathf.RoundToInt(sampleRate * 0.65f);
        float[] sirenSamples = new float[sirenLen];
        float sirenPhase = 0f;
        for (int i = 0; i < sirenLen; i++)
        {
            float t = (float)i / sampleRate;
            float sweep = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * (1f / 0.65f) * t);
            float freq = Mathf.Lerp(600f, 920f, sweep);
            sirenPhase += 2f * Mathf.PI * freq / sampleRate;
            float env = Mathf.Sin(Mathf.PI * (float)i / sirenLen);
            sirenSamples[i] = Mathf.Sin(sirenPhase) * env * 0.5f;
        }
        procSirenClip = AudioClip.Create("ProcSiren", sirenLen, 1, sampleRate, false);
        procSirenClip.SetData(sirenSamples, 0);

        // Looping VIP Helicopter Rotor Hum (22 Hz fundamental rotor pulse + 66 Hz drone)
        int chopperLen = sampleRate; // 1 full second seamless loop
        float[] chopperSamples = new float[chopperLen];
        for (int i = 0; i < chopperLen; i++)
        {
            float t = (float)i / sampleRate;
            chopperSamples[i] = (Mathf.Sin(2f * Mathf.PI * 22f * t) * 0.55f +
                                 Mathf.Sin(2f * Mathf.PI * 44f * t) * 0.30f +
                                 Mathf.Sin(2f * Mathf.PI * 66f * t) * 0.15f) * 0.3f;
        }
        procChopperHumClip = AudioClip.Create("ProcChopperHum", chopperLen, 1, sampleRate, false);
        procChopperHumClip.SetData(chopperSamples, 0);

        // 3D Laser Cage Shatter Clip (resonant glass-like ping + noise burst)
        int shatterLen = sampleRate * 35 / 100;
        float[] shatterSamples = new float[shatterLen];
        for (int i = 0; i < shatterLen; i++)
        {
            float t = (float)i / sampleRate;
            float decay = Mathf.Exp(-t * 14f);
            float ping = Mathf.Sin(2f * Mathf.PI * 2400f * t) * 0.4f + Mathf.Sin(2f * Mathf.PI * 1800f * t) * 0.25f;
            shatterSamples[i] = (Random.Range(-1f, 1f) * 0.45f + ping) * decay * 0.5f;
        }
        procGlassShatterClip = AudioClip.Create("ProcGlassShatter", shatterLen, 1, sampleRate, false);
        procGlassShatterClip.SetData(shatterSamples, 0);
    }

    public void PlaySFX(AudioClip customClip, AudioClip fallbackClip, float pitch = 1f)
    {
        if (sfxSource == null) return;
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(customClip != null ? customClip : fallbackClip);
    }

    public void PlayHaptic(float amplitude = 0.5f, float duration = 0.15f)
    {
        try
        {
            OVRInput.SetControllerVibration(amplitude, amplitude, OVRInput.Controller.Touch);
            StartCoroutine(StopHapticAfter(duration));
        }
        catch { }
    }

    IEnumerator StopHapticAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        try
        {
            OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.Touch);
        }
        catch { }
    }

    public void PlayDiceRattleSound()
    {
        PlaySFX(diceRollSound, procDiceClip, UnityEngine.Random.Range(0.95f, 1.25f));
    }

    public void SpawnCelebrationFireworks(Vector3 worldPos)
    {
        StartCoroutine(CelebrationFireworksCoroutine(worldPos));
    }

    IEnumerator CelebrationFireworksCoroutine(Vector3 originPos)
    {
        Vector3 centerParkPos = transform.position + Vector3.up * 0.12f;
        Vector3 launchStart = (originPos != Vector3.zero) ? originPos : centerParkPos;

        int count = 18;
        GameObject[] spheres = new GameObject[count];
        Vector3[] burstVelocities = new Vector3[count];

        Color[] fireworkColors = new Color[]
        {
            new Color(1f, 0.84f, 0.15f), // Gold
            new Color(0.15f, 0.90f, 1.0f), // Cyan
            new Color(0.18f, 0.95f, 0.45f) // Emerald
        };

        Vector3 burstApex = centerParkPos + Vector3.up * 0.65f;
        float launchDuration = 0.42f;
        float elapsed = 0f;

        PlaySFX(diceRollSound, procDiceClip, 1.35f);

        for (int i = 0; i < count; i++)
        {
            GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            s.name = $"CelebrationFireworkSpark_{i}";
            s.transform.position = launchStart;
            s.transform.localScale = Vector3.one * 0.038f;
            Collider c = s.GetComponent<Collider>();
            if (c != null) Destroy(c);

            Color col = fireworkColors[i % fireworkColors.Length];
            Renderer r = s.GetComponent<Renderer>();
            if (r != null) r.material = CreateColoredMaterial(col, 0.95f, 0.1f);

            spheres[i] = s;

            float angleH = (i / (float)count) * Mathf.PI * 2f;
            float pitch = Random.Range(-0.35f, 0.85f);
            Vector3 dir = new Vector3(Mathf.Cos(angleH), pitch, Mathf.Sin(angleH)).normalized;
            burstVelocities[i] = dir * Random.Range(0.65f, 1.45f);
        }

        while (elapsed < launchDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / launchDuration;
            Vector3 currentLaunchPos = Vector3.Lerp(launchStart, burstApex, Mathf.SmoothStep(0f, 1f, t));
            for (int i = 0; i < count; i++)
            {
                if (spheres[i] != null)
                {
                    spheres[i].transform.position = currentLaunchPos + Random.insideUnitSphere * 0.015f;
                }
            }
            yield return null;
        }

        PlaySFX(cashRegisterSound, procCashClip, 1.25f);
        PlayHaptic(0.6f, 0.25f);

        float burstDuration = 1.35f;
        elapsed = 0f;

        while (elapsed < burstDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / burstDuration;
            float gravity = -0.98f * elapsed;

            for (int i = 0; i < count; i++)
            {
                if (spheres[i] != null)
                {
                    spheres[i].transform.position += (burstVelocities[i] + Vector3.up * gravity) * Time.deltaTime;
                    float scale = Mathf.Lerp(0.042f, 0.005f, progress);
                    spheres[i].transform.localScale = Vector3.one * scale;
                }
            }
            yield return null;
        }

        for (int i = 0; i < count; i++)
        {
            if (spheres[i] != null) Destroy(spheres[i]);
        }
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

    public void ToggleMarketAtmosphere()
    {
        isMarketNightMode = !isMarketNightMode;
        foreach (Light l in cityNightLights)
        {
            if (l != null) l.intensity = isMarketNightMode ? 0.07f : 0.03f;
        }
        if (passthroughLayer != null)
        {
            StopCoroutine("FadePassthroughOpacity");
            StartCoroutine(FadePassthroughOpacity(isMarketNightMode ? 0.22f : 1.0f));
        }
    }

    IEnumerator FadePassthroughOpacity(float targetOpacity)
    {
        float startOpacity = passthroughLayer != null ? passthroughLayer.textureOpacity : 1.0f;
        float duration = 0.8f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (passthroughLayer != null)
                passthroughLayer.textureOpacity = Mathf.Lerp(startOpacity, targetOpacity, elapsed / duration);
            yield return null;
        }
        if (passthroughLayer != null)
            passthroughLayer.textureOpacity = targetOpacity;
    }

    public void TriggerComfortVignetteTransition(System.Action teleportAction)
    {
        StartCoroutine(RunComfortVignetteRoutine(teleportAction));
    }

    private IEnumerator RunComfortVignetteRoutine(System.Action teleportAction)
    {
        Transform cam = Camera.main != null ? Camera.main.transform : null;
        if (cam == null)
        {
            teleportAction?.Invoke();
            yield break;
        }

        if (comfortVignetteSphere == null)
        {
            comfortVignetteSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            comfortVignetteSphere.name = "VR_Comfort_Tunneling_Vignette";
            Destroy(comfortVignetteSphere.GetComponent<Collider>());
            comfortVignetteSphere.transform.localScale = Vector3.one * 0.45f;
            comfortVignetteMaterial = CreateColoredMaterial(new Color(0.01f, 0.01f, 0.02f, 0f), 0.1f);
            if (comfortVignetteMaterial.HasProperty("_Cull")) comfortVignetteMaterial.SetFloat("_Cull", 1f);
            comfortVignetteSphere.GetComponent<Renderer>().material = comfortVignetteMaterial;
        }

        comfortVignetteSphere.SetActive(true);
        comfortVignetteSphere.transform.position = cam.position;
        comfortVignetteSphere.transform.SetParent(cam, true);

        // 0.35s Comfort Vignette: 0.15s Fade In + 0.05s Teleport frame + 0.15s Fade Out
        float elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / 0.15f) * 0.98f;
            if (comfortVignetteMaterial != null) comfortVignetteMaterial.color = new Color(0.01f, 0.01f, 0.02f, alpha);
            yield return null;
        }
        if (comfortVignetteMaterial != null) comfortVignetteMaterial.color = new Color(0.01f, 0.01f, 0.02f, 0.98f);

        teleportAction?.Invoke();
        yield return new WaitForSeconds(0.05f);

        elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            float alpha = (1f - Mathf.Clamp01(elapsed / 0.15f)) * 0.98f;
            if (comfortVignetteMaterial != null) comfortVignetteMaterial.color = new Color(0.01f, 0.01f, 0.02f, alpha);
            yield return null;
        }

        if (comfortVignetteSphere != null)
        {
            comfortVignetteSphere.transform.SetParent(null);
            comfortVignetteSphere.SetActive(false);
        }
    }
}
