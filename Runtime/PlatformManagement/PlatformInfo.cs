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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace AnyVR.PlatformManagement
{
    /// <summary>
    ///     Static class to provide miscellaneous information about the platform the software is running on.
    /// </summary>
    public static class PlatformInfo
    {
        // /// <summary>
        // /// The platform the software is running on.
        // /// </summary>
        // public static Platform Platform => GetPlatform();
        //
        // /// <summary>
        // /// The platform family the software is running on.
        // /// </summary>
        // public static PlatformType PlatformType => GetPlatformType();

        // /// <summary>
        // /// The XR hardware that is used.
        // /// </summary>
        // public static XRHardwareType XRHardwareType => GetXRHardwareType();

        /// <summary>
        ///     Reference to the HMD, if there is one. Is null otherwise.
        /// </summary>
        public static InputDevice? Headset => LookupInputDevice(XRNode.Head);

        /// <summary>
        ///     Reference to the left XR controller, if there is one. Is null otherwise.
        /// </summary>
        public static InputDevice? LeftController => LookupInputDevice(XRNode.LeftHand);

        /// <summary>
        ///     Reference to the right XR controller, if there is one. Is null otherwise.
        /// </summary>
        public static InputDevice? RightController => LookupInputDevice(XRNode.RightHand);

        private static void CheckInitializationStatus()
        {
            const string msg = "[PlatformInfo] PlatformInfo is not fully initialized yet. Wait until PlatformManager.IsXrStartupAttempted is true before calling this method.";
            Assert.IsTrue(PlatformManager.Instance.IsXrStartupAttempted, msg);
        }

        private static Platform GetAndroidPlatform()
        {
            CheckInitializationStatus();
            if (!XRSettings.isDeviceActive)
                return Platform.Android;

            if (XRGeneralSettings.Instance.Manager.activeLoader.name.ToLower().Contains("meta"))
            {
                return Platform.MetaQuest;
            }
            if (XRGeneralSettings.Instance.Manager.activeLoader.name.ToLower().Contains("pico"))
            {
                return Platform.Pico;
            }
            return Platform.GenericXR;
        }

        private static Platform GetGenericPlatform()
        {
            CheckInitializationStatus();

            if (XRSettings.isDeviceActive)
            {
                return Platform.GenericXR;
            }
            if (SystemInfo.deviceType == DeviceType.Handheld)
            {
                return Platform.GenericMobile;
            }

            return Platform.GenericDesktop;
        }

        private static Platform GetPlatform()
        {
            CheckInitializationStatus();
            RuntimePlatform platform = Application.platform;

            return platform switch
            {
                RuntimePlatform.WindowsEditor => XRSettings.isDeviceActive ? Platform.WindowsXREditor : Platform.WindowsEditor,
                RuntimePlatform.WindowsPlayer => XRSettings.isDeviceActive ? Platform.WindowsXR : Platform.Windows,
                RuntimePlatform.Android => GetAndroidPlatform(), // Checks if device is a smartphone or a standalone XR device.
                RuntimePlatform.LinuxPlayer => Platform.Linux,
                RuntimePlatform.WindowsServer => Platform.WindowsServer,
                RuntimePlatform.LinuxServer => Platform.LinuxServer,
                RuntimePlatform.WebGLPlayer => Platform.DesktopWeb,
                _ => GetGenericPlatform()
            };
        }

        public static PlatformType GetPlatformType()
        {
            CheckInitializationStatus();

            Platform platform = GetPlatform();
            switch (platform)
            {
                case <= Platform.GenericDesktop:
                    return PlatformType.Desktop;
                case <= Platform.GenericXR:
                    return PlatformType.XR;
                case <= Platform.GenericMobile:
                    return PlatformType.Mobile;
                case <= Platform.WindowsServer:
                    return PlatformType.Server;
                default:
                    // You should never see this.
                    Debug.LogError("[PlatformInfo] Platform type could not be identified. Undefined behavior.");
                    return PlatformType.Unknown;
            }
        }

        /// <summary>
        ///     Determines if the used platform is an XR platform.
        /// </summary>
        /// <returns>Whether the used platform is an XR platform.</returns>
        public static bool IsXRPlatform()
        {
            CheckInitializationStatus();
            return GetPlatformType() == PlatformType.XR;
        }

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
            switch (inputDevices.Count)
            {
                case <= 0:
                    return null;
                case > 1:
                    Debug.LogWarning($"[PlatformInfo] Found multiple input devices for {node}. Using the first one.");
                    break;
            }
            return inputDevices[0];

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

            if (inputDevice == null)
                return IsHandTrackingEnabled() ? XRHardwareType.Handtracked : XRHardwareType.Unknown;

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
        ///     Returns a readable description of the system the software is ran on. Yields information
        ///     about the device (name and model), also its OS and what kind of platform family it belongs
        ///     to.
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
    ///     Enumeration of all supported platforms.
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
    ///     Enumeration of all supported platform families.
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
    ///     Enumeration for the type of XR hardware that is used.
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
