# Reference Integration

This sample includes the complete UI demo: scripts, scene, logo, input actions,
and the TextMesh Pro font resources used by the scene. All sample scripts use
the `Gamers.Client.Samples` namespace.

## Setup

The SDK supports Unity 2022.3 or later. The bundled scene was authored in Unity
6000.4.9f1; use that version or later for this URP scene.

Before importing the sample, install these sample-only dependencies:

- NativeWebSocket: Package Manager > Add package from git URL:
  `https://github.com/endel/NativeWebSocket.git#ea014c9ae534d56111962d96f89f8a046e302dc9`
- Unity UI (`com.unity.ugui`; this project uses 2.0.0, including TextMesh Pro).
- Input System (`com.unity.inputsystem`; this project uses 1.19.0).
- Universal RP (`com.unity.render-pipelines.universal`; this project uses 17.4.0).

The SDK installs Newtonsoft.Json through its declared package dependency.
Enable the Input System under Project Settings > Player > Active Input Handling
(Input System Package or Both), restarting the Editor if requested.

1. Select Gamers Client Helper in Package Manager and import Reference Integration.
2. Open `Scenes/SampleScene.unity` under the imported sample folder.
   Use a URP project, or assign the included `Settings/UniversalRP.asset` in
   Project Settings > Graphics and the applicable Quality levels.
3. Inspect the `WebSocketTransport` component. The scene uses the hosted evaluation
   endpoint; replace its URL with your game-server endpoint for your integration.
4. Enter Play mode and use the authentication, join, and leaderboard buttons.

`ReferenceTransport` is also included as a mock transport for standalone code
examples. The supplied scene uses `WebSocketTransport` for live server replies.

The included Text resources are the scene's required subset of TMP Essentials.
Their metadata is preserved. If your project already has TMP Essentials, retain
one copy of each asset/GUID when importing. The Liberation Sans license is in
`Text/Fonts/LiberationSans - OFL.txt`.

## Editing the sample

Edit the imported sample in your project's `Assets` folder. Keep every asset's
`.meta` file when moving it so scene and button references survive. These edits
do not modify the SDK tarball. Reimporting the sample restores the bundled
version and can overwrite local edits; preserve your changes before reimporting.
