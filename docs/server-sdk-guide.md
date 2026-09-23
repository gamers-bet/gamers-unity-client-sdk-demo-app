# Server SDK Guide (.NET and Java)
Version 1.0.0

The Server SDKs wrap the Game API for code running on **your** servers. Both are a hand-written façade over a client generated from the same OpenAPI document that powers the API reference, so every endpoint in the spec is reachable, and both add what you would otherwise hand-roll: auth headers, API versioning, retry with backoff, idempotency keys, and typed errors.

Companion documents: [Developer Guide](dev-guide.md) · [Unity Client SDK Guide](integration-guide.md) · [Reference Game-Server Guide](reference-server-guide.md)

> These SDKs hold your API key. They must never be shipped inside a game client. Clients talk to your server — see the [Unity Client SDK Guide](integration-guide.md).

## 1. Which credentials go where

| Call type | Needs | Examples |
|---|---|---|
| Game-scoped | `X-API-KEY` only | create/start/complete/cancel events, submit results, create and manage tournaments, templates, attempts, scores, gameplay modes, metrics |
| Player-scoped | `X-API-KEY` **and** that player's JWT | join event, join tournament, start a tournament entry, read a player's profile or balance |

Player JWTs come from the two-step email flow (`POST /auth/request` → `POST /auth/verify`) and are stored **server-side**, keyed by your own player id. Both SDKs give you a pluggable store for this; both ship an in-memory default that is fine for tests and wrong for production.

## 2. Distribution

Neither SDK is published to a public registry. Build from source.

| | .NET | Java |
|---|---|---|
| Targets | `netstandard2.1`, `net8.0` | Java 17 |
| Source | `sdks/server-dotnet` | `sdks/server-java` |
| Build | `dotnet build` | `./mvnw -B compile` |
| Test | `dotnet test` | `./mvnw -B test` |
| Consume | project reference, or pack locally | `./mvnw install -DskipTests`, then depend on `com.gamers:gamers-server-java:1.0.0` |
| Regenerate | `./generate.sh` | `./generate.sh` |

`generate.sh` reads `apps/developer/server/assets/docs/openapi.yaml`. Regenerate after an API version bump rather than hand-editing generated code.

---

# .NET SDK

## 3. Configure

```csharp
using Gamers.Sdk;
using Gamers.Sdk.Auth;

var config = new GamersServerConfig("https://api.gamers.dev", apiKey)
{
    Timeout                  = TimeSpan.FromSeconds(30),    // per attempt
    OverallTimeout           = TimeSpan.FromSeconds(120),   // whole call, across retries
    MaxRetries               = 3,
    RetryDelay               = TimeSpan.FromMilliseconds(250),
    RetryBackoffMultiplier   = 2.0,
    MaxRetryDelay            = TimeSpan.FromSeconds(30),
    ApiVersion               = "2",                         // standardised error envelope
    EnableIdempotencyKeys    = true
};

var client = new GamersServerClient(config, tokens: new InMemoryPlayerTokenStore());
```

Those are the defaults; the values are shown so you can see what you are opting out of. `GamersServerClient` is thread-safe and cheap to hold for the life of the process — register it as a singleton.

The client sends `X-API-KEY`, the player's `Authorization: Bearer …` on player-scoped calls, `X-API-Version`, a fresh `X-Request-ID` per call, and an `Idempotency-Key` on every `POST`/`PUT`/`PATCH`/`DELETE`. It retries `408`, `429`, and `5xx` responses plus network failures, with jittered exponential backoff, honouring `Retry-After`.

> **Both timeouts matter.** `Timeout` is per attempt. With `MaxRetries = 3` a call bounded only by `Timeout` could run four times that, plus backoff. `OverallTimeout` is what keeps the worst case predictable; `TimeSpan.Zero` disables it.

### Supplying your own transport

There are three paths, and only two of them keep the SDK's behaviour:

