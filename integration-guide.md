# Gamers Client Integration Guide

Package: `com.gamers.client`<br>
Current package version: `1.0.0`<br>
Client-to-server protocol version: `1`

The **Gamers Client Helper** is a transport-agnostic Unity package for a server-authoritative Gamers integration. It provides five awaitable player operations, typed message contracts, a state machine, error handling, and read-only display models.

- [C# API reference](api-reference.md)
- [Changelog](../CHANGELOG.md)

## 1. Requirements

| Requirement | Current value |
|---|---|
| Unity package minimum | Unity `2022.3.0`, as declared in `package.json` |
| API compatibility | .NET Standard 2.1 |
| Runtime dependency | `com.unity.nuget.newtonsoft-json` `3.2.1` |
| Game-server | Required; the package does not call the Gamers Game API |
| Client transport | Required implementation of `IGamersTransport` |
| Gamers configuration | A registered game and server-side Game API credentials |
| Editor attribution | Gamers developer ID entered under **Edit → Project Settings → Gamers** |

The developer's game-server owns Game API credentials, player authentication state, score validation, and all administrative operations. Never put a Game API key or player JWT in a client project.

## 2. Compatibility

| Target | Support |
|---|---|
| Unity | 2022.3 LTS or newer |
| Platforms | Windows, macOS, 64-bit Android (ARM64), and iOS |
| Scripting backends | Mono and IL2CPP |
| Render pipelines | Built-in, URP, and HDRP; the runtime contains no rendering code |

The package includes a `link.xml` that preserves `GamersClient.Runtime` and `GamersClient.Samples` from IL2CPP managed-code stripping.

## 3. Architecture and security boundary

```text
Unity client (GamersClientFlow + IGamersTransport)
  │ developer-owned networking
  ▼
Developer's game-server (Gamers Server SDK or equivalent)
  │ X-API-KEY and server-side player JWT
  ▼
Gamers Game API
```

The Unity client:

- requests authentication, event entry, tournament entry, and leaderboard data through the game-server
- contains no Game API URL, API key, or player JWT
- never submits trusted scores or results
- never creates, updates, completes, rewards, or cancels events or tournaments

The game-server:

- authenticates its own client session and derives the player identity from that session
- stores the Game API key and player JWT outside the client
- validates client input and authoritative gameplay results
- calls the Game API
- maps internal failures to safe `ErrorReply` values
- echoes the request `correlationId` and `protocolVersion` on every terminal reply

Event and tournament state are separate. Do not use an event identifier in a tournament message or infer one competition's state from the other.

## 4. Installation

Public releases use Unity Package Manager through the Unity Asset Store.

### Unity Asset Store

After publication, install the package from its Unity Asset Store listing through **Window → Package Manager**.

### Local package

For an evaluation package supplied by Gamers:

1. Extract the package directory.
2. Open **Window → Package Manager**.
3. Select **+ → Add package from disk…**.
4. Select `package.json` at the root of the extracted package.

UPM resolves `com.unity.nuget.newtonsoft-json` automatically.

### Tarball

If Gamers supplies a `.tgz` package, import it through **Package Manager → Add package from tarball…**.

### Demo app

A complete, UI-driven sample project is available at [`gamers-bet/gamers-unity-client-sdk-demo-app`](https://github.com/gamers-bet/gamers-unity-client-sdk-demo-app). It includes a sample scene, a `WebSocketTransport` reference implementation, and prebuilt app packages for Windows, macOS, Android, and iOS.

## 5. Required game-server protocol

Every request and reply uses this envelope:

```json
{
  "type": "tournament:join",
  "protocolVersion": 1,
  "correlationId": "generated-by-gamers-client-flow"
}
```

The game-server must follow these rules:

1. Echo the request `correlationId` on exactly one terminal reply, including errors.
2. Echo `protocolVersion: 1`.
3. Omit `correlationId` only for a deliberate unsolicited push.
4. Return player-safe errors. Never forward stack traces, credentials, internal URLs, or raw provider errors.
5. Derive the player from the authenticated game-server session, not from a client-supplied player identifier.
6. Keep event and tournament handlers, identifiers, and authorization checks separate.

A missing terminal reply becomes `TimeoutException`. A duplicate or late reply cannot complete a newer request. A mismatched reply type becomes `GamersClientException` with code `PROTOCOL_MISMATCH`. A version other than `1` becomes `UNSUPPORTED_PROTOCOL_VERSION`.

## 6. Implement `IGamersTransport`

The package does not choose a networking stack. Implement `IGamersTransport` with Netcode for GameObjects, Mirror, Photon, WebSockets, REST, or the game's existing client/server channel.

```csharp
using Gamers.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public sealed class MyNetworkTransport : IGamersTransport
{
    public event Action<GamersServerReply> ReplyReceived;

    public async Task SendAsync(
        GamersClientMessage message,
        CancellationToken ct = default)
    {
        var json = JsonConvert.SerializeObject(message);
        await MyGameServerConnection.SendAsync(json, ct);
    }

    public void OnServerMessage(string json)
    {
        var envelope = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        var type = envelope["type"]?.ToString();

        GamersServerReply reply = type switch
        {
            "auth:code-requested" => JsonConvert.DeserializeObject<AuthCodeRequestedReply>(json),
            "auth:result" => JsonConvert.DeserializeObject<AuthResultReply>(json),
            "event:join-result" => JsonConvert.DeserializeObject<EventJoinResultReply>(json),
            "tournament:join-result" => JsonConvert.DeserializeObject<TournamentJoinResultReply>(json),
            "tournament:leaderboard" => JsonConvert.DeserializeObject<LeaderboardSnapshotReply>(json),
            "error" => JsonConvert.DeserializeObject<ErrorReply>(json),
            _ => new ErrorReply
            {
                Code = "UNKNOWN_MESSAGE",
                Message = $"Unknown message type: {type}"
            }
        };

        RunOnUnityMainThread(() => ReplyReceived?.Invoke(reply));
    }
}
```

`SendAsync` completes when the message has been accepted by the transport. The operation returned by `GamersClientFlow` completes later, after `ReplyReceived` delivers the matching reply.

If networking callbacks run on a background thread, `RunOnUnityMainThread` must marshal to Unity's main thread before raising `ReplyReceived`.

## 7. Use `GamersClientFlow`

Create one flow for the relevant client session, subscribe to optional notifications, and dispose it when the scene or session ends.

```csharp
using Gamers.Client;
using System;
using System.Threading;
using UnityEngine;

public sealed class GamersExample : MonoBehaviour
{
    private GamersClientFlow _flow;
    private CancellationTokenSource _lifetime;

    private void Start()
    {
        _lifetime = new CancellationTokenSource();
        _flow = new GamersClientFlow(new MyNetworkTransport());

        _flow.OnStateChanged += state => Debug.Log($"State: {state}");
        _flow.OnLeaderboardUpdated += UpdateLeaderboardUi;
        _flow.OnError += error => Debug.LogError($"{error.Code}: {error.Message}");
    }

    private void OnDestroy()
    {
        _lifetime.Cancel();
        _lifetime.Dispose();
        _flow?.Dispose();
    }

    public async void OnEmailSubmitted(string email)
    {
        try
        {
            var reply = await _flow.RequestAuthAsync(email, _lifetime.Token);
            ShowCodeEntry(reply.Email);
        }
        catch (ArgumentException ex)
        {
            ShowError(ex.Message);
        }
        catch (TimeoutException)
        {
            ShowError("The game-server did not respond.");
        }
        catch (GamersClientException ex)
        {
            ShowError($"{ex.Code}: {ex.Message}");
        }
        catch (OperationCanceledException)
        {
        }
    }
}
```

Do not use `async void` outside Unity event handlers. Application services should return `Task` so callers can await and test failures.

### Operations

| Method | Client message | Successful reply |
|---|---|---|
| `RequestAuthAsync(email, ct)` | `auth:request` | `AuthCodeRequestedReply` |
| `SubmitCodeAsync(email, code, ct)` | `auth:submit-code` | `AuthResultReply` |
| `JoinEventAsync(eventId, ct)` | `event:join` | `EventJoinResultReply` |
| `JoinTournamentAsync(tournamentId, ct)` | `tournament:join` | `TournamentJoinResultReply` |
| `RequestLeaderboardAsync(tournamentId, metricId, offset, limit, ct)` | `tournament:leaderboard` | `LeaderboardSnapshotReply` |

See the [C# API reference](api-reference.md) for every parameter, return value, property, event, constructor, enum value, wire field, and exception.

## 8. Error handling

Handle errors at the awaited operation. `OnError` is a notification for other UI or diagnostics and is also used for unsolicited server errors.

| Exception | Action |
|---|---|
| `ArgumentException` | Correct local input before sending. |
| `GamersClientException` | Branch on `Code`; retry only when `Retryable` is true. Include `CorrelationId` in diagnostics. |
| `TimeoutException` | Offer retry or reconnect. Confirm the game-server returns one terminal reply for every request. |
| `OperationCanceledException` | Treat as expected when leaving the scene or cancelling the operation. |
| `ObjectDisposedException` | Stop using the disposed flow and create a new session flow if needed. |
| Transport exception | Apply transport-specific reconnect or offline behavior. |

Client-generated `GamersClientException.Code` values are:

- `AUTH_FAILED`
- `JOIN_EVENT_FAILED`
- `JOIN_TOURNAMENT_FAILED`
- `PROTOCOL_MISMATCH`
- `UNSUPPORTED_PROTOCOL_VERSION`

The game-server can add its own documented, player-safe error codes.

## 9. Lifecycle, concurrency, and threading

- Call public flow methods from Unity's main thread.
- Raise `IGamersTransport.ReplyReceived` on the main thread.
- One flow can have multiple outstanding requests because correlation identifiers route each reply independently.
- Avoid starting overlapping operations that compete for the same UI state unless the UI handles the resulting state transitions.
- `Dispose()` unsubscribes from the transport and cancels all outstanding operations.
- The flow does not dispose `IGamersTransport`; the caller owns its connection lifecycle.
- Set `RequestTimeout` before starting operations. Zero or a negative value disables timeout handling and should be used only when another bounded timeout exists.

## 10. Verified Solutions attribution

The package includes Unity's Verified Solutions attribution implementation in the Editor-only `GamersClient.Editor` assembly.

1. Open **Edit → Project Settings → Gamers**.
2. Enter the non-secret Gamers developer ID from the developer portal.
3. Press Enter or select **Send Attribution Event**.
4. The page reports when the event has been accepted once for the project.

The identifier is stored in per-user `EditorUserSettings`. It is not read by runtime code and is not included in player builds. If Editor Analytics is disabled, nothing is sent. Attribution failure does not block or change the runtime client flow.

`GamersAttributionSettingsProvider` and `VSAttribution` are implementation details. Customers should use the Project Settings page rather than call those classes directly.

## 11. Security checklist

Before release, verify:

- [ ] No Game API key, player JWT, or direct Game API URL appears in the client project or player build.
- [ ] The game-server authenticates the client session and derives the player identifier itself.
- [ ] Score and result submission happens only from trusted game-server logic.
- [ ] Event and tournament authorization paths remain separate.
- [ ] Every request receives exactly one terminal reply with the same correlation identifier and protocol version.
- [ ] Errors sent to the client are safe and contain no internal exception details.
- [ ] Client UI treats DTOs as display data rather than trusted authority.
- [ ] Production transport uses TLS and validates certificates normally; HTTP/REST transports use HTTPS to satisfy iOS App Transport Security.
- [ ] Logs avoid email addresses, verification codes, tokens, and other sensitive values.

## 12. Known limitations

### No production transport is included

`IGamersTransport` must be implemented with the game's authenticated networking channel. The bundled Reference Integration uses a fake transport and is a contract example, not production networking.

### No event or tournament discovery

The current flow joins competitions by identifier but does not list them. Supply available event and tournament identifiers through the game's own catalogue or backend. Keep event and tournament identifiers separate.

### No result or prize notification contract

Push results and prize notifications through the game's existing server channel. Do not submit or trust results from client code.

## 13. Reproducing failure paths

The package's fake or test transport can exercise client behavior without a real tenant:

| Case | Reproduction | Expected result |
|---|---|---|
| Invalid input | Call an operation with an empty required identifier or email. | `ArgumentException` before a message is sent. |
| Timeout | Set a short positive `RequestTimeout`; accept the send and raise no reply. | `TimeoutException`; pending operation count returns to zero. |
| Cancellation | Pass a cancelled token or cancel it while waiting. | `OperationCanceledException`; pending operation is removed. |
| Server error | Raise `ErrorReply` with the request correlation identifier and protocol version `1`. | `GamersClientException`; `OnError` also fires. |
| Wrong reply type | Answer a join request with an auth reply using the matching correlation identifier. | `GamersClientException` with `PROTOCOL_MISMATCH`. |
| Wrong protocol | Return a matching reply with a version other than `1`. | `GamersClientException` with `UNSUPPORTED_PROTOCOL_VERSION`. |
| Unsuccessful auth | Return `AuthResultReply.Success == false`. | `GamersClientException` with `AUTH_FAILED`. |
| Unsuccessful event join | Return `EventJoinResultReply.Success == false`. | `GamersClientException` with `JOIN_EVENT_FAILED`. |
| Unsuccessful tournament join | Return `TournamentJoinResultReply.Success == false`. | `GamersClientException` with `JOIN_TOURNAMENT_FAILED`. |

Backend-dependent expired-code, closed-competition, and insufficient-balance scenarios require controlled test fixtures.

## 14. Dependency and data disclosure

Runtime dependency:

- `com.unity.nuget.newtonsoft-json` `3.2.1`

Player data:

- The package sends player email addresses and verification codes only through `IGamersTransport` to the developer's game-server. It does not persist them to disk.
- Treat email addresses, verification codes, user identifiers, display names, and leaderboard identifiers as sensitive or personal data where applicable. Do not include them in logs or diagnostics.
- Use only non-sensitive, intentionally public aliases and opaque identifiers in leaderboard responses. Never expose an email address, wallet address, authentication identifier, or internal account identifier as a leaderboard identity.
- The developer owns the client transport and game-server and is responsible for transport security, access controls, retention, deletion, and appropriate player-facing privacy notices.

Editor-only telemetry:

- Unity Verified Solutions attribution sends action name, partner name, and the non-secret Gamers developer identifier through Unity Editor Analytics.
- It respects the user's Editor Analytics preference.
- No player email, verification code, Game API key, JWT, or gameplay result is sent by the attribution integration.

The Gamers backend service is governed separately from the Unity package. Review the service terms presented during Gamers account registration.

## 15. Support

Contact `support@gamers.dev`. Include:

- package version
- Unity version, platform, architecture, and scripting backend
- transport implementation and connection state
- exception type and `GamersClientException.Code`
- request `CorrelationId`
- non-secret game-server request or trace identifier when available

Do not send API keys, JWTs, session tokens, cookies, verification codes, email addresses, or other player credentials in a support request.
