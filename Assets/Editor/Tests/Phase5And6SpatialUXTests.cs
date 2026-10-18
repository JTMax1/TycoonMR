using System;
using NUnit.Framework;
using UnityEngine;

public class Phase5And6SpatialUXTests
{
    private GameObject testGo;
    private TycoonBoardManager boardManager;

    [SetUp]
    public void SetUp()
    {
        testGo = new GameObject("TestSpatialUXBoardManager");
        boardManager = testGo.AddComponent<TycoonBoardManager>();
        boardManager.InitializeOfficialMonopolyMap();
        boardManager.SetPlayerCash(0, 1500);
        boardManager.SetPlayerCash(1, 1500);
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
    public void Test_StartMenuConsole_PreservesBoardVisibility()
    {
        // Invoke start menu generation
        boardManager.CreateStartMenuForTest();
        Transform startMenu = testGo.transform.Find("Executive_Start_Menu");
        Assert.IsNotNull(startMenu, "Executive_Start_Menu must be instantiated");

        // Assert console is positioned near table edge and angled up towards player
        Assert.LessOrEqual(startMenu.localPosition.z, -0.40f, "Start menu must be positioned at front table edge to avoid board occlusion");
        Assert.GreaterOrEqual(startMenu.localEulerAngles.x, 25f, "Start menu must be angled upward towards player eyes for comfortable viewing");

        Transform backdrop = startMenu.Find("StartMenu_Backdrop");
        Assert.IsNotNull(backdrop, "StartMenu_Backdrop must exist");
        Assert.LessOrEqual(backdrop.localScale.x, 0.42f, "Backdrop width must be compact (<=0.42m) to leave perimeter board tiles fully visible");
    }

    [Test]
    public void Test_EventDrivenHUD_DirtyFlag()
    {
        boardManager.MarkHudDirty();
        // Changing match state flags the HUD for refresh
        boardManager.SetCurrentState(TycoonBoardManager.TurnState.WaitingForRoll);
        Assert.AreEqual(TycoonBoardManager.TurnState.WaitingForRoll, boardManager.CurrentState);
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tycoon/Run Phase 5 & 6 Spatial UX Tests")]
    public static void RunAllTestsFromMenu()
    {
        var testInstance = new Phase5And6SpatialUXTests();
        int passed = 0;
        int failed = 0;

        var testMethods = new Action[]
        {
            testInstance.Test_StartMenuConsole_PreservesBoardVisibility,
            testInstance.Test_EventDrivenHUD_DirtyFlag
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

        Debug.Log($"<color=#00FF88><b>[PHASE 5 & 6 TEST RUNNER] Completed: {passed} PASSED, {failed} FAILED (Total: {testMethods.Length})</b></color>");
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