```csharp
// 1. Share a connection pool, keep the SDK pipeline. Preferred.
var shared = new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) };
var client = GamersServerClient.WithHandler(config, shared);   // 'shared' is never disposed by the SDK

// 2. Build the pipeline yourself — e.g. to wrap it in a logging handler.
//    Order is IdempotencyKeyHandler -> RetryDelegatingHandler -> your handler.
var pipeline   = GamersServerClient.CreateHandler(config, new SocketsHttpHandler());
var httpClient = new HttpClient(pipeline) { Timeout = Timeout.InfiniteTimeSpan };

// 3. Pass a ready-made HttpClient. The SDK uses it AS-IS.
var bare = new GamersServerClient(config, tokens: null, sharedClient: myHttpClient);
```

Path 3 installs **no retry and no idempotency key** — the SDK sends through your client unchanged. Use it only when you intend to own the whole stack. `Timeout.InfiniteTimeSpan` in path 2 is deliberate: a global `HttpClient.Timeout` would cancel the retry chain that `Timeout`/`OverallTimeout` are already bounding.

Idempotency must sit **outside** retry, so the key is generated once and reused across attempts. Get that order backwards and every retry carries a fresh key, which is the same as having none.

## 4. Authenticating a player

```csharp
await client.RequestAuthCodeAsync(email);                    // emails a 6-digit code
await client.VerifyCodeAsync(playerId, email, code);         // stores the JWT under playerId
```

`playerId` is **your** identifier — account id, session id, whatever you key players by. Every player-scoped call takes it as its first argument and looks the token up in `IPlayerTokenStore`.

The default `InMemoryPlayerTokenStore` does not survive a restart and is not shared across processes. For anything beyond a single instance, implement the interface over Redis or your session store:

```csharp
public interface IPlayerTokenStore
{
    PlayerToken? Get(string playerId);      // must return null when expired
    void Set(string playerId, string token, DateTimeOffset? expiresAt = null);
    void Remove(string playerId);
}
```

## 5. A full tournament round

```csharp
var tournament = await client.CreateTournamentAsync(request);
await client.OpenTournamentAsync(tournament.Id);

var entry = await client.JoinTournamentAsync(playerId, tournament.Id);          // player-scoped
entry     = await client.StartTournamentEntryAsync(playerId, tournament.Id, entry.Id);

var attempt = await client.CreateAttemptAsync(tournament.Id, entry.Id);         // game-scoped
await client.SubmitAttemptScoresAsync(tournament.Id, entry.Id, attempt.Id, scores);

var board = await client.GetTournamentLeaderboardAsync(tournament.Id);
await client.CloseTournamentAsync(tournament.Id);
await client.CompleteTournamentAsync(tournament.Id);
```

Events follow the same shape: `CreateEventAsync` → `StartEventAsync` → `JoinEventAsync(playerId, …)` → `SubmitEventResultsAsync` → `CompleteEventAsync`. The façade also covers gameplay modes and metrics, event and tournament templates, teams, and player profile/balance. A runnable end-to-end example is in `sdks/server-dotnet/samples/SampleConsole`.

## 6. Error handling

```csharp
try
{
    await client.JoinEventAsync(playerId, eventId);
}
catch (PlayerNotAuthenticatedException)
{
    // No stored token for this player — restart the email/code flow.
}
catch (GamersApiException ex) when (ex.ErrorCode == "USER_ALREADY_JOINED")
{
    // Expected, benign.
}
catch (GamersApiException ex)
{
    logger.LogError(ex, "Gamers API {Code} ({Status}), request {RequestId}",
        ex.ErrorCode, ex.StatusCode, ex.RequestId);
    throw;
}
```

`GamersApiException` exposes `ErrorCode`, `StatusCode`, and `RequestId`. Branch on `ErrorCode`, never on the message text, and always log `RequestId` — it is what support needs to trace the call.

A tournament that no longer exists for your game surfaces as `GamersApiException` with `StatusCode` `404` and `ErrorCode` `TOURNAMENT_NOT_FOUND` (previously `400` / `INVALID_TOURNAMENT_ID`). It is permanent — stop polling that tournament rather than retrying:

