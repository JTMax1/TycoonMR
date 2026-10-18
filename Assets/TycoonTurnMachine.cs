using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public partial class TycoonBoardManager
{
    // =========================================================================
    // TYCOON TURN MACHINE: DICE, TOKEN MOVEMENT, TILE LANDINGS, UPGRADES & JAIL
    // =========================================================================

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

            Vector3 launchVel = (physDie1.GetComponent<Rigidbody>() != null) ? physDie1.GetComponent<Rigidbody>().linearVelocity : (Vector3.up * 1.5f + Vector3.forward * 0.5f);
            colocationAndPolish?.BroadcastDiceThrow(launchVel, -1, -1);

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

        int d1 = physDie1 != null ? physDie1.ReadTopFaceFromOrientation() : UnityEngine.Random.Range(1, 7);
        int d2 = physDie2 != null ? physDie2.ReadTopFaceFromOrientation() : UnityEngine.Random.Range(1, 7);

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
                ShatterJailCage(0);
                RefreshDashboard($"ROLLED DOUBLES [{d1}+{d2}]!", "Laser Cage shattered! Released from Jail!");
                yield return new WaitForSeconds(0.8f);
            }
            else
            {
                playerCash[0] -= 50;
                freeParkingJackpot += 50;
                inJail[0] = false;
                ShatterJailCage(0);
                RefreshDashboard($"PAID $50 BAIL [{d1}+{d2}]", "Laser Cage shattered! Bail added to Free Parking Jackpot!");
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
            Vector3 laneOffset = Vector3.zero;
            if (playerIdx == 0) laneOffset = new Vector3(-0.015f, 0.018f, -0.012f);
            else if (playerIdx == 1) laneOffset = new Vector3(0.015f, 0.018f, -0.012f);
            else if (playerIdx == 2) laneOffset = new Vector3(-0.015f, 0.018f, 0.012f);
            else if (playerIdx == 3) laneOffset = new Vector3(0.015f, 0.018f, 0.012f);
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
                RefreshDashboard($"PASSED GO! +${goSalary} SALARY", $"{(playerIdx == 0 ? "You" : GetPlayerName(playerIdx))} collected ${goSalary}.");
            }
        }
    }

    IEnumerator EvaluateTileLanding(int playerIdx, bool rolledDoubles)
    {
        BoardTile tile = board[playerPos[playerIdx]];
        string who = (playerIdx == 0) ? "YOU" : GetPlayerName(playerIdx).ToUpper();

        if (tile != null && tile.tileTransform != null)
        {
            Renderer tr = tile.tileTransform.GetComponent<Renderer>();
            if (tr != null) FlashRendererDynamicColor(tr, Color.cyan, 0.45f);
        }

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
                            SetRivalReaction(1, AvatarMood.Thinking, $"Thinking about buying {tile.name}?");
                            RefreshDashboard(
                                $"LANDED ON {tile.name} (${tile.price})",
                                "Stamp Seal or Right Pinch to BUY | Rip Contract or Left Pinch to PASS"
                            );

                            if (tactileInteractions != null)
                            {
                                tactileInteractions.SpawnExecutiveTradeContract(
                                    $"{tile.name} — ${tile.price}",
                                    $"Base Rent: ${tile.rentByLevel[0]}\nFull Set Rent: ${tile.rentByLevel[0] * 2}\nTouch STAMP SEAL to Buy, or RIP to Pass.",
                                    () =>
                                    {
                                        PurchaseCurrentTile(0);
                                        tactileInteractions.DismissTradeContract();
                                    },
                                    () =>
                                    {
                                        PassTurnDecision();
                                        tactileInteractions.DismissTradeContract();
                                    }
                                );
                            }
                            yield break;
                        }
                        else
                        {
                            RefreshDashboard($"{tile.name} (${tile.price})", "Insufficient funds to purchase deed.");
                        }
                    }
                    else
                    {
                        int buyReserve = (aiDifficulty == AIDifficulty.Casual) ? 200 : (aiDifficulty == AIDifficulty.Billionaire ? 30 : 100);
                        if (playerCash[playerIdx] >= tile.price + buyReserve)
                        {
                            PurchaseCurrentTile(playerIdx);
                            yield return new WaitForSeconds(0.9f);
                        }
                    }
                }
                else if (tile.owner != playerIdx)
                {
                    int rent = CalculateAuthenticRent(tile, lastDiceTotal);

                    // Phase 21: Defensive Sabotage / Laser-Grid Heist Minigame when rent >= 100
                    if (playerIdx == 0 && rent >= 100 && minigameOrchestrator != null)
                    {
                        RefreshDashboard("SECURITY LASER GRID DETECTED!", "Hack 3 green nodes without tripping the laser to slash rent!");
                        bool heistDone = false;
                        minigameOrchestrator.RunLaserHeistSabotageMinigame(rent, (discounted) =>
                        {
                            rent = discounted;
                            heistDone = true;
                        });
                        while (!heistDone) yield return null;
                    }

                    // Phase 21: Physical Rent Hand-Offs (Grab & Throw Cash Stack From Briefcase)
                    if (playerIdx == 0 && rent > 0 && executiveBriefcaseAndTrophy != null)
                    {
                        bool handoffDone = false;
                        executiveBriefcaseAndTrophy.ShowGrabbableRentCashBundle(playerIdx, tile.owner, rent, () =>
                        {
                            handoffDone = true;
                        });
                        while (!handoffDone) yield return null;
                    }

                    playerCash[playerIdx] -= rent;
                    playerCash[tile.owner] += rent;
                    PlaySFX(cashRegisterSound, procCashClip, 0.9f);
                    StartCoroutine(SpawnFlyingCoinsStream(playerTokens[playerIdx].position, playerTokens[tile.owner].position, 8));

                    if (playerIdx == 0)
                    {
                        SetRivalReaction(tile.owner, AvatarMood.HappyCheer, $"Pay up! Thanks for the ${rent} rent on {tile.name}!");
                    }
                    else
                    {
                        if (tile.owner == 0)
                        {
                            SetRivalReaction(playerIdx, AvatarMood.AngrySlam, $"Ugh! Paid you ${rent} rent on {tile.name}!");
                            OnRivalPaidRentToPlayer?.Invoke(playerIdx, rent);
                        }
                        else
                            SetRivalReaction(playerIdx, AvatarMood.Thinking, $"Paid ${rent} rent to {GetPlayerName(tile.owner)} on {tile.name}.");
                    }

                    RefreshDashboard(
                        $"{who} PAID ${rent} RENT ON {tile.name}!",
                        $"Paid to {(tile.owner == 0 ? "YOU" : GetPlayerName(tile.owner).ToUpper())}. (Touch [TAKEOVER] on your turn to buy out AI properties!)"
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
                int taxDue = tile.price;
                if (playerIdx == 0 && minigameOrchestrator != null)
                {
                    RefreshDashboard($"ASSESSING {tile.name}...", "Turn the 3 steam valves to claim tax relief deductions!");
                    bool taxDone = false;
                    int discountWon = 0;
                    minigameOrchestrator.RunPipeRepairMinigame((discount) =>
                    {
                        discountWon = discount;
                        taxDone = true;
                    });
                    while (!taxDone) yield return null;
                    taxDue = Mathf.Max(10, tile.price - discountWon);
                    playerCash[0] -= taxDue;
                    freeParkingJackpot += taxDue;
                    PlaySFX(cashRegisterSound, procCashClip, 0.95f);
                    RefreshDashboard($"ASSESSED {tile.name}: PAID ${taxDue}!", $"Deduction saved you ${discountWon}! ${taxDue} added to Free Parking Jackpot.");
                }
                else
                {
                    playerCash[playerIdx] -= taxDue;
                    freeParkingJackpot += taxDue;
                    PlaySFX(cashRegisterSound, procCashClip, 0.9f);
                    if (jackpotGoldPile != null)
                        StartCoroutine(SpawnFlyingCoinsStream(playerTokens[playerIdx].position, jackpotGoldPile.position, 6));
                    RefreshDashboard($"{who} PAID ${taxDue} {tile.name.ToUpper()}", $"Tax proceeds added to Free Parking Jackpot (${freeParkingJackpot}).");
                }
                yield return new WaitForSeconds(1.1f);
                break;

            case TileType.FreeParking:
                if (freeParkingJackpot > 0)
                {
                    int pot = freeParkingJackpot;
                    playerCash[playerIdx] += pot;
                    freeParkingJackpot = 0;
                    PlaySFX(cashRegisterSound, procCashClip, 1.3f);
                    if (jackpotGoldPile != null)
                        StartCoroutine(SpawnFlyingCoinsStream(jackpotGoldPile.position, playerTokens[playerIdx].position, 12));
                    SpawnCelebrationFireworks(tile.tileTransform.position);
                    RefreshDashboard($"JACKPOT! {who} WON ${pot}!", "Collected the entire Free Parking Jackpot pool!");
                    yield return new WaitForSeconds(1.4f);
                }
                else
                {
                    RefreshDashboard($"{who} RESTING ON FREE PARKING", "Jackpot pool is currently empty.");
                    yield return new WaitForSeconds(0.8f);
                }
                break;

            case TileType.Chance:
            case TileType.Chest:
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
        string who = (playerIdx == 0) ? "YOU" : GetPlayerName(playerIdx).ToUpper();
        string deckName = isChance ? "CHANCE DIRECTIVE" : "COMMUNITY CHEST";
        Color deckColor = isChance ? new Color(0.95f, 0.50f, 0.12f) : new Color(0.18f, 0.62f, 0.95f);

        int pick = UnityEngine.Random.Range(0, 5);
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
                if (playerIdx == 0 && minigameOrchestrator != null)
                {
                    ShowSpecialEventCard(deckName, "BANK ERROR IN YOUR FAVOR!\nVault Cash Swat Minigame!\nSwat floating dollar bills!", deckColor);
                    RefreshDashboard("BANK ERROR IN YOUR FAVOR!", "Swat fluttering dollar bills in front of you!");
                    bool minigameDone = false;
                    int wonCash = 100;
                    minigameOrchestrator.RunVaultCashSwatMinigame((totalBonus) =>
                    {
                        wonCash = totalBonus;
                        minigameDone = true;
                    });
                    while (!minigameDone) yield return null;
                    playerCash[0] += wonCash;
                    PlaySFX(cashRegisterSound, procCashClip, 1.15f);
                    if (jackpotGoldPile != null)
                        StartCoroutine(SpawnFlyingCoinsStream(jackpotGoldPile.position, playerTokens[0].position, Mathf.Min(12, wonCash / 25)));
                    RefreshDashboard($"VAULT CASH COLLECTED: +${wonCash}!", $"Bank Error payout credited to your account.");
                }
                else
                {
                    playerCash[playerIdx] += 150;
                    PlaySFX(cashRegisterSound, procCashClip, 1.1f);
                    if (jackpotGoldPile != null)
                        StartCoroutine(SpawnFlyingCoinsStream(jackpotGoldPile.position, playerTokens[playerIdx].position, 7));
                    ShowSpecialEventCard(deckName, "BUILDING & LOAN MATURES!\nCollect $150 from the Bank.", deckColor);
                    RefreshDashboard($"{who} COLLECTED $150 DIVIDEND!", "Building & Loan matured (+$150).");
                }
                break;

            case 2:
                for (int other = 0; other < activePlayerCount; other++)
                {
                    if (other != playerIdx && !isBankrupt[other])
                    {
                        playerCash[playerIdx] -= 50;
                        playerCash[other] += 50;
                        if (playerTokens[playerIdx] != null && playerTokens[other] != null)
                            StartCoroutine(SpawnFlyingCoinsStream(playerTokens[playerIdx].position, playerTokens[other].position, 5));
                    }
                }
                PlaySFX(cashRegisterSound, procCashClip, 0.9f);
                ShowSpecialEventCard(deckName, "ELECTED CHAIRMAN OF THE BOARD!\nPay each rival mogul $50.", deckColor);
                RefreshDashboard($"{who}: CHAIRMAN OF THE BOARD", "Paid $50 dividend to rival moguls.");
                break;

            case 3:
                int repairBill = 100;
                if (playerIdx == 0 && minigameOrchestrator != null)
                {
                    ShowSpecialEventCard(deckName, "CITY STREET REPAIRS!\nBurst Steam Pipe detected!\nShut off the 3 red pressure valves!", deckColor);
                    RefreshDashboard("CITY STREET REPAIRS!", "Physically turn the 3 pressure valves to reduce your repair bill!");
                    bool minigameDone = false;
                    int discountWon = 0;
                    minigameOrchestrator.RunPipeRepairMinigame((discount) =>
                    {
                        discountWon = discount;
                        minigameDone = true;
                    });
                    while (!minigameDone) yield return null;
                    repairBill = Mathf.Max(10, 100 - discountWon);
                    playerCash[0] -= repairBill;
                    freeParkingJackpot += repairBill;
                    if (jackpotGoldPile != null)
                        StartCoroutine(SpawnFlyingCoinsStream(playerTokens[0].position, jackpotGoldPile.position, Mathf.Max(2, repairBill / 20)));
                    RefreshDashboard($"REPAIRS COMPLETE: PAID ${repairBill}!", $"Quick handiwork saved you ${discountWon}! Paid ${repairBill} into Jackpot.");
                }
                else
                {
                    playerCash[playerIdx] -= repairBill;
                    freeParkingJackpot += repairBill;
                    if (jackpotGoldPile != null)
                        StartCoroutine(SpawnFlyingCoinsStream(playerTokens[playerIdx].position, jackpotGoldPile.position, 6));
                    ShowSpecialEventCard(deckName, "CITY STREET REPAIRS!\nPay $100 into the Free Parking Jackpot.", deckColor);
                    RefreshDashboard($"{who} PAID $100 STREET REPAIRS", $"Added $100 to Free Parking Jackpot (${freeParkingJackpot}).");
                }
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

    public void PurchaseCurrentTile(int playerIdx)
    {
        BoardTile tile = board[playerPos[playerIdx]];
        if (playerCash[playerIdx] < tile.price) return;

        playerCash[playerIdx] -= tile.price;
        tile.owner = playerIdx;
        PlaySFX(cashRegisterSound, procCashClip, 1.0f);
        HideDeedCard();
        if (tactileInteractions != null) tactileInteractions.DismissTradeContract();

        StartCoroutine(SpawnFlyingCoinsStream(playerTokens[playerIdx].position, tile.tileTransform.position, 6));

        if (tile.type == TileType.Property && tile.buildingLevel == 0)
        {
            tile.buildingLevel = 1;
        }

        ApplyTileOwnershipVisuals(tile, playerIdx);
        RebuildMonopolySynergyBeams();

        if (OwnsFullColorGroup(playerIdx, tile.colorGroup))
        {
            SpawnCelebrationFireworks(tile.tileTransform != null ? tile.tileTransform.position : transform.position);
            if (playerIdx == 0)
                SetRivalReaction(AvatarMood.AngrySlam, $"Uh oh! You completed a Monopoly on {tile.name}!");
            else
                SetRivalReaction(AvatarMood.HappyCheer, $"I completed a Monopoly on {tile.name}!");
        }
        else
        {
            if (playerIdx == 0)
                SetRivalReaction(AvatarMood.Neutral, $"Nice pickup on {tile.name}...");
            else
                SetRivalReaction(AvatarMood.HappyCheer, $"I just acquired {tile.name}!");
        }

        RefreshDashboard(
            $"{GetPlayerName(playerIdx).ToUpper()} BOUGHT {tile.name} (${tile.price})!",
            "Tile claimed & Level 1 structure erected! Touch [BUILD (U)] on your turn to upgrade."
        );

        SaveGameState();

        if (playerIdx == 0)
        {
            StartCoroutine(EndPlayerTurn(false));
        }
    }

    public void PassTurnDecision()
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
        if (tactileInteractions != null) tactileInteractions.DismissTradeContract();
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

        if (playerIdx == 0 && tactileInteractions != null)
        {
            tactileInteractions.SpawnHologramBlueprint(targetTile, (chosenStyle) =>
            {
                ExecuteBuildingUpgrade(playerIdx, targetTile, actualBuildCost, chosenStyle);
            });
        }
        else
        {
            ExecuteBuildingUpgrade(playerIdx, targetTile, actualBuildCost, 0);
        }
    }

    void ExecuteBuildingUpgrade(int playerIdx, BoardTile targetTile, int actualBuildCost, int chosenStyle = 0)
    {
        if (targetTile == null || playerCash[playerIdx] < actualBuildCost || targetTile.buildingLevel >= 5) return;

        playerCash[playerIdx] -= actualBuildCost;
        targetTile.buildingLevel++;
        buildCycleCursor = (targetTile.index + 1) % 40;

        PlaySFX(buildUpgradeSound, procBuildClip, 0.9f + (targetTile.buildingLevel * 0.08f));
        StartCoroutine(SpawnFlyingCoinsStream(playerTokens[playerIdx].position, targetTile.tileTransform.position, 5));
        ConstructChronological3DBuilding(targetTile, chosenStyle);

        string tierLabel = (targetTile.buildingLevel == 5)
            ? "LVL 5 SKYSCRAPER HOTEL"
            : $"HOUSE LEVEL {targetTile.buildingLevel} OF 4";

        if (targetTile.buildingLevel == 5)
        {
            SpawnCelebrationFireworks(targetTile.tileTransform != null ? targetTile.tileTransform.position : transform.position);
            if (playerIdx == 0)
            {
                SetRivalReaction(AvatarMood.AngrySlam, $"A Skyscraper Hotel on {targetTile.name}?! Dangerous!");
            }
        }

        string styleLabel = (chosenStyle == 1) ? " [CLASSIC EXECUTIVE]" : " [MODERN GLASS]";
        RefreshDashboard(
            $"BUILT {tierLabel} ON {targetTile.name}!{styleLabel}",
            $"Paid ${actualBuildCost} | New Rent: ${CalculateAuthenticRent(targetTile, 7)}"
        );

        if (selectedTileIndex == targetTile.index)
        {
            ShowDeedCardForTile(targetTile);
        }
        UpdateLiveHUDStats();
        SaveGameState();
    }

    IEnumerator EndPlayerTurn(bool playerRolledDoubles)
    {
        if (playerCash[0] < 0)
        {
            AttemptEmergencyBankruptcyLiquidation(0);
            if (playerCash[0] < 0)
            {
                SetRivalReaction(1, AvatarMood.HappyCheer, "Look who's bankrupt! The boardroom belongs to us!");
                RefreshDashboard("BANKRUPTCY! AI RIVALS WIN!", "Touch [NEW GAME] or press N to launch a fresh match!");
                currentState = TurnState.WaitingForRoll;
                yield break;
            }
        }

        for (int a = 1; a < activePlayerCount; a++)
        {
            if (!isBankrupt[a] && playerCash[a] < 0)
            {
                AttemptEmergencyBankruptcyLiquidation(a);
                if (playerCash[a] < 0)
                {
                    EliminatePlayerForBankruptcy(a);
                }
            }
        }

        if (CheckAllRivalsBankrupt())
        {
            SetRivalReaction(1, AvatarMood.AngrySlam, "All rivals are bankrupt! You conquer the entire empire!");
            RefreshDashboard("VICTORY! ALL RIVALS BANKRUPT!", "You conquered the 40-Tile Metropolis Empire! Monopoly Master!");
            SpawnCelebrationFireworks(transform.position + Vector3.up * 0.15f);
            OnVictoryTrophyTriggered?.Invoke();
            currentState = TurnState.WaitingForRoll;
            yield break;
        }

        if (playerRolledDoubles && !inJail[0])
        {
            currentState = TurnState.WaitingForRoll;
            CurrentTurnPlayer = 0;
            OnTurnStarted?.Invoke(0);
            RefreshDashboard("DOUBLES BONUS ROLL!", "Pinch to Distance-Pull & Throw the 3D Dice again!");
            yield break;
        }

        currentState = TurnState.AITurn;

        int buildThreshold = (aiDifficulty == AIDifficulty.Billionaire) ? 300 : ((aiDifficulty == AIDifficulty.Casual) ? 750 : 500);

        for (int aiIdx = 1; aiIdx < activePlayerCount; aiIdx++)
        {
            if (isBankrupt[aiIdx]) continue;
            CurrentTurnPlayer = aiIdx;
            OnTurnStarted?.Invoke(aiIdx);

            SetRivalReaction(aiIdx, AvatarMood.Thinking, $"{GetPlayerName(aiIdx)}: My turn! Analyzing the board...");
            yield return new WaitForSeconds(0.65f);

            // AI Upgrade phase
            if (playerCash[aiIdx] > buildThreshold)
            {
                TryBuildChronologically(aiIdx);
                yield return new WaitForSeconds(0.45f);
            }

            // AI Roll dice
            int aiD1 = UnityEngine.Random.Range(1, 7);
            int aiD2 = UnityEngine.Random.Range(1, 7);
            lastDiceTotal = aiD1 + aiD2;
            if (die1Transform != null) die1Transform.localRotation = GetFlatDieRotationForValue(aiD1);
            if (die2Transform != null) die2Transform.localRotation = GetFlatDieRotationForValue(aiD2);
            PlaySFX(diceRollSound, procDiceClip, 0.95f);

            if (inJail[aiIdx])
            {
                inJail[aiIdx] = false;
                playerCash[aiIdx] -= 50;
                freeParkingJackpot += 50;
                ShatterJailCage(aiIdx);
            }

            RefreshDashboard($"{GetPlayerName(aiIdx).ToUpper()} ROLLED [{aiD1}+{aiD2}={lastDiceTotal}]", $"{GetPlayerName(aiIdx)} is advancing {lastDiceTotal} steps clockwise...");
            yield return StartCoroutine(MoveTokenSteps(aiIdx, lastDiceTotal));
            yield return StartCoroutine(EvaluateTileLanding(aiIdx, false));

            if (playerCash[aiIdx] < 0)
            {
                AttemptEmergencyBankruptcyLiquidation(aiIdx);
                if (playerCash[aiIdx] < 0)
                {
                    EliminatePlayerForBankruptcy(aiIdx);
                    if (CheckAllRivalsBankrupt())
                    {
                        SetRivalReaction(1, AvatarMood.AngrySlam, "All rivals are bankrupt! You conquer the entire empire!");
                        RefreshDashboard("VICTORY! ALL RIVALS BANKRUPT!", "You conquered the 40-Tile Metropolis Empire! Monopoly Master!");
                        SpawnCelebrationFireworks(transform.position + Vector3.up * 0.15f);
                        OnVictoryTrophyTriggered?.Invoke();
                        currentState = TurnState.WaitingForRoll;
                        yield break;
                    }
                }
            }

            yield return new WaitForSeconds(0.5f);
        }

        totalTurnsCompleted++;
        if (totalTurnsCompleted % 5 == 0)
        {
            ShiftMarketCycle();
        }

        SaveGameState();

        currentState = TurnState.WaitingForRoll;
        CurrentTurnPlayer = 0;
        OnTurnStarted?.Invoke(0);
        if (inJail[0])
        {
            RefreshDashboard("IN JAIL!", "IN JAIL! POKE [BUY DEED] TO PAY $50 BAIL NOW, OR THROW DICE FOR DOUBLES!");
        }
        else
        {
            RefreshDashboard("YOUR TURN — DISTANCE-GRAB & THROW DICE", "Pinch with your hand to Pull & Throw the 3D Dice | Touch [TAKEOVER] to buy out AI!");
        }
    }

    void ShiftMarketCycle()
    {
        List<MarketCycle> options = new List<MarketCycle>();
        foreach (MarketCycle mc in System.Enum.GetValues(typeof(MarketCycle)))
        {
            if (mc != currentMarketCycle) options.Add(mc);
        }
        currentMarketCycle = options[UnityEngine.Random.Range(0, options.Count)];

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
                cycleName = "BULL BOOM";
                desc = "All property rents are BOOSTED by +50%!";
                aiLine = "The market is booming! Rents are soaring—time to extract maximum capital!";
                break;
            case MarketCycle.BearRecession:
                cycleName = "BEAR RECESSION";
                desc = "Property rents drop 30%, but building costs are DISCOUNTED by 30%!";
                aiLine = "A recession hit the metropolis. Time to buy and build at a discount!";
                break;
            case MarketCycle.TechIPOSurge:
                cycleName = "TECH IPO SURGE";
                desc = "Railroad & Utility payouts multiplied by 2.5x!";
                aiLine = "Tech IPO frenzy! Transit and utilities are yielding massive windfalls!";
                break;
            default:
                cycleName = "STANDARD MARKET";
                desc = "Economy has normalized to standard boardroom rules.";
                aiLine = "The economic market has stabilized. Back to standard negotiations.";
                break;
        }

        PlaySFX(cashRegisterSound, procCashClip, 1.25f);
        SetRivalReaction(AvatarMood.Thinking, aiLine);
        RefreshDashboard($"ECONOMIC SHIFT: {cycleName}", desc);
        UpdateLiveHUDStats();
    }

    void SendToJail(int playerIdx)
    {
        playerPos[playerIdx] = 10;
        inJail[playerIdx] = true;
        PositionTokenOnTile(playerIdx, 10);

        if (procSirenClip != null)
        {
            PlaySFX(procSirenClip, procSirenClip, 1.0f);
        }

        GameObject cage = SpawnJailLaserCage(playerIdx);
        if (cage != null)
        {
            StartCoroutine(DropJailCageCoroutine(cage));
        }

        if (playerIdx == 0)
        {
            ShowJailLockpickButton();
        }
    }

    GameObject SpawnJailLaserCage(int playerIdx)
    {
        if (playerIdx < 0 || playerIdx >= 4) return null;
        if (jailCageHolders[playerIdx] != null)
        {
            Destroy(jailCageHolders[playerIdx]);
            jailCageHolders[playerIdx] = null;
        }

        if (board[10] == null || board[10].tileTransform == null) return null;

        Vector3 laneOffset;
        switch (playerIdx)
        {
            case 0: laneOffset = new Vector3(-0.015f, 0.018f, -0.012f); break;
            case 1: laneOffset = new Vector3( 0.015f, 0.018f, -0.012f); break;
            case 2: laneOffset = new Vector3(-0.015f, 0.018f,  0.012f); break;
            case 3: laneOffset = new Vector3( 0.015f, 0.018f,  0.012f); break;
            default: laneOffset = Vector3.up * 0.018f; break;
        }

        Vector3 cageCenter = board[10].tileTransform.localPosition + laneOffset;

        GameObject cageHolder = new GameObject($"Laser_Jail_Cage_P{playerIdx}");
        cageHolder.transform.SetParent(transform, false);
        cageHolder.transform.localPosition = cageCenter;
        cageHolder.transform.localRotation = Quaternion.identity;

        Color laserColor = new Color(1.0f, 0.24f, 0.06f);
        Material laserMat = CreateColoredMaterial(laserColor, 0.95f, 0.1f);
        if (laserMat.HasProperty("_EmissionColor"))
        {
            laserMat.EnableKeyword("_EMISSION");
            laserMat.SetColor("_EmissionColor", laserColor * 2.5f);
        }

        float dx = 0.013f;
        float dz = 0.015f;
        Vector3[] barCorners = new Vector3[]
        {
            new Vector3(-dx, 0.028f, -dz),
            new Vector3( dx, 0.028f, -dz),
            new Vector3(-dx, 0.028f,  dz),
            new Vector3( dx, 0.028f,  dz)
        };

        for (int i = 0; i < 4; i++)
        {
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bar.name = $"Laser_Bar_{i}";
            bar.transform.SetParent(cageHolder.transform, false);
            bar.transform.localPosition = barCorners[i];
            bar.transform.localScale = new Vector3(0.004f, 0.028f, 0.004f);
            Destroy(bar.GetComponent<Collider>());
            bar.GetComponent<Renderer>().material = laserMat;
        }

        GameObject topCap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        topCap.name = "Laser_Energy_Cap";
        topCap.transform.SetParent(cageHolder.transform, false);
        topCap.transform.localPosition = new Vector3(0f, 0.056f, 0f);
        topCap.transform.localScale = new Vector3(0.034f, 0.002f, 0.038f);
        Destroy(topCap.GetComponent<Collider>());
        topCap.GetComponent<Renderer>().material = laserMat;

        jailCageHolders[playerIdx] = cageHolder;
        return cageHolder;
    }

    IEnumerator DropJailCageCoroutine(GameObject cage)
    {
        if (cage == null) yield break;
        Vector3 finalPos = cage.transform.localPosition;
        Vector3 startPos = finalPos + new Vector3(0f, 0.12f, 0f);
        cage.transform.localPosition = startPos;

        float duration = 0.25f;
        float elapsed = 0f;
        while (elapsed < duration && cage != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easeT = t * t;
            cage.transform.localPosition = Vector3.Lerp(startPos, finalPos, easeT);
            yield return null;
        }

        if (cage != null)
        {
            cage.transform.localPosition = finalPos;
            PlaySFX(tokenMoveSound, procStepClip, 0.65f);
            PlayHaptic(0.4f, 0.15f);
        }
    }

    public void ShatterJailCage(int playerIdx)
    {
        if (playerIdx < 0 || playerIdx >= 4) return;
        if (playerIdx == 0) HideJailLockpickButton();
        if (jailCageHolders[playerIdx] == null) return;
        StartCoroutine(ShatterJailCageCoroutine(playerIdx));
    }

    IEnumerator ShatterJailCageCoroutine(int playerIdx)
    {
        GameObject cage = jailCageHolders[playerIdx];
        jailCageHolders[playerIdx] = null;
        if (cage == null) yield break;

        PlaySFX(procGlassShatterClip, procDiceClip, 1.15f);
        if (playerIdx == 0) PlayHaptic(0.6f, 0.22f);

        Transform[] pieces = cage.GetComponentsInChildren<Transform>();
        List<Transform> shatterBars = new List<Transform>();
        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i] != null && pieces[i] != cage.transform)
            {
                shatterBars.Add(pieces[i]);
            }
        }

        Vector3[] burstDirs = new Vector3[shatterBars.Count];
        for (int i = 0; i < shatterBars.Count; i++)
        {
            Vector3 local = shatterBars[i].localPosition;
            Vector3 horiz = new Vector3(local.x, 0f, local.z).normalized;
            if (horiz.sqrMagnitude < 0.001f) horiz = UnityEngine.Random.insideUnitSphere;
            burstDirs[i] = (horiz + Vector3.up * 1.6f).normalized;
        }

        float elapsed = 0f;
        float duration = 0.35f;
        while (elapsed < duration && cage != null)
        {
            elapsed += Time.deltaTime;
            float dt = Time.deltaTime;
            for (int i = 0; i < shatterBars.Count; i++)
            {
                if (shatterBars[i] != null)
                {
                    shatterBars[i].localPosition += burstDirs[i] * (0.16f * dt);
                    shatterBars[i].Rotate(new Vector3(400f, 300f, 500f) * dt);
                    shatterBars[i].localScale = Vector3.Max(Vector3.zero, shatterBars[i].localScale - Vector3.one * (0.014f * dt));
                }
            }
            yield return null;
        }

        if (cage != null) Destroy(cage);
    }

    void AttemptEmergencyBankruptcyLiquidation(int p)
    {
        if (playerCash[p] >= 0) return;

        bool liquidatedSomething = true;
        int totalRecovered = 0;

        // 1. Sell back buildings across all owned properties, 1 level at a time
        while (playerCash[p] < 0 && liquidatedSomething)
        {
            liquidatedSomething = false;
            for (int i = 0; i < 40; i++)
            {
                BoardTile tile = board[i];
                if (tile != null && tile.owner == p && tile.buildingLevel > 0)
                {
                    int refund = tile.buildCost / 2;
                    playerCash[p] += refund;
                    totalRecovered += refund;
                    tile.buildingLevel--;
                    if (tile.spawnedBuilding != null) Destroy(tile.spawnedBuilding);
                    if (tile.buildingLevel > 0) ConstructChronological3DBuilding(tile);
                    liquidatedSomething = true;
                    if (playerCash[p] >= 0) break;
                }
            }
        }

        // 2. Mortgage unmortgaged properties (+50% price)
        if (playerCash[p] < 0)
        {
            liquidatedSomething = true;
            while (playerCash[p] < 0 && liquidatedSomething)
            {
                liquidatedSomething = false;
                for (int i = 0; i < 40; i++)
                {
                    BoardTile tile = board[i];
                    if (tile != null && tile.owner == p && !tile.isMortgaged && tile.buildingLevel <= 0)
                    {
                        int mortgageVal = tile.price / 2;
                        playerCash[p] += mortgageVal;
                        totalRecovered += mortgageVal;
                        tile.isMortgaged = true;
                        if (tile.ownershipBadge != null)
                        {
                            Renderer r = tile.ownershipBadge.GetComponent<Renderer>();
                            if (r != null) r.material = CreateColoredMaterial(new Color(0.48f, 0.12f, 0.16f), 0.95f);
                        }
                        liquidatedSomething = true;
                        if (playerCash[p] >= 0) break;
                    }
                }
            }
        }

        if (totalRecovered > 0)
        {
            string debtorName = (p == 0) ? "YOU" : "MOGUL AI";
            PlaySFX(cashRegisterSound, procCashClip, 0.9f);
            RefreshDashboard($"EMERGENCY LIQUIDATION: {debtorName}", $"{debtorName} liquidated assets for +${totalRecovered}! Balance: ${playerCash[p]}");
            UpdateLiveHUDStats();
            SaveGameState();
        }
    }

    void EliminatePlayerForBankruptcy(int p)
    {
        isBankrupt[p] = true;
        if (playerTokens[p] != null) playerTokens[p].gameObject.SetActive(false);

        for (int i = 0; i < 40; i++)
        {
            if (board[i] != null && board[i].owner == p)
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

        RebuildMonopolySynergyBeams();
        SetRivalReaction(p, AvatarMood.AngrySlam, "My empire has collapsed! I am completely bankrupt...", 4.0f);
        RefreshDashboard($"{GetPlayerName(p).ToUpper()} IS BANKRUPT!", $"{GetPlayerName(p)} has been eliminated from the boardroom match!");
        UpdateLiveHUDStats();
    }
}
