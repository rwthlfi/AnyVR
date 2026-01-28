using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.XR.Management;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace AnyVR.Editor
{
    public static class BuildScript
    {
        private const string OpenXRLoaderName = "UnityEngine.XR.OpenXR.OpenXRLoader";
        private static readonly string Path = PackageInfo.FindForPackageName("rwth.lfi.anyvr").assetPath;
        private static readonly string DockerFile = System.IO.Path.Combine(Path, "Editor/Dockerfile");
        private static readonly string DockerIgnoreFile = System.IO.Path.Combine(Path, "Editor/.dockerignore");
        private static readonly string BinaryName = Application.productName.Replace(" ", "-");

        private static string[] Scenes => EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        private static void SetEnableOpenXR(BuildTargetGroup buildTargetGroup, bool enable)
        {
            XRGeneralSettings generalSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
            XRManagerSettings settings = generalSettings.AssignedSettings;

            switch (enable)
            {
                case true when !XRPackageMetadataStore.IsLoaderAssigned(OpenXRLoaderName, buildTargetGroup):
                    XRPackageMetadataStore.AssignLoader(settings, OpenXRLoaderName, buildTargetGroup);
                    break;
                case false when XRPackageMetadataStore.IsLoaderAssigned(OpenXRLoaderName, buildTargetGroup):
                    XRPackageMetadataStore.RemoveLoader(settings, OpenXRLoaderName, buildTargetGroup);
                    break;
            }
        }

        [MenuItem("Build/Build Linux Server")]
        public static void BuildLinuxServer()
        {
            SetEnableOpenXR(BuildTargetGroup.Standalone, false);
            EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Server;
            BuildReport report = BuildPipeline.BuildPlayer(Scenes, $"Builds/LinuxServer/{BinaryName}.x86_64",
                BuildTarget.StandaloneLinux64, BuildOptions.None);
            Assert.IsTrue(report.summary.result == BuildResult.Succeeded, "Build Failed");

            string dir = System.IO.Path.GetDirectoryName(report.summary.outputPath);
            Assert.IsFalse(string.IsNullOrWhiteSpace(dir));

            string sourceText = File.ReadAllText(DockerFile);
            string destFile = System.IO.Path.Combine(dir, "Dockerfile");

            File.WriteAllText(destFile, sourceText.Replace("gamebinary", $"{BinaryName}.x86_64"));
            File.Copy(DockerIgnoreFile, System.IO.Path.Combine(dir, ".dockerignore"), true);
        }

        [MenuItem("Build/Build Linux Client")]
        public static void BuildLinuxClient()
        {
            SetEnableOpenXR(BuildTargetGroup.Standalone, false);
            EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Player;
            BuildReport report = BuildPipeline.BuildPlayer(Scenes, $"Builds/LinuxClient/{BinaryName}",
                BuildTarget.StandaloneLinux64, BuildOptions.None);
            Assert.IsTrue(report.summary.result == BuildResult.Succeeded, "Build Failed");
        }

        [MenuItem("Build/Build Windows Client")]
        public static void BuildWindowsClient()
        {
            SetEnableOpenXR(BuildTargetGroup.Standalone, true);
            EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Player;
            BuildReport report = BuildPipeline.BuildPlayer(Scenes, $"Builds/WindowsClient/{BinaryName}",
                BuildTarget.StandaloneWindows64, BuildOptions.None);
            Assert.IsTrue(report.summary.result == BuildResult.Succeeded, "Build Failed");
        }

        [MenuItem("Build/Build WebGL Client")]
        public static void BuildWebGLClient()
        {
            SetEnableOpenXR(BuildTargetGroup.WebGL, true);
            EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Player;
            BuildReport report = BuildPipeline.BuildPlayer(Scenes, $"Builds/WebGL/{BinaryName}",
                BuildTarget.WebGL, BuildOptions.None);
            Assert.IsTrue(report.summary.result == BuildResult.Succeeded, "Build Failed");
        }

        [MenuItem("Build/Build Android Client")]
        public static void BuildAndroidClient()
        {
            SetEnableOpenXR(BuildTargetGroup.Android, true);
            EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Player;
            BuildReport report = BuildPipeline.BuildPlayer(Scenes, $"Builds/Android/{BinaryName}.apk",
                BuildTarget.Android, BuildOptions.None);
            Assert.IsTrue(report.summary.result == BuildResult.Succeeded, "Build Failed");
        }

        [MenuItem("Build/Clean")]
        public static void BuildClean()
        {
            const string buildDir = "Builds";

            if (!Directory.Exists(buildDir))
            {
                Debug.Log("Clean failed. Build directory does not exist");
                return;
            }

            string[] subDirs = Directory.GetDirectories(buildDir);

            try
            {
                foreach (string dir in subDirs)
                {
                    Directory.Delete(dir, true);
                }
            }
            catch (Exception e)
            {
                Debug.Log("Clean failed");
                Debug.LogError(e.ToString());
                return;
            }

            Debug.Log("Clean succeeded");
        }
    }
}
