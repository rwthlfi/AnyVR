# AnyVR

**AnyVR** is an open-source framework for creating multi-platform, multi-user XR experiences.

---

## Install

Add this to your manifest.json file:

```
    "rwth.lfi.anyvr": "https://git.rwth-aachen.de/LFI/xr/anyvr.git#package",
    "io.livekit.unity": "https://github.com/livekit/client-sdk-unity-web.git",
    "io.livekit.livekit-sdk": "https://github.com/livekit/client-sdk-unity.git",
    "com.firstgeargames.fishnet": "https://github.com/FirstGearGames/FishNet.git?path=Assets/FishNet",
    "com.firstgeargames.fishnet.bayou": "https://github.com/FirstGearGames/Bayou.git?path=FishNet/Plugins/Bayou",
```

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
