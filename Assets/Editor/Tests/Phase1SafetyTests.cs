using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class Phase1SafetyTests
{
    private const string PREF_ANCHOR_UUID = "Tycoon_SpatialAnchor_UUID";
    private const string PREF_ANCHOR_POS_X = "Tycoon_Anchor_PosX";
    private const string PREF_ANCHOR_POS_Y = "Tycoon_Anchor_PosY";
    private const string PREF_ANCHOR_POS_Z = "Tycoon_Anchor_PosZ";
    private const string PREF_ANCHOR_ROT_Y = "Tycoon_Anchor_RotY";

    [TearDown]
    public void TearDown()
    {
        // Clean up any test keys
        PlayerPrefs.DeleteKey(PREF_ANCHOR_UUID);
        PlayerPrefs.DeleteKey(PREF_ANCHOR_POS_X);
        PlayerPrefs.DeleteKey(PREF_ANCHOR_POS_Y);
        PlayerPrefs.DeleteKey(PREF_ANCHOR_POS_Z);
        PlayerPrefs.DeleteKey(PREF_ANCHOR_ROT_Y);
        PlayerPrefs.DeleteKey("Tycoon_Comfort_Vignette");
        TycoonBoardManager.ClearMatchSave();
        PlayerPrefs.Save();
    }

    [Test]
    public void Test_ClearMatchSave_RemovesOnlyMatchKeys_PreservesSpatialCalibrationAndPreferences()
    {
        // 1. Seed match keys
        PlayerPrefs.SetInt("Tycoon_HasSave", 1);
        PlayerPrefs.SetInt("Tycoon_ActivePlayers", 4);
        PlayerPrefs.SetInt("Tycoon_AIDifficulty", 2);
        PlayerPrefs.SetInt("Tycoon_StartCashConfig", 2000);
        PlayerPrefs.SetInt("Tycoon_Cash_P0", 1850);
        PlayerPrefs.SetInt("Tycoon_Pos_P0", 7);
        PlayerPrefs.SetInt("Tycoon_Bankrupt_P0", 0);
        PlayerPrefs.SetInt("Tycoon_InJail_P0", 1);
        PlayerPrefs.SetInt("Tycoon_Jackpot", 750);
        PlayerPrefs.SetInt("Tycoon_MarketCycle", 1);
        PlayerPrefs.SetInt("Tycoon_TotalTurns", 15);
        PlayerPrefs.SetFloat("Tycoon_BoardScale", 0.90f);
        PlayerPrefs.SetInt("Tycoon_Tile_1_Owner", 0);
        PlayerPrefs.SetInt("Tycoon_Tile_1_Lvl", 3);
        PlayerPrefs.SetInt("Tycoon_Tile_1_Mortgaged", 0);

        // 2. Seed spatial anchor and calibration keys
        string expectedUuid = "d8f4e2c1-9876-4321-abcd-ef0123456789";
        PlayerPrefs.SetString(PREF_ANCHOR_UUID, expectedUuid);
        PlayerPrefs.SetFloat(PREF_ANCHOR_POS_X, 0.15f);
        PlayerPrefs.SetFloat(PREF_ANCHOR_POS_Y, 0.82f);
        PlayerPrefs.SetFloat(PREF_ANCHOR_POS_Z, -0.40f);
        PlayerPrefs.SetFloat(PREF_ANCHOR_ROT_Y, 90f);
        PlayerPrefs.SetInt("Tycoon_Comfort_Vignette", 1);
        PlayerPrefs.Save();

        // 3. Execute scoped reset
        TycoonBoardManager.ClearMatchSave();

        // 4. Verify match keys are wiped
        Assert.IsFalse(PlayerPrefs.HasKey("Tycoon_HasSave"), "Tycoon_HasSave should be deleted");
        Assert.IsFalse(PlayerPrefs.HasKey("Tycoon_Cash_P0"), "Tycoon_Cash_P0 should be deleted");
        Assert.IsFalse(PlayerPrefs.HasKey("Tycoon_Pos_P0"), "Tycoon_Pos_P0 should be deleted");
        Assert.IsFalse(PlayerPrefs.HasKey("Tycoon_Jackpot"), "Tycoon_Jackpot should be deleted");
        Assert.IsFalse(PlayerPrefs.HasKey("Tycoon_Tile_1_Owner"), "Tycoon_Tile_1_Owner should be deleted");
        Assert.IsFalse(PlayerPrefs.HasKey("Tycoon_Tile_1_Lvl"), "Tycoon_Tile_1_Lvl should be deleted");

        // 5. Verify system calibration and anchor keys SURVIVE
        Assert.IsTrue(PlayerPrefs.HasKey(PREF_ANCHOR_UUID), "Spatial anchor UUID must survive match reset");
        Assert.AreEqual(expectedUuid, PlayerPrefs.GetString(PREF_ANCHOR_UUID));
        Assert.AreEqual(0.15f, PlayerPrefs.GetFloat(PREF_ANCHOR_POS_X), 0.001f);
        Assert.AreEqual(0.82f, PlayerPrefs.GetFloat(PREF_ANCHOR_POS_Y), 0.001f);
        Assert.AreEqual(-0.40f, PlayerPrefs.GetFloat(PREF_ANCHOR_POS_Z), 0.001f);
        Assert.AreEqual(90f, PlayerPrefs.GetFloat(PREF_ANCHOR_ROT_Y), 0.001f);
        Assert.AreEqual(1, PlayerPrefs.GetInt("Tycoon_Comfort_Vignette"));
    }

    [Test]
    public void Test_NetworkStateSync_WhenDisabled_RejectsStateMutation()
    {
        GameObject go = new GameObject("TestBoardManagerGO");
        try
        {
            TycoonBoardManager boardManager = go.AddComponent<TycoonBoardManager>();

            // Confirm network state sync is disabled by default
            Assert.IsFalse(boardManager.enableNetworkStateSync, "enableNetworkStateSync must default to false for safety");

            // Capture initial state
            int initialTurn = boardManager.CurrentTurnPlayer;
            int[] initialCash = boardManager.GetPlayerCashArray();
            int[] initialPos = boardManager.GetPlayerPosArray();

            // Attempt unsolicited network sync injection
            boardManager.SyncStateFromNetwork(
                3, // Rogue turn
                new int[] { 99999, 99999, 99999, 99999 }, // Rogue cash
                new int[] { 25, 25, 25, 25 }, // Rogue positions
                new int[40],
                new int[40],
                "rogue-anchor-uuid"
            );

            // Assert authoritative local state remains completely unchanged
            Assert.AreEqual(initialTurn, boardManager.CurrentTurnPlayer, "CurrentTurnPlayer must not be modified by unsolicited network packet");
            CollectionAssert.AreEqual(initialCash, boardManager.GetPlayerCashArray(), "PlayerCash array must not be corrupted by unauthenticated packet");
            CollectionAssert.AreEqual(initialPos, boardManager.GetPlayerPosArray(), "PlayerPos array must not be altered by network packet when sync is disabled");
        }
        finally
        {
            GameObject.DestroyImmediate(go);
        }
    }

    [Test]
    public void Test_ColocationSync_DefaultsDisabled_AndRejectsUnauthenticatedPackets()
    {
        GameObject go = new GameObject("TestColocationGO");
        try
        {
            TycoonBoardManager boardManager = go.AddComponent<TycoonBoardManager>();
            TycoonColocationAndPolish colocation = go.AddComponent<TycoonColocationAndPolish>();

            // Assert safe baseline defaults
            Assert.IsFalse(colocation.enableLocalWiFiColocationSync, "enableLocalWiFiColocationSync must default to false");
            Assert.IsFalse(colocation.isNetworkStateSyncAuthorized, "isNetworkStateSyncAuthorized must default to false");

            // Attempt to apply an incoming packet via reflection to verify the containment guard
            MethodInfo applyMethod = typeof(TycoonColocationAndPolish).GetMethod("ApplyIncomingNetworkPacket", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(applyMethod, "ApplyIncomingNetworkPacket method should exist");

            string packetJson = "{\"packetType\":\"STATE_SYNC\",\"senderId\":\"remote_rogue_device\",\"currentTurn\":2,\"playerCash\":[9999,9999,9999,9999],\"playerPos\":[15,15,15,15]}";

            int initialTurn = boardManager.CurrentTurnPlayer;
            int[] initialCash = boardManager.GetPlayerCashArray();

            applyMethod.Invoke(colocation, new object[] { packetJson });

            Assert.AreEqual(initialTurn, boardManager.CurrentTurnPlayer);
            CollectionAssert.AreEqual(initialCash, boardManager.GetPlayerCashArray());
        }
        finally
        {
            GameObject.DestroyImmediate(go);
        }
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tycoon/Run Phase 1 Safety Tests")]
    public static void RunAllTestsFromMenu()
    {
        var testInstance = new Phase1SafetyTests();
        int passed = 0;
        int failed = 0;

        try
        {
            testInstance.Test_ClearMatchSave_RemovesOnlyMatchKeys_PreservesSpatialCalibrationAndPreferences();
            Debug.Log("<color=#00FF88>[TEST PASS]</color> Test_ClearMatchSave_RemovesOnlyMatchKeys_PreservesSpatialCalibrationAndPreferences");
            passed++;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TEST FAIL] Test_ClearMatchSave_RemovesOnlyMatchKeys_PreservesSpatialCalibrationAndPreferences: {ex}");
            failed++;
        }
        finally
        {
            testInstance.TearDown();
        }

        try
        {
            testInstance.Test_NetworkStateSync_WhenDisabled_RejectsStateMutation();
            Debug.Log("<color=#00FF88>[TEST PASS]</color> Test_NetworkStateSync_WhenDisabled_RejectsStateMutation");
            passed++;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TEST FAIL] Test_NetworkStateSync_WhenDisabled_RejectsStateMutation: {ex}");
            failed++;
        }
        finally
        {
            testInstance.TearDown();
        }

        try
        {
            testInstance.Test_ColocationSync_DefaultsDisabled_AndRejectsUnauthenticatedPackets();
            Debug.Log("<color=#00FF88>[TEST PASS]</color> Test_ColocationSync_DefaultsDisabled_AndRejectsUnauthenticatedPackets");
            passed++;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TEST FAIL] Test_ColocationSync_DefaultsDisabled_AndRejectsUnauthenticatedPackets: {ex}");
            failed++;
        }
        finally
        {
            testInstance.TearDown();
        }

        Debug.Log($"<color=#00FF88><b>[PHASE 1 TEST RUNNER] Completed: {passed} PASSED, {failed} FAILED</b></color>");
    }

    [UnityEditor.InitializeOnLoadMethod]
    private static void AutoRunOnEditorLoad()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            RunAllTestsFromMenu();
        };
    }
#endif
}
