#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

namespace GridBattle.EditorTools
{
    /// <summary>Bấm Play trong Editor luôn vào MainMenu trước (giống bản build), dù đang mở scene nào.</summary>
    [InitializeOnLoad]
    public static class PlayFromMainMenu
    {
        private const string Path = "Assets/Scenes/MainMenu.unity";

        static PlayFromMainMenu() =>
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(Path);
    }
}
#endif