```csharp
catch (GamersApiException ex) when (ex.ErrorCode == "TOURNAMENT_NOT_FOUND")
{
    // Gone for good — drop it from the poll set and refresh the tournament list.
    trackedTournaments.Remove(tournamentId);
}
```

---

# Java SDK

## 7. Configure

```java
import com.gamers.sdk.GamersServerClient;
import com.gamers.sdk.config.GamersServerClientConfig;
import com.gamers.sdk.retry.RetryConfig;

var config = GamersServerClientConfig.builder()
        .baseUri("https://api.gamers.dev")
        .apiKey(apiKey)
        .connectTimeout(Duration.ofSeconds(10))
        .readTimeout(Duration.ofSeconds(30))
        .retryConfig(RetryConfig.defaults())      // 3 retries, 250ms → ×2 → capped at 5s
        .tokenProvider(new MySecureTokenProvider())
        .build();

var client = GamersServerClient.create(config);
```

Again, those are the defaults. `RetryConfig.defaults()` retries `408, 429, 500, 502, 503, 504` plus I/O and timeout failures.

| Property | Default |
|---|---|
| `baseUri` | `https://api.gamers.dev` |
| `apiKey` | required |
| `connectTimeout` / `readTimeout` | `10s` / `30s` |
| `retryConfig` | `RetryConfig.defaults()` |
| `tokenProvider` | `MemoryTokenProvider` |
| `defaultHeaders` | `X-API-Version: 2` |
| `requestIdSupplier` | `UUID.randomUUID()` |
| `enableIdempotencyKeys` | `true` |
| `idempotencyKeyHeaderName` | `Idempotency-Key` |
| `idempotencyKeySupplier` | `UUID.randomUUID()` |

The client is thread-safe, owns the HTTP connection pool and the token store, and is meant to be one per process. It implements `AutoCloseable`: closing it clears the bound player and releases the underlying `HttpClient` executor when the SDK created it. A caller-supplied `HttpClient` is never closed.

## 8. Authenticating a player, and the `forPlayer` trap

```java
client.auth().requestCode(email).join();
client.auth().verifyCode("player-1", email, code).join();   // stores the JWT under "player-1"
```

Player-scoped calls need a bound player id. There are three ways to supply it, and they are not equally safe:

```java
// Best — the typed convenience methods take the id explicitly. No binding at all.
var entry = client.joinTournament("player-1", tournamentId).join();

// Fine — callForPlayer scopes the binding to one call and restores the previous value after.
var profile = client.callForPlayer("player-1", () -> client.getUserProfile("player-1")).join();

// Dangerous — forPlayer binds a ThreadLocal that stays set until clearPlayerId().
client.forPlayer("player-1");
```

`forPlayer` is the trap. On a servlet container or executor pool the thread is reused, so a forgotten `clearPlayerId()` attaches the previous player's JWT to an unrelated later request. Prefer the explicit-id methods (`joinEvent`, `joinTournament`, `startTournamentEntry`, `getUserProfile`, `getUserBalance`); reach for `callForPlayer` when you need a binding across several calls.

A player-scoped call with no stored token completes exceptionally with `GamersSdkException`, status `401`, error code `PLAYER_NOT_AUTHENTICATED`.

### Token storage

`TokenProvider` is player-scoped. **This changed incompatibly in 0.5.0** — a single-token store could serve one player's request with another player's token. If you are upgrading from 0.1.19 or earlier, your implementation must change:

```java
public interface TokenProvider {
    @Nullable PlayerToken getToken(String playerId);      // null when absent or expired
    void setToken(String playerId, String token, @Nullable Instant expiresAt);
    void setToken(String playerId, String token);
    void remove(String playerId);
    void clear();
}
```

The default `MemoryTokenProvider` is suitable for tests. Production should supply something durable and shared — Redis, a vault, your identity provider.

## 9. Calling the API

Two surfaces, and you will use both:

```java
// Typed convenience methods — already mapped to GamersSdkException.
var event = client.createEvent(request).join();
client.startEvent(event.getId()).join();
client.completeEvent(event.getId()).join();

// Generated API groups — full coverage, but they throw the checked ApiException.
// Wrap the future in withMappedException to get the unchecked GamersSdkException instead.
var events = client.withMappedException(client.events().getEvents()).join();
```

