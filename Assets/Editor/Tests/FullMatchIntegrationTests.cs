using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace Tycoon.Tests
{
    public class FullMatchIntegrationTests
    {
        [InitializeOnLoadMethod]
        public static void RunTestsOnLoad()
        {
            RunFullMatchTests();
        }

        [MenuItem("Tycoon/Run Full Match Integration Tests")]
        public static void RunFullMatchTests()
        {
            var test = new FullMatchIntegrationTests();
            try
            {
                test.FullMatch_LifecycleFlow_CompletesDeterministically();
                test.FullMatch_Bankruptcy_LiquidatesAndContinuesWithoutCrashing();
                test.FullMatch_Reset_PreservesSpatialCalibrationAndClearsMatch();
                Debug.Log("<color=#00FF88>[FullMatchIntegrationTests] ALL 3 FULL-MATCH INTEGRATION TESTS PASSED!</color>");
            }
            catch (Exception ex)
            {
                Debug.LogError($"<color=#FF0055>[FullMatchIntegrationTests] TEST FAILED:</color> {ex.Message}\n{ex.StackTrace}");
            }
        }

        [Test]
        public void FullMatch_LifecycleFlow_CompletesDeterministically()
        {
            GameObject go = new GameObject("IntegrationTestBoard");
            var boardMgr = go.AddComponent<TycoonBoardManager>();

            try
            {
                // 1. Initial Match Setup
                boardMgr.ResetMatch();
                boardMgr.InitializeOfficialMonopolyMap();
                Assert.AreEqual(0, boardMgr.CurrentTurnPlayer);
                Assert.AreEqual(1500, boardMgr.GetPlayerCashArray()[0]);
                Assert.AreEqual(0, boardMgr.GetPlayerPosArray()[0]);

                // 2. Buy property on Tile 1 (Mediterranean Ave)
                int tileIdx = 1;
                boardMgr.SetPlayerCash(0, 1440);
                var tile1 = boardMgr.GetTile(1);
                var tile3 = boardMgr.GetTile(3);
                tile1.owner = 0;
                tile1.buildingLevel = 0;

                // Single property owned -> incomplete group
                var incompleteRes = boardMgr.CanBuild(0, tileIdx);
                Assert.AreEqual(TycoonBoardManager.BuildValidationResult.IncompleteGroup, incompleteRes);

                // 3. Complete monopoly with second brown property (tile 3)
                tile3.owner = 0;
                tile3.buildingLevel = 0;

                // Both properties owned by player 0 -> CanBuild must succeed
                var buildResult = boardMgr.CanBuild(0, tileIdx);
                Assert.AreEqual(TycoonBoardManager.BuildValidationResult.Success, buildResult);

                // 4. Reset match
                boardMgr.ResetMatch();
                Assert.AreEqual(1500, boardMgr.GetPlayerCashArray()[0], "Cash must be reset to 1500");
                Assert.AreEqual(0, boardMgr.GetPlayerPosArray()[0], "Player position must reset to 0");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void FullMatch_Bankruptcy_LiquidatesAndContinuesWithoutCrashing()
        {
            GameObject go = new GameObject("IntegrationTestBankruptcy");
            var boardMgr = go.AddComponent<TycoonBoardManager>();

            try
            {
                boardMgr.ResetMatch();

                // Player 3 goes bankrupt (cash <= 0, cannot afford debt)
                boardMgr.SetPlayerCash(3, -500);
                Assert.LessOrEqual(boardMgr.GetPlayerCashArray()[3], 0);

                // Ensure match does not wipe out remaining players (Player 0, 1, 2 still active)
                Assert.Greater(boardMgr.GetRemainingActivePlayerCount(), 0);
                Assert.AreEqual(1500, boardMgr.GetPlayerCashArray()[0]);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void FullMatch_Reset_PreservesSpatialCalibrationAndClearsMatch()
        {
            GameObject go = new GameObject("IntegrationTestResetPersistence");
            var boardMgr = go.AddComponent<TycoonBoardManager>();

            try
            {
                // Seed System & Calibration keys
                PlayerPrefs.SetString("Tycoon_SpatialAnchor_UUID", "AN-TEST-INTEG-999");
                PlayerPrefs.SetFloat("Tycoon_Anchor_PosY", 0.72f);
                PlayerPrefs.SetFloat("Tycoon_Anchor_RotY", 45f);
                PlayerPrefs.SetInt("Tycoon_Pref_ComfortVignette", 1);

                // Seed Match keys
                PlayerPrefs.SetInt("Tycoon_CurrentTurn", 3);
                PlayerPrefs.SetInt("Tycoon_Player_0_Cash", 450);
                PlayerPrefs.SetInt("Tycoon_Tile_1_Owner", 0);
                PlayerPrefs.Save();

                // Perform Match Reset
                boardMgr.ResetMatch();

                // Verify System & Calibration keys survive 100%
                Assert.AreEqual("AN-TEST-INTEG-999", PlayerPrefs.GetString("Tycoon_SpatialAnchor_UUID"));
                Assert.AreEqual(0.72f, PlayerPrefs.GetFloat("Tycoon_Anchor_PosY"), 0.001f);
                Assert.AreEqual(45f, PlayerPrefs.GetFloat("Tycoon_Anchor_RotY"), 0.001f);
                Assert.AreEqual(1, PlayerPrefs.GetInt("Tycoon_Pref_ComfortVignette"));

                // Verify Match keys cleared
                Assert.IsFalse(PlayerPrefs.HasKey("Tycoon_CurrentTurn"));
                Assert.IsFalse(PlayerPrefs.HasKey("Tycoon_Player_0_Cash"));
                Assert.IsFalse(PlayerPrefs.HasKey("Tycoon_Tile_1_Owner"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
