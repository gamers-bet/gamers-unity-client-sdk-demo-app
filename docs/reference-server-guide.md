# Reference Game-Server Guide
Version 1.0.0

The reference game-server is a minimal Spring Boot WebSocket application that bridges the Unity client protocol to the Gamers Game API using the Java Server SDK. It exists to show you the **shape** of the server side of an integration: how messages are routed, how the SDK is called, how player identity is established, and how Game API errors become something safe to show a player.

It is a reference implementation, not a supported product, and not a starting point you can deploy. [§6](#6-what-you-must-replace) lists what you have to replace first — read it before you build on this.

Source: `sdks/reference-server`. Companion documents: [Developer Guide](dev-guide.md) · [Unity Client SDK Guide](integration-guide.md) · [Server SDK Guide](reference-server-guide.md)

## 1. Where it sits

```
Unity client (GamersClientFlow)
  │ JSON envelopes over ws://…/ws/gamers
  ▼
Reference game-server (Spring Boot + Java Server SDK)
  │ X-API-KEY, player JWTs held server-side
  ▼
Gamers Game API
```

The client never holds a credential. The bridge holds the API key, performs the email/code flow on the player's behalf, and keeps the resulting JWT in the SDK's token store — it is never sent back to the client.

## 2. Build and run

Requires **JDK 25**. The Java Server SDK itself targets Java 17, so your own game-server does not have to move — only this reference build does.

The server depends on `com.gamers:gamers-server-java`, which is not on Maven Central, so the SDK has to be installed locally first. From the repo root:

```bash
npm run build-reference-server
```

Or by hand:

```bash
cd sdks/server-java
./mvnw -B install -DskipTests
./mvnw -B -f ../reference-server/pom.xml package
```

Then:

```bash
export GAMERS_API_KEY=your-api-key
export GAMERS_API_BASE_URL=https://api.gamers.dev
export GAMERS_DEV_TOKEN_ENABLED=true
export GAMERS_DEV_TOKEN_SECRET=$(openssl rand -hex 32)
export GAMERS_DEV_TOKEN_ACCESS_KEY=$(openssl rand -hex 32)
java -jar sdks/reference-server/target/gamers-reference-server-1.0.0.jar
```

> `https://api.gamers.bet` is production. Point this at the test environment.

The server refuses to start on a configuration it cannot serve: `gamers.ws.require-auth=true` without
a token secret would reject every handshake, and an enabled dev-token endpoint without a secret or an
access key would fail — or worse, mint tokens for anyone. Both fail at startup rather than at request
time.

### Dev-token quickstart

When `gamers.ws.require-auth=true`, the WebSocket handshake requires a token. Ask the reference server for one, presenting the shared access key:

```bash
curl -X POST http://localhost:8080/dev/session-token \
  -H "Content-Type: application/json" \
  -H "X-Gamers-Dev-Access-Key: $GAMERS_DEV_TOKEN_ACCESS_KEY" \
  -d '{"playerId":"your-stable-player-id"}'
```

Connect with the token:

```bash
ws://localhost:8080/ws/gamers?token=<token>
```

A handshake with a missing or expired token is rejected with `401`.

The token is a compact HMAC-SHA256 JWT-like token signed with `gamers.dev.token.secret`. The endpoint
mints a token for **any** player id, so it is gated on `gamers.dev.token.access-key` and throttled to
30 requests per minute per caller. Set strong values for both, keep them private, and disable the
endpoint in production (`gamers.dev.token.enabled=false`) once you have replaced handshake
authentication with your own signed session token.

### Configuration

| Property | Default | Purpose |
|---|---|---|
| `gamers.api.key` | `$GAMERS_API_KEY` | Game API key |
| `gamers.api.base-uri` | `$GAMERS_API_BASE_URL`, else `https://api.gamers.dev` | Game API base URI |
| `server.port` | `$PORT`, else `8080` | HTTP/WebSocket port |
| `gamers.ws.require-auth` | `true` | Reject unauthenticated handshakes. Requires a token secret |
| `gamers.ws.allowed-origins` | *(empty)* | Comma-separated handshake origins. Empty means same-origin only, so browser clients are rejected with `403`; native clients send no `Origin` and are unaffected |
| `gamers.dev.token.enabled` | `false` | Enable `POST /dev/session-token` |
| `gamers.dev.token.secret` | `$GAMERS_DEV_TOKEN_SECRET` | Secret used to sign dev tokens |
| `gamers.dev.token.access-key` | `$GAMERS_DEV_TOKEN_ACCESS_KEY` | Shared key required in `X-Gamers-Dev-Access-Key` to mint a token |
| `gamers.dev.token.ttl-minutes` | `60` | Token validity |
| `spring.web.socket.server.max-text-message-buffer-size` | `65536` | Max inbound WebSocket text message in bytes |
| `gamers.ws.ping-interval-seconds` | `30` | How often the server pings each client |
| `gamers.ws.pong-timeout-seconds` | `90` | Close session if no pong within this time |

## 3. The protocol

JSON envelopes on `ws://localhost:8080/ws/gamers`. Every envelope — inbound and outbound — carries `protocolVersion` and `type`; every reply also carries the `correlationId` of the request that caused it.

```json
{ "protocolVersion": 1, "type": "tournament:join", "correlationId": "…", "tournamentId": "trn_123" }
```

### Client → Server

| `type` | Fields |
|---|---|
| `auth:request` | `email` |
| `auth:submit-code` | `email`, `code` |
| `event:discover` | `status`?, `offset`?, `limit`? |
| `event:join` | `eventId` |
| `tournament:discover` | `status`?, `offset`?, `limit`? |
| `tournament:join` | `tournamentId` |
| `tournament:entry-start` | `tournamentId`, `entryId` |
| `tournament:attempt` | `tournamentId`, `entryId` |
| `tournament:submit-scores` | `tournamentId`, `entryId`, `attemptId`, `scores` (list of `{metricId, score}`) |
| `tournament:leaderboard` | `tournamentId`, `metricId`?, `offset`?, `limit`? |
| `profile:request` | — |

### Server → Client

| `type` | Purpose |
|---|---|
| `auth:code-requested` | Verification code requested |
| `auth:result` | Authentication result |
| `event:discover-result` | List of available events |
| `event:join-result` | Event join result |
| `tournament:discover-result` | List of available tournaments |
| `tournament:join-result` | Tournament join result |
| `tournament:entry-started` | Entry start result |
| `tournament:attempt-created` | Attempt created |
| `tournament:scores-submitted` | Scores submitted |
| `tournament:leaderboard` | Leaderboard snapshot |
| `profile:result` | Player profile and balance |
| `error` | Safe player-facing error: `code`, `message`, `retryable` |

> **Warning:** `tournament:submit-scores` is a development/testing path. Real games must validate
> scores server-side before calling the Game API; do not accept client-submitted scores as
> authoritative.

### The two rules

1. **Every inbound message produces exactly one reply carrying the same `correlationId`** — including parse failures, unknown message types, and unexpected exceptions. The Unity client keys its pending requests on that id. A swallowed message does not fail fast; it leaves the caller waiting for its full 30-second timeout.
2. **Every envelope carries `protocolVersion`**, currently `1` (`Protocol.VERSION`). A missing, non-numeric, or mismatched version is rejected with `UNSUPPORTED_PROTOCOL_VERSION` rather than parsed optimistically, so a shipped mobile or console build cannot silently misread a newer server. Add fields freely within a version; increment only for a breaking change, and in lockstep with `GamersClientFlow.ProtocolVersion` in the Unity package.

An unrecognised or missing `type` is answered with `UNKNOWN_MESSAGE`; a payload that is not valid JSON gets `MALFORMED_MESSAGE`. Both still carry the request's `correlationId` when it can be read.

### Event display fallback

The Game API `Event` model does not expose `name`, `startAt`, or `endAt`. The reference server display
data for `event:discover-result` falls back to:

- `name` → the `id` of the event
- `startDate` → `createdAt`
- `endDate` → `updatedAt`

This is a deliberate fallback so discovery replies can still be rendered. It is a limitation of the
underlying API model, not the reference server.

## 4. How it is wired

Five design decisions are worth copying:

- **One shared `GamersServerClient` for the whole process** (`WebSocketConfig`). It is thread-safe, and it owns the HTTP connection pool and the player token store. One per session would mean one connection pool per player.
- **Player identity is established at handshake time**, in `GamersHandshakeInterceptor`, before the socket opens — not per message. The identity comes from a dev token or from your own verified session token.
- **`SessionState` holds the per-connection player id, and it is never read from message content.** A client that could name its own player id could act as any player. This is the single most important line in the whole bridge.
- **Each session is wrapped in `ConcurrentWebSocketSessionDecorator`.** Replies are written from `CompletableFuture` completion threads, and a raw `WebSocketSession` does not support concurrent sends.
- **`MessageDispatcher` translates `GamersSdkException` into safe `error` replies.** Raw Game API errors are not forwarded to the client; player JWTs are held server-side and never logged.
- **`SessionRateLimiter`** enforces a per-session token bucket (30 messages per 10 seconds). Excess messages receive `RATE_LIMITED` with a `retryable` flag.
- **Ping/pong keepalive** closes unresponsive clients after `gamers.ws.pong-timeout-seconds`.

## 5. Scope

The bridge covers the player-facing journey:

- Request and submit an auth code
- Discover and join events
- Discover and join tournaments
- Start a tournament entry
- Create an attempt
- Submit practice scores
- Request a tournament leaderboard
- Request player profile and balance

Everything administrative — creating and managing events and tournaments, prize distribution, templates — is **server-authoritative on purpose** and must never be driven by a message from a game client. Call the Server SDK directly from your own trusted code paths. The SDK exposes all 53 Game API operations; this bridge deliberately exposes only the player journey.

## 6. What you must replace

These are deliberate simplifications, not bugs. Every one of them has to go before a real deployment.

### Dev-token authentication is not your auth

The reference server issues its own dev tokens with `POST /dev/session-token`. This is a convenience
for development and testing. Replace it with a check against your own auth system — typically a
signed session token issued when the player logged into your game — and derive the player id from
that. Set `gamers.ws.require-auth=true` so a handshake without a token is rejected outright.

### Player identity depends on the token

A stable player id survives reconnects as long as the same signed token is presented. The SDK's
in-memory token store is keyed by that player id, so the token must be stable across sessions. A real
integration maps the connection to **your** stable player id. `SessionState.isAuthenticated()` tells
you which case you are in — refuse sensitive operations when it is false.

### There is no persistence

Session state is in memory in a single process. Restart and every player is unauthenticated. There is
no clustering, no sticky-session handling, and no shared token store. A real deployment needs a
`TokenProvider` backed by something durable and shared (Redis, your identity service) rather than the
SDK's in-memory default.

### There is no authorization beyond identity

The dispatcher performs the action the message asks for. It does not check whether *this* player may
join *that* tournament, whether they have already joined, or whether your game's own rules permit it.
Those checks are yours, and they belong before the SDK call.

## 7. Operational notes

- **Rate limits.** The Game API allows 1,000 requests per minute per IP. One game-server behind one
  egress IP shares that budget across all its players. The reference server adds per-session rate
  limiting (30 messages per 10 seconds) to protect itself.
- **Logging.** Errors are logged with the session id. Player JWTs are never logged and never sent to
  the client. Set `LOG_FORMAT=json` for structured logs.
- **Containers.** `sdks/reference-server/Dockerfile` builds from the monorepo root (it needs the Java
  SDK sources), runs as a non-root user, sizes the heap from the container limit, honours `$PORT`, and
  shuts down gracefully so in-flight requests and open sessions are closed on redeploy. `/health` is
  the health-check path.
- **Known issues** are tracked in `LIMITATIONS.md` alongside the source, so you can tell "already
  known" from "new" before filing anything.

## 8. Support

Send the session id from the server logs and the `correlationId` of the request, and say what you
expected the reply to be. Those two ids trace a request end to end.
