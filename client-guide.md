# Gamers Unity Client Helper

Transport-agnostic Unity client helper for the Gamers server-authoritative integration. Supports Unity 2022.3 LTS or newer, targeting Windows, macOS, Android and iOS.

- [Integration guide](integration-guide.md)
- [C# API reference](api-reference.md)
- [Changelog](CHANGELOG.md)

## Architecture

This package runs inside the game client, but the game client **never** calls the Gamers Game API directly or through a proxy. Instead, it sends messages to the developer's own game-server, which holds the Gamers API key and player JWTs.

```
Unity client (GamersClientFlow + IGamersTransport)
  │ your own networking
  ▼
Your game-server (Gamers Server SDK)
  │ X-API-KEY
  ▼
Gamers game-api
```

## Installation

Public releases use Unity Package Manager (UPM) through the Unity Asset Store.

### Unity Asset Store

After publication, install the package from its Unity Asset Store listing through **Window → Package Manager**.

### UPM from disk

For an evaluation package supplied by Gamers:

1. Extract the package directory.
2. In Unity, open **Window → Package Manager**.
3. Select **+ → Add package from disk...**.
4. Select `package.json` at the root of the extracted package.

UPM resolves `com.unity.nuget.newtonsoft-json` automatically.

### UPM tarball

If Gamers supplies a `.tgz` package, import it through **Package Manager → Add package from tarball...**.

## Quick start

Implement `IGamersTransport` using your game's networking layer, then use `GamersClientFlow`:

```csharp
using Gamers.Client;
using UnityEngine;

public class GamersExample : MonoBehaviour
{
    private GamersClientFlow _flow;

    void Start()
    {
        var transport = new MyNetworkTransport(); // you implement this
        _flow = new GamersClientFlow(transport);

        // Events are optional: they let code elsewhere in the scene observe the same outcomes.
        _flow.OnStateChanged += state => Debug.Log($"State: {state}");
        _flow.OnLeaderboardUpdated += snapshot => Debug.Log($"Leaderboard has {snapshot.Entries.Count} entries");
        _flow.OnError += error => Debug.LogError($"Error: {error.Code} - {error.Message}");
    }

    void OnDestroy()
    {
        // Cancels anything still in flight so no awaiting handler is left hanging.
        _flow?.Dispose();
    }

    public async void OnEmailSubmitted(string email)
    {
        try
        {
            await _flow.RequestAuthAsync(email);
            Debug.Log("Verification code requested.");
        }
        catch (TimeoutException) { Debug.LogError("The game-server did not respond."); }
        catch (GamersClientException ex) { Debug.LogError($"RequestAuth failed: {ex.Code} - {ex.Message}"); }
    }

    public async void OnJoinTournamentClicked(string tournamentId)
    {
        try
        {
            // Completes when the join actually succeeded - not when the message was sent.
            var result = await _flow.JoinTournamentAsync(tournamentId);
            Debug.Log($"Entry {result.EntryId} created for {result.TournamentId}");
        }
        catch (TimeoutException) { Debug.LogError("The game-server did not respond."); }
        catch (GamersClientException ex) { Debug.LogError($"Join failed: {ex.Code} - {ex.Message} (retryable: {ex.Retryable})"); }
    }
}
```

### What `await` means here

Each operation returns a `Task<TReply>` that completes when the **game-server's reply arrives**, so
the awaited value is the real outcome. Three rules follow:

- **Your transport must echo `correlationId`** on exactly one reply per request. That is how a reply
  is routed back to the call that made it, and how a late or duplicate reply is prevented from
  affecting a newer request.
- **Failures throw** `GamersClientException` (with `Code`, `Message`, `Retryable`). A game-server that
  never answers throws `TimeoutException` after `RequestTimeout`, which defaults to 30 seconds.
- **Replies with no correlation id are treated as unsolicited server pushes** and raised through the
  matching event — `OnLeaderboardUpdated`, `OnError`, and so on.

See `integration-guide.md` for a complete transport example and message reference.

## What is in this package?

- `GamersClientFlow` — state machine and high-level API
- `IGamersTransport` — interface for your client↔server networking layer
- Client message contracts (`RequestAuthMessage`, `SubmitCodeMessage`, `JoinEventMessage`, `JoinTournamentMessage`, `RequestLeaderboardMessage`)
- Server reply contracts (`AuthCodeRequestedReply`, `AuthResultReply`, `EventJoinResultReply`, `TournamentJoinResultReply`, `LeaderboardSnapshotReply`, `ErrorReply`)
- Read-only display models (`TournamentInfo`, `EventInfo`, `LeaderboardEntry`, `LeaderboardPlayerMetric`, `PrizeInfo`)
- Editor-only Verified Solutions attribution under **Edit → Project Settings → Gamers**
- Bundled integration and complete C# API documentation

## What is NOT in this package?

- No HTTP client for the Gamers Game API
- No API key or JWT storage
- No create/update/delete operations for events, tournaments, scores, or results — those belong on the game-server

## Packaging

This package is distributed in UPM format through the Unity Asset Store. UPM resolves declared dependencies and surfaces package compatibility information.

## Samples

Open the Package Manager window, select the Gamers Client Helper package, and import the sample:

- **Reference Integration** — example `MonoBehaviour` wired to a fake transport

## Demo app

A complete, UI-driven sample project is available at [`gamers-bet/gamers-unity-client-sdk-demo-app`](https://github.com/gamers-bet/gamers-unity-client-sdk-demo-app). The repository includes:

- A sample scene wired to a `WebSocketTransport`
- Prebuilt app packages for Windows, macOS, Android, and iOS
- Step-by-step install and usage instructions

## Requirements

- Unity 2022.3 LTS or newer
- .NET Standard 2.1
- `com.unity.nuget.newtonsoft-json` 3.2.1
- IL2CPP and Mono scripting backends
- 64-bit Android, iOS, Windows, and macOS
- A developer-owned game-server and `IGamersTransport` implementation

## IL2CPP and Android

The package ships a `link.xml` that preserves the `GamersClient.Runtime` and `GamersClient.Samples` assemblies. This prevents IL2CPP's managed code stripping (Medium/High) from removing Json.NET property setters on the message and model types.

Android is supported as a 64-bit (ARM64) target. Use the IL2CPP scripting backend for player builds to satisfy Google Play's 64-bit requirement and to verify the same stripping path used on iOS.

## Developer documentation

`integration-guide.md` is the canonical integration guide. The complete C# reference is bundled at `api-reference.md`. Keep both files and `CHANGELOG.md` synchronized with every public API change.

## License

See `LICENSE.pdf` in this package for the End User License Agreement.
