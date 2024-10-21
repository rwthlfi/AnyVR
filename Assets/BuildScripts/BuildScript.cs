// AnyVR is a multiuser, multiplatform XR framework.
// Copyright (C) 2024 Engineering Hydrology, RWTH Aachen University.
// 
// AnyVR is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published
// by the Free Software Foundation, either version 3 of the License,
// or (at your option) any later version.
// 
// AnyVR is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANT-
// ABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with AnyVR.
// If not, see <https://www.gnu.org/licenses/>.

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AnyVR.BuildScripts
{
    public class BuildScript
    {
        private const string k_name = "anyvr";
        private const string k_windowsClientBuildPath = "Builds/windows-client";
        private const string k_webglClientBuildPath = "Builds/webgl-client";
        private const string k_androidClientBuildPath = "Builds/android-client";
        private const string k_windowsServerBuildPath = "Builds/windows-server";
        private const string k_linuxServerBuildPath = "Builds/linux-server";

        private static void BuildPlayer(string path, BuildTarget target, bool isServer)
        {
            string directoryName = Path.GetDirectoryName(path);
            if (directoryName == null)
            {
                return;
            }
            
            string[] scenes =
            {
                "Assets/Scenes/GlobalScene.unity", 
                "Assets/Scenes/GlobalScene.unity", 
                "Assets/Scenes/LobbyScenes/InputModality_DemoScene.unity", 
                "Assets/Scenes/LobbyScenes/PhysicsDemo.unity"
            };

            BuildPlayerOptions buildOptions = new()
            {
                scenes = scenes,
                locationPathName = path,
                target = target,
                options = BuildOptions.None, // BuildOptions.Development,
                subtarget = isServer ? (int)StandaloneBuildSubtarget.Server : (int)StandaloneBuildSubtarget.Player
            };

            Directory.CreateDirectory(directoryName);

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Build succeeded: {path}");
            }
            else
            {
                Debug.LogError($"Build failed: {report.summary.result}");
            }
        }

        [MenuItem("Build/Build All")]
        public static void BuildAll()
        {
            BuildWindowsClient();
            BuildWebGLClient();
            BuildAndroidClient();
            BuildWindowsServer();
            BuildLinuxServer();
        }

        [MenuItem("Build/Build Windows Client")]
        public static void BuildWindowsClient()
        {
            string path = Path.Combine(k_windowsClientBuildPath, $"{k_name}.exe");
            BuildPlayer(path, BuildTarget.StandaloneWindows64, false);
        }
        
        [MenuItem("Build/Build WebGL Client")]
        public static void BuildWebGLClient()
        {
            string path = Path.Combine(k_webglClientBuildPath);
            BuildPlayer(path, BuildTarget.WebGL, false);
        }
        
        [MenuItem("Build/Build Android Client")]
        public static void BuildAndroidClient()
        {
            string path = Path.Combine(k_androidClientBuildPath, $"{k_name}.apk");
            BuildPlayer(path, BuildTarget.Android, false);
        }
        
        [MenuItem("Build/Build Windows Server")]
        public static void BuildWindowsServer()
        {
            string path = Path.Combine(k_windowsServerBuildPath, $"{k_name}.exe");
            BuildPlayer(path, BuildTarget.StandaloneWindows64, true);
        }
        
        [MenuItem("Build/Build Linux Server")]
        public static void BuildLinuxServer()
        {
            string path = Path.Combine(k_linuxServerBuildPath, $"{k_name}.x86_64");
            BuildPlayer(path, BuildTarget.StandaloneWindows64, true);
        }
    }
}

#endif