Available groups: `auth()`, `authentication()`, `entries()`, `events()`, `eventTemplates()`, `games()`, `teams()`, `tournaments()`, `tournamentTemplates()`, `users()`.

## 10. Error handling

```java
try {
    client.joinEvent("player-1", eventId).join();
} catch (CompletionException e) {
    if (e.getCause() instanceof GamersSdkException ex) {
        if ("USER_ALREADY_JOINED".equals(ex.getErrorCode())) return;   // benign
        log.error("Gamers API {} ({}) details={}",
                  ex.getErrorCode(), ex.getStatusCode(), ex.getDetails());
    }
    throw e;
}
```

A tournament that no longer exists for your game surfaces as `GamersSdkException` with status `404` and error code `TOURNAMENT_NOT_FOUND` (previously `400` / `INVALID_TOURNAMENT_ID`). It is permanent — remove the tournament from your poll set and refresh the tournament list rather than retrying.

`GamersSdkException` exposes `getStatusCode()`, `getErrorCode()`, `getApiMessage()`, and `getDetails()`. Because `join()` wraps failures, always unwrap `CompletionException` (or `ExecutionException` from `get()`) before matching on the cause.

---

## 11. Common error codes

Branch on the code, not the message. The full list is in the [OpenAPI document](https://developer.gamers.dev/docs/openapi); these are the ones you will actually hit:

| Code | Meaning | Usual response |
|---|---|---|
| `INVALID_API_KEY` | Key missing, wrong, or revoked | Fix configuration; do not retry |
| `INVALID_TOKEN` / `EXPIRED_TOKEN` | Player JWT bad or stale | Re-run the email/code flow for that player |
| `USER_ALREADY_JOINED` | Duplicate join | Treat as success |
| `NO_WALLET` / `INSUFFICIENT_NATIVE_TOKEN` | Player cannot cover the stake | Prompt a top-up |
| `EVENT_NOT_OPEN` | Wrong lifecycle state | Fix your sequencing |
| `TOURNAMENT_NOT_FOUND` | HTTP `404`. No tournament with that ID belongs to your game — never created, deleted, or another game's | Permanent. Drop the ID from any polling set and re-list tournaments; never retry it |
| `MAX_ENTRIES_REACHED` / `TOURNAMENT_FULL` | Capacity reached | Surface to the player; do not retry |
| `RESULTS_MISMATCH` | Submitted results do not match entries | Reconcile before resubmitting |
| `VALIDATION_ERROR` | Bad request body | Inspect `details` |
| `RATE_LIMIT_EXCEEDED` | Over 1,000 req/min per IP | Back off; both SDKs retry `429` for you |
| `IDEMPOTENCY_KEY_CONFLICT` | Key reused with a different payload, or the first request is still in flight | Use a fresh key, or wait and retry |
| `IDEMPOTENCY_STORAGE_UNAVAILABLE` | The API failed closed rather than risk a duplicate write | Retry the same key once storage recovers |

## 12. Operational notes

- **Idempotency.** The API caches only successful `2xx` responses per key, for 24 hours. A `4xx`/`5xx` releases the key, so a retry re-executes rather than replaying a failure. A replayed response carries an `Idempotency-Key` **response** header; a first execution does not. `POST /auth/request` and `POST /auth/verify` accept the header and deliberately ignore it. See [Idempotency](dev-guide.md#idempotency) in the Developer Guide.
- **Rate limits.** 1,000 requests per minute **per IP**. One game-server behind one egress IP shares that budget across all its players.
- **Currency.** All monetary values are USD to 2 decimal places — `decimal` in .NET, `BigDecimal` in Java. Never use floating point.
- **Prize distribution is asynchronous.** The API returns before payouts settle. Do not extend timeouts waiting for them.
- **Environments.** `https://api.gamers.dev` is test; `https://api.gamers.bet` is production. Rotate production keys through the Developer Portal.

## 13. Support

Include the SDK version and the `X-Request-ID` of the failing call.
