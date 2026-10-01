# Reference Integration

This folder is the complete UI demo for this project: scripts, scene, logo,
input actions, URP settings and the TextMesh Pro font resources used by the
scene. All sample scripts use the `Gamers.Client.Samples` namespace.

It extends the **Reference Integration** sample shipped in `com.gamers.client`.
The package sample contains only `ReferenceIntegration.cs`, the mock
`ReferenceTransport.cs` and an assembly definition. The scene,
`WebSocketTransport` and the other files here exist only in this demo project.

## Setup

The SDK supports Unity 2022.3 or later. The scene was authored in Unity
6000.4.9f1; use that version or later for this URP scene.

Install these demo-only dependencies before opening the scene:

- NativeWebSocket: Package Manager > Add package from git URL:
  `https://github.com/endel/NativeWebSocket.git#ea014c9ae534d56111962d96f89f8a046e302dc9`
- Unity UI (`com.unity.ugui`; this project uses 2.0.0, including TextMesh Pro).
- Input System (`com.unity.inputsystem`; this project uses 1.19.0).
- Universal RP (`com.unity.render-pipelines.universal`; this project uses 17.4.0).

The SDK installs Newtonsoft.Json through its declared package dependency.
Enable the Input System under Project Settings > Player > Active Input Handling
(Input System Package or Both), restarting the Editor if requested.

1. Open `Scenes/SampleScene.unity` in this folder.
   Use a URP project, or assign the included `Settings/UniversalRP.asset` in
   Project Settings > Graphics and the applicable Quality levels.
2. Inspect the `WebSocketTransport` component. The scene uses the hosted evaluation
   endpoint; replace its URL with your game-server endpoint for your integration.
3. Enter Play mode and use the authentication, join, and leaderboard buttons.

`ReferenceTransport` is also included as a mock transport for standalone code
examples. The scene uses `WebSocketTransport` for live server replies.

The included Text resources are the scene's required subset of TMP Essentials.
Their metadata is preserved. If your project already has TMP Essentials, retain
one copy of each asset/GUID when copying this folder. The Liberation Sans license is in
`Text/Fonts/LiberationSans - OFL.txt`.

## Editing the demo

Edit the files in this folder directly. Keep every asset's `.meta` file when
moving or copying it so scene and button references survive. To use the demo in
another project, copy the whole folder with its `.meta` files.

Do not reimport the sample from Package Manager over this folder. The import only
brings the package's small sample, can overwrite or duplicate the scripts here,
and does not restore the scene or `WebSocketTransport`.
