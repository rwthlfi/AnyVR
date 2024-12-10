# AnyVR

**AnyVR** is an open-source framework for creating multi-platform, multi-user XR experiences.

---

## Download

```bash
git clone https://git.rwth-aachen.de/LFI/xr/anyvr
```

- Open the Project with the Unity Editor.

- Import these samples from the 'XR Interaction Toolkit' Package via the PackageManager.

  - Starter Assets
  - Spatial Keyboard

## Platform Support

- [x] Windows
- [x] Android
- [x] WebGL

## Dependencies

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

## Building

### Client Builds

1. **Enable OpenXR** in the **XR Plug-In Management** tab in the project settings.
2. Select the desired platform (Windows, Android, or WebGL) in the Build Settings.
3. Build the project

### Server Build

- **Disable OpenXR** in the **XR Plug-In Management** tab in the project settings.
- Switch to the 'Dedicated Server' build target and select 'Linux' as the target platform.
- Build the server.

#### Docker Deployment

1. Build the server and copy the provided Dockerfile in the build directory.
2. Build the docker image.
3. Make sure to expose the port 7777 for both tcp and udp when deploying.
