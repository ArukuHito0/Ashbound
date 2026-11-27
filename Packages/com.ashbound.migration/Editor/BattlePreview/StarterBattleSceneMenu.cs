using Ashbound.BattleScene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Ashbound.Editor
{
    internal static class StarterBattleSceneMenu
    {
        private const string ThemePath = "Packages/com.ashbound.migration/Runtime/BattleScene/StarterBattle.tss";
        private const string ScenePath = "Assets/AshboundStarterBattle.unity";

        [MenuItem("ASHBOUND/試遊シーンを作成")]
        private static void CreateScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) is SceneAsset existing)
            {
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                return;
            }

            var master = AssetDatabase.LoadAssetAtPath<TextAsset>(StarterBattleSetup.MasterPath);
            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            if (master == null || theme == null)
            {
                Debug.LogError("ASHBOUND の戦闘マスターまたは UI テーマが見つかりません。");
                return;
            }

            var original = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                var screen = new GameObject("Ashbound Battle", typeof(StarterBattleScreen));
                SceneManager.MoveGameObjectToScene(screen, scene);
                var component = new SerializedObject(screen.GetComponent<StarterBattleScreen>());
                component.FindProperty("master").objectReferenceValue = master;
                component.FindProperty("theme").objectReferenceValue = theme;
                component.ApplyModifiedPropertiesWithoutUndo();

                var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color32(21, 19, 20, 255);

                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                    Debug.LogError("ASHBOUND の試遊シーンを保存できませんでした。");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (original.IsValid() && original.isLoaded)
                    SceneManager.SetActiveScene(original);
            }

            var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
        }
    }
}

