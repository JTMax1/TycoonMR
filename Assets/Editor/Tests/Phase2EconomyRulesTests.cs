using System;
using NUnit.Framework;
using UnityEngine;

public class Phase2EconomyRulesTests
{
    private GameObject testGo;
    private TycoonBoardManager boardManager;

    [SetUp]
    public void SetUp()
    {
        testGo = new GameObject("TestEconomyBoardManager");
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
    public void Test_BaseRent_OnUndevelopedProperty_WithoutMonopoly()
    {
        var tile1 = boardManager.GetTile(1); // Mediterranean Ave (rentByLevel: [2, 10, 30, 90, 160, 250])
        var tile3 = boardManager.GetTile(3); // Baltic Ave

        tile1.owner = 0;
        tile1.buildingLevel = 0;
        tile1.isMortgaged = false;
        tile3.owner = -1; // Incomplete group

        int rent = boardManager.CalculateAuthenticRent(tile1, 7);
        Assert.AreEqual(2, rent, "Unimproved property without monopoly should charge base rent ($2)");
    }

    [Test]
    public void Test_MonopolyRent_DoubledOnUndevelopedLevel0()
    {
        var tile1 = boardManager.GetTile(1);
        var tile3 = boardManager.GetTile(3);

        tile1.owner = 0;
        tile1.buildingLevel = 0;
        tile1.isMortgaged = false;

        tile3.owner = 0;
        tile3.buildingLevel = 0;
        tile3.isMortgaged = false;

        // Player 0 owns full Brown group (tiles 1 and 3)
        Assert.IsTrue(boardManager.OwnsFullColorGroup(0, 0), "Player 0 must own complete group 0");

        int rent = boardManager.CalculateAuthenticRent(tile1, 7);
        Assert.AreEqual(4, rent, "Unimproved property with complete monopoly must charge 2x base rent ($4)");
    }

    [Test]
    public void Test_MonopolyRent_DoesNotDoubleHouseRent()
    {
        var tile1 = boardManager.GetTile(1);
        var tile3 = boardManager.GetTile(3);

        tile1.owner = 0;
        tile1.buildingLevel = 1; // 1 House
        tile1.isMortgaged = false;

        tile3.owner = 0;
        tile3.buildingLevel = 1;
        tile3.isMortgaged = false;

        int rent = boardManager.CalculateAuthenticRent(tile1, 7);
        Assert.AreEqual(10, rent, "Property with 1 house must charge exact house rent tier ($10), not 2x house rent ($20)");
    }

    [Test]
    public void Test_MortgagedProperty_PaysZeroRent()
    {
        var tile1 = boardManager.GetTile(1);
        tile1.owner = 0;
        tile1.buildingLevel = 0;
        tile1.isMortgaged = true;

        int rent = boardManager.CalculateAuthenticRent(tile1, 7);
        Assert.AreEqual(0, rent, "Mortgaged property must charge $0 rent");
    }

    [Test]
    public void Test_RailroadRent_ScalesByCountOwned()
    {
        var rr1 = boardManager.GetTile(5);  // Reading RR
        var rr2 = boardManager.GetTile(15); // Pennsylvania RR
        var rr3 = boardManager.GetTile(25); // B&O RR
        var rr4 = boardManager.GetTile(35); // Short Line

        // 1 RR owned
        rr1.owner = 0;
        rr2.owner = -1;
        rr3.owner = -1;
        rr4.owner = -1;
        Assert.AreEqual(25, boardManager.CalculateAuthenticRent(rr1, 7), "1 Railroad rent must be $25");

        // 2 RR owned
        rr2.owner = 0;
        Assert.AreEqual(50, boardManager.CalculateAuthenticRent(rr1, 7), "2 Railroads rent must be $50");

        // 3 RR owned
        rr3.owner = 0;
        Assert.AreEqual(100, boardManager.CalculateAuthenticRent(rr1, 7), "3 Railroads rent must be $100");

        // 4 RR owned
        rr4.owner = 0;
        Assert.AreEqual(200, boardManager.CalculateAuthenticRent(rr1, 7), "4 Railroads rent must be $200");
    }

    [Test]
    public void Test_UtilityRent_ScalesByCountOwned()
    {
        var util1 = boardManager.GetTile(12); // Electric Co
        var util2 = boardManager.GetTile(28); // Water Works

        // 1 Utility owned
        util1.owner = 0;
        util2.owner = -1;
        Assert.AreEqual(28, boardManager.CalculateAuthenticRent(util1, 7), "1 Utility must charge 4x dice roll (7 * 4 = 28)");

        // 2 Utilities owned
        util2.owner = 0;
        Assert.AreEqual(70, boardManager.CalculateAuthenticRent(util1, 7), "Both Utilities must charge 10x dice roll (7 * 10 = 70)");
    }

    [Test]
    public void Test_CanBuild_RejectsWhenColorGroupIncomplete()
    {
        var tile1 = boardManager.GetTile(1);
        var tile3 = boardManager.GetTile(3);

        tile1.owner = 0;
        tile3.owner = -1; // Unowned, group incomplete

        var result = boardManager.CanBuild(0, 1);
        Assert.AreEqual(TycoonBoardManager.BuildValidationResult.IncompleteGroup, result, "CanBuild must reject incomplete color group");
    }

    [Test]
    public void Test_CanBuild_RejectsWhenColorGroupHasMortgage()
    {
        var tile1 = boardManager.GetTile(1);
        var tile3 = boardManager.GetTile(3);

        tile1.owner = 0;
        tile3.owner = 0;
        tile3.isMortgaged = true;

        var result = boardManager.CanBuild(0, 1);
        Assert.AreEqual(TycoonBoardManager.BuildValidationResult.GroupHasMortgage, result, "CanBuild must reject building when group has mortgaged property");
    }

    [Test]
    public void Test_CanBuild_EnforcesEvenBuilding()
    {
        var tile1 = boardManager.GetTile(1);
        var tile3 = boardManager.GetTile(3);

        tile1.owner = 0;
        tile1.buildingLevel = 1;
        tile3.owner = 0;
        tile3.buildingLevel = 0;

        // Tile 1 has 1 house, Tile 3 has 0 houses. Building on Tile 1 again must be rejected
        var resultTile1 = boardManager.CanBuild(0, 1);
        Assert.AreEqual(TycoonBoardManager.BuildValidationResult.UnevenDevelopment, resultTile1, "CanBuild must enforce even building on higher-level tile");

        // Building on Tile 3 must succeed to bring it to level 1
        var resultTile3 = boardManager.CanBuild(0, 3);
        Assert.AreEqual(TycoonBoardManager.BuildValidationResult.Success, resultTile3, "CanBuild must allow building on lower-level tile to maintain even distribution");
    }

    [Test]
    public void Test_CanBuild_RejectsWhenMaxLevelReached()
    {
        var tile1 = boardManager.GetTile(1);
        var tile3 = boardManager.GetTile(3);

        tile1.owner = 0;
        tile1.buildingLevel = 5; // Max level
        tile3.owner = 0;
        tile3.buildingLevel = 5;

        var result = boardManager.CanBuild(0, 1);
        Assert.AreEqual(TycoonBoardManager.BuildValidationResult.MaxLevelReached, result, "CanBuild must reject building when already at Level 5 Skyscraper Hotel");
    }

    [Test]
    public void Test_CanBuild_RejectsWhenInsufficientFunds()
    {
        var tile1 = boardManager.GetTile(1);
        var tile3 = boardManager.GetTile(3);

        tile1.owner = 0;
        tile1.buildingLevel = 0;
        tile3.owner = 0;
        tile3.buildingLevel = 0;

        boardManager.SetPlayerCash(0, 20); // Build cost is $50

        var result = boardManager.CanBuild(0, 1);
        Assert.AreEqual(TycoonBoardManager.BuildValidationResult.InsufficientFunds, result, "CanBuild must reject building when player has insufficient cash");
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tycoon/Run Phase 2 Economy Rules Tests")]
    public static void RunAllTestsFromMenu()
    {
        var testInstance = new Phase2EconomyRulesTests();
        int passed = 0;
        int failed = 0;

        var testMethods = new Action[]
        {
            testInstance.Test_BaseRent_OnUndevelopedProperty_WithoutMonopoly,
            testInstance.Test_MonopolyRent_DoubledOnUndevelopedLevel0,
            testInstance.Test_MonopolyRent_DoesNotDoubleHouseRent,
            testInstance.Test_MortgagedProperty_PaysZeroRent,
            testInstance.Test_RailroadRent_ScalesByCountOwned,
            testInstance.Test_UtilityRent_ScalesByCountOwned,
            testInstance.Test_CanBuild_RejectsWhenColorGroupIncomplete,
            testInstance.Test_CanBuild_RejectsWhenColorGroupHasMortgage,
            testInstance.Test_CanBuild_EnforcesEvenBuilding,
            testInstance.Test_CanBuild_RejectsWhenMaxLevelReached,
            testInstance.Test_CanBuild_RejectsWhenInsufficientFunds
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

        Debug.Log($"<color=#00FF88><b>[PHASE 2 TEST RUNNER] Completed: {passed} PASSED, {failed} FAILED (Total: {testMethods.Length})</b></color>");
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
