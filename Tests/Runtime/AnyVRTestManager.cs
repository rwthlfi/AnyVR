using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace Tests.Runtime
{
    internal class AnyVRTestManager
    {
        private static readonly string s_path = UnityEditor.PackageManager.PackageInfo.FindForPackageName("rwth.lfi.anyvr").assetPath;
        private static readonly string s_testScenesFolder = Path.Combine(s_path, "Tests/Scenes");

        internal static void AddTestScenesToBuildSettings()
        {
            string[] testSceneGuids = AssetDatabase.FindAssets("t:Scene", new[]
            {
                s_testScenesFolder
            });
            string[] testScenePaths = testSceneGuids.Select(AssetDatabase.GUIDToAssetPath).ToArray();

            List<EditorBuildSettingsScene> existingScenes = EditorBuildSettings.scenes
                .Where(s => !s.path.StartsWith(s_testScenesFolder))
                .ToList();

            existingScenes.AddRange(testScenePaths.Select(p => new EditorBuildSettingsScene(p, true)));

            EditorBuildSettings.scenes = existingScenes.ToArray();
        }

        internal static void RemoveTestScenesFromBuildSettings()
        {
            EditorBuildSettings.scenes = EditorBuildSettings.scenes
                .Where(s => !s.path.StartsWith(s_testScenesFolder))
                .ToArray();
        }
    }
}