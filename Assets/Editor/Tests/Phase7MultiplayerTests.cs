using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace Tycoon.Tests
{
    public class Phase7MultiplayerTests
    {
        [InitializeOnLoadMethod]
        public static void RunTestsOnLoad()
        {
            RunPhase7Tests();
        }

        [MenuItem("Tycoon/Run Phase 7 Tests")]
        public static void RunPhase7Tests()
        {
            var test = new Phase7MultiplayerTests();
            try
            {
                test.SeatAssignment_AssignsUniqueSeats_AndReleasesCleanly();
                test.RollDiceIntent_RejectsNonTurnPlayer_AndInvalidPhase();
                test.BuyPropertyIntent_RejectsForgedPrice_InsufficientFunds_AndExistingOwner();
                test.AuctionBidIntent_RejectsLowBids_AndInsufficientFunds();
                test.SpatialColocation_DecoupledFromGameState();
                Debug.Log("<color=#00FF88>[Phase7MultiplayerTests] ALL 5 TESTS PASSED SUCCESFULLY!</color>");
            }
            catch (Exception ex)
            {
                Debug.LogError($"<color=#FF0055>[Phase7MultiplayerTests] TEST FAILED:</color> {ex.Message}\n{ex.StackTrace}");
            }
        }

        [Test]
        public void SeatAssignment_AssignsUniqueSeats_AndReleasesCleanly()
        {
            GameObject go = new GameObject("TestColocationHost");
            var boardMgr = go.AddComponent<TycoonBoardManager>();
            var colocation = go.AddComponent<TycoonColocationAndPolish>();

            try
            {
                // Assign 4 clients
                Assert.IsTrue(colocation.TryAssignSeat(101, out int s0), "Client 101 should get a seat");
                Assert.AreEqual(0, s0);

                Assert.IsTrue(colocation.TryAssignSeat(102, out int s1), "Client 102 should get a seat");
                Assert.AreEqual(1, s1);

                Assert.IsTrue(colocation.TryAssignSeat(103, out int s2), "Client 103 should get a seat");
                Assert.AreEqual(2, s2);

                Assert.IsTrue(colocation.TryAssignSeat(104, out int s3), "Client 104 should get a seat");
                Assert.AreEqual(3, s3);

                // 5th client should be rejected (all 4 seats occupied)
                Assert.IsFalse(colocation.TryAssignSeat(105, out int s4), "5th client should be rejected");
                Assert.AreEqual(-1, s4);

                // Idempotent: Client 102 querying again returns seat 1
                Assert.IsTrue(colocation.TryAssignSeat(102, out int reQuery), "Client 102 re-query should return seat 1");
                Assert.AreEqual(1, reQuery);

                // Release seat 1 (client 102 leaves)
                colocation.ReleaseSeat(102);
                Assert.AreEqual(-1, colocation.GetSeatForClient(102));

                // Now client 105 can take the vacated seat 1
                Assert.IsTrue(colocation.TryAssignSeat(105, out int vacatedSeat), "Vacated seat 1 should be available");
                Assert.AreEqual(1, vacatedSeat);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RollDiceIntent_RejectsNonTurnPlayer_AndInvalidPhase()
        {
            GameObject go = new GameObject("TestColocationHost");
            go.AddComponent<TycoonBoardManager>();
            var colocation = go.AddComponent<TycoonColocationAndPolish>();

            try
            {
                // Current turn is player 0, phase is WaitingForRoll
                var validIntent = new TycoonColocationAndPolish.RequestRollDiceIntent
                {
                    clientId = 100,
                    seatId = 0,
                    sessionGeneration = 1
                };
                var res = colocation.ValidateRollDiceIntent(validIntent, 0, TycoonBoardManager.TurnState.WaitingForRoll);
                Assert.AreEqual(TycoonColocationAndPolish.IntentValidationResult.Approved, res);

                // Player 1 tries to roll during Player 0's turn
                var badTurnIntent = new TycoonColocationAndPolish.RequestRollDiceIntent
                {
                    clientId = 101,
                    seatId = 1,
                    sessionGeneration = 1
                };
                var badTurnRes = colocation.ValidateRollDiceIntent(badTurnIntent, 0, TycoonBoardManager.TurnState.WaitingForRoll);
                Assert.AreEqual(TycoonColocationAndPolish.IntentValidationResult.RejectedNotActiveTurn, badTurnRes);

                // Player 0 tries to roll during BuyOrPassDecision phase
                var badPhaseRes = colocation.ValidateRollDiceIntent(validIntent, 0, TycoonBoardManager.TurnState.BuyOrPassDecision);
                Assert.AreEqual(TycoonColocationAndPolish.IntentValidationResult.RejectedInvalidPhase, badPhaseRes);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void BuyPropertyIntent_RejectsForgedPrice_InsufficientFunds_AndExistingOwner()
        {
            GameObject go = new GameObject("TestColocationHost");
            go.AddComponent<TycoonBoardManager>();
            var colocation = go.AddComponent<TycoonColocationAndPolish>();

            try
            {
                int[] playerCash = new int[] { 1500, 1500, 1500, 1500 };
                int[] tileOwners = new int[40];
                for (int i = 0; i < 40; i++) tileOwners[i] = -1;

                int authoritativeCost = 200;
                int tileIdx = 3;

                // 1. Valid purchase
                var validIntent = new TycoonColocationAndPolish.RequestBuyPropertyIntent
                {
                    clientId = 100,
                    seatId = 0,
                    tileIdx = tileIdx,
                    offeredPrice = authoritativeCost
                };
                var res = colocation.ValidateBuyPropertyIntent(validIntent, 0, TycoonBoardManager.TurnState.BuyOrPassDecision, playerCash, tileOwners, authoritativeCost);
                Assert.AreEqual(TycoonColocationAndPolish.IntentValidationResult.Approved, res);

                // 2. Client tries to forge price (e.g. offeredPrice = 1 instead of 200)
                var forgedIntent = new TycoonColocationAndPolish.RequestBuyPropertyIntent
                {
                    clientId = 100,
                    seatId = 0,
                    tileIdx = tileIdx,
                    offeredPrice = 1
                };
                var forgedRes = colocation.ValidateBuyPropertyIntent(forgedIntent, 0, TycoonBoardManager.TurnState.BuyOrPassDecision, playerCash, tileOwners, authoritativeCost);
                Assert.AreEqual(TycoonColocationAndPolish.IntentValidationResult.RejectedForgedValues, forgedRes);

                // 3. Tile already owned by player 2
                tileOwners[tileIdx] = 2;
                var ownedRes = colocation.ValidateBuyPropertyIntent(validIntent, 0, TycoonBoardManager.TurnState.BuyOrPassDecision, playerCash, tileOwners, authoritativeCost);
                Assert.AreEqual(TycoonColocationAndPolish.IntentValidationResult.RejectedTileAlreadyOwned, ownedRes);

                // 4. Insufficient cash
                tileOwners[tileIdx] = -1;
                playerCash[0] = 50; // less than 200
                var brokeRes = colocation.ValidateBuyPropertyIntent(validIntent, 0, TycoonBoardManager.TurnState.BuyOrPassDecision, playerCash, tileOwners, authoritativeCost);
                Assert.AreEqual(TycoonColocationAndPolish.IntentValidationResult.RejectedInsufficientFunds, brokeRes);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void AuctionBidIntent_RejectsLowBids_AndInsufficientFunds()
        {
            GameObject go = new GameObject("TestColocationHost");
            go.AddComponent<TycoonBoardManager>();
            var colocation = go.AddComponent<TycoonColocationAndPolish>();

            try
            {
                int[] playerCash = new int[] { 1500, 300, 1500, 1500 };
                int currentHighestBid = 200;
                int minIncrement = 20;

                // 1. Valid bid: 250 (>= 200 + 20 and player 1 has 300)
                var validBid = new TycoonColocationAndPolish.RequestAuctionBidIntent
                {
                    clientId = 101,
                    seatId = 1,
                    bidAmount = 250
                };
                var res = colocation.ValidateAuctionBidIntent(validBid, true, currentHighestBid, minIncrement, playerCash);
                Assert.AreEqual(TycoonColocationAndPolish.IntentValidationResult.Approved, res);

                // 2. Bid too low: 210 (< 200 + 20)
                var lowBid = new TycoonColocationAndPolish.RequestAuctionBidIntent
                {
                    clientId = 101,
                    seatId = 1,
                    bidAmount = 210
                };
                var lowRes = colocation.ValidateAuctionBidIntent(lowBid, true, currentHighestBid, minIncrement, playerCash);
                Assert.AreEqual(TycoonColocationAndPolish.IntentValidationResult.RejectedBidTooLow, lowRes);

                // 3. Insufficient funds: bid 500 when cash is only 300
                var highBid = new TycoonColocationAndPolish.RequestAuctionBidIntent
                {
                    clientId = 101,
                    seatId = 1,
                    bidAmount = 500
                };
                var highRes = colocation.ValidateAuctionBidIntent(highBid, true, currentHighestBid, minIncrement, playerCash);
                Assert.AreEqual(TycoonColocationAndPolish.IntentValidationResult.RejectedInsufficientFunds, highRes);

                // 4. Inactive auction
                var inactiveRes = colocation.ValidateAuctionBidIntent(validBid, false, currentHighestBid, minIncrement, playerCash);
                Assert.AreEqual(TycoonColocationAndPolish.IntentValidationResult.RejectedInvalidPhase, inactiveRes);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SpatialColocation_DecoupledFromGameState()
        {
            GameObject go = new GameObject("TestColocationHost");
            go.AddComponent<TycoonBoardManager>();
            var colocation = go.AddComponent<TycoonColocationAndPolish>();

            try
            {
                Assert.AreEqual(TycoonColocationAndPolish.ColocationAlignmentState.Uninitialized, colocation.CurrentAlignmentState);

                colocation.SetAlignmentState(TycoonColocationAndPolish.ColocationAlignmentState.HostCreatingAnchor);
                Assert.AreEqual(TycoonColocationAndPolish.ColocationAlignmentState.HostCreatingAnchor, colocation.CurrentAlignmentState);

                // Transition to fallback without altering any economic state
                colocation.SetAlignmentState(TycoonColocationAndPolish.ColocationAlignmentState.AlignmentFailedManualFallback);
                Assert.AreEqual(TycoonColocationAndPolish.ColocationAlignmentState.AlignmentFailedManualFallback, colocation.CurrentAlignmentState);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
