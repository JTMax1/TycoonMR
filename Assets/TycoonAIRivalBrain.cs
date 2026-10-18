using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public partial class TycoonBoardManager : MonoBehaviour
{
    void Create3DRivalMogulAvatar()
    {
        Create3DRivalMogulAvatars();
    }

    void Create3DRivalMogulAvatars()
    {
        // Player 1 (MOGUL ALPHA — Aggressive Shark): North (+Z, rot 180)
        CreateSingleAIAvatar(1, new Vector3(0f, 0.14f, (boardSize * 0.5f) + 0.22f), Quaternion.Euler(0f, 180f, 0f));

        // Player 2 (BARONESS BETA — Tech & Transit): West (-X, rot 90)
        CreateSingleAIAvatar(2, new Vector3(-(boardSize * 0.5f) - 0.22f, 0.14f, 0f), Quaternion.Euler(0f, 90f, 0f));

        // Player 3 (TITAN GAMMA — Conservative Banker): East (+X, rot -90)
        CreateSingleAIAvatar(3, new Vector3((boardSize * 0.5f) + 0.22f, 0.14f, 0f), Quaternion.Euler(0f, -90f, 0f));

        UpdateActivePlayerVisibilities();
    }

    GameObject GetAvatarPrefabForPlayer(int p)
    {
        if (p == 1)
        {
            if (rivalAvatarPrefab != null) return rivalAvatarPrefab;
            if (rivalAvatarPrefabs != null && rivalAvatarPrefabs.Count > 0 && rivalAvatarPrefabs[0] != null) return rivalAvatarPrefabs[0];
            return null;
        }
        if (p == 2)
        {
            if (rival2AvatarPrefab != null) return rival2AvatarPrefab;
            if (rivalAvatarPrefabs != null && rivalAvatarPrefabs.Count > 1 && rivalAvatarPrefabs[1] != null) return rivalAvatarPrefabs[1];
            if (rivalAvatarPrefabs != null && rivalAvatarPrefabs.Count > 0 && rivalAvatarPrefabs[0] != null) return rivalAvatarPrefabs[0];
            return rivalAvatarPrefab;
        }
        if (p == 3)
        {
            if (rival3AvatarPrefab != null) return rival3AvatarPrefab;
            if (rivalAvatarPrefabs != null && rivalAvatarPrefabs.Count > 2 && rivalAvatarPrefabs[2] != null) return rivalAvatarPrefabs[2];
            if (rivalAvatarPrefabs != null && rivalAvatarPrefabs.Count > 0 && rivalAvatarPrefabs[0] != null) return rivalAvatarPrefabs[0];
            return rivalAvatarPrefab;
        }
        return null;
    }

    void CreateSingleAIAvatar(int p, Vector3 localPos, Quaternion localRot)
    {
        string aiName = GetPlayerName(p);
        Color sigColor = GetPlayerColor(p);

        GameObject avatarHolder = new GameObject($"Rival_Avatar_P{p}_{aiName.Replace(" ", "_")}");
        avatarHolder.transform.SetParent(transform, false);
        avatarHolder.transform.localPosition = localPos;
        avatarHolder.transform.localRotation = localRot;
        rivalAvatarRoots[p] = avatarHolder.transform;

        GameObject customAvatarPrefab = GetAvatarPrefabForPlayer(p);
        if (customAvatarPrefab != null && customAvatarPrefab.GetComponent<OVRCameraRig>() == null)
        {
            GameObject customModel = Instantiate(customAvatarPrefab, avatarHolder.transform);
            StripRoguePhysicsFromImportedPrefab(customModel);
            customModel.transform.localPosition = Vector3.zero;
            NormalizePrefabSize(customModel, 0.35f);
            rivalHeadTransforms[p] = customModel.transform;
        }
        else
        {
            Material suitMat = CreateColoredMaterial(new Color(0.10f, 0.12f, 0.18f), 0.85f, 0.4f);
            Material lapelMat = CreateColoredMaterial(sigColor, 0.9f, 0.85f);
            Material headMat = CreateColoredMaterial(new Color(0.82f, 0.86f, 0.94f), 0.9f, 0.7f);
            Material visorMat = CreateColoredMaterial(sigColor, 0.95f);

            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            torso.name = $"{aiName}_Torso";
            torso.transform.SetParent(avatarHolder.transform, false);
            torso.transform.localPosition = new Vector3(0f, -0.04f, 0f);
            torso.transform.localScale = new Vector3(0.18f, 0.14f, 0.11f);
            Destroy(torso.GetComponent<Collider>());
            torso.GetComponent<Renderer>().material = suitMat;

            GameObject tie = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tie.name = $"{aiName}_Tie";
            tie.transform.SetParent(torso.transform, false);
            tie.transform.localPosition = new Vector3(0f, 0.25f, 0.48f);
            tie.transform.localScale = new Vector3(0.18f, 0.55f, 0.12f);
            Destroy(tie.GetComponent<Collider>());
            tie.GetComponent<Renderer>().material = lapelMat;

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = $"{aiName}_Head";
            head.transform.SetParent(avatarHolder.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.16f, 0f);
            head.transform.localScale = new Vector3(0.12f, 0.135f, 0.12f);
            Destroy(head.GetComponent<Collider>());
            head.GetComponent<Renderer>().material = headMat;
            rivalHeadTransforms[p] = head.transform;

            GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = $"{aiName}_Visor";
            visor.transform.SetParent(head.transform, false);
            visor.transform.localPosition = new Vector3(0f, 0.08f, 0.44f);
            visor.transform.localScale = new Vector3(0.72f, 0.22f, 0.22f);
            Destroy(visor.GetComponent<Collider>());
            rivalVisorRenderers[p] = visor.GetComponent<Renderer>();
            rivalVisorRenderers[p].material = visorMat;

            GameObject lHand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lHand.name = $"{aiName}_Left_Hand";
            lHand.transform.SetParent(avatarHolder.transform, false);
            lHand.transform.localPosition = new Vector3(-0.16f, 0.02f, 0.10f);
            lHand.transform.localScale = new Vector3(0.045f, 0.035f, 0.055f);
            Destroy(lHand.GetComponent<Collider>());
            lHand.GetComponent<Renderer>().material = lapelMat;
            rivalLeftHands[p] = lHand.transform;

            GameObject rHand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rHand.name = $"{aiName}_Right_Hand";
            rHand.transform.SetParent(avatarHolder.transform, false);
            rHand.transform.localPosition = new Vector3(0.16f, 0.02f, 0.10f);
            rHand.transform.localScale = new Vector3(0.045f, 0.035f, 0.055f);
            Destroy(rHand.GetComponent<Collider>());
            rHand.GetComponent<Renderer>().material = lapelMat;
            rivalRightHands[p] = rHand.transform;
        }

        GameObject bubbleCanvas = new GameObject($"{aiName}_Speech_Bubble");
        bubbleCanvas.transform.SetParent(avatarHolder.transform, false);
        bubbleCanvas.transform.localPosition = new Vector3(0f, 0.29f, 0f);
        bubbleCanvas.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        bubbleCanvas.transform.localScale = Vector3.one * 0.0005f;

        Canvas c = bubbleCanvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.WorldSpace;
        bubbleCanvas.GetComponent<RectTransform>().sizeDelta = new Vector2(620f, 110f);

        rivalSpeechBubbles[p] = CreateUGUIText(bubbleCanvas.transform, "BubbleText", Vector2.zero, 30, sigColor);
        rivalSpeechBubbles[p].text = $"{aiName}: \"Let's see what you've got!\"";

        AudioSource vSource = avatarHolder.AddComponent<AudioSource>();
        vSource.spatialBlend = 0.85f;
        vSource.playOnAwake = false;
        vSource.minDistance = 0.2f;
        vSource.maxDistance = 3.5f;
        vSource.rolloffMode = AudioRolloffMode.Logarithmic;
        rivalVoiceSources[p] = vSource;

        if (p == 1)
        {
            rivalAvatarRoot = rivalAvatarRoots[1];
            rivalHeadTransform = rivalHeadTransforms[1];
            rivalLeftHandTransform = rivalLeftHands[1];
            rivalRightHandTransform = rivalRightHands[1];
            rivalVisorRenderer = rivalVisorRenderers[1];
            rivalSpeechBubbleTMP = rivalSpeechBubbles[1];
        }
    }

    void PlayRivalVoiceCadence(int aiPlayerIdx, AvatarMood mood)
    {
        if (aiPlayerIdx < 1 || aiPlayerIdx > 3) return;
        if (rivalVoiceSources[aiPlayerIdx] == null) return;
        StartCoroutine(VoiceCadenceCoroutine(aiPlayerIdx, mood));
    }

    IEnumerator VoiceCadenceCoroutine(int aiIdx, AvatarMood mood)
    {
        AudioSource src = rivalVoiceSources[aiIdx];
        if (src == null) yield break;

        AudioClip clip = (aiIdx == 1) ? procVoiceAlpha : ((aiIdx == 2) ? procVoiceBeta : procVoiceGamma);
        if (clip == null) yield break;

        float[] pitches;
        float delay;

        switch (mood)
        {
            case AvatarMood.HappyCheer:
                pitches = new float[] { 1.0f, 1.18f, 1.35f };
                delay = 0.085f;
                break;
            case AvatarMood.AngrySlam:
                pitches = new float[] { 0.95f, 0.85f, 0.75f };
                delay = 0.075f;
                break;
            case AvatarMood.Thinking:
                pitches = new float[] { 1.02f, 0.98f, 1.04f };
                delay = 0.11f;
                break;
            default:
                pitches = new float[] { 1.0f, 1.05f, 1.0f };
                delay = 0.09f;
                break;
        }

        for (int i = 0; i < pitches.Length; i++)
        {
            if (src == null) yield break;
            src.pitch = pitches[i];
            src.PlayOneShot(clip, 0.85f);
            yield return new WaitForSeconds(delay);
        }
    }

    public void SetRivalReaction(int aiPlayerIdx, AvatarMood mood, string speechLine, float duration = 3.0f)
    {
        if (aiPlayerIdx < 1 || aiPlayerIdx > 3) aiPlayerIdx = 1;
        currentRivalMoods[aiPlayerIdx] = mood;
        moodResetTimers[aiPlayerIdx] = duration;

        if (rivalSpeechBubbles[aiPlayerIdx] != null)
        {
            rivalSpeechBubbles[aiPlayerIdx].text = $"{GetPlayerName(aiPlayerIdx)}: \"{speechLine}\"";
        }

        if (rivalVisorRenderers[aiPlayerIdx] != null)
        {
            Color visorCol = GetPlayerColor(aiPlayerIdx);
            if (mood == AvatarMood.HappyCheer) visorCol = new Color(0.20f, 1f, 0.45f);
            else if (mood == AvatarMood.AngrySlam) visorCol = new Color(1f, 0.18f, 0.20f);
            else if (mood == AvatarMood.Thinking) visorCol = new Color(1f, 0.84f, 0.20f);
            rivalVisorRenderers[aiPlayerIdx].material = CreateColoredMaterial(visorCol, 0.95f);
        }

        PlayRivalVoiceCadence(aiPlayerIdx, mood);

        if (aiPlayerIdx == 1)
        {
            currentRivalMood = mood;
            moodResetTimer = duration;
        }
    }

    public void SetRivalReaction(AvatarMood mood, string speechLine, float duration = 3.0f)
    {
        SetRivalReaction(1, mood, speechLine, duration);
    }

    void AnimateRivalMogulAvatar()
    {
        float t = Time.time;
        for (int p = 1; p <= 3; p++)
        {
            if (rivalAvatarRoots[p] == null || !rivalAvatarRoots[p].gameObject.activeSelf) continue;

            if (moodResetTimers[p] > 0f)
            {
                moodResetTimers[p] -= Time.deltaTime;
                if (moodResetTimers[p] <= 0f)
                {
                    currentRivalMoods[p] = AvatarMood.Neutral;
                    if (rivalVisorRenderers[p] != null)
                        rivalVisorRenderers[p].material = CreateColoredMaterial(GetPlayerColor(p), 0.95f);
                }
            }

            Vector3 basePos;
            if (p == 1) basePos = new Vector3(0f, 0.14f, (boardSize * 0.5f) + 0.22f);
            else if (p == 2) basePos = new Vector3(-(boardSize * 0.5f) - 0.22f, 0.14f, 0f);
            else basePos = new Vector3((boardSize * 0.5f) + 0.22f, 0.14f, 0f);

            rivalAvatarRoots[p].localPosition = basePos + new Vector3(0f, Mathf.Sin(t * 2.2f + p * 1.5f) * 0.008f, 0f);

            if (rivalHeadTransforms[p] != null && rivalLeftHands[p] != null && rivalRightHands[p] != null)
            {
                switch (currentRivalMoods[p])
                {
                    case AvatarMood.HappyCheer:
                        rivalHeadTransforms[p].localRotation = Quaternion.Euler(Mathf.Sin(t * 12f) * 10f, 0f, 0f);
                        rivalLeftHands[p].localPosition = new Vector3(-0.16f, 0.12f + Mathf.Sin(t * 14f) * 0.045f, 0.10f);
                        rivalRightHands[p].localPosition = new Vector3(0.16f, 0.12f + Mathf.Cos(t * 14f) * 0.045f, 0.10f);
                        break;

                    case AvatarMood.AngrySlam:
                        rivalHeadTransforms[p].localRotation = Quaternion.Euler(8f, Mathf.Sin(t * 22f) * 18f, 0f);
                        float slamY = 0.01f + Mathf.Abs(Mathf.Sin(t * 16f)) * 0.06f;
                        rivalLeftHands[p].localPosition = new Vector3(-0.15f, slamY, 0.14f);
                        rivalRightHands[p].localPosition = new Vector3(0.15f, slamY, 0.14f);
                        break;

                    default:
                        rivalHeadTransforms[p].localRotation = Quaternion.Euler(Mathf.Sin(t * 1.5f + p) * 4f, Mathf.Cos(t * 1.1f + p) * 8f, 0f);
                        rivalLeftHands[p].localPosition = new Vector3(-0.16f, 0.02f + Mathf.Sin(t * 2.5f + p) * 0.01f, 0.10f);
                        rivalRightHands[p].localPosition = new Vector3(0.16f, 0.02f + Mathf.Cos(t * 2.5f + p) * 0.01f, 0.10f);
                        break;
                }
            }
        }
    }

    public void TryExecuteHostileTakeover()
    {
        BoardTile currentTile = board[playerPos[0]];
        BoardTile targetTile = null;

        if (currentTile.owner > 0 && currentTile.owner < activePlayerCount && (currentTile.type == TileType.Property || currentTile.type == TileType.Railroad || currentTile.type == TileType.Utility))
        {
            targetTile = currentTile;
        }
        else
        {
            for (int i = 0; i < 40; i++)
            {
                if (board[i].owner > 0 && board[i].owner < activePlayerCount && board[i].colorGroup >= 0 && PlayerOwnsAnyInGroup(0, board[i].colorGroup))
                {
                    targetTile = board[i];
                    break;
                }
            }
            if (targetTile == null)
            {
                for (int i = 0; i < 40; i++)
                {
                    if (board[i].owner > 0 && board[i].owner < activePlayerCount)
                    {
                        targetTile = board[i];
                        break;
                    }
                }
            }
        }

        if (targetTile == null)
        {
            RefreshDashboard("NO RIVAL PROPERTY FOR TAKEOVER", "No rivals own properties for takeover yet! Roll and play on.");
            return;
        }

        int rivalOwner = targetTile.owner;
        string rivalName = GetPlayerName(rivalOwner);

        int buyoutPrice = (targetTile.price * 2) + (targetTile.buildingLevel * targetTile.buildCost);
        if (playerCash[0] < buyoutPrice)
        {
            RefreshDashboard($"HOSTILE TAKEOVER NEEDS ${buyoutPrice}", $"You need ${buyoutPrice} to force {rivalName} to sell {targetTile.name}!");
            SetRivalReaction(rivalOwner, AvatarMood.HappyCheer, $"Haha! You can't afford to buy out {targetTile.name}!");
            return;
        }

        playerCash[0] -= buyoutPrice;
        playerCash[rivalOwner] += buyoutPrice;
        targetTile.owner = 0;

        PlaySFX(cashRegisterSound, procCashClip, 1.3f);
        PlaySFX(buildUpgradeSound, procBuildClip, 1.15f);
        StartCoroutine(SpawnFlyingCoinsStream(playerTokens[0].position, playerTokens[rivalOwner].position, 10));

        ApplyTileOwnershipVisuals(targetTile, 0);
        RebuildMonopolySynergyBeams();
        SetRivalReaction(rivalOwner, AvatarMood.AngrySlam, $"NO! You seized {targetTile.name} in a Hostile Takeover!");
        ShowSpecialEventCard("HOSTILE TAKEOVER!", $"You paid ${buyoutPrice} to seize\n<b>{targetTile.name}</b>\nfrom {rivalName}!", new Color(0.95f, 0.74f, 0.15f));
        RefreshDashboard($"HOSTILE TAKEOVER: SEIZED {targetTile.name}!", $"Paid ${buyoutPrice} buyout to {rivalName}! Property is now YOURS!");

        if (OwnsFullColorGroup(0, targetTile.colorGroup))
        {
            SpawnCelebrationFireworks(targetTile.tileTransform != null ? targetTile.tileTransform.position : transform.position);
        }
    }

    void ExecuteTradeDeal(BoardTile targetTile)
    {
        if (targetTile == null || targetTile.owner <= 0 || targetTile.owner >= activePlayerCount) return;
        int aiIdx = targetTile.owner;
        string aiName = GetPlayerName(aiIdx);

        // Count properties in this color group and AI's holdings
        int totalInGroup = 0;
        int aiInGroup = 0;
        if (targetTile.colorGroup >= 0)
        {
            for (int i = 0; i < 40; i++)
            {
                if (board[i].colorGroup == targetTile.colorGroup)
                {
                    totalInGroup++;
                    if (board[i].owner == aiIdx) aiInGroup++;
                }
            }
        }

        // If the AI already holds 2/3 of that color group, Mogul AI refuses
        if (totalInGroup > 0 && aiInGroup >= 2 && ((float)aiInGroup / totalInGroup) >= 0.65f)
        {
            SetRivalReaction(aiIdx, AvatarMood.AngrySlam, "No way! I'm building a monopoly here—use Hostile Takeover if you dare!");
            RefreshDashboard("TRADE OFFER REFUSED!", $"{aiName} refuses: holds {aiInGroup}/{totalInGroup} properties and wants a Monopoly!");
            PlaySFX(diceRollSound, procDiceClip, 0.75f);
            return;
        }

        // Check if player owns any solitary property (not part of a completed Monopoly) to offer in a Deed-for-Deed + Cash Merger
        BoardTile playerSolitary = null;
        for (int i = 0; i < 40; i++)
        {
            BoardTile candidate = board[i];
            if (candidate != null && candidate.owner == 0 && (candidate.type == TileType.Property || candidate.type == TileType.Railroad || candidate.type == TileType.Utility))
            {
                if (candidate.colorGroup < 0 || !OwnsFullColorGroup(0, candidate.colorGroup))
                {
                    playerSolitary = candidate;
                    break;
                }
            }
        }

        bool aiLowOnCash = playerCash[aiIdx] < 400;
        bool aiOwnsOnlyThisInGroup = (aiInGroup <= 1);
        bool canOfferBuyout = aiLowOnCash || aiOwnsOnlyThisInGroup;

        if (playerSolitary != null)
        {
            // Deed-for-Deed + Cash Merger
            int cashSweetener = Mathf.Max(0, (targetTile.price - playerSolitary.price) / 2) + 50;
            if (playerCash[0] < cashSweetener)
            {
                RefreshDashboard("TRADE CANCELLED", $"You need ${cashSweetener} cash sweetener to complete the merger for {targetTile.name}.");
                return;
            }

            if (tactileInteractions != null)
            {
                tactileInteractions.SpawnExecutiveTradeContract(
                    $"MERGER: {targetTile.name.ToUpper()}",
                    $"Trade: {playerSolitary.name} + ${cashSweetener} Cash\nAcquire: {targetTile.name} from {aiName}\n\nStamp wax seal to sign, or rip apart to reject!",
                    () =>
                    {
                        if (playerCash[0] < cashSweetener) return;
                        playerCash[0] -= cashSweetener;
                        playerCash[aiIdx] += cashSweetener;

                        targetTile.owner = 0;
                        ApplyTileOwnershipVisuals(targetTile, 0);

                        playerSolitary.owner = aiIdx;
                        ApplyTileOwnershipVisuals(playerSolitary, aiIdx);

                        SetRivalReaction(aiIdx, AvatarMood.HappyCheer, "Pleasure doing business with you!");
                        RefreshDashboard($"DEAL ACCEPTED: SWAPPED {playerSolitary.name} FOR {targetTile.name}!", $"Merger sealed with ${cashSweetener} cash sweetener!");

                        StartCoroutine(SpawnFlyingCoinsStream(playerTokens[0].position, playerTokens[aiIdx].position, 8));
                        PlaySFX(cashRegisterSound, procCashClip, 1.1f);
                        RebuildMonopolySynergyBeams();
                        UpdateLiveHUDStats();
                        SaveGameState();
                    },
                    () =>
                    {
                        RefreshDashboard("MERGER REJECTED", $"{aiName} retracts the trade proposal.");
                    }
                );
                return;
            }
            else
            {
                playerCash[0] -= cashSweetener;
                playerCash[aiIdx] += cashSweetener;

                targetTile.owner = 0;
                ApplyTileOwnershipVisuals(targetTile, 0);

                playerSolitary.owner = aiIdx;
                ApplyTileOwnershipVisuals(playerSolitary, aiIdx);

                SetRivalReaction(aiIdx, AvatarMood.HappyCheer, "Pleasure doing business with you!");
                RefreshDashboard($"DEAL ACCEPTED: SWAPPED {playerSolitary.name} FOR {targetTile.name}!", $"Merger sealed with ${cashSweetener} cash sweetener!");

                StartCoroutine(SpawnFlyingCoinsStream(playerTokens[0].position, playerTokens[aiIdx].position, 8));
                PlaySFX(cashRegisterSound, procCashClip, 1.1f);
            }
        }
        else if (canOfferBuyout)
        {
            // 1.35x Negotiated Buyout
            int buyoutPrice = (int)(targetTile.price * 1.35f);
            if (playerCash[0] < buyoutPrice)
            {
                RefreshDashboard("TRADE CANCELLED", $"You need ${buyoutPrice} for a 1.35x Negotiated Buyout of {targetTile.name}.");
                return;
            }

            if (tactileInteractions != null)
            {
                tactileInteractions.SpawnExecutiveTradeContract(
                    $"BUYOUT: {targetTile.name.ToUpper()}",
                    $"Buyout Valuation: ${buyoutPrice} (1.35x Base)\nAcquire: {targetTile.name} from {aiName}\n\nStamp wax seal to sign, or rip apart to reject!",
                    () =>
                    {
                        if (playerCash[0] < buyoutPrice) return;
                        playerCash[0] -= buyoutPrice;
                        playerCash[aiIdx] += buyoutPrice;

                        targetTile.owner = 0;
                        ApplyTileOwnershipVisuals(targetTile, 0);

                        SetRivalReaction(aiIdx, AvatarMood.HappyCheer, "Pleasure doing business with you!");
                        RefreshDashboard($"DEAL ACCEPTED: 1.35x BUYOUT OF {targetTile.name}!", $"Acquired for ${buyoutPrice} (saving 32% vs 2x Hostile Takeover)!");

                        StartCoroutine(SpawnFlyingCoinsStream(playerTokens[0].position, playerTokens[aiIdx].position, 8));
                        PlaySFX(cashRegisterSound, procCashClip, 1.1f);
                        RebuildMonopolySynergyBeams();
                        UpdateLiveHUDStats();
                        SaveGameState();
                    },
                    () =>
                    {
                        RefreshDashboard("BUYOUT REJECTED", $"{aiName} keeps {targetTile.name}.");
                    }
                );
                return;
            }
            else
            {
                playerCash[0] -= buyoutPrice;
                playerCash[aiIdx] += buyoutPrice;

                targetTile.owner = 0;
                ApplyTileOwnershipVisuals(targetTile, 0);

                SetRivalReaction(aiIdx, AvatarMood.HappyCheer, "Pleasure doing business with you!");
                RefreshDashboard($"DEAL ACCEPTED: 1.35x BUYOUT OF {targetTile.name}!", $"Acquired for ${buyoutPrice} (saving 32% vs 2x Hostile Takeover)!");

                StartCoroutine(SpawnFlyingCoinsStream(playerTokens[0].position, playerTokens[aiIdx].position, 8));
                PlaySFX(cashRegisterSound, procCashClip, 1.1f);
            }
        }
        else
        {
            SetRivalReaction(aiIdx, AvatarMood.Thinking, "I have plenty of cash and I like this property. Give me a solitary deed or use Hostile Takeover!");
            RefreshDashboard("TRADE STALEMATE", $"{aiName} demands a deed swap, or use [TAKEOVER (T)] for 2x cash!");
            return;
        }

        RebuildMonopolySynergyBeams();
        UpdateLiveHUDStats();
        SaveGameState();

        // Check if player completed a Full Color Group Monopoly
        if (OwnsFullColorGroup(0, targetTile.colorGroup))
        {
            SpawnCelebrationFireworks(targetTile.tileTransform != null ? targetTile.tileTransform.position : transform.position);
            RefreshDashboard($"MONOPOLY COMPLETE ON {targetTile.name.ToUpper()}!", "Full color set assembled! Rents doubled on unimproved properties!");
        }

        if (selectedTileIndex == targetTile.index)
        {
            ShowDeedCardForTile(targetTile);
        }
    }

    GameObject Create3DGavel(Transform parent)
    {
        GameObject gavelRoot = new GameObject("3D_Auction_Gavel");
        gavelRoot.transform.SetParent(parent, false);
        gavelRoot.transform.localPosition = new Vector3(0f, 0.28f, -0.02f);
        gavelRoot.transform.localScale = Vector3.one * 0.75f;

        if (auctionGavelPrefab != null)
        {
            GameObject gavelModel = Instantiate(auctionGavelPrefab, gavelRoot.transform, false);
            StripRoguePhysicsFromImportedPrefab(gavelModel);
            NormalizePrefabSize(gavelModel, 0.12f);
            return gavelRoot;
        }

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

    IEnumerator AnimateGavelStrike(GameObject gavel = null)
    {
        if (gavel == null)
        {
            GameObject found = GameObject.Find("3D_Auction_Gavel");
            if (found != null) gavel = found;
        }
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
        int highestBidder = -1; // -1 = opening/no bidder yet, 0 = Player, 1..3 = AI

        int[] aiMaxBids = new int[4];
        float[] nextAiBidTimes = new float[4];
        bool[] aiDroppedOut = new bool[4];

        for (int a = 1; a < activePlayerCount; a++)
        {
            if (isBankrupt[a])
            {
                aiDroppedOut[a] = true;
                continue;
            }

            float multiplier = 1.1f;
            if (aiDifficulty == AIDifficulty.Billionaire) multiplier = 1.35f;
            else if (aiDifficulty == AIDifficulty.Casual) multiplier = 0.90f;

            // Distinct AI Personalities
            if (a == 1) multiplier += 0.12f; // Mogul Alpha: Aggressive Shark bids high
            else if (a == 2 && (tile.type == TileType.Railroad || tile.type == TileType.Utility || tile.colorGroup == 1 || tile.colorGroup == 3))
                multiplier += 0.25f; // Baroness Beta: Tech & Transit Tycoon loves Railroads/Utilities/Cyan/Orange
            else if (a == 3)
                multiplier -= 0.10f; // Titan Gamma: Conservative Banker

            int maxBid = (int)(tile.price * multiplier) + Random.Range(-15, 25);
            aiMaxBids[a] = Mathf.Min(maxBid, playerCash[a]);
            nextAiBidTimes[a] = Time.time + 0.8f + (a * 0.35f) + Random.Range(0f, 0.4f);
            aiDroppedOut[a] = false;
        }

        float timer = 6.0f;
        bool playerDroppedOut = false;
        auctionPlayerBidFlag = false;
        auctionPlayerDropFlag = false;

        PlaySFX(cashRegisterSound, procCashClip, 1.3f);
        SetRivalReaction(1, AvatarMood.Thinking, $"An auction for {tile.name}! Opening bid: ${currentBid}. Let's bid!");

        while (timer > 0f)
        {
            timer -= Time.deltaTime;

            if (!playerDroppedOut)
            {
                if (auctionPlayerDropFlag)
                {
                    auctionPlayerDropFlag = false;
                    playerDroppedOut = true;
                    RefreshDashboard("AUCTION: YOU DROPPED OUT!", "You conceded the auction to Rival Moguls.");
                    PlaySFX(tokenMoveSound, procStepClip, 0.8f);
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
                        colocationAndPolish?.BroadcastAuctionBid(0, currentBid);
                        SetRivalReaction(1, AvatarMood.Neutral, $"You raised to ${currentBid}?! Let me calculate...");
                    }
                    else
                    {
                        RefreshDashboard("INSUFFICIENT FUNDS TO BID", $"Need ${nextPlayerBid} to place bid.");
                    }
                }
            }

            for (int a = 1; a < activePlayerCount; a++)
            {
                if (!aiDroppedOut[a] && Time.time >= nextAiBidTimes[a] && highestBidder != a)
                {
                    int nextAiBid = (highestBidder == -1) ? currentBid : currentBid + 20;
                    if (nextAiBid <= aiMaxBids[a] && playerCash[a] >= nextAiBid)
                    {
                        currentBid = nextAiBid;
                        highestBidder = a;
                        nextAiBidTimes[a] = Time.time + 1.2f + Random.Range(0f, 0.4f);
                        timer = Mathf.Max(timer, 2.5f);
                        PlaySFX(diceRollSound, procDiceClip, 1.2f);
                        StartCoroutine(AnimateGavelStrike(gavel));
                        colocationAndPolish?.BroadcastAuctionBid(a, currentBid);
                        string taunt = (Random.value > 0.5f) ? $"{GetPlayerName(a)} bids ${currentBid}!" : $"Outbid! It's mine for ${currentBid}!";
                        SetRivalReaction(a, AvatarMood.HappyCheer, taunt);
                    }
                    else
                    {
                        aiDroppedOut[a] = true;
                        SetRivalReaction(a, AvatarMood.AngrySlam, $"Too steep for {GetPlayerName(a)}! Dropping out at ${currentBid}.");
                    }
                }
            }

            string bidderName = (highestBidder == 0) ? "YOU" : (highestBidder > 0 ? GetPlayerName(highestBidder).ToUpper() : "NONE");
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
            SetRivalReaction(1, AvatarMood.AngrySlam, $"You sniped {tile.name} for ${currentBid}... well played.");
        }
        else if (highestBidder > 0)
        {
            playerCash[highestBidder] -= currentBid;
            tile.owner = highestBidder;
            if (tile.type == TileType.Property && tile.buildingLevel == 0) tile.buildingLevel = 1;
            ApplyTileOwnershipVisuals(tile, highestBidder);
            RebuildMonopolySynergyBeams();
            PlaySFX(cashRegisterSound, procCashClip, 1.25f);
            StartCoroutine(SpawnFlyingCoinsStream(playerTokens[highestBidder].position, tile.tileTransform.position, 6));
            RefreshDashboard($"AUCTION WON BY {GetPlayerName(highestBidder).ToUpper()} FOR ${currentBid}!", $"{GetPlayerName(highestBidder)} claimed {tile.name} at auction!");
            SetRivalReaction(highestBidder, AvatarMood.HappyCheer, $"A brilliant asset acquisition for ${currentBid}!");
        }
        else
        {
            RefreshDashboard("AUCTION PASSED WITH NO BIDS", $"No bids placed for {tile.name}. Returned to bank.");
        }

        if (highestBidder >= 0 && OwnsFullColorGroup(highestBidder, tile.colorGroup))
        {
            SpawnCelebrationFireworks(tile.tileTransform != null ? tile.tileTransform.position : transform.position);
        }

        SaveGameState();
        HideDeedCard();
        yield return new WaitForSeconds(1.8f);
        StartCoroutine(EndPlayerTurn(false));
    }

    bool CheckAllRivalsBankrupt()
    {
        for (int a = 1; a < activePlayerCount; a++)
        {
            if (!isBankrupt[a]) return false;
        }
        return true;
    }
}
