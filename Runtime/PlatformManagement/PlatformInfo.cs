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
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace AnyVR.PlatformManagement
{
    /// <summary>
    /// Static class to provide miscellaneous information about the platform the software is running on.
    /// </summary>
    public static class PlatformInfo
    {
        /// <summary>
        /// The platform the software is running on.
        /// </summary>
        [Obsolete("Use GetPlatformAsync() instead. This will be removed in a future version.")]
        public static Platform Platform => GetPlatform();

        /// <summary>
        /// The platform family the software is running on.
        /// </summary>
        [Obsolete("Use GetPlatformTypeAsync() instead. This will be removed in a future version.")]
        public static PlatformType PlatformType => GetPlatformType();

        /// <summary>
        /// The XR hardware that is used.
        /// </summary>
        [Obsolete("Use GetXRHardwareTypeAsync() instead. This will be removed in a future version.")]
        public static XRHardwareType XRHardwareType => GetXRHardwareType();

        /// <summary>
        /// Reference to the HMD, if there is one. Is null otherwise.
        /// </summary>
        public static InputDevice? Headset => LookupInputDevice(XRNode.Head);

        /// <summary>
        /// Reference to the left XR controller, if there is one. Is null otherwise.
        /// </summary>
        public static InputDevice? LeftController => LookupInputDevice(XRNode.LeftHand);

        /// <summary>
        /// Reference to the right XR controller, if there is one. Is null otherwise.
        /// </summary>
        public static InputDevice? RightController => LookupInputDevice(XRNode.RightHand);

        internal static TaskCompletionSource<bool> s_xRInitializationTCS = new();
        /// <summary>
        /// Whether the XR system is initialized, if an XR system is active. <see langword="false"> otherwise.
        /// </summary>
        public static bool IsXRInitialized => IsXRPlatform() ? s_xRInitializationTCS.Task.IsCompleted : false;

        private static bool s_isEverythingInitialized = false;



        public static async Task Initialize()
        {
            await s_xRInitializationTCS.Task;
            s_isEverythingInitialized = true;
        }

        private static void CheckInitializationStatus()
        {
            if (!s_isEverythingInitialized)
            {
                Debug.LogWarning("[PlatformInfo] PlatformInfo is not fully initialized yet. Results may be faulty. Consider using async function instead.");
            }
        }

        private static bool HasActiveXRDeviceAttached()
        {
            CheckInitializationStatus();
            return XRSettings.isDeviceActive;
        }

        private static Platform GetWindowsEditorPlatform()
        {
            CheckInitializationStatus();
            if (HasActiveXRDeviceAttached())
            {
                return Platform.WindowsXREditor;
            }
            else
            {
                return Platform.WindowsEditor;
            }
        }

        private static Platform GetWindowsPlatform()
        {
            CheckInitializationStatus();
            if (HasActiveXRDeviceAttached())
            {
                return Platform.WindowsXR;
            }
            else
            {
                return Platform.Windows;
            }
        }

        private static Platform GetAndroidPlatform()
        {
            CheckInitializationStatus();
            if (HasActiveXRDeviceAttached())
            {
                if (XRGeneralSettings.Instance.Manager.activeLoader.name.ToLower().Contains("meta"))
                {
                    return Platform.MetaQuest;
                }
                else if (XRGeneralSettings.Instance.Manager.activeLoader.name.ToLower().Contains("pico"))
                {
                    return Platform.Pico;
                }
                else
                {
                    return Platform.GenericXR;
                }
            }
            else
            {
                return Platform.Android;
            }
        }

        private static Platform GetGenericPlatform()
        {
            CheckInitializationStatus();
            if (HasActiveXRDeviceAttached())
            {
                return Platform.GenericXR;
            }
            else if (SystemInfo.deviceType == DeviceType.Handheld)
            {
                return Platform.GenericMobile;
            }
            else
            {
                return Platform.GenericDesktop;
            }
        }

        private static Platform GetPlatform()
        {
            CheckInitializationStatus();
            RuntimePlatform platform = Application.platform;
            if (platform == RuntimePlatform.WindowsEditor)
            {
                // Identifies if XR platform is coupled with editor.
                return GetWindowsEditorPlatform();
            }
            else if (platform == RuntimePlatform.WindowsPlayer)
            {
                // Identifies if XR platform is coupled with player.
                return GetWindowsPlatform();
            }
            else if (platform == RuntimePlatform.Android)
            {
                // Checks if device is a smartphone or a standalone XR device.
                return GetAndroidPlatform();

            }
            else if (platform == RuntimePlatform.LinuxPlayer)
            {
                return Platform.Linux;
            }
            else if (platform == RuntimePlatform.WindowsServer)
            {
                return Platform.WindowsServer;
            }
            else if (platform == RuntimePlatform.LinuxServer)
            {
                return Platform.LinuxServer;
            }
            else if (platform == RuntimePlatform.WebGLPlayer)
            {
                return Platform.DesktopWeb;
            }
            else
            {
                return GetGenericPlatform();
            }
        }
        public static async Task<Platform> GetPlatformAsync()
        {
            if (!s_isEverythingInitialized)
            {
                await Initialize();
            }
            return GetPlatform();
        }

        private static PlatformType GetPlatformType()
        {
            CheckInitializationStatus();
            if (GetPlatform() <= Platform.GenericDesktop)
            {
                return PlatformType.Desktop;
            }
            else if (GetPlatform() <= Platform.GenericXR)
            {
                return PlatformType.XR;
            }
            else if (GetPlatform() <= Platform.GenericMobile)
            {
                return PlatformType.Mobile;
            }
            else if (GetPlatform() <= Platform.WindowsServer)
            {
                return PlatformType.Server;
            }
            else
            {
                // You should never see this.
                Debug.LogError("[PlatformInfo] Platform type could not be identified. Undefined behavior.");
                return PlatformType.Unknown;
            }
        }
        public static async Task<PlatformType> GetPlatformTypeAsync()
        {
            if (!s_isEverythingInitialized)
            {
                await Initialize();
            }
            return GetPlatformType();
        }

        /// <summary>
        /// Determines if the used platform is an XR platform.
        /// </summary>
        /// <returns>Whether the used platform is an XR platform.</returns>
        public static bool IsXRPlatform()
        {
            CheckInitializationStatus();
            return GetPlatformType() == PlatformType.XR;
        }

        /// <summary>
        /// Determines if the used platform is an XR platform. Is an async method.
        /// </summary>
        public static async Task<bool> IsXRPlatformAsync()
        {
            if (!s_isEverythingInitialized)
            {
                await Initialize();
            }
            return IsXRPlatform();
        }

        /// <summary>
        /// Determines if the used platform is a server.
        /// </summary>
        /// <returns>Whether the used platform is a server.</returns>
        public static bool IsServer() => GetPlatformType() == PlatformType.Server;

        private static InputDevice? LookupInputDevice(XRNode node)
        {
            CheckInitializationStatus();
            if (!IsXRPlatform())
            {
                Debug.LogWarning("[PlatformInfo] This device is no XR device.");
                return null;
            }

            List<InputDevice> inputDevices = new();
            InputDevices.GetDevicesAtXRNode(node, inputDevices);
            if (inputDevices.Count > 0)
            {
                if (inputDevices.Count > 1)
                {
                    Debug.LogWarning($"[PlatformInfo] Found multiple input devices for {node}. Using the first one.");
                }
                return inputDevices[0];
            }

            return null;
        }

        private static XRHardwareType GetXRHardwareType()
        {
            CheckInitializationStatus();
            if (!IsXRPlatform())
            {
                Debug.LogWarning("[PlatformInfo] This device is no XR device.");
                return XRHardwareType.None;
            }

            InputDevice? inputDevice = LeftController == null ? LeftController : RightController;

            if (inputDevice != null)
            {
                string deviceName = inputDevice?.name;
                if (!string.IsNullOrEmpty(deviceName))
                {
                    if (deviceName.ToLower().Contains("oculus") || deviceName.ToLower().Contains("quest"))
                    {
                        return XRHardwareType.Quest;
                    }

                    if (deviceName.ToLower().Contains("pico"))
                    {
                        return XRHardwareType.Pico;
                    }
                }

                Debug.LogWarning("[PlatformInfo] Could not identify your XR headset, defaulting to Quest series.");
                return XRHardwareType.Quest;
            }

            if (IsHandTrackingEnabled())
            {
                return XRHardwareType.Handtracked;
            }

            return XRHardwareType.Unknown;
        }
        public static async Task<XRHardwareType> GetXRHardwareTypeAsync()
        {
            if (!s_isEverythingInitialized)
            {
                await Initialize();
            }
            return GetXRHardwareType();
        }

        /// <summary>
        ///     Returns whether the used XR hardware has handtracking enabled.
        /// </summary>
        /// <returns>Whether the used XR hardware has handtracking enabled.</returns>
        public static bool IsHandTrackingEnabled()
        {
            CheckInitializationStatus();
            // TODO implement me.
            return false;
        }

        /// <summary>
        /// Returns a readable description of the system the software is ran on. Yields information
        /// about the device (name and model), also its OS and what kind of platform family it belongs
        /// to.
        /// </summary>
        /// <returns>The device description as a string.</returns>
        public static string GetDeviceDescription()
        {
            CheckInitializationStatus();
            return "[PlatformInfo]\n" +
                   $"Device {SystemInfo.deviceName} ({SystemInfo.deviceModel}):\n" +
                   $"Platform: {GetPlatform()} ({GetPlatformType()}{(IsXRPlatform() ? $" ({GetXRHardwareType()})" : "")})\n" +
                   $"OS: {SystemInfo.operatingSystem}, ({SystemInfo.operatingSystemFamily} family)" +
                   (IsXRPlatform() ? $"\nCharacteristics: {Headset?.characteristics}" : "");
        }
    }

    /// <summary>
    /// Enumeration of all supported platforms.
    /// </summary>
    public enum Platform
    {
        // Desktop based platforms.
        WindowsEditor,
        Windows,
        Linux,
        DesktopWeb,
        GenericDesktop,

        // Immersive standalone XR platforms.
        WindowsXREditor,
        MetaQuest,
        Pico,
        WindowsXR,
        GenericXR,

        // Mobile platforms.
        Android,
        GenericMobile,

        // Server platforms.
        LinuxServer,
        WindowsServer,

        // Undefined platforms.
        Unknown
    }

    /// <summary>
    /// Enumeration of all supported platform families.
    /// </summary>
    public enum PlatformType
    {
        Desktop,
        XR,
        Mobile,
        Server,

        Unknown
    }

    /// <summary>
    /// Enumeration for the type of XR hardware that is used.
    /// </summary>
    public enum XRHardwareType
    {
        Handtracked,
        Quest,
        Pico,
        OpenXR,
        None,
        Unknown
    }
}