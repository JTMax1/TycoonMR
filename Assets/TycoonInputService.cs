using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using Oculus.Interaction;

public partial class TycoonBoardManager
{
    // =========================================================================
    // TYCOON INPUT SERVICE: HAND TRACKING, TOUCH CONTROLLERS, GRAB & POKING
    // =========================================================================

    void CacheMetaInteractionSDKHands()
    {
#pragma warning disable CS0618
        scenePokeInteractors = UnityEngine.Object.FindObjectsByType<PokeInteractor>(FindObjectsSortMode.None);
        sceneOVRHands = UnityEngine.Object.FindObjectsByType<OVRHand>(FindObjectsSortMode.None);
#pragma warning restore CS0618
    }

    int DetermineHierarchyHandedness(Transform t)
    {
        Transform curr = t;
        while (curr != null)
        {
            string n = curr.name.ToLower();
            if (n.Contains("left") || n.Contains("lhand") || n.Contains("hand_l") || n.Contains("_l_") || n.EndsWith("_l")) return -1;
            if (n.Contains("right") || n.Contains("rhand") || n.Contains("hand_r") || n.Contains("_r_") || n.EndsWith("_r")) return 1;
            curr = curr.parent;
        }
        return 0;
    }

    // --- READS BOTH META INTERACTIONS RIG (`OVRHand` / `PokeInteractor`), HARDWARE `OVRPlugin`, AND NEW INPUT SYSTEM ---
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
#pragma warning disable CS0618
            scenePokeInteractors = UnityEngine.Object.FindObjectsByType<PokeInteractor>(FindObjectsSortMode.None);
#pragma warning restore CS0618
        }

        if (scenePokeInteractors != null)
        {
            for (int i = 0; i < scenePokeInteractors.Length; i++)
            {
                PokeInteractor pi = scenePokeInteractors[i];
                if (pi == null || !pi.gameObject.activeInHierarchy || !pi.enabled) continue;

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

        // 2. Read Pinch Gestures & Hand Positions from Meta's OVRHand
        if (sceneOVRHands == null || sceneOVRHands.Length == 0)
        {
#pragma warning disable CS0618
            sceneOVRHands = UnityEngine.Object.FindObjectsByType<OVRHand>(FindObjectsSortMode.None);
#pragma warning restore CS0618
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

        Transform refSpace = Camera.main != null && Camera.main.transform.parent != null ? Camera.main.transform.parent : transform;

        // 3. Fallback to OVRPlugin if either hand wasn't tracked by ISDK / OVRHand
        if (OVRManager.isHmdPresent)
        {
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
                        isRightPinching |= (rightState.Pinches & OVRPlugin.HandFingerPinch.Index) != 0;
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
                        isLeftPinching |= (leftState.Pinches & OVRPlugin.HandFingerPinch.Index) != 0;
                    }
                }
                catch { }
            }
        }

        // 4. Hybrid Quest Touch Controller Support
        // First: Unity's New Input System (safe on all platforms with zero legacy Input calls!)
        bool rControllerPinchNew = false;
        bool lControllerPinchNew = false;

        var rXR = XRController.rightHand;
        if (rXR != null)
        {
            try
            {
                if (rXR.devicePosition != null)
                {
                    Vector3 pos = rXR.devicePosition.ReadValue();
                    if (pos.sqrMagnitude > 0.0001f)
                    {
                        if (!isRightHandTracked)
                        {
                            isRightHandTracked = true;
                            rightHandWorldPos = refSpace.TransformPoint(pos);
                        }
                    }
                }
                var rTrig = rXR.GetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("triggerPressed") 
                         ?? rXR.GetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("triggerButton");
                if (rTrig != null && rTrig.isPressed) rControllerPinchNew = true;

                var rGrip = rXR.GetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("gripPressed")
                         ?? rXR.GetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("gripButton");
                if (rGrip != null && rGrip.isPressed) rControllerPinchNew = true;

                var rBtn = rXR.GetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("primaryButton");
                if (rBtn != null && rBtn.isPressed) rControllerPinchNew = true;
            }
            catch { }
        }

        var lXR = XRController.leftHand;
        if (lXR != null)
        {
            try
            {
                if (lXR.devicePosition != null)
                {
                    Vector3 pos = lXR.devicePosition.ReadValue();
                    if (pos.sqrMagnitude > 0.0001f)
                    {
                        if (!isLeftHandTracked)
                        {
                            isLeftHandTracked = true;
                            leftHandWorldPos = refSpace.TransformPoint(pos);
                        }
                    }
                }
                var lTrig = lXR.GetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("triggerPressed") 
                         ?? lXR.GetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("triggerButton");
                if (lTrig != null && lTrig.isPressed) lControllerPinchNew = true;

                var lGrip = lXR.GetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("gripPressed")
                         ?? lXR.GetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("gripButton");
                if (lGrip != null && lGrip.isPressed) lControllerPinchNew = true;

                var lBtn = lXR.GetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("primaryButton");
                if (lBtn != null && lBtn.isPressed) lControllerPinchNew = true;
            }
            catch { }
        }

        isRightPinching |= rControllerPinchNew;
        isLeftPinching |= lControllerPinchNew;

        // Second: Only call OVRInput when in VR / HMD is present safely inside try-catch:
        if (OVRManager.isHmdPresent)
        {
            try
            {
                bool rTouchConnected = OVRInput.IsControllerConnected(OVRInput.Controller.RTouch);
                if (rTouchConnected)
                {
                    if (!isRightHandTracked)
                    {
                        isRightHandTracked = true;
                        Vector3 ctrlLocal = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
                        rightHandWorldPos = refSpace.TransformPoint(ctrlLocal);
                    }

                    bool rControllerPinch = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch) ||
                                            OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch) ||
                                            OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.RTouch);
                    isRightPinching |= rControllerPinch;
                }

                bool lTouchConnected = OVRInput.IsControllerConnected(OVRInput.Controller.LTouch);
                if (lTouchConnected)
                {
                    if (!isLeftHandTracked)
                    {
                        isLeftHandTracked = true;
                        Vector3 ctrlLocal = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
                        leftHandWorldPos = refSpace.TransformPoint(ctrlLocal);
                    }

                    bool lControllerPinch = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch) ||
                                            OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch) ||
                                            OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.LTouch);
                    isLeftPinching |= lControllerPinch;
                }
            }
            catch { }
        }

        // Two-Hand Pinch Move & Scale
        if (!isInPenthouseVR && !isPlayerHoldingDice && isRightHandTracked && isLeftHandTracked && isRightPinching && isLeftPinching)
        {
            float verticalDiff = Mathf.Abs(leftHandWorldPos.y - rightHandWorldPos.y);
            if (verticalDiff < 0.03f)
            {
                deskSnapPinchTimer += Time.deltaTime;
                if (deskSnapPinchTimer >= 1.5f)
                {
                    deskSnapPinchTimer = 0f;
                    float deskHeight = (leftHandWorldPos.y + rightHandWorldPos.y) * 0.5f;
                    SnapBoardToDeskHeight(deskHeight);
                }
            }
            else
            {
                deskSnapPinchTimer = 0f;
            }

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
            deskSnapPinchTimer = 0f;
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

    void HandlePhysicalDiceGrabAndThrow(bool rPinchDown, bool lPinchDown, bool rPinchHeld, bool lPinchHeld)
    {
        if (currentState != TurnState.WaitingForRoll || physDie1 == null || physDie2 == null) return;

        if (!isPlayerHoldingDice)
        {
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
                    isGrabbingDiceWithRight = grabRight;
                    activeHandGrabTransform.position = candidatePos;

                    physDie1.BeginHandGrab(activeHandGrabTransform, new Vector3(-0.028f, 0f, 0f));
                    physDie2.BeginHandGrab(activeHandGrabTransform, new Vector3(0.028f, 0f, 0f));

                    PlaySFX(diceRollSound, procDiceClip, 1.15f);
                    RefreshDashboard("PULLED 3D DICE TO " + (isGrabbingDiceWithRight ? "RIGHT" : "LEFT") + " HAND!", "Shake your hand and release your pinch to THROW the dice onto Central Park!");
                }
            }
        }
        else
        {
            Vector3 lockedHandPos = isGrabbingDiceWithRight ? rightHandWorldPos : leftHandWorldPos;
            if (activeHandGrabTransform != null && lockedHandPos.y > -50f)
            {
                activeHandGrabTransform.position = lockedHandPos;
            }

            bool grabbingHandPinching = isGrabbingDiceWithRight ? rPinchHeld : lPinchHeld;
            bool grabbingHandTracked = isGrabbingDiceWithRight ? isRightHandTracked : isLeftHandTracked;

            if (!grabbingHandPinching || (!grabbingHandTracked && !rPinchHeld && !lPinchHeld))
            {
                isPlayerHoldingDice = false;
                StartCoroutine(ExecutePhysicsDiceRollTurn(false));
            }
        }
    }

    void HandlePhysicalDeedCrystalGrab(bool rPinchDown, bool lPinchDown, bool rPinchHeld, bool lPinchHeld)
    {
        if (currentState != TurnState.BuyOrPassDecision || grabbableDeedCrystal == null || !grabbableDeedCrystal.activeSelf) return;

        Grabbable crystalGrabbable = grabbableDeedCrystal.GetComponent<Grabbable>();

        if (!isHoldingDeedCrystal)
        {
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

    int CheckPhysicalButtonTouch()
    {
        if (Time.time < lastButtonTouchTime) return -1;
        if (!isRightHandTracked && !isLeftHandTracked) return -1;

        for (int i = 0; i < buttonTransforms.Length; i++)
        {
            if (buttonTransforms[i] == null) continue;
            Collider btnCol = buttonTransforms[i].GetComponent<Collider>();
            if (btnCol == null) continue;

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

    bool CheckOfferDealButtonPoke()
    {
        if (offerDealButtonTransform == null || !offerDealButtonTransform.gameObject.activeInHierarchy) return false;
        if (Time.time < lastButtonTouchTime) return false;
        if (!isRightHandTracked && !isLeftHandTracked) return false;

        Collider col = offerDealButtonTransform.GetComponent<Collider>();
        if (col == null) return false;

        bool rightPoked = isRightHandTracked && rightHandWorldPos.y > -50f && Vector3.Distance(col.ClosestPoint(rightHandWorldPos), rightHandWorldPos) < 0.018f;
        bool leftPoked = isLeftHandTracked && leftHandWorldPos.y > -50f && Vector3.Distance(col.ClosestPoint(leftHandWorldPos), leftHandWorldPos) < 0.018f;

        if (rightPoked || leftPoked)
        {
            lastButtonTouchTime = Time.time + 0.50f;
            StartCoroutine(AnimateButtonPress(offerDealButtonTransform));
            return true;
        }
        return false;
    }

    int CheckStartMenuButtonPoke()
    {
        if (Time.time < lastButtonTouchTime) return -1;
        if (!isRightHandTracked && !isLeftHandTracked) return -1;

        for (int i = 0; i < startMenuButtonTransforms.Length; i++)
        {
            if (startMenuButtonTransforms[i] == null) continue;
            Collider btnCol = startMenuButtonTransforms[i].GetComponent<Collider>();
            if (btnCol == null) continue;

            bool rightPoked = isRightHandTracked && Vector3.Distance(btnCol.ClosestPoint(rightHandWorldPos), rightHandWorldPos) < 0.022f;
            bool leftPoked = isLeftHandTracked && Vector3.Distance(btnCol.ClosestPoint(leftHandWorldPos), leftHandWorldPos) < 0.022f;

            if (rightPoked || leftPoked)
            {
                lastButtonTouchTime = Time.time + 0.40f;
                StartCoroutine(AnimateButtonPress(startMenuButtonTransforms[i]));
                return i;
            }
        }
        return -1;
    }

    void SnapBoardToDeskHeight(float deskHeight)
    {
        lockedBoardPosition.y = deskHeight;
        transform.position = lockedBoardPosition;
        OnBoardPoseUpdated?.Invoke(lockedBoardPosition, lockedBoardRotation);
        SaveGameState();
        PlaySFX(cashRegisterSound, procBuildClip, 1.2f);
        PlayHaptic(0.5f, 0.2f);
        RefreshDashboard("DESK HEIGHT SNAPPED & LOCKED!", $"Board surface aligned directly to physical table height: {deskHeight:F2}m.");
    }

    public void RecenterBoardInFrontOfPlayer()
    {
        if (isInPenthouseVR) return;
        lastButtonTouchTime = Time.time + 0.8f;

        Transform eye = Camera.main != null ? Camera.main.transform : null;
        if (eye == null)
        {
            OVRCameraRig rig = UnityEngine.Object.FindAnyObjectByType<OVRCameraRig>();
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
        OnBoardPoseUpdated?.Invoke(lockedBoardPosition, lockedBoardRotation);
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

    public void TogglePenthouseScaleShift()
    {
        TriggerComfortVignetteTransition(ExecutePenthouseScaleShift);
    }

    private void ExecutePenthouseScaleShift()
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

    private int vrVantageIndex = 0;

    void ToggleRooftopVantagePoint()
    {
        TriggerComfortVignetteTransition(ExecuteRooftopVantagePoint);
    }

    private void ExecuteRooftopVantagePoint()
    {
        Transform eye = Camera.main != null ? Camera.main.transform : null;
        if (!isInPenthouseVR || eye == null) return;
        vrVantageIndex = (vrVantageIndex + 1) % 3;

        if (vrVantageIndex == 0) // 1. Central Park Street Plaza
        {
            isStandingOnRooftop = false;
            transform.position = new Vector3(eye.position.x, eye.position.y - 1.65f, eye.position.z);
            PlaySFX(tokenMoveSound, procStepClip, 0.9f);
            RefreshDashboard("CENTRAL PARK STREET PLAZA", "Standing in Central Park looking at the metropolis skyline. Left Pinch: Boardwalk Tour.");
            UpdateVRButtonLabel(vrRooftopTeleportButton, "BOARDWALK TOUR\n(Left Pinch)");
        }
        else if (vrVantageIndex == 1) // 2. Boardwalk Sidewalk Tour
        {
            isStandingOnRooftop = false;
            Vector3 tile39Pos = (board != null && board.Length > 39 && board[39].tileTransform != null) ? board[39].tileTransform.localPosition : new Vector3(0.38f, 0.028f, -0.38f);
            transform.position = eye.position - (transform.rotation * (tile39Pos * 14f)) - new Vector3(0f, 1.65f, 0f);
            PlaySFX(tokenMoveSound, procStepClip, 1.15f);
            RefreshDashboard("BOARDWALK SIDEWALK TOUR", "1:1 street level on the Boardwalk sidewalk looking down the avenue! Left Pinch: Skyscraper Rooftop.");
            UpdateVRButtonLabel(vrRooftopTeleportButton, "ROOFTOP VIEW\n(Left Pinch)");
        }
        else // 3. Penthouse Skyscraper Rooftop
        {
            isStandingOnRooftop = true;
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
            RefreshDashboard($"PENTHOUSE ROOFTOP: {bestTile.name}", "Looking down over your skyline from the highest penthouse! Left Pinch: Central Park Plaza.");
            UpdateVRButtonLabel(vrRooftopTeleportButton, "CENTRAL PARK\n(Left Pinch)");
        }
    }
}
