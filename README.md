# AnyVR

**AnyVR** is an open-source framework for creating multi-platform, multi-user XR experiences.

---

## Install

1. Open a project.

2. Install XR Plugin Management.

3. Add OpenXR Plug-in Provider.

4. In OpenXR Settings: Add 'Oculus Touch Controller Profile' to Enabled Interaction Profiles.

5. Import xr starter assets & spatial keyboard from XRI's samples catalogue.

6. Import the following packages with the Unity Package Manager (you can add these lines to your `manifest.json`).
   <br><br>**Important:** You will have to include `"io.livekit.unity"`, if and only if you aim to build for the web, and `"io.livekit.livekit-sdk"` otherwise. For more info, see below.

```
    "rwth.lfi.anyvr": "https://github.com/rwthlfi/AnyVR.git",
    "io.livekit.unity": "https://github.com/livekit/client-sdk-unity-web.git",
    "io.livekit.livekit-sdk": "https://github.com/livekit/client-sdk-unity.git",
    "com.firstgeargames.fishnet": "https://github.com/FirstGearGames/FishNet.git?path=Assets/FishNet",
    "com.firstgeargames.fishnet.bayou": "https://github.com/FirstGearGames/Bayou.git?path=FishNet/Plugins/Bayou",
```


7. Import LobbySetup Sample (Optional).
   This serves as example how to setup an online scene.

8. Enable AnyVR Tests (Optional):
   Add '"testables": "rwth.lfi.anyvr"' to the manifest.

### LiveKit

The project integrates LiveKit for real-time voice communication.
To support both Standalone and WebGL builds, the following LiveKit packages are included:

- [client-sdk-unity](https://github.com/livekit/client-sdk-unity) (Standalone)
- [client-sdk-unity-web](https://github.com/livekit/client-sdk-unity-web) (WegGL)

#### Handling Namespace Conflicts

Both packages define the same namespace, which leads to **type ambiguity errors**.

You can resolve this issue in two ways:

1. Use the menu option AnyVr > Set Target to adjust the assembly definitions in the PackageCache automatically.
2. Only include the LiveKit package for the current build target via the PackageManager.

