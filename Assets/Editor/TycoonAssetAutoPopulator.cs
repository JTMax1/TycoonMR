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
}
#endif
