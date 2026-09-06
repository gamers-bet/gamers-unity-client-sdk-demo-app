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

The demo uses **`WebSocketTransport`** as its concrete `IGamersTransport` implementation. This script is **part of this demo project only** — it is not shipped inside the `com.gamers.client` package. It lives alongside the imported package sample at `Assets/Samples/Gamers Client Helper/1.0.0/Reference Integration/` and handles:

- WebSocket connection to the Gamers backend (`wss://.../ws/gamers`)
- JSON message serialization and deserialization
- Server reply routing for auth, event/tournament join results, leaderboard snapshots, and errors
- Automatic dispatch of the WebSocket message queue

### External dependency

The WebSocket transport uses `com.endel.nativewebsocket` from GitHub. Install it via **Window → Package Manager → + → Add package from git URL...** with:

```
https://github.com/endel/NativeWebSocket.git#upm
```

The `com.gamers.client` package itself does not require this package; it is used only by this demo's `WebSocketTransport`. See [docs/client-guide.md](docs/client-guide.md#external-dependency-demo-only) for details.

### Relationship to the packaged sample

The package ships a **Reference Integration** sample containing `ReferenceIntegration.cs` and `ReferenceTransport.cs`, where `ReferenceTransport` is a mock transport that logs outgoing JSON and simulates replies so the flow can be exercised with no server. This demo imports that sample and then adapts it for live traffic:

- `WebSocketTransport.cs` is added by this demo and is not in the package.
- `ReferenceIntegration.cs` is modified to resolve the transport via `GetComponent<WebSocketTransport>()` instead of constructing a `ReferenceTransport`.
- `GamersClient.Samples.asmdef` gains an `endel.nativewebsocket` assembly reference.

Re-importing the sample from Package Manager overwrites these edits and reverts the demo to the mock transport.

## Usage

Open the project in Unity, load the sample scene, and use the on-screen inputs to interact with the Gamers integration. The `ReferenceIntegration` component communicates with the backend through this demo's `WebSocketTransport`.

> **Note for reviewers:** This demo is preconfigured to connect to a hosted test server (`wss://reference-server-dev-internal-testing.up.railway.app/ws/gamers`) provided for evaluation purposes. In a production integration, set the `Url` field on the `WebSocketTransport` component to your own game-server WebSocket endpoint.

## Installation & Getting Started

1. Add the `com.gamers.client` package to your Unity project via Package Manager (`Window > Package Manager > Add package from git URL...`).
2. Provide an `IGamersTransport` implementation — write your own, or copy this demo's `WebSocketTransport` and set its `Url` and optional `Player Id Header` in the Inspector. The package's own sample ships `ReferenceTransport`, a mock transport intended for local experimentation rather than production use.
3. Create a `GamersClientFlow` with the transport, optionally setting `RequestTimeout`.
4. Subscribe to the flow events (`OnAuthCodeRequested`, `OnAuthenticated`, `OnTournamentJoined`, `OnEventJoined`, `OnLeaderboardUpdated`, `OnError`) and call the async methods (`RequestAuthAsync`, `SubmitCodeAsync`, `JoinTournamentAsync`, `JoinEventAsync`, `RequestLeaderboardAsync`).
5. The `ReferenceIntegration` component from the package sample is a MonoBehaviour that demonstrates this setup in `Start`.

## Verified Solutions Attribution

This project uses the Unity Verified Solutions Attribution integration included with the `com.gamers.client` package. For implementation details, privacy, and verification steps, see [docs/unity-verified-solutions-attribution.md](docs/unity-verified-solutions-attribution.md).
