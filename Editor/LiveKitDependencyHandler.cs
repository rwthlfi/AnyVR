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

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace AnyVR.Editor
{
    public static class LiveKitPackageHandler
    {
        private const string LiveKitName = "io.livekit.livekit-sdk";
        private const string LiveKitWebName = "io.livekit.unity";
        private const string LiveKitImportUrl = "https://github.com/livekit/client-sdk-unity.git";
        private const string LiveKitWebImportUrl = "https://github.com/livekit/client-sdk-unity-web.git";
        private const string AsmDefPath = "Runtime/livekit.unity.Runtime.asmdef";

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.delayCall += InstallPackages;
        }

        [MenuItem("AnyVR/Install LiveKit Packages")]
        private static void InstallPackages()
        {
            bool liveKitExists = PackageInfo.FindForPackageName(LiveKitName) != null;
            bool liveKitWebExists = PackageInfo.FindForPackageName(LiveKitWebName) != null;

            if (!liveKitExists || !liveKitWebExists)
            {
                InstallMissingPackages(liveKitExists, liveKitWebExists);
                Debug.LogWarning("LiveKit packages added. Waiting for script compilation ...");
            }

            RefreshAsmDefs();
        }

        private static void InstallMissingPackages(bool liveKitExists, bool liveKitWebExists)
        {
#if UNITY_EDITOR
            if (!liveKitExists)
            {
                Client.Add(LiveKitImportUrl);
            }

            if (!liveKitWebExists)
            {
                Client.Add(LiveKitWebImportUrl);
            }
#endif
        }

        [MenuItem("AnyVR/Refresh LiveKit AsmDefs")]
        private static void RefreshAsmDefs()
        {
            bool isWebGl = EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL;
            try
            {
                PackageInfo liveKitPackage = PackageInfo.FindForPackageName(LiveKitName);
                PackageInfo liveKitWebPackage = PackageInfo.FindForPackageName(LiveKitWebName);

                if (liveKitPackage == null || liveKitWebPackage == null)
                {
                    Debug.LogError("LiveKit packages not found!");
                    return;
                }

                string asmdefStandalonePath = Path.Combine(liveKitPackage.assetPath, AsmDefPath);
                string asmdefWebPath = Path.Combine(liveKitWebPackage.assetPath, AsmDefPath);

                if (!File.Exists(asmdefStandalonePath) || !File.Exists(asmdefWebPath))
                {
                    Debug.LogError("Assembly definition files not found!");
                    return;
                }

                List<string> standaloneInclude = new()
                {
                    "WindowsStandalone64", "LinuxStandalone64", "Android"
                };
                List<string> webInclude = new()
                {
                    "WebGL"
                };

                if (isWebGl)
                {
                    webInclude.Add("Editor");
                }
                else
                {
                    standaloneInclude.Add("Editor");
                }

                bool update = ModifyAsmDef(asmdefStandalonePath, standaloneInclude.ToArray()) | ModifyAsmDef(asmdefWebPath, webInclude.ToArray());
                if (update)
                {
                    CompilationPipeline.RequestScriptCompilation();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to configure LiveKit: {e.Message}");
            }
        }

        private static bool ModifyAsmDef(string path, string[] platforms)
        {
            string content = File.ReadAllText(path);
            AsmdefFile config = JsonUtility.FromJson<AsmdefFile>(content);
            config.includePlatforms = platforms;
            string newContent = JsonUtility.ToJson(config, true);
            if (string.Equals(content, newContent))
            {
                return false;
            }
            File.WriteAllText(path, JsonUtility.ToJson(config, true));
            return true;
        }
    }

    // ReSharper disable InconsistentNaming
    [Serializable]
    internal class AsmdefFile
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