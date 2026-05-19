using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class SceneAutoStart
{
    static SceneAutoStart()
    {
        // Set the playModeStartScene to the Loading scene so it always starts there
        var loadingScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Loading.unity");
        if (loadingScene != null)
        {
            EditorSceneManager.playModeStartScene = loadingScene;
        }
    }
}
