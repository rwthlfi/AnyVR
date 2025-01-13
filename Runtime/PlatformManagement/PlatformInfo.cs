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
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace AnyVR.PlatformManagement
{
    /// <summary>
    ///     Static information class to provide miscellaneous information about the platform the software is running on.
    /// </summary>
    public static class PlatformInfo
    {
        /// <summary>
        ///     The platform the software is running on.
        /// </summary>
        public static Platform Platform => GetPlatform();

        /// <summary>
        ///     The platform family the software is running on.
        /// </summary>
        public static PlatformFamily PlatformFamily => GetPlatformFamily();

        /// <summary>
        ///     The XR hardware that is used.
        /// </summary>
        public static XRHardwareType XRHardwareType => GetXRHardwareType();

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

        private static Platform GetPlatform()
        {
            throw new NotImplementedException();
        }

        private static PlatformFamily GetPlatformFamily()
        {
            if (IsXRPlatform())
            {
                return PlatformFamily.XR;
            }

            if (SystemInfo.deviceType == DeviceType.Handheld)
            {
                return PlatformFamily.Mobile;
            }

            if (SystemInfo.deviceType == DeviceType.Desktop)
            {
                return PlatformFamily.Desktop;
            }

            // You should never see this.
            return PlatformFamily.Unknown;
        }

        private static InputDevice? LookupInputDevice(XRNode node)
        {
            if (!IsXRPlatform())
            {
                Debug.LogWarning("[PlatformInfo] This device is no XR device.");
                return null;
            }

            List<InputDevice> inputDevices = new();
            InputDevices.GetDevicesAtXRNode(node, inputDevices);
            if (inputDevices.Count > 0)
            {
                return inputDevices[0];
            }

            return null;
        }

        private static XRHardwareType GetXRHardwareType()
        {
            if (!IsXRPlatform())
            {
                Debug.LogWarning("[PlatformInfo] This device is no XR device.");
                return XRHardwareType.None;
            }

            InputDevice? inputDevice = LeftController;
            if (inputDevice == null)
            {
                inputDevice = RightController;
            }

            if (inputDevice != null)
            {
                Debug.Log(
                    $"[PlatformInfo] XR-Controller {inputDevice?.name} with characteristics {inputDevice?.characteristics} identified.");
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

            //if (HasHandTrackingEnabled())
            //{
            //    return XRHardwareType.Handtracked;
            //}

            return XRHardwareType.Unknown;
        }

        /// <summary>
        ///     Determines if the used platform is an XR platform.
        /// </summary>
        /// <returns>Whether the used platform is an XR platform.</returns>
        public static bool IsXRPlatform()
        {
            return XRGeneralSettings.Instance.Manager.activeLoader != null;
        }

        /// <summary>
        ///     Returns whether the used XR hardware has handtracking enabled.
        /// </summary>
        /// <returns>Whether the used XR hardware has handtracking enabled.</returns>
        public static bool HasHandTrackingEnabled()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Returns a readable description of the system the software is ran on. Yields information
        ///     about the device (name and model), also its OS and what kind of platform family it belongs
        ///     to.
        /// </summary>
        /// <returns>The device description as a string.</returns>
        public static string GetDeviceDescription()
        {
            return "[PlatformInfo]\n" +
                   $"Device {SystemInfo.deviceName} ({SystemInfo.deviceModel}):\n" +
                   $"Platform: {PlatformFamily}" + (IsXRPlatform() ? $" ({XRHardwareType})" : "") + "\n" +
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

        // Undefined platforms.
        Unknown
    }

    /// <summary>
    ///     Enumeration of all supported platform families.
    /// </summary>
    public enum PlatformFamily
    {
        Desktop,
        XR,
        Mobile,

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