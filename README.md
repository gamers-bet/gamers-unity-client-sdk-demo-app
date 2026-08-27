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

## Requirements

- Unity 6000.x or later
- `Gamers.Client.Samples` package
- TextMeshPro

## Usage

Open the project in Unity, load the sample scene, and use the on-screen inputs to interact with the Gamers integration. The `ReferenceIntegration` component communicates with the backend through the `WebSocketTransport` sample.
