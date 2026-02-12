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
    "rwth.lfi.anyvr": "https://github.com/rwthlfi/AnyVR.git"
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