#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class TycoonAssetAutoPopulator
{
    static TycoonAssetAutoPopulator()
    {
        EditorApplication.delayCall += () =>
        {
            AutoPopulateSceneManagers(false);
        };
    }

    [MenuItem("Tycoon MR/Auto-Populate City Asset Prefabs")]
    public static void MenuAutoPopulate()
    {
        AutoPopulateSceneManagers(true);
    }

    public static void AutoPopulateSceneManagers(bool forceSave)
    {
        TycoonBoardManager[] managers = Object.FindObjectsByType<TycoonBoardManager>(FindObjectsInactive.Include);
        if (managers == null || managers.Length == 0) return;

        bool anyModified = false;
        foreach (TycoonBoardManager mgr in managers)
        {
            if (mgr != null)
            {
                mgr.AutoPopulateEmptyPrefabs();
                EditorUtility.SetDirty(mgr);
                anyModified = true;
            }
        }

        if (anyModified)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                if (forceSave)
                {
                    EditorSceneManager.SaveScene(activeScene);
                    Debug.Log("<color=#00FF88>[Tycoon MR]</color> Successfully auto-populated city asset prefabs and saved scene!");
                }
            }
        }
    }

    [MenuItem("Tycoon MR/Debug Dice Face Mappings")]
    public static void DebugDiceFaces()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Dice_6/Prefabs/Dice_d6_Plastic Glossy Pure write.prefab");
        if (prefab == null) return;

        GameObject die = Object.Instantiate(prefab);
        die.transform.position = new Vector3(0, 100, 0);
        die.transform.rotation = Quaternion.identity;

        MeshFilter mf = prefab.GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;
        Mesh m = mf.sharedMesh;
        Vector3[] verts = m.vertices;
        Vector3[] normals = m.normals;
        Vector2[] uvs = m.uv;
        int[] tris = m.triangles;

        Vector3[] dirs = new Vector3[] { Vector3.up, Vector3.down, Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
        string[] dnames = new string[] { "+Y", "-Y", "+X", "-X", "+Z", "-Z" };

        for (int d = 0; d < dirs.Length; d++)
        {
            Vector3 targetDir = dirs[d];
            Vector2 uvSum = Vector2.zero;
            int count = 0;

            for (int t = 0; t < tris.Length; t += 3)
            {
                Vector3 n = (normals[tris[t]] + normals[tris[t + 1]] + normals[tris[t + 2]]) / 3f;
                if (Vector3.Dot(n.normalized, targetDir) > 0.7f)
                {
                    uvSum += uvs[tris[t]] + uvs[tris[t + 1]] + uvs[tris[t + 2]];
                    count += 3;
                }
            }

            if (count > 0)
            {
                Vector2 avgUV = uvSum / count;
                string pipName = "Unknown";
                if (avgUV.x < 0.33f && avgUV.y > 0.66f) pipName = "6";
                else if (avgUV.x < 0.33f && avgUV.y > 0.33f) pipName = "5";
                else if (avgUV.x < 0.33f && avgUV.y <= 0.33f) pipName = "2";
                else if (avgUV.x >= 0.33f && avgUV.x < 0.66f && avgUV.y > 0.33f) pipName = "4";
                else if (avgUV.x >= 0.33f && avgUV.x < 0.66f && avgUV.y <= 0.33f) pipName = "3";
                else if (avgUV.x >= 0.66f && avgUV.y <= 0.33f) pipName = "1";

                Debug.Log($"<color=#00FFFF>[Dice Face Mapping]</color> Direction {dnames[d]}: Avg UV=({avgUV.x:F2}, {avgUV.y:F2}) => Face {pipName}");
            }
        }
        Object.DestroyImmediate(die);
    }
}
#endif
