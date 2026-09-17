# Unity Gamers Demo App

A Unity sample project demonstrating how to integrate the Gamers client SDK.

## Documentation

- [Client guide](docs/client-guide.md) — package overview, architecture, requirements
- [Integration guide](docs/integration-guide.md) — canonical integration walkthrough
- [C# API reference](docs/api-reference.md) — every public type, event, and enum
- [Verified Solutions attribution](docs/unity-verified-solutions-attribution.md)

## Overview

This project provides a simple UI-driven demo for authenticating users, joining tournaments and events, and displaying leaderboards via the `Gamers.Client.Samples` integration.

## Compatibility

- **Unity:** 2022.3 LTS or later
- **Supported platforms:** Windows, macOS, Android, and iOS
- **Package:** `com.gamers.client`

The SDK is designed for Unity projects that integrate with the Gamers.bet platform for user authentication, paid tournament and event entry, and leaderboard functionality.

## Main Components

- **`GamersButtonBridge.cs`** — Connects UI input fields to `ReferenceIntegration` for:
  - Email-based authentication
  - Verification code submission
  - Tournament and event joining
  - Leaderboard display

- **`InGameLogger.cs`** — Captures Unity console logs and displays them in a `TMP_Text` UI element for in-game debugging.

## Network Transport

The demo uses **`WebSocketTransport`** as its concrete `IGamersTransport` implementation. It is bundled in the main package’s **Reference Integration** sample, under `Scripts/`, and handles:

- WebSocket connection to the Gamers backend (`wss://.../ws/gamers`)
- JSON message serialization and deserialization
- Server reply routing for auth, event/tournament join results, leaderboard snapshots, and errors
- Automatic dispatch of the WebSocket message queue

### External dependency

The WebSocket transport uses `com.endel.nativewebsocket` from GitHub. Install it via **Window → Package Manager → + → Add package from git URL...** with:

```
https://github.com/endel/NativeWebSocket.git#upm
```

The runtime SDK does not require NativeWebSocket; install it before importing the complete Reference Integration sample. See [docs/client-guide.md](docs/client-guide.md#external-dependency-demo-only) for details.

### Relationship to the packaged sample

The main package ships the complete **Reference Integration** sample: scene, UI scripts, WebSocket transport, logo, input actions and required font resources. `ReferenceTransport` remains available for mock examples; the included scene uses `WebSocketTransport`.

The editable demo lives at `Assets/Samples/Gamers Client Helper/1.0.0/Reference Integration/`. Run `node scripts/package-client.mjs` to synchronize it into `PackageSource/com.gamers.client/Samples~/ReferenceIntegration` and rebuild the tarball. Reimporting the rebuilt sample preserves the complete demo.

See the [sample README](Assets/Samples/Gamers%20Client%20Helper/1.0.0/Reference%20Integration/README.md) for dependency setup. The SDK retains its Unity 2022.3 minimum; the bundled scene was authored in Unity 6000.4.9f1.

## Usage

Open the project in Unity, load the sample scene, and use the on-screen inputs to interact with the Gamers integration. The `ReferenceIntegration` component communicates with the backend through this demo's `WebSocketTransport`.

> **Note for reviewers:** This demo is preconfigured to connect to a hosted test server (`wss://reference-server-dev-internal-testing.up.railway.app/ws/gamers`) provided for evaluation purposes. In a production integration, set the `Url` field on the `WebSocketTransport` component to your own game-server WebSocket endpoint.

## Installation & Getting Started

1. Add the [`com.gamers.client`](https://github.com/gamers-bet/gamers-unity-client-sdk-demo-app/raw/main/com.gamers.client-1.0.0.tgz) package to your Unity project via Package Manager (`Window > Package Manager > Add package from tarball...`).
2. Provide an `IGamersTransport` implementation — write your own, or copy this demo's `WebSocketTransport` and set its `Url` and optional `Player Id Header` in the Inspector. The package sample includes this same WebSocket transport and also provides `ReferenceTransport` for mock examples.
3. Create a `GamersClientFlow` with the transport, optionally setting `RequestTimeout`.
4. Subscribe to the flow events (`OnAuthCodeRequested`, `OnAuthenticated`, `OnTournamentJoined`, `OnEventJoined`, `OnLeaderboardUpdated`, `OnError`) and call the async methods (`RequestAuthAsync`, `SubmitCodeAsync`, `JoinTournamentAsync`, `JoinEventAsync`, `RequestLeaderboardAsync`).
5. The `ReferenceIntegration` component from the package sample is a MonoBehaviour that demonstrates this setup in `Start`.

## Verified Solutions Attribution

This project uses the Unity Verified Solutions Attribution integration included with the `com.gamers.client` package. For implementation details, privacy, and verification steps, see [docs/unity-verified-solutions-attribution.md](docs/unity-verified-solutions-attribution.md).
