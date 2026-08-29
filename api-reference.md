# Gamers Client C# API Reference

Package: `com.gamers.client`<br>
Runtime assembly: `GamersClient.Runtime`<br>
Editor assembly: `GamersClient.Editor`<br>
Protocol version: `1`

This reference documents every public type intended for use in the Gamers Unity client package. The package is a transport-agnostic helper for a server-authoritative integration. It never calls the Gamers Game API, stores a player JWT, or contains a Game API key.

For installation, architecture, compatibility, examples, limitations, and known issues, see the [integration guide](integration-guide.md). See the package-root [changelog](../CHANGELOG.md) for version history.

## 1. API conventions

### Awaitable operations

The five `GamersClientFlow` operations return `Task<TReply>`. A task completes only after the game-server sends the matching terminal reply. Completion of `IGamersTransport.SendAsync` means only that the transport accepted the outgoing message.

Every outgoing message receives a unique `correlationId`. The game-server must copy that value and protocol version `1` into exactly one terminal reply. A reply with no outstanding correlation identifier is handled as an unsolicited server push.

### Threading

Create and call `GamersClientFlow` on Unity's main thread. `IGamersTransport.ReplyReceived` must also be raised on the main thread. A transport that receives data on another thread must marshal the callback to Unity's synchronization context first.

### Nullability and serialization

The package targets Unity's .NET Standard 2.1 profile and does not enable nullable reference types. A reference-type property can therefore contain `null` when a server omits an optional field. Contracts use Newtonsoft.Json and the JSON property names shown below.

### Sensitive data

Email addresses, verification codes, user identifiers, display names, and leaderboard identifiers can be sensitive or personal data. Use encrypted transport, do not write these values to logs, and do not persist verification codes. Leaderboard responses must use only non-sensitive, intentionally public aliases and opaque public identifiers.

### Operation exceptions

Unless a method section says otherwise, an operation can produce:

| Exception | Condition |
|---|---|
| `ArgumentException` | A required string argument is empty or whitespace. |
| `ObjectDisposedException` | The flow was disposed before the request could be sent. |
| `OperationCanceledException` | The caller's cancellation token was cancelled, or `Dispose()` cancelled the outstanding operation. |
| `TimeoutException` | No terminal reply arrived within `RequestTimeout`. |
| `GamersClientException` | The server returned `ErrorReply`, returned an unsuccessful result, used an unsupported protocol version, or returned the wrong reply type. |
| Transport-defined exception | `IGamersTransport.SendAsync` failed. Transport exceptions propagate unchanged. |

`GamersClientException.Code` is stable for programmatic decisions. Do not branch on message text.

## 2. `GamersClientFlow`

Namespace: `Gamers.Client`

High-level state machine and request/reply coordinator.

### Constructor

```csharp
public GamersClientFlow(IGamersTransport transport)
```

| Parameter | Description |
|---|---|
| `transport` | Non-null game client-to-server transport. The flow subscribes to its `ReplyReceived` event but does not own or dispose it. |

Throws `ArgumentNullException` when `transport` is `null`.

### Constants and properties

| Member | Type | Description |
|---|---|---|
| `ProtocolVersion` | `const int` | Supported client-to-server envelope version. The value is `1`. |
| `RequestTimeout` | `TimeSpan` | Maximum wait for a terminal reply. Defaults to 30 seconds. Zero or a negative value disables the timeout. |
| `State` | `GamersClientState` | Current flow state. Read-only outside the class. |
| `Email` | `string` | Sensitive player email currently being authenticated, or `null` before authentication starts. Do not log or store it beyond the authentication flow. |
| `CurrentEventId` | `string` | Successfully joined event identifier, or `null`. Event and tournament state are independent. |
| `CurrentTournamentId` | `string` | Successfully joined tournament identifier, or `null`. Event and tournament state are independent. |
| `PendingOperationCount` | `int` | Number of requests waiting for terminal replies. Intended for diagnostics. |

### Events

