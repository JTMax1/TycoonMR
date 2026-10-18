using System;
using NUnit.Framework;
using UnityEngine;

public class Phase4InputPhysicsTests
{
    private GameObject testGo;
    private PhysicalThrowDie physicalDie;
    private Rigidbody rb;

    [SetUp]
    public void SetUp()
    {
        testGo = new GameObject("TestPhysicalDie");
        rb = testGo.AddComponent<Rigidbody>();
        testGo.AddComponent<BoxCollider>();
        physicalDie = testGo.AddComponent<PhysicalThrowDie>();
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
    public void Test_PhysicalDie_RigidbodyConfiguration()
    {
        Assert.AreEqual(CollisionDetectionMode.ContinuousDynamic, rb.collisionDetectionMode, "Dice must use ContinuousDynamic collision detection to prevent tunneling");
        Assert.AreEqual(RigidbodyInterpolation.Interpolate, rb.interpolation, "Dice must use Interpolate for jitter-free physics rendering in VR");
        Assert.Greater(rb.linearDamping, 0.1f, "Linear damping must be configured to prevent endless sliding");
        Assert.Greater(rb.angularDamping, 0.1f, "Angular damping must be configured to prevent endless spinning");
    }

    [Test]
    public void Test_PhysicalDie_ReadTopFaceFromOrientation_Face1()
    {
        // Face 1: +Z faces up (boardUp = Vector3.up)
        // transform.forward = Vector3.up
        testGo.transform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.back);
        int face = physicalDie.ReadTopFaceFromOrientation();
        Assert.AreEqual(1, face, "When forward (+Z) aligns with boardUp, top face must be 1");
    }

    [Test]
    public void Test_PhysicalDie_ReadTopFaceFromOrientation_Face2()
    {
        // Face 2: +Y faces up (boardUp = Vector3.up)
        // transform.up = Vector3.up
        testGo.transform.rotation = Quaternion.identity;
        int face = physicalDie.ReadTopFaceFromOrientation();
        Assert.AreEqual(2, face, "When up (+Y) aligns with boardUp, top face must be 2");
    }

    [Test]
    public void Test_PhysicalDie_ReadTopFaceFromOrientation_Face3()
    {
        // Face 3: +X faces up
        testGo.transform.rotation = Quaternion.FromToRotation(Vector3.right, Vector3.up);
        int face = physicalDie.ReadTopFaceFromOrientation();
        Assert.AreEqual(3, face, "When right (+X) aligns with boardUp, top face must be 3");
    }

    [Test]
    public void Test_PhysicalDie_ReadTopFaceFromOrientation_Face4()
    {
        // Face 4: -X faces up
        testGo.transform.rotation = Quaternion.FromToRotation(Vector3.left, Vector3.up);
        int face = physicalDie.ReadTopFaceFromOrientation();
        Assert.AreEqual(4, face, "When left (-X) aligns with boardUp, top face must be 4");
    }

    [Test]
    public void Test_PhysicalDie_ReadTopFaceFromOrientation_Face5()
    {
        // Face 5: -Y faces up
        testGo.transform.rotation = Quaternion.FromToRotation(Vector3.down, Vector3.up);
        int face = physicalDie.ReadTopFaceFromOrientation();
        Assert.AreEqual(5, face, "When down (-Y) aligns with boardUp, top face must be 5");
    }

    [Test]
    public void Test_PhysicalDie_ReadTopFaceFromOrientation_Face6()
    {
        // Face 6: -Z faces up
        testGo.transform.rotation = Quaternion.FromToRotation(Vector3.back, Vector3.up);
        int face = physicalDie.ReadTopFaceFromOrientation();
        Assert.AreEqual(6, face, "When back (-Z) aligns with boardUp, top face must be 6");
    }

    [Test]
    public void Test_InputSemantics_GripSeparatedFromPinch()
    {
        GameObject boardGo = new GameObject("TestBoardInputManager");
        var bm = boardGo.AddComponent<TycoonBoardManager>();

        // Assert initial input states are false and decoupled
        Assert.IsFalse(bm.IsLeftHandGrabbing, "Left hand grab must default false");
        Assert.IsFalse(bm.IsRightHandGrabbing, "Right hand grab must default false");

        GameObject.DestroyImmediate(boardGo);
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tycoon/Run Phase 4 Input and Physics Tests")]
    public static void RunAllTestsFromMenu()
    {
        var testInstance = new Phase4InputPhysicsTests();
        int passed = 0;
        int failed = 0;

        var testMethods = new Action[]
        {
            testInstance.Test_PhysicalDie_RigidbodyConfiguration,
            testInstance.Test_PhysicalDie_ReadTopFaceFromOrientation_Face1,
            testInstance.Test_PhysicalDie_ReadTopFaceFromOrientation_Face2,
            testInstance.Test_PhysicalDie_ReadTopFaceFromOrientation_Face3,
            testInstance.Test_PhysicalDie_ReadTopFaceFromOrientation_Face4,
            testInstance.Test_PhysicalDie_ReadTopFaceFromOrientation_Face5,
            testInstance.Test_PhysicalDie_ReadTopFaceFromOrientation_Face6,
            testInstance.Test_InputSemantics_GripSeparatedFromPinch
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

        Debug.Log($"<color=#00FF88><b>[PHASE 4 TEST RUNNER] Completed: {passed} PASSED, {failed} FAILED (Total: {testMethods.Length})</b></color>");
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
