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

using UnityEngine;
using UnityEditor;
using System.IO;
using System;

namespace AnyVR
{
    public class LiveKitPackageHandler : EditorWindow
    {
        private static string GetLiveKitAsmdefPath(string packageName)
        {
            string packageCacheDir = Path.Combine(Application.dataPath, "../Library/PackageCache");
            string[] matchingDirectories = Directory.GetDirectories(packageCacheDir, $"{packageName}@*");

            return matchingDirectories.Length > 0 ? Path.Combine(matchingDirectories[0], "Runtime/livekit.unity.Runtime.asmdef") : string.Empty;
        }

        private static void EditAssemblyDefinition(bool isWebGl)
        {
            string standaloneAsmdefPath = GetLiveKitAsmdefPath("io.livekit.livekit-sdk");
            string webglAsmdefPath = GetLiveKitAsmdefPath("io.livekit.unity");

            Debug.Log($"standalone:\t {standaloneAsmdefPath}");
            Debug.Log($"webgl:\t {webglAsmdefPath}");

            string standaloneAsmdefContent = File.ReadAllText(standaloneAsmdefPath);
            AsmdefFile standalone = JsonUtility.FromJson<AsmdefFile>(standaloneAsmdefContent);
            string webGlAsmdefContent = File.ReadAllText(webglAsmdefPath);
            AsmdefFile wegbl = JsonUtility.FromJson<AsmdefFile>(webGlAsmdefContent);

            if (isWebGl)
            {
                wegbl.includePlatforms = new[] { "Editor", "WebGL" };
                standalone.includePlatforms = new[] { "WindowsStandalone64", "LinuxStandalone64" };
            }
            else
            {
                wegbl.includePlatforms = new[] { "WebGL" };
                standalone.includePlatforms = new[] { "Editor", "WindowsStandalone64", "LinuxStandalone64" };
            }

            File.WriteAllText(standaloneAsmdefPath, JsonUtility.ToJson(standalone, true));
            File.WriteAllText(webglAsmdefPath, JsonUtility.ToJson(wegbl, true));

        }

        [MenuItem("AnyVr/SetTarget/Standalone")]
        private static void MakeStandalone()
        {
            string standaloneAsmdefPath = GetLiveKitAsmdefPath("io.livekit.livekit-sdk");
            string webglAsmdefPath = GetLiveKitAsmdefPath("io.livekit.unity");
            Debug.Log($"standalone:\t {standaloneAsmdefPath}");
            Debug.Log($"webgl:\t {webglAsmdefPath}");
            EditAssemblyDefinition(false);
        }

        [MenuItem("AnyVr/SetTarget/WebGL")]
        private static void MakeWebGl()
        {
            EditAssemblyDefinition(true);
        }
    }

    [Serializable]
    public class AsmdefFile
    {
        public string name;
        public string rootNamespace;
        public string[] references;
        public string[] includePlatforms;
        public string[] excludePlatforms;
        public bool allowUnsafeCode;
        public bool overrideReferences;
        public string[] precompiledReferences;
        public bool autoReferenced;
        public string[] defineConstraints;
        public string[] versionDefines;
        public bool noEngineReferences;
    }
}