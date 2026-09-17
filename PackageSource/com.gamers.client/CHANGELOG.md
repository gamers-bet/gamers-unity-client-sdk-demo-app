# Changelog

## Submission packaging revision

- Bundled the complete UI demo in the Reference Integration sample, including
  its scene, WebSocket transport, UI scripts, art, input actions and required fonts.
- Added `Gamers.Client.Samples` namespaces to all sample scripts and updated
  serialized component/button type names and sample assembly references.
- Added sample dependency/setup instructions and a repeatable packaging command.

## [1.0.0] - 2026-08-20

### Added

- Editor-only `GamersClient.Editor` assembly with Unity's Verified Solutions attribution script
  (`VSAttribution.cs`, based on Unity's public script with the namespace changed and public XML
  documentation expanded) and a **Project Settings → Gamers** page. Entering your Gamers developer
  ID there sends Unity's Verified Solutions attribution event once per project. The ID is stored only in the
  project's editor user settings; nothing in this assembly ships in player builds, and the
  runtime's credential-free architecture is unchanged.
- Complete bundled C# API reference covering every public runtime type, member, parameter, return
  value, exception, wire field, and Editor-only attribution entry point.
- Evidence-based compatibility, setup, security, known-issue, workaround, and failure-reproduction
  documentation for the Verified Solutions review.
- Compile-time documentation gate that rejects new undocumented Runtime APIs.

### Changed

- Package documentation and changelog metadata now use public Developer Portal routes instead of
  private GitHub URLs.

## [0.5.0] - 2026-08-06

Version aligned across all three SDKs, and raised out of 0.1.x because this release is breaking.

### Changed — BREAKING

- **Operations now complete when the game-server replies, not when the transport accepts the
  message.** `RequestAuthAsync`, `SubmitCodeAsync`, `JoinEventAsync`, `JoinTournamentAsync` and
  `RequestLeaderboardAsync` return `Task<TReply>` instead of `Task`. Awaiting one now means the
  operation actually succeeded. Existing `await _flow.JoinTournamentAsync(id);` calls still
  compile; the result is simply available if you want it.
- **Failures throw.** A server `ErrorReply`, an unsuccessful result, a reply of the wrong type or
  an unsupported protocol version fault the returned task with `GamersClientException`. The `OnError`
  event still fires for code elsewhere in the scene.
- **Replies are matched to the operation that created their correlation id.** A late or duplicate
  reply resolves its own task and can no longer drive the state of a newer request. Previously the
  flow only checked that a correlation id was in a pending set, so a delayed reply for tournament A
  could move the state machine while tournament B was the current request.
- **Your transport must echo `correlationId` on exactly one reply per request.** A transport written
  against 0.1.1 that does not echo it will see its replies treated as unsolicited server pushes.
- `RequestLeaderboardAsync` gained `offset` and `limit` parameters before `ct`; callers passing a
  `CancellationToken` positionally must switch to a named argument.

### Added

- `GamersClientFlow.RequestTimeout` (default 30s). A request that receives no reply faults with
  `TimeoutException` instead of waiting forever. Pending operations no longer accumulate: a single
  lost reply used to leave the pending set permanently non-empty, which silently suppressed every
  later uncorrelated server push.
- `GamersClientException` with `Code`, `Message`, `Retryable` and `CorrelationId`.
- `GamersClientFlow.ProtocolVersion` and validation of the reply envelope's `protocolVersion`.
  Replies from a server speaking a different version are rejected with `UNSUPPORTED_PROTOCOL_VERSION`
  rather than deserialized optimistically.
- `GamersClientFlow.PendingOperationCount` for diagnostics.
- `Dispose()` cancels outstanding operations so awaiting handlers do not hang on scene unload.
- Unsolicited server pushes are delivered unconditionally through their events, no longer gated on
  whether a request happens to be in flight.
- A headless test runner (`sdks/unity-client/HeadlessTests`) that executes the package's NUnit tests
  and compile-checks the sample with `dotnet test`, no Unity Editor or licence required. It runs in
  CI on every change; the Editor job remains for asmdef, IL2CPP and player-build coverage.

### Fixed

- The `ReferenceIntegration` sample did not compile: it used `Exception` without `using System;`,
  and `ReferenceTransport` used `LeaderboardEntry` without `using Gamers.Client.Models;`. Both are
  now covered by the headless compile check.

### Notes

- The `.unitypackage` exporter removed in 0.1.3 stays removed, deliberately. A `.unitypackage`
  cannot declare the `com.unity.nuget.newtonsoft-json` dependency this package needs, so every
  developer would have to install Newtonsoft by hand first. UPM (disk, Git URL, or `npm pack`
  tarball) is the supported path.

## [0.1.3] - 2026-08-05

### Fixed

- `GamersClient.Samples.asmdef` now references `Unity.Newtonsoft.Json` so sample code that serializes JSON compiles.
- `ReferenceTransport` now replies to all supported client messages, including auth-code requests and leaderboard requests.
- `ReferenceIntegration` subscribes to `OnAuthCodeRequested` and catches exceptions in `async void` UI handlers.
- `integration-guide.md` deserializer includes `auth:code-requested` and `event:join-result`, and documents protocol version, correlation, and leaderboard pagination.
- README UPM Git URL is now pinned to a release tag; `.unitypackage` instructions replaced with UPM tarball instructions.
- Removed in-package `.unitypackage` exporter and added `files` to `package.json` for clean UPM tarball output.
- Unity CI workflow re-enabled with path-conditional triggering.

### Added

- `ProtocolVersion` to `GamersClientMessage` and `GamersServerReply` envelopes.
- `AuthCodeRequestedReply` and `OnAuthCodeRequested` event to acknowledge verification code requests.
- `ErrorReply.Retryable` flag to indicate when the client may safely retry.
- `LeaderboardPlayerMetric`, `bestScore`, `attemptCount`, `estimatedPrize`, and `metrics` to `LeaderboardEntry`.
- Pagination (`offset`, `limit`) and `sortDirection`/`totalEntries` to leaderboard request/reply.

## [0.1.2] - 2026-08-05
- `GamersClientFlow` now stamps every outgoing message with a unique `CorrelationId` and tracks pending requests.
- `GamersClientFlow` only dispatches server replies whose `CorrelationId` matches a pending request, preventing late or duplicate replies from triggering stale state transitions.
- `ErrorReply` built by the flow now carries the originating reply's `CorrelationId`.

## [0.1.1] - 2026-08-02
- Replaced client-with-proxy architecture with server-authoritative client helper
- Removed direct game-api HTTP client and JWT storage from client builds
- Added `IGamersTransport` and `GamersClientFlow` for client↔game-server communication
- Added serializable message and reply contracts
- Added read-only display models
- Renamed package from `com.gamers.sdk` to `com.gamers.client`
- Moved previous proxy-HTTP client implementation to `sdks/server-dotnet/` as a server SDK starting point

## [0.1.0] - 2026-07-21
- Initial release
- Full Game API coverage
- Client-with-proxy architecture: no API key in client builds
- `Task<T>` async API, Newtonsoft.Json serialization
- Windows, macOS, iOS support
