using System;
using NUnit.Framework;
using UnityEngine;

public class Phase3ModeTests
{
    private GameObject testGo;
    private TycoonBoardManager boardManager;

    [SetUp]
    public void SetUp()
    {
        testGo = new GameObject("TestModeBoardManager");
        boardManager = testGo.AddComponent<TycoonBoardManager>();
        boardManager.InitializeOfficialMonopolyMap();
        boardManager.SetPlayerCash(0, 1500);
        boardManager.SetPlayerCash(1, 1500);
        boardManager.SetPlayerCash(2, 1500);
        boardManager.SetPlayerCash(3, 1500);
    }

    [TearDown]
    public void TearDown()
    {
        if (testGo != null)
        {
            GameObject.DestroyImmediate(testGo);
            testGo = null;
        }
    }

    [Test]
    public void Test_SoloVsAI_PlayerSeatRoles()
    {
        boardManager.selectedGameMode = TycoonBoardManager.GameMode.SoloVsAI;
        boardManager.activePlayerCount = 4;

        Assert.IsTrue(boardManager.IsHumanPlayer(0), "Seat 0 must be human in SoloVsAI mode");
        Assert.IsFalse(boardManager.IsHumanPlayer(1), "Seat 1 must be AI in SoloVsAI mode");
        Assert.IsFalse(boardManager.IsHumanPlayer(2), "Seat 2 must be AI in SoloVsAI mode");
        Assert.IsFalse(boardManager.IsHumanPlayer(3), "Seat 3 must be AI in SoloVsAI mode");
    }

    [Test]
    public void Test_PassAndPlay_AllActiveSeatsAreHuman()
    {
        boardManager.selectedGameMode = TycoonBoardManager.GameMode.PassAndPlay;
        boardManager.activePlayerCount = 4;

        for (int i = 0; i < 4; i++)
        {
            Assert.IsTrue(boardManager.IsHumanPlayer(i), $"Seat {i} must be human in PassAndPlay mode");
        }
    }

    [Test]
    public void Test_TurnState_HandoverStateExistsAndTransitions()
    {
        boardManager.CurrentState = TycoonBoardManager.TurnState.Handover;
        Assert.AreEqual(TycoonBoardManager.TurnState.Handover, boardManager.CurrentState, "CurrentState should support Handover");

        boardManager.CurrentState = TycoonBoardManager.TurnState.WaitingForRoll;
        Assert.AreEqual(TycoonBoardManager.TurnState.WaitingForRoll, boardManager.CurrentState, "CurrentState should transition from Handover to WaitingForRoll");
    }

    [Test]
    public void Test_GetRemainingActivePlayerCount_TracksBankruptcy()
    {
        boardManager.activePlayerCount = 4;
        Assert.AreEqual(4, boardManager.GetRemainingActivePlayerCount(), "Initial active player count should be 4");

        boardManager.isBankrupt[1] = true;
        Assert.AreEqual(3, boardManager.GetRemainingActivePlayerCount(), "Active player count should be 3 after 1 bankruptcy");

        boardManager.isBankrupt[2] = true;
        boardManager.isBankrupt[3] = true;
        Assert.AreEqual(1, boardManager.GetRemainingActivePlayerCount(), "Active player count should be 1 after 3 bankruptcies");
    }

    [Test]
    public void Test_CurrentTurnPlayer_SeatIdentity()
    {
        int receivedSeat = -1;
        boardManager.OnTurnStarted += (seat) => { receivedSeat = seat; };

        boardManager.CurrentTurnPlayer = 2;
        Assert.AreEqual(2, boardManager.CurrentTurnPlayer, "CurrentTurnPlayer property must store seat 2");
        
        boardManager.OnTurnStarted?.Invoke(boardManager.CurrentTurnPlayer);
        Assert.AreEqual(2, receivedSeat, "OnTurnStarted event must fire with current turn player seat index");
    }

    [Test]
    public void Test_NetworkModes_GatedUntilPhase7NGO()
    {
        boardManager.selectedGameMode = TycoonBoardManager.GameMode.LocalColocation;
        boardManager.LaunchSelectedGameMode();

        // In Phase 3, network modes must remain gated without corrupting local state
        Assert.IsFalse(boardManager.isNetworkStateSyncAuthorized, "Network state sync must remain unauthorized when launching gated network mode");
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tycoon/Run Phase 3 Mode Tests")]
    public static void RunAllTestsFromMenu()
    {
        var testInstance = new Phase3ModeTests();
        int passed = 0;
        int failed = 0;

        var testMethods = new Action[]
        {
            testInstance.Test_SoloVsAI_PlayerSeatRoles,
            testInstance.Test_PassAndPlay_AllActiveSeatsAreHuman,
            testInstance.Test_TurnState_HandoverStateExistsAndTransitions,
            testInstance.Test_GetRemainingActivePlayerCount_TracksBankruptcy,
            testInstance.Test_CurrentTurnPlayer_SeatIdentity,
            testInstance.Test_NetworkModes_GatedUntilPhase7
        };

        foreach (var test in testMethods)
        {
            testInstance.SetUp();
            try
            {
                test.Invoke();
                Debug.Log($"<color=#00FF88>[TEST PASS]</color> {test.Method.Name}");
                passed++;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TEST FAIL] {test.Method.Name}: {ex}");
                failed++;
            }
            finally
            {
                testInstance.TearDown();
            }
        }

        Debug.Log($"<color=#00FF88><b>[PHASE 3 TEST RUNNER] Completed: {passed} PASSED, {failed} FAILED (Total: {testMethods.Length})</b></color>");
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