| Event | Payload | When raised |
|---|---|---|
| `OnStateChanged` | `GamersClientState` | After `State` changes. |
| `OnAuthCodeRequested` | `AuthCodeRequestedReply` | After an auth-code request succeeds or the server pushes an uncorrelated confirmation. |
| `OnAuthenticated` | `AuthResultReply` | After authentication succeeds or the server pushes an uncorrelated success. |
| `OnEventJoined` | `EventJoinResultReply` | After an event join succeeds. |
| `OnTournamentJoined` | `TournamentJoinResultReply` | After a tournament join succeeds. |
| `OnLeaderboardUpdated` | `LeaderboardSnapshotReply` | After a requested or pushed leaderboard snapshot arrives. |
| `OnError` | `ErrorReply` | When an operation fails with `GamersClientException`, or the server pushes an uncorrelated error. The returned task remains the authoritative result for an awaited operation. |

### `RequestAuthAsync`

```csharp
public Task<AuthCodeRequestedReply> RequestAuthAsync(
    string email,
    CancellationToken ct = default)
```

Requests a verification code for `email`.

| Parameter | Description |
|---|---|
| `email` | Nonempty player email address that should receive the verification code. Treat it as sensitive personal data. |
| `ct` | Optional cancellation token. |

Returns the game-server's `AuthCodeRequestedReply`. Throws the [operation exceptions](#operation-exceptions). `ArgumentException` identifies `email` when it is invalid.

### `SubmitCodeAsync`

```csharp
public Task<AuthResultReply> SubmitCodeAsync(
    string email,
    string code,
    CancellationToken ct = default)
```

Submits the verification code received by the player.

| Parameter | Description |
|---|---|
| `email` | Nonempty email address that received the code. Treat it as sensitive personal data. |
| `code` | Nonempty, short-lived verification code. Treat it as an authentication credential and never log or persist it. |
| `ct` | Optional cancellation token. |

