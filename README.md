# Unity Gamers Demo App

A Unity sample project demonstrating how to integrate the Gamers client SDK.

## Overview

This project provides a simple UI-driven demo for authenticating users, joining tournaments and events, and displaying leaderboards via the `Gamers.Client.Samples` integration.

## Main Components

- **`GamersButtonBridge.cs`** — Connects UI input fields to `ReferenceIntegration` for:
  - Email-based authentication
  - Verification code submission
  - Tournament and event joining
  - Leaderboard display

- **`InGameLogger.cs`** — Captures Unity console logs and displays them in a `TMP_Text` UI element for in-game debugging.

## Network Transport

The demo uses **`WebSocketTransport`** (from the `Gamers Client Helper` sample package) as the concrete `IGamersTransport` implementation. It handles:

- WebSocket connection to the Gamers backend (`wss://.../ws/gamers`)
- JSON message serialization and deserialization
- Server reply routing for auth, event/tournament join results, leaderboard snapshots, and errors
- Automatic dispatch of the WebSocket message queue


## Usage

Open the project in Unity, load the sample scene, and use the on-screen inputs to interact with the Gamers integration. The `ReferenceIntegration` component communicates with the backend through the `WebSocketTransport` sample.

## SDK Integration

1. Add the `com.gamers.client` package to your Unity project via Package Manager (`Window > Package Manager > Add package from git URL...`).
2. Provide an `IGamersTransport` implementation — use the included `WebSocketTransport` and set its `Url` and optional `Player Id Header` in the Inspector, or implement your own transport.
3. Create a `GamersClientFlow` with the transport, optionally setting `RequestTimeout`.
4. Subscribe to the flow events (`OnAuthCodeRequested`, `OnAuthenticated`, `OnTournamentJoined`, `OnEventJoined`, `OnLeaderboardUpdated`, `OnError`) and call the async methods (`RequestAuthAsync`, `SubmitCodeAsync`, `JoinTournamentAsync`, `JoinEventAsync`, `RequestLeaderboardAsync`).
5. The included `ReferenceIntegration` component is a sample MonoBehaviour that demonstrates this setup in `Start`.
