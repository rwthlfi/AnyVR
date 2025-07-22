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

```
    "rwth.lfi.anyvr": "https://github.com/rwthlfi/AnyVR.git",
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

#### Namespace Conflict

Both packages define the same namespace, which leads to **type ambiguity errors**.

The `LiveKitDependencyHandler.cs` editor script resolves this by including only one package based on your build target.

Conditional compilation ensures no editor errors occur.

> **Note**: The voicechat is disabled in the editor.