Returns `AuthResultReply` after successful authentication. Throws the [operation exceptions](#operation-exceptions). An unsuccessful authentication result becomes `GamersClientException` with code `AUTH_FAILED`.

### `JoinEventAsync`

```csharp
public Task<EventJoinResultReply> JoinEventAsync(
    string eventId,
    CancellationToken ct = default)
```

Requests an event entry for the authenticated player represented by the game-server session.

| Parameter | Description |
|---|---|
| `eventId` | Nonempty event identifier. |
| `ct` | Optional cancellation token. |

Returns `EventJoinResultReply` after the event join succeeds. Throws the [operation exceptions](#operation-exceptions). An unsuccessful result becomes `GamersClientException` with code `JOIN_EVENT_FAILED`.

### `JoinTournamentAsync`

```csharp
public Task<TournamentJoinResultReply> JoinTournamentAsync(
    string tournamentId,
    CancellationToken ct = default)
```

Requests a tournament entry for the authenticated player represented by the game-server session.

| Parameter | Description |
|---|---|
| `tournamentId` | Nonempty tournament identifier. |
| `ct` | Optional cancellation token. |

Returns `TournamentJoinResultReply` after the tournament join succeeds. Throws the [operation exceptions](#operation-exceptions). An unsuccessful result becomes `GamersClientException` with code `JOIN_TOURNAMENT_FAILED`.

### `RequestLeaderboardAsync`

```csharp
public Task<LeaderboardSnapshotReply> RequestLeaderboardAsync(
    string tournamentId,
    string metricId = null,
    int offset = 0,
    int limit = 50,
    CancellationToken ct = default)
```

Requests a tournament leaderboard.

| Parameter | Description |
|---|---|
| `tournamentId` | Nonempty tournament identifier. |
| `metricId` | Optional metric identifier. Omit it for the default by-player view. |
| `offset` | Zero-based pagination offset. The game-server validates its accepted range. |
| `limit` | Maximum entries requested. Defaults to `50`; the game-server validates its accepted range. |
| `ct` | Optional cancellation token. |

Returns `LeaderboardSnapshotReply`. Throws the [operation exceptions](#operation-exceptions). The client validates `tournamentId`; pagination validation belongs to the game-server.

### `Dispose`

```csharp
public void Dispose()
```

Unsubscribes from `IGamersTransport.ReplyReceived` and cancels all pending operations. It is safe to call more than once. It does not dispose the caller-owned transport.

## 3. Transport API

### `IGamersTransport`

Namespace: `Gamers.Client`

```csharp
public interface IGamersTransport
{
    Task SendAsync(GamersClientMessage message, CancellationToken ct = default);
    event Action<GamersServerReply> ReplyReceived;
}
```

#### `SendAsync`

Serializes and sends a non-null message to the developer's game-server. The returned task completes when the send is accepted, not when a server reply arrives. It should throw `OperationCanceledException` when `ct` is cancelled. Network, serialization, and transport failures may use implementation-specific exceptions.

#### `ReplyReceived`

Raise this event once for each deserialized game-server reply. Raise it on Unity's main thread. For request/reply traffic, preserve the outgoing `correlationId` and protocol version. A late duplicate cannot complete a newer pending request; any reply without an outstanding correlation identifier is handled as a server push.

## 4. Error API

### `GamersClientException`

Namespace: `Gamers.Client`

Thrown for protocol and game-server failures.

| Property | Type | Description |
|---|---|---|
| `Code` | `string` | Machine-readable error code. |
| `Message` | `string` | Player-safe message inherited from `Exception`. |
| `Retryable` | `bool` | Whether retrying the same request is safe. |
| `CorrelationId` | `string` | Originating request identifier when known. |

Constructor:

```csharp
public GamersClientException(
    string code,
    string message,
    bool retryable = false,
    string correlationId = null)
```

`FromReply(ErrorReply reply)` creates an exception from a non-null server error. It throws `ArgumentNullException` for a null reply.

Client-generated codes are:

| Code | Meaning |
|---|---|
| `AUTH_FAILED` | The server returned an unsuccessful authentication result. |
| `JOIN_EVENT_FAILED` | The server returned an unsuccessful event join result. |
| `JOIN_TOURNAMENT_FAILED` | The server returned an unsuccessful tournament join result. |
| `PROTOCOL_MISMATCH` | The terminal reply type did not match the request. |
| `UNSUPPORTED_PROTOCOL_VERSION` | The server reply used a protocol version other than `1`. |

A game-server can also return its own safe codes in `ErrorReply.Code`.

## 5. State API

### `GamersClientState`

Namespace: `Gamers.Client`

| Value | Meaning |
|---|---|
| `Idle` | No operation has started. |
| `RequestingAuth` | An authentication-code request is in progress. |
| `AwaitingCode` | The code request succeeded and player input is expected. |
| `AwaitingAuthResult` | A verification code was submitted. |
| `Authenticated` | Authentication succeeded. |
| `JoiningEvent` | An event join is in progress. |
| `JoinedEvent` | The event join succeeded. |
| `JoiningTournament` | A tournament join is in progress. |
| `JoinedTournament` | The tournament join succeeded. |
| `Error` | The latest handled operation or server push produced an error. |

## 6. Message envelopes

All contracts are in namespace `Gamers.Client`, are marked `[Serializable]`, and use Newtonsoft.Json.

### `GamersClientMessage`

Abstract base for client-to-server messages.

| JSON field | C# property | Type | Description |
|---|---|---|---|
| `type` | `Type` | `string` | Abstract message discriminator. |
| `protocolVersion` | `ProtocolVersion` | `int` | Envelope version. `GamersClientFlow` sets it to `1`. |
| `correlationId` | `CorrelationId` | `string` | Request identifier generated by `GamersClientFlow`. |

### `GamersServerReply`

Abstract base for server-to-client replies.

| JSON field | C# property | Type | Description |
|---|---|---|---|
| `type` | `Type` | `string` | Abstract reply discriminator. |
| `protocolVersion` | `ProtocolVersion` | `int` | Must equal `1`. |
| `correlationId` | `CorrelationId` | `string` | Must echo the request identifier for terminal replies. Omit only for deliberate server pushes. |

## 7. Client-to-server contracts

Every concrete message has a parameterless constructor for deserialization and the populated constructor shown below. Constructors assign values but do not validate them; use `GamersClientFlow` for validated requests.

### `RequestAuthMessage`

Discriminator: `auth:request`

```csharp
public RequestAuthMessage()
public RequestAuthMessage(string email)
```

| JSON field | Property | Type | Description |
|---|---|---|---|
| `email` | `Email` | `string` | Sensitive player email address that should receive the verification code. |

### `SubmitCodeMessage`

Discriminator: `auth:submit-code`

```csharp
public SubmitCodeMessage()
public SubmitCodeMessage(string email, string code)
```

| JSON field | Property | Type | Description |
|---|---|---|---|
| `email` | `Email` | `string` | Sensitive player email address that received the code. |
| `code` | `Code` | `string` | Sensitive, short-lived authentication code entered by the player. Never log or persist it. |

### `JoinEventMessage`

Discriminator: `event:join`

```csharp
public JoinEventMessage()
public JoinEventMessage(string eventId)
```

| JSON field | Property | Type | Description |
|---|---|---|---|
| `eventId` | `EventId` | `string` | Event to join. |

### `JoinTournamentMessage`

Discriminator: `tournament:join`

```csharp
public JoinTournamentMessage()
public JoinTournamentMessage(string tournamentId)
```

| JSON field | Property | Type | Description |
|---|---|---|---|
| `tournamentId` | `TournamentId` | `string` | Tournament to join. |

### `RequestLeaderboardMessage`

Discriminator: `tournament:leaderboard`

```csharp
public RequestLeaderboardMessage()
public RequestLeaderboardMessage(
    string tournamentId,
    string metricId = null,
    int offset = 0,
    int limit = 50)
```

| JSON field | Property | Type | Description |
|---|---|---|---|
| `tournamentId` | `TournamentId` | `string` | Tournament whose leaderboard is requested. |
| `metricId` | `MetricId` | `string` | Optional metric filter. |
| `offset` | `Offset` | `int` | Zero-based pagination offset. |
| `limit` | `Limit` | `int` | Maximum entries requested. Defaults to `50`. |

## 8. Server-to-client contracts

Concrete reply classes have public parameterless constructors for Newtonsoft.Json deserialization.

### `AuthCodeRequestedReply`

Discriminator: `auth:code-requested`

| JSON field | Property | Type | Description |
|---|---|---|---|
| `email` | `Email` | `string` | Sensitive player email address to which the code was sent. Do not log it. |
| `message` | `Message` | `string` | Player-safe status text. |

### `AuthResultReply`

Discriminator: `auth:result`

| JSON field | Property | Type | Description |
|---|---|---|---|
| `success` | `Success` | `bool` | Whether authentication succeeded. |
| `userId` | `UserId` | `string` | Opaque Gamers identifier for the authenticated player; otherwise it can be `null`. Treat it as personal data and do not display or log it. |
| `message` | `Message` | `string` | Player-safe status or failure text. |

### `EventJoinResultReply`

Discriminator: `event:join-result`

| JSON field | Property | Type | Description |
|---|---|---|---|
| `eventId` | `EventId` | `string` | Event identifier. |
| `entryId` | `EntryId` | `string` | Created entry identifier on success; otherwise it can be `null`. |
| `success` | `Success` | `bool` | Whether the join succeeded. |
| `message` | `Message` | `string` | Player-safe failure text when unsuccessful. |

### `TournamentJoinResultReply`

Discriminator: `tournament:join-result`

| JSON field | Property | Type | Description |
|---|---|---|---|
| `tournamentId` | `TournamentId` | `string` | Tournament identifier. |
| `entryId` | `EntryId` | `string` | Created entry identifier on success; otherwise it can be `null`. |
| `success` | `Success` | `bool` | Whether the join succeeded. |
| `message` | `Message` | `string` | Player-safe failure text when unsuccessful. |

### `LeaderboardSnapshotReply`

Discriminator: `tournament:leaderboard`

| JSON field | Property | Type | Description |
|---|---|---|---|
| `tournamentId` | `TournamentId` | `string` | Tournament identifier. |
| `metricId` | `MetricId` | `string` | Metric identifier for a by-metric view; otherwise `null`. |
| `metricName` | `MetricName` | `string` | Metric display name for a by-metric view; otherwise `null`. |
| `sortDirection` | `SortDirection` | `string` | Ordering direction supplied by the game-server. |
| `offset` | `Offset` | `int` | Offset represented by this page. |
| `limit` | `Limit` | `int` | Requested page size. |
| `totalEntries` | `TotalEntries` | `int` | Total entries available for the selected view. |
| `entries` | `Entries` | `List<LeaderboardEntry>` | Entries ordered by rank. Defaults to an empty list. |

### `ErrorReply`

Discriminator: `error`

| JSON field | Property | Type | Description |
|---|---|---|---|
| `code` | `Code` | `string` | Machine-readable, player-safe error code. |
| `message` | `Message` | `string` | Player-safe error text. |
| `retryable` | `Retryable` | `bool` | Whether retrying the same request is safe. |

## 9. Display models

Display models are mutable only for JSON deserialization. Treat values received from the game-server as read-only UI data. Never use client model values to authorize a player, accept a score, or award a prize.

### `LeaderboardEntry`

Namespace: `Gamers.Client.Models`

| JSON field | Property | Type | Description |
|---|---|---|---|
| `rank` | `Rank` | `int` | One-based rank. |
| `displayName` | `DisplayName` | `string` | Intentionally public player display name or alias. Never use an email address or other sensitive value. |
| `playerId` | `PlayerId` | `string` | Developer-side player identifier intended for public leaderboard display. Use a non-sensitive, opaque value, never an email address, wallet address, authentication identifier, or internal account identifier. |
| `score` | `Score` | `decimal?` | Score in a by-metric view; `null` in a by-player view. |
| `metricId` | `MetricId` | `string` | Metric identifier in a by-metric view. |
| `bestScore` | `BestScore` | `decimal?` | Best score in a by-metric view. |
| `attemptCount` | `AttemptCount` | `int?` | Attempt count in a by-metric view. |
| `estimatedPrize` | `EstimatedPrize` | `decimal?` | Estimated prize when available. |
| `metrics` | `Metrics` | `List<LeaderboardPlayerMetric>` | Per-metric aggregates in a by-player view. Defaults to an empty list. |

### `LeaderboardPlayerMetric`

Namespace: `Gamers.Client.Models`

| JSON field | Property | Type | Description |
|---|---|---|---|
| `metricId` | `MetricId` | `string` | Metric identifier. |
| `metricName` | `MetricName` | `string` | Optional metric display name. |
| `bestScore` | `BestScore` | `decimal` | Player's best score for the metric. |
| `attemptCount` | `AttemptCount` | `int` | Number of submitted attempts. |
| `estimatedPrize` | `EstimatedPrize` | `decimal?` | Estimated prize when available. |

### `EventInfo`

Namespace: `Gamers.Client.Models`

This type is reserved for event display data supplied by a game-server. The current five-operation flow does not include event discovery.

| JSON field | Property | Type | Description |
|---|---|---|---|
| `id` | `Id` | `string` | Event identifier. |
| `name` | `Name` | `string` | Display name. A game-server can use the identifier as a fallback. |
| `status` | `Status` | `EventStatus` | Event lifecycle status. |
| `startDate` | `StartDate` | `string` | ISO 8601 timestamp supplied by the game-server. |
| `endDate` | `EndDate` | `string` | ISO 8601 timestamp supplied by the game-server. |

### `TournamentInfo`

Namespace: `Gamers.Client.Models`

This type is reserved for tournament display data supplied by a game-server. The current five-operation flow does not include tournament discovery.

| JSON field | Property | Type | Description |
|---|---|---|---|
| `id` | `Id` | `string` | Tournament identifier. |
| `name` | `Name` | `string` | Tournament display name. |
| `status` | `Status` | `TournamentStatus` | Tournament lifecycle status. |
| `startDate` | `StartDate` | `string` | ISO 8601 start timestamp. |
| `endDate` | `EndDate` | `string` | ISO 8601 end timestamp. |
| `entryFee` | `EntryFee` | `decimal` | Entry fee denominated in `Currency`. |
| `currency` | `Currency` | `string` | Currency code for `EntryFee`. |

### `PrizeInfo`

Namespace: `Gamers.Client.Models`

| JSON field | Property | Type | Description |
|---|---|---|---|
| `position` | `Position` | `int` | One-based finishing position. |
| `amount` | `Amount` | `decimal` | Prize amount denominated in `Currency`. |
| `currency` | `Currency` | `string` | Currency code for `Amount`. |

## 10. Display enums

Namespace: `Gamers.Client.Models.Common`

Enums serialize as the uppercase wire values shown below.

### `EventStatus`

| C# value | JSON value | Meaning |
|---|---|---|
| `Open` | `OPEN` | Event is open. |
| `Started` | `STARTED` | Event has started. |
| `Completed` | `COMPLETED` | Event is closed and results are being processed. |
| `Rewarded` | `REWARDED` | Event is closed and winners have been rewarded. |
| `Cancelled` | `CANCELLED` | Event was cancelled and any required refunds are pending. |
| `Refunded` | `REFUNDED` | Applicable entry fees were refunded. |

### `TournamentStatus`

| C# value | JSON value | Meaning |
|---|---|---|
| `Draft` | `DRAFT` | Configuration in progress. |
| `Pending` | `PENDING` | Scheduled but not open. |
| `Opened` | `OPENED` | Open for entry. |
| `Closed` | `CLOSED` | Closed to new entries. |
| `Completed` | `COMPLETED` | Tournament window is closed and results are being processed. |
| `Cancelled` | `CANCELLED` | Tournament was cancelled before completion. |
| `Refunded` | `REFUNDED` | Applicable entry fees were refunded. |

## 11. Editor-only attribution API

Namespace: `Gamers.Client.Editor`<br>
Assembly: `GamersClient.Editor`<br>
Player builds: excluded

These types implement Unity Verified Solutions attribution. They are public because Unity's static attribution script exposes a public entry point, but they are package implementation details rather than supported runtime extension points. Developers should use **Edit → Project Settings → Gamers** instead of calling them directly.

### `GamersAttributionSettingsProvider`

`CreateSettingsProvider()` is discovered by Unity through `[SettingsProvider]` and returns the **Project Settings → Gamers** page. The page stores the Gamers developer ID in per-user `EditorUserSettings` and attempts to send one attribution event per project.

### `VSAttribution`

```csharp
public static AnalyticsResult SendAttributionEvent(
    string actionName,
    string partnerName,
    string customerUid)
```

| Parameter | Description |
|---|---|
| `actionName` | Action that triggered attribution. The package uses `Configure`. |
| `partnerName` | Verified Solutions partner name. The package uses `Gamers`. |
| `customerUid` | Non-secret Gamers developer identifier. |

Returns Unity's `AnalyticsResult`. When Editor Analytics is disabled, it returns `AnalyticsDisabled` and sends nothing. The implementation catches analytics failures and does not affect runtime integration.

## 12. API stability

The supported customer surface is `GamersClientFlow`, `IGamersTransport`, `GamersClientException`, `GamersClientState`, the message/reply contracts, and display models listed here. Private members and the Editor attribution implementation are not extension points. Review the changelog before upgrading because protocol or method-signature changes are called out as breaking changes.
