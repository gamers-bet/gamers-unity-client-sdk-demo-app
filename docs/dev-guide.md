# Game API Developer Guide 
Version 1.0.0

Welcome to the Game API Developer Guide. This document provides everything you need to integrate your game with our 
platform, manage events, and interact with our developer portal.

## 1. Introduction

The Game API is a RESTful service that allows you to create and manage competitive gaming events with blockchain integration. 
You can create events, cancel events, start events, complete events, submit event results and manage player participation.

**Currency**: All monetary values (balances, stakes, prizes) are denominated in **USD** using standard decimal notation (2 decimal places). For example: `10.50` represents $10.50 USD.

This guide will walk you through the following:
- **Getting Started**: How to get your API keys and authenticate with the service.
- **Developer Portal**: An overview of the tools available in our developer website.
- **Core API Concepts**: A detailed look at managing events, players, and results.
- **Game Configuration**: Managing gameplay modes, metrics, and event templates.
- **Tournaments**: Extended competitions with multiple attempts and leaderboards.
- **Testing & Integration**: Information about testing tools and integration testing.
- **API Reference**: A link to our full, interactive API documentation.
- **[Version History](#10-version-history)**: A summary of changes in each version.

## 1.1 Test Environment

The following URLs make up the test environment for game integrations:

| Service | URL | Description |
|---------|-----|-------------|
| **Developer Portal** | [https://developer.gamers.dev/](https://developer.gamers.dev/) | Your central hub for managing your game integration |
| **Game API** | [https://api.gamers.dev/](https://api.gamers.dev/) | REST API endpoint for all game integration requests |
| **Player App** | [https://app.gamers.dev/](https://app.gamers.dev/) | Public-facing site where players load their wallets, view stats, manage their accounts, etc. |

## 1.2 Developer Portal

The [Developer Portal](https://developer.gamers.dev/) is your central hub for managing your integration. Here's what you can do:

-   **Dashboard**: Get an at-a-glance view of your active events, player statistics, and API usage.
-   **Games**: Register and manage your games. Add gameplay modes, metrics, and event templates for your game.
-   **Wallet**: View your token balance and transaction history. This is where you can see prize distributions and other financial activities.
-   **Test Users**: Create and manage test user accounts for development and testing purposes.
-   **Documentation**: Access the interactive API documentation.

## 1.3 SDKs

We provide official SDKs to speed up game integration:

- **.NET Server SDK** (`sdks/server-dotnet`): For game servers built with .NET/Unity dedicated servers. Targets `netstandard2.1` and `net8.0`.
- **Java Server SDK** (`sdks/server-java`): For game servers built with Java 17. A hand-written facade over a generated OpenAPI client with retry, auth, and configuration helpers.
- **Unity Client SDK** (`sdks/unity-client`): A server-authoritative helper for Unity clients. The client never calls the Game API directly; it communicates with your game server via `IGamersTransport`.

Each has its own guide, and there is a fourth for the reference game-server that bridges the two:

| Guide                                                                                       | Covers |
|---------------------------------------------------------------------------------------------|--------|
| [Unity Client SDK Guide](integration-guide.md)                                              | Installing the UPM package, implementing `IGamersTransport`, driving `GamersClientFlow`, compatibility, limitations, and known issues |
| [Unity Client C# API Reference](api-reference.md) | Every public client type, member, parameter, return value, exception, event, enum, and wire field |
| [Server SDK Guide](server-sdk-guide.md)                                                     | The .NET and Java SDKs side by side — configuration, player token storage, calling the API, and error handling |
| [Reference Game-Server Guide](reference-server-guide.md)                                    | Building and running the reference WebSocket bridge, and what must be replaced before you deploy anything based on it |

A **TypeScript server SDK** is planned but not yet available.

> **Distribution:** the SDKs are not yet published to any public package registry. Registered developers receive them directly from us, along with a reference game-server implementation and integration documentation. Do not expect to resolve them from NuGet, Maven Central, or npm.

The **.NET Server SDK** sends `X-API-KEY`, `X-API-Version`, `X-Request-ID`, and `Idempotency-Key` on write requests.

The **Java Server SDK** sends `X-API-KEY`, `X-API-Version`, `X-Request-ID`, and `Idempotency-Key` on every request by default (all configurable through `GamersServerClientConfig`).

The **Unity Client SDK** is transport-agnostic and does not send HTTP headers; it communicates with the developer's game server.

### 1.4 Environments

| Environment | URL | Purpose |
|-------------|-----|---------|
| Test | `https://api.gamers.dev` | Stable test environment for development and QA |
| Production | `https://api.gamers.bet` | Live production API |

Use the same `X-API-KEY` format in both environments. Production API keys must be rotated through the Developer Portal.

### 1.5 Compatibility and rate limits

The Game API enforces a rate limit of **1,000 requests per minute per IP address**. Exceeding this returns `429 Too Many Requests` with `RateLimit-Limit`, `RateLimit-Remaining`, and `RateLimit-Reset` headers.

Supported SDK languages and targets:

- **.NET Server SDK**: `netstandard2.1` and `net8.0`.
- **Java Server SDK**: Java 17.
- **Unity Client Helper**: Unity 2021.3 LTS+, installed as a UPM package (from disk, a Git URL, or a `.tgz` tarball).

See [1.3 SDKs](#13-sdks) for how to obtain them — they are not on public registries yet.

## 2. Getting Started

To integrate your game with the Game API, you'll need two types of authentication:

1. **API Key**: Required for all requests to authenticate your game/application
2. **Player JWT Tokens**: Required for player-specific actions like joining events or checking balances

This section covers how to obtain and use both authentication methods. Start by getting your API key from the Developer Portal, then implement the user authentication flow to allow players to interact with events through your game.

### 2.1. API Key

All requests to the Game API must include a valid API key in the `X-API-KEY` header. 
The API key is used to authenticate your requests and ensure that you have the necessary permissions to access the Game API.
You can generate and manage your API keys from the [Developer Portal](https://developer.gamers.dev/).
This key must be stored securely and should not be exposed in client-side code.

### 2.2. User Authentication

User-specific actions, such as adding a player to an event, require a player authentication token.
This is created through a two-step authentication process:

1.  **Request a Verification Code**: Ask the user to enter their Gamers.bet account email address. Make a `POST` request to `/auth/request` with the user's email address. This will send a 6-digit code to their email.

    ```http
    POST /auth/request
    Host: api.gamers.dev
    Content-Type: application/json
    X-API-KEY: YOUR_API_KEY

    {
      "email": "player@example.com"
    }
    ```

2.  **Verify the Code**: Ask the user to enter the verification code. Make a `POST` request to `/auth/verify` with the user's email and the code they received. A successful verification will return a player authentication token.

    ```http
    POST /auth/verify
    Host: api.gamers.dev
    Content-Type: application/json
    X-API-KEY: YOUR_API_KEY

    {
      "email": "player@example.com",
      "code": "123456"
    }
    ```

3.  **Use the JWT**: Include the received JWT in the `Authorization` header as a Bearer token for all subsequent user-authenticated requests.

    ```http
    GET /user/profile
    Host: api.gamers.dev
    Authorization: Bearer PLAYERS_JWT_TOKEN
    X-API-KEY: YOUR_API_KEY
    ```

The JWT should be securely stored server-side and used for all subsequent user-authenticated requests until it expires. **JWT tokens are valid for 30 days.**

## 3. Core API Concepts

### 3.1. Event Lifecycle

Managing a gaming event follows a specific lifecycle with the following statuses:

- **OPEN**: Event is created
- **STARTED**: Event is active and players can join
- **COMPLETED**: Event has ended, results submitted, processing prizes
- **REWARDED**: Event is complete and winners have been paid
- **CANCELLED**: Event was cancelled before completion
- **REFUNDED**: Event was cancelled and all stakes have been refunded

#### Event Management Steps:

1.  **Create Event** (`POST /event`): Define the event's parameters, including the game mode, prizes, and entry fee. Event starts in OPEN status.
2.  **Start Event** (`PATCH /event/{id}/start`): Changes status to STARTED and opens the event for players to join.
3.  **Player Entry** (`POST /entry/{eventId}`): A player joins the event. This requires user authentication.
4.  **Submit Results** (`POST /event/{id}/results`): After the event has concluded, submit the results for all participants.
5.  **Complete Event** (`PATCH /event/{id}/complete`): Changes status to COMPLETED, finalizes the event, and triggers the prize distribution process.
6.  **Cancel Event** (`PATCH /event/{id}/cancel`): Cancels the event and initiates refund process if there are entry fees.

### 3.2. Example: Creating and Running an Event

Here is a quick example of the API calls involved in running a simple event:

1.  **Create the event (with all optional parameters specified):**
    ```bash
    curl -X POST https://api.gamers.dev/event \
      -H "Content-Type: application/json" \
      -H "X-API-KEY: YOUR_API_KEY" \
      -d '{
            "gameplayModeId": "cm33et37c000012saq4pkrjzy",
            "publicAccess": true,
            "stakeType": "FLAT",
            "stakeAmount": 10,
            "maxEntries": 10,
            "prizes": [
                {
                    "position": 1,
                    "description": "First place",
                    "metricId": "cmde3pq280001lmv8haniv1ur",
                    "amount": 50
                },
                {
                    "position": 2,
                    "description": "Second place",
                    "metricId": "cmde3pq280001lmv8haniv1ur",
                    "amount": 30
                },
                {
                    "position": 3,
                    "description": "Third place",
                    "metricId": "cmde3pq280001lmv8haniv1ur",
                    "amount": 20
                }
            ]
        }'
    ```
    
    **Minimal example using defaults** (publicAccess=true, stakeType="FLAT", stakeAmount=1, maxEntries=100):
    ```bash
    curl -X POST https://api.gamers.dev/event \
      -H "Content-Type: application/json" \
      -H "X-API-KEY: YOUR_API_KEY" \
      -d '{
            "gameplayModeId": "cm33et37c000012saq4pkrjzy",
            "prizes": [
                {
                    "position": 1,
                    "description": "Winner takes all",
                    "metricId": "cmde3pq280001lmv8haniv1ur",
                    "amount": 100
                }
            ]
        }'
    ```

2.  **Start the event (using the ID returned from the previous step):**
    ```bash
    curl -X PATCH https://api.gamers.dev/event/evt_12345/start \
      -H "X-API-KEY: YOUR_API_KEY"
    ```

3.  **A player joins (requires a player's JWT):**
    ```bash
    curl -X POST https://api.gamers.dev/entry/evt_12345 \
      -H "Authorization: Bearer PLAYER_JWT_TOKEN" \
      -H "X-API-KEY: YOUR_API_KEY"
    ```

4.  **Submit event results:**
    ```bash
    curl -X POST https://api.gamers.dev/event/evt_12345/results \
      -H "Content-Type: application/json" \
      -H "X-API-KEY: YOUR_API_KEY" \
      -d '{
            "results": [
                {
                    "userId": "user_abc123",
                    "metricId": "cmde3pq280001lmv8haniv1ur",
                    "metricValue": 1500
                },
                {
                    "userId": "user_def456",
                    "metricId": "cmde3pq280001lmv8haniv1ur",
                    "metricValue": 1200
                }
            ]
        }'
    ```

    **Multi-metric format** (when tracking multiple metrics per player):
    ```bash
    curl -X POST https://api.gamers.dev/event/evt_12345/results \
      -H "Content-Type: application/json" \
      -H "X-API-KEY: YOUR_API_KEY" \
      -d '{
            "results": [
                {
                    "userId": "user_abc123",
                    "metrics": [
                        { "metricId": "metric_kills", "value": 15 },
                        { "metricId": "metric_time", "value": 245 }
                    ]
                },
                {
                    "userId": "user_def456",
                    "metrics": [
                        { "metricId": "metric_kills", "value": 12 },
                        { "metricId": "metric_time", "value": 310 }
                    ]
                }
            ]
        }'
    ```

    > **Note**: You can use either the legacy format (`userId`, `metricId`, `metricValue`) or the multi-metric format (`userId`, `metrics: [{ metricId, value }]`). Do not mix both formats in the same result object.

5.  **Complete the event (triggers prize distribution):**
    ```bash
    curl -X PATCH https://api.gamers.dev/event/evt_12345/complete \
      -H "X-API-KEY: YOUR_API_KEY"
    ```

6.  **Cancel an event (if needed):**
    ```bash
    curl -X PATCH https://api.gamers.dev/event/evt_12345/cancel \
      -H "X-API-KEY: YOUR_API_KEY"
    ```

### 3.3. Additional API Endpoints

Beyond the core event lifecycle, the API provides additional endpoints for managing and monitoring your integration:

#### Event Management
- `GET /event` - List all events for your game (ordered by creation date, newest first)
- `GET /entry/{eventId}` - List all entries for a specific event

#### User Management (requires JWT)
- `GET /user/profile` - Get authenticated user's profile information
- `GET /user/balance` - Get user's current token balance

#### Game Configuration
- `GET /game/gameplay` - Get available gameplay modes for your game
- `POST /game/gameplay` - Create a new gameplay mode
- `PUT /game/gameplay/{modeId}` - Update an existing gameplay mode
- `DELETE /game/gameplay/{modeId}` - Delete a gameplay mode
- `GET /game/metrics` - Get available metrics that can be tracked in events
- `POST /game/metrics` - Create a new metric
- `PUT /game/metrics/{metricId}` - Update an existing metric
- `DELETE /game/metrics/{metricId}` - Delete a metric

#### Team Management
- `POST /team/events/{eventId}/teams` - Create a team for a team event
- `GET /team/events/{eventId}/teams` - Get all teams for an event
- `GET /team/teams/{teamId}` - Get a team by ID
- `POST /team/teams/{teamId}/members` - Add a member to a team

#### Event Templates
- `GET /event-template` - Get all event templates
- `GET /event-template/{templateId}` - Get a specific template
- `POST /event-template/create-event` - Create an event from a template

**Note**: Event template management (create, update, delete) is available both through the [Developer Portal](https://developer.gamers.dev/) and via the Game API using `POST /event-template`, `PUT /event-template/{templateId}`, and `DELETE /event-template/{templateId}`.

#### Example: Listing Events
```bash
curl -X GET https://api.gamers.dev/event \
  -H "X-API-KEY: YOUR_API_KEY"
```

#### Example: Listing Event Entries
```bash
curl -X GET https://api.gamers.dev/entry/evt_12345 \
  -H "X-API-KEY: YOUR_API_KEY"
```

#### Example: Get User Balance
```bash
curl -X GET https://api.gamers.dev/user/balance \
  -H "Authorization: Bearer PLAYER_JWT_TOKEN" \
  -H "X-API-KEY: YOUR_API_KEY"
```

#### Example: Get Gameplay Modes
```bash
curl -X GET https://api.gamers.dev/game/gameplay \
  -H "X-API-KEY: YOUR_API_KEY"
```

#### Example: Get Metrics
```bash
curl -X GET https://api.gamers.dev/game/metrics \
  -H "X-API-KEY: YOUR_API_KEY"
```

### 3.4. Event Requirements and Validation

#### Creating Events - Required vs Optional Parameters

When creating an event via `POST /event`, only two parameters are **required**:
- `gameplayModeId` - ID of the gameplay mode to use
- `prizes` - Array of prize configurations (at least one prize required)

All other parameters are **optional** with the following default values:
- `publicAccess` - **Default: `true`** - Whether event is publicly accessible
- `stakeType` - **Default: `"FLAT"`** - Type of stake required (`FREE` or `FLAT`)
- `stakeAmount` - **Default: `1`** - Amount required to participate (minimum: 0)
- `maxEntries` - **Default: `100`** - Maximum number of participants (minimum: 2)
- `isTeamEvent` - **Default: `false`** - Whether this is a team-based event

#### Prize Configuration
- Prize `type` field is optional, defaults to `"PERCENTAGE"` (currently the only supported value for events)
- `position` is required (1 = first place, 2 = second, etc.)
- Prize `amount` values represent percentages (e.g., 50 for 50%)
- Prize amounts must total exactly **100**
- Each `position` must be unique per `(metricId, target)` combination
- Each prize must reference a valid `metricId` from your game's metrics
- At least one prize must be defined
- For team events: `target` can be `"TEAM"` (default) or `"INDIVIDUAL"`, with optional `cascadingPercentage` (0-100) and `teamPrizeIndex`

> **Note**: Tournament prizes use `prizeType` (with `"FIXED"` and `"PERCENTAGE"` options) instead of `type`. See the [Tournaments](#6-tournaments) section for details.

#### Stake Types
- `FREE`: No entry fee required
- `FLAT`: Fixed entry fee amount (default)

#### Event Constraints
- Minimum 2 players required (`maxEntries >= 2`)
- Events must have a valid `gameplayModeId` and `metricId`
- Results can only be submitted for events in `STARTED` status
- Results must be submitted for ALL confirmed participants (count must match exactly)

## 5. Game Configuration Management

The Game API provides comprehensive endpoints for managing your game's configuration, including gameplay modes, metrics, and event templates.

### 5.1. Gameplay Modes

Gameplay modes define the rules and configuration for events. The rake percentage and rake split are automatically inherited from your game's default settings and cannot be customized per mode.

#### Creating a Gameplay Mode
```bash
curl -X POST https://api.gamers.dev/game/gameplay \
  -H "Content-Type: application/json" \
  -H "X-API-KEY: YOUR_API_KEY" \
  -d '{
        "name": "Battle Royale",
        "description": "100 players compete until one remains",
        "gameMeta": "BR100"
      }'
```

**Parameters:**
- `name` (required) - Display name of the gameplay mode
- `description` (required) - Detailed description
- `gameMeta` (optional) - Game's internal identifier for this mode

**Note:** The `rake` and `rakeSplit` values are automatically set from your game's `defaultRake` and `defaultRakeSplit` settings and cannot be specified during creation.

#### Updating a Gameplay Mode
```bash
curl -X PUT https://api.gamers.dev/game/gameplay/gm_12345 \
  -H "Content-Type: application/json" \
  -H "X-API-KEY: YOUR_API_KEY" \
  -d '{
        "name": "Battle Royale Solo",
        "description": "Updated description for the mode",
        "gameMeta": "BR_SOLO"
      }'
```

**Note:** All fields are optional when updating. Only provided fields will be changed. The `rake` and `rakeSplit` values cannot be modified as they are tied to your game's default settings.

#### Deleting a Gameplay Mode
```bash
curl -X DELETE https://api.gamers.dev/game/gameplay/gm_12345 \
  -H "X-API-KEY: YOUR_API_KEY"
```

**Important:** Cannot delete a mode that is currently used by any events.

### 5.2. Game Metrics

Metrics define what player performance data can be tracked and used for determining event winners.

#### Creating a Metric
```bash
curl -X POST https://api.gamers.dev/game/metrics \
  -H "Content-Type: application/json" \
  -H "X-API-KEY: YOUR_API_KEY" \
  -d '{
        "name": "Kills",
        "description": "Number of eliminations"
      }'
```

**Parameters:**
- `name` (required) - Display name of the metric
- `description` (required) - Description of what this metric measures

#### Updating a Metric
```bash
curl -X PUT https://api.gamers.dev/game/metrics/metric_12345 \
  -H "Content-Type: application/json" \
  -H "X-API-KEY: YOUR_API_KEY" \
  -d '{
        "description": "Updated description for kills"
      }'
```

**Note:** All fields are optional when updating.

#### Deleting a Metric
```bash
curl -X DELETE https://api.gamers.dev/game/metrics/metric_12345 \
  -H "X-API-KEY: YOUR_API_KEY"
```

**Important:** Cannot delete a metric that is currently used by any event prizes.

### 5.3. Event Templates

Event templates are pre-configured event setups that streamline event creation by eliminating the need to specify all parameters each time. Templates ensure consistency across similar events and are particularly useful for recurring tournaments or standardized game modes.

#### Template Management Workflow

**Creating Templates:**
Templates are created and managed through the **Developer Console** (web application), not via the API. This allows you to:
1. Navigate to the Templates section in the Developer Console
2. Define your event configuration including:
   - Gameplay mode
   - Stake type and amount
   - Maximum entries
   - Prize structure
   - **Team settings** (optional): Enable team play
3. Save the template with a unique name

#### Team Event Templates

Templates can be configured for team-based play where players compete as groups rather than individuals. When creating a team template in the Developer Console:

1. **Enable Team Event**: Check the "Team Event" option

**Team Event Behavior:**
- Prizes are awarded to winning **teams** rather than individuals
- Team members share the prize pool equally
- Results are aggregated per team (total score, average, high score)
- The `teams` parameter is required when creating events from team templates

**Using Templates via API:**
Once templates are created in the Developer Console, you can use the API to:
- Retrieve available templates
- Create events from templates
- View template details

This separation ensures that template configuration is managed centrally while event creation can be automated through your game integration.

#### Listing All Templates
```bash
curl -X GET https://api.gamers.dev/event-template \
  -H "X-API-KEY: YOUR_API_KEY"
```

#### Getting a Specific Template
```bash
curl -X GET https://api.gamers.dev/event-template/tpl_12345 \
  -H "X-API-KEY: YOUR_API_KEY"
```

**Note:** Event template management (create, update, delete) is available both through the [Developer Portal](https://developer.gamers.dev/) and via the Game API. The API provides full template CRUD plus event creation from templates.

#### Creating an Event from a Template

This is the primary way to use templates in your game integration. This endpoint creates a new event using a template's configuration and automatically adds specified participants. The event is automatically started after creation.

**Key Features:**
- **All-or-nothing validation**: All participants are validated before the event is created
- **Batch admission**: All participant reservations succeed before anyone is admitted. Individual funding transfers settle separately after admission.
- **Auth token based**: Participants are identified by their authentication tokens (obtained via `/auth/request` → `/auth/verify`)

#### Authentication Flow

Participant tokens are obtained via the standard authentication flow:

1. Each player authenticates via `/auth/request` → `/auth/verify`
2. Your game server stores the returned tokens
3. When creating an event, pass all participant tokens to the endpoint

> **Important:** Auth tokens are **game-scoped**. The tokens passed as `participantTokens` or `memberTokens` must have been obtained via `/auth/verify` using the **same game's API key** (`X-API-KEY`) that you use to call `POST /event-template/create-event`. Tokens issued for a different game will fail with `GAME_MISMATCH` in the `failures` array.

#### Individual Event Example

```bash
curl -X POST https://api.gamers.dev/event-template/create-event \
  -H "Content-Type: application/json" \
  -H "X-API-KEY: YOUR_API_KEY" \
  -d '{
        "templateName": "1v1 Ranked Match",
        "participantTokens": [
          "eyJhbGciOiJIUzI1NiIs...",
          "eyJhbGciOiJIUzI1NiIs..."
        ]
      }'
```

**Parameters:**
- `templateName` (required) - Name of the template to use
- `participantTokens` (required for non-team events) - Array of auth tokens (minimum: 2)

**Success Response (201)**:
```json
{
  "event": {
    "id": "evt_001",
    "gameId": "game_fps_001",
    "gameModeId": "gm_001",
    "status": "STARTED",
    "stakeAmount": 10,
    "maxEntries": 10,
    "createdAt": "2025-01-31T10:00:00Z"
  },
  "participants": [
    { "entryId": "entry_001", "userId": "user_abc123", "address": "0x1234..." },
    { "entryId": "entry_002", "userId": "user_def456", "address": "0x5678..." }
  ],
  "blockchainTxHash": null
}
```

`blockchainTxHash` remains present for compatibility and is always `null`. It is deprecated and will be removed in a later API revision. Use the event and participant fields to continue gameplay after admission.

#### Team Event Example

When using a team template, provide the `teams` parameter with member tokens:

```bash
curl -X POST https://api.gamers.dev/event-template/create-event \
  -H "Content-Type: application/json" \
  -H "X-API-KEY: YOUR_API_KEY" \
  -d '{
        "templateName": "2v2 Team Battle",
        "teams": [
          {
            "name": "Alpha Squad",
            "memberTokens": ["token_player1", "token_player2"]
          },
          {
            "name": "Beta Force", 
            "memberTokens": ["token_player3", "token_player4"]
          }
        ]
      }'
```

**Team Parameters:**
- `templateName` (required) - Name of the team template to use
- `teams` (required for team templates) - Array of team objects:
  - `name` (required) - Display name for the team
  - `memberTokens` (required) - Array of auth tokens for team members (minimum: 1 per team)

**Success Response (201)**:
```json
{
  "event": {
    "id": "evt_team_001",
    "gameId": "game_fps_001",
    "gameModeId": "gm_team_battle",
    "status": "STARTED",
    "stakeType": "FLAT",
    "stakeAmount": 5,
    "maxEntries": 4,
    "isTeamEvent": true,
    "createdAt": "2025-01-31T11:00:00Z",
    "EventEntries": [
      { "id": "entry_001", "userId": "user_001" },
      { "id": "entry_002", "userId": "user_002" },
      { "id": "entry_003", "userId": "user_003" },
      { "id": "entry_004", "userId": "user_004" }
    ],
    "EventPrizes": [{ "id": "prize_001", "position": 1, "amount": 100 }],
    "Teams": [
      { "id": "team_alpha", "name": "Alpha Squad", "TeamMembers": [
        { "eventEntryId": "entry_001", "EventEntry": { "id": "entry_001", "userId": "user_001" } },
        { "eventEntryId": "entry_002", "EventEntry": { "id": "entry_002", "userId": "user_002" } }
      ] },
      { "id": "team_beta", "name": "Beta Force", "TeamMembers": [
        { "eventEntryId": "entry_003", "EventEntry": { "id": "entry_003", "userId": "user_003" } },
        { "eventEntryId": "entry_004", "EventEntry": { "id": "entry_004", "userId": "user_004" } }
      ] }
    ]
  },
  "participants": [
    { "entryId": "entry_001", "userId": "user_001", "address": "wallet_001" },
    { "entryId": "entry_002", "userId": "user_002", "address": "wallet_002" },
    { "entryId": "entry_003", "userId": "user_003", "address": "wallet_003" },
    { "entryId": "entry_004", "userId": "user_004", "address": "wallet_004" }
  ],
  "blockchainTxHash": null
}
```

**Use Cases:**
- **Matchmaking**: After your matchmaking system pairs players, use a template to quickly create the event
- **Private Tournaments**: Create invite-only tournaments where you know all participants in advance
- **Scheduled Events**: Pre-configure event settings in the Developer Console, then trigger event creation at the scheduled time
- **Recurring Events**: Use the same template repeatedly for daily/weekly tournaments with identical configurations
- **Team Competitions**: Create team-based events where groups of players compete together

**Example Workflow (Individual Event):**
1. In the Developer Console, create a template called "Daily 1v1 Duel" with:
   - Gameplay mode: "Duel"
   - Stake: $5 FLAT
   - Max entries: 2
   - Prize: 100% to winner based on "Score" metric
2. In your game server, when two players are matched:
   ```javascript
   // Players authenticate and your server stores their tokens
   const player1Token = "eyJhbGciOiJIUzI1NiIs..."; // from /auth/verify
   const player2Token = "eyJhbGciOiJIUzI1NiIs..."; // from /auth/verify
   
   // Create event from template
   const response = await fetch('https://api.gamers.dev/event-template/create-event', {
     method: 'POST',
     headers: {
       'Content-Type': 'application/json',
       'X-API-KEY': YOUR_API_KEY
     },
     body: JSON.stringify({
       templateName: "Daily 1v1 Duel",
       participantTokens: [player1Token, player2Token]
     })
   });
   
   const result = await response.json();
   // Event created, started, and both players are entered
   // result.event.id - use to track the match
   // result.blockchainTxHash is a deprecated compatibility field and is always null
   ```
3. After the match ends, submit results using the event ID

**Example Workflow (Team Event):**
1. In the Developer Console, create a team template called "2v2 Team Match" with:
   - Gameplay mode: "Team Deathmatch"
   - Stake: $10 FLAT
   - Max entries: 4
   - Team Event: ✓ Enabled
   - Prize: 100% to winning team based on "Score" metric
2. In your game server, when teams are formed:
   ```javascript
   // Players authenticate and your server stores their tokens
   const team1 = { name: "Red Team", memberTokens: [token1, token2] };
   const team2 = { name: "Blue Team", memberTokens: [token3, token4] };
   
   // Create team event from template
   const response = await fetch('https://api.gamers.dev/event-template/create-event', {
     method: 'POST',
     headers: {
       'Content-Type': 'application/json',
       'X-API-KEY': YOUR_API_KEY
     },
     body: JSON.stringify({
       templateName: "2v2 Team Match",
       teams: [team1, team2]
     })
   });
   
   const result = await response.json();
   // Event created with teams - each player entered and linked to their team
   ```
3. After the match ends, submit individual player results
4. The system aggregates results by team and awards prizes to winning team members

#### Team Event Payload Examples

The following examples show the request payloads for different team configurations. All requests use:
- **Endpoint:** `POST /event-template/create-event`
- **Headers:** `Content-Type: application/json`, `X-API-KEY: YOUR_API_KEY`

##### 3v3 Squad Battle

Two teams of 3 players each competing head-to-head.

**Request Payload:**
```json
{
  "templateName": "3v3 Squad Battle",
  "teams": [
    {
      "name": "Team Alpha",
      "memberTokens": [
        "eyJhbGciOi...player1_token",
        "eyJhbGciOi...player2_token",
        "eyJhbGciOi...player3_token"
      ]
    },
    {
      "name": "Team Bravo",
      "memberTokens": [
        "eyJhbGciOi...player4_token",
        "eyJhbGciOi...player5_token",
        "eyJhbGciOi...player6_token"
      ]
    }
  ]
}
```

**Success Response (201):**
```json
{
  "event": {
    "id": "evt_3v3_001",
    "gameId": "game_fps_001",
    "gameModeId": "gm_team_battle",
    "status": "STARTED",
    "stakeType": "FLAT",
    "stakeAmount": 5,
    "isTeamEvent": true,
    "maxEntries": 6,
    "EventEntries": [
      { "id": "entry_001", "userId": "user_001" },
      { "id": "entry_002", "userId": "user_002" },
      { "id": "entry_003", "userId": "user_003" },
      { "id": "entry_004", "userId": "user_004" },
      { "id": "entry_005", "userId": "user_005" },
      { "id": "entry_006", "userId": "user_006" }
    ],
    "EventPrizes": [{ "id": "prize_001", "position": 1, "amount": 100 }],
    "Teams": [
      { "id": "team_001", "name": "Team Alpha", "TeamMembers": [
        { "eventEntryId": "entry_001", "EventEntry": { "id": "entry_001", "userId": "user_001" } },
        { "eventEntryId": "entry_002", "EventEntry": { "id": "entry_002", "userId": "user_002" } },
        { "eventEntryId": "entry_003", "EventEntry": { "id": "entry_003", "userId": "user_003" } }
      ] },
      { "id": "team_002", "name": "Team Bravo", "TeamMembers": [
        { "eventEntryId": "entry_004", "EventEntry": { "id": "entry_004", "userId": "user_004" } },
        { "eventEntryId": "entry_005", "EventEntry": { "id": "entry_005", "userId": "user_005" } },
        { "eventEntryId": "entry_006", "EventEntry": { "id": "entry_006", "userId": "user_006" } }
      ] }
    ]
  },
  "participants": [
    { "entryId": "entry_001", "userId": "user_001", "address": "wallet_001" },
    { "entryId": "entry_002", "userId": "user_002", "address": "wallet_002" },
    { "entryId": "entry_003", "userId": "user_003", "address": "wallet_003" },
    { "entryId": "entry_004", "userId": "user_004", "address": "wallet_004" },
    { "entryId": "entry_005", "userId": "user_005", "address": "wallet_005" },
    { "entryId": "entry_006", "userId": "user_006", "address": "wallet_006" }
  ],
  "blockchainTxHash": null
}
```

##### 4v4 Competitive Match

Two teams of 4 players each.

**Request Payload:**
```json
{
  "templateName": "4v4 Competitive",
  "teams": [
    {
      "name": "Phoenix Squad",
      "memberTokens": [
        "token_phoenix_1",
        "token_phoenix_2",
        "token_phoenix_3",
        "token_phoenix_4"
      ]
    },
    {
      "name": "Shadow Unit",
      "memberTokens": [
        "token_shadow_1",
        "token_shadow_2",
        "token_shadow_3",
        "token_shadow_4"
      ]
    }
  ]
}
```

##### Multi-Team Battle Royale (4 Teams)

Four teams competing in a free-for-all style event.

**Request Payload:**
```json
{
  "templateName": "Team Battle Royale",
  "teams": [
    {
      "name": "North Faction",
      "memberTokens": ["token_n1", "token_n2"]
    },
    {
      "name": "South Faction",
      "memberTokens": ["token_s1", "token_s2"]
    },
    {
      "name": "East Faction",
      "memberTokens": ["token_e1", "token_e2"]
    },
    {
      "name": "West Faction",
      "memberTokens": ["token_w1", "token_w2"]
    }
  ]
}
```

The `201` response has eight participant records (`entryId`, `userId`, `address`) and four teams with nested membership under `event.Teams[].TeamMembers[].EventEntry`, as in the 3v3 example. The required `blockchainTxHash` is `null`.

##### Asymmetric Teams (3v2)

Teams with different player counts (useful for handicap matches).

**Request Payload:**
```json
{
  "templateName": "Asymmetric Match",
  "teams": [
    {
      "name": "Challengers",
      "memberTokens": ["token_c1", "token_c2", "token_c3"]
    },
    {
      "name": "Champions",
      "memberTokens": ["token_ch1", "token_ch2"]
    }
  ]
}
```

#### Team Validation Error Responses

When team creation fails, the response includes details for each failed participant:

**Mixed Validation Failures (400):**
```json
{
  "success": false,
  "error": {
    "code": "PARTICIPANT_VALIDATION_FAILED",
    "message": "One or more participants failed validation",
    "failures": [
      {
        "token": "expired_token_abc",
        "reason": "EXPIRED_TOKEN",
        "userId": "user_123"
      },
      {
        "token": "broke_player_token",
        "reason": "INSUFFICIENT_BALANCE",
        "userId": "user_456",
        "balance": 5.00,
        "required": 10.00
      },
      {
        "token": "invalid_token_xyz",
        "reason": "INVALID_TOKEN"
      }
    ]
  },
  "timestamp": "2026-06-18T19:00:00.000Z"
}
```

**Possible `reason` values:**
| Reason | Description | Action |
|--------|-------------|--------|
| `INVALID_TOKEN` | Token malformed or unrecognized | Re-authenticate player |
| `EXPIRED_TOKEN` | Token has expired | Refresh or re-authenticate |
| `INSUFFICIENT_BALANCE` | Wallet balance too low | Player needs to add funds |
| `NO_WALLET` | Player has no active wallet | Direct to wallet setup |

#### Validation Errors

If any participant fails validation, the entire request fails with detailed error information. No event is created.

**Invalid Token (400)**:
```json
{
  "success": false,
  "error": {
    "code": "PARTICIPANT_VALIDATION_FAILED",
    "message": "One or more participants failed validation",
    "failures": [
      { "token": "invalid_token_abc", "reason": "INVALID_TOKEN" }
    ]
  },
  "timestamp": "2026-06-18T19:00:00.000Z"
}
```

**Expired Token (400)**:
```json
{
  "success": false,
  "error": {
    "code": "PARTICIPANT_VALIDATION_FAILED",
    "message": "One or more participants failed validation",
    "failures": [
      { "token": "expired_token_xyz", "reason": "EXPIRED_TOKEN", "userId": "user_123" }
    ]
  },
  "timestamp": "2026-06-18T19:00:00.000Z"
}
```

**Insufficient Balance (400)**:
```json
{
  "success": false,
  "error": {
    "code": "PARTICIPANT_VALIDATION_FAILED",
    "message": "One or more participants failed validation",
    "failures": [
      {
        "token": "token_broke_user",
        "reason": "INSUFFICIENT_BALANCE",
        "userId": "user_456",
        "balance": 5.00,
        "required": 10.00
      }
    ]
  },
  "timestamp": "2026-06-18T19:00:00.000Z"
}
```

**No Wallet (400)**:
```json
{
  "success": false,
  "error": {
    "code": "PARTICIPANT_VALIDATION_FAILED",
    "message": "One or more participants failed validation",
    "failures": [
      { "token": "token_no_wallet", "reason": "NO_WALLET", "userId": "user_789" }
    ]
  },
  "timestamp": "2026-06-18T19:00:00.000Z"
}
```

**Multiple Failures (400)**:
```json
{
  "success": false,
  "error": {
    "code": "PARTICIPANT_VALIDATION_FAILED",
    "message": "One or more participants failed validation",
    "failures": [
      { "token": "token_1", "reason": "INVALID_TOKEN" },
      { "token": "token_2", "reason": "EXPIRED_TOKEN", "userId": "user_002" },
      { "token": "token_3", "reason": "INSUFFICIENT_BALANCE", "userId": "user_003", "balance": 3.50, "required": 10.00 }
    ]
  },
  "timestamp": "2026-06-18T19:00:00.000Z"
}
```

#### Integration Example

```javascript
class GameServerIntegration {
  constructor(apiKey) {
    this.apiKey = apiKey;
    this.baseUrl = 'https://api.gamers.dev';
  }

  // Create event with all matched players in one call
  async createMatchEvent(templateName, playerTokens) {
    const response = await fetch(`${this.baseUrl}/event-template/create-event`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-API-KEY': this.apiKey
      },
      body: JSON.stringify({
        templateName,
        participantTokens: playerTokens
      })
    });

    if (!response.ok) {
      const error = await response.json();
      
      // Handle validation failures
      if (error.error === 'PARTICIPANT_VALIDATION_FAILED') {
        console.error('Participant validation failed:', error.failures);
        
        // Notify affected players
        for (const failure of error.failures) {
          if (failure.reason === 'INSUFFICIENT_BALANCE') {
            // Notify player they need to add funds
            this.notifyInsufficientFunds(failure.userId, failure.balance, failure.required);
          } else if (failure.reason === 'EXPIRED_TOKEN') {
            // Request player to re-authenticate
            this.requestReauth(failure.userId);
          }
        }
        throw new Error('Match creation failed - participant validation error');
      }
      
      throw new Error(`API error: ${error.message}`);
    }

    const result = await response.json();
    
    // Event created successfully with all participants
    console.log(`Event ${result.event.id} created with ${result.participants.length} players`);
    
    return result;
  }
}
```

## 6. Tournaments

Tournaments are extended competitions that run over longer time periods, allowing players to make multiple attempts to achieve their best scores. Unlike single-attempt events, tournaments provide a more engaging competitive experience where players can improve their performance over time.

### 6.1. Tournament vs Event: When to Use Each

| Feature | Event | Tournament |
|---------|-------|------------|
| **Duration** | Single session | hours to weeks |
| **Competition Style** | all players compete at once | players compete any time during tournament duration |
| **Attempts** | One attempt per player | Multiple attempts allowed |
| **Scoring** | Final result at event end | Best score across all attempts |
| **Entry Window** | Join prior to game start | Join anytime during tournament duration |
| **Use Case** | Quick matches, matchmaking | Leaderboard competitions, seasonal challenges |

**Use Events for:**
- Real-time multiplayer matches
- Quick 1v1 or team competitions
- Matchmaking scenarios
- Immediate result requirements

**Use Tournaments for:**
- Daily/Weekly/Monthly high score challenges
- Seasonal leaderboard competitions
- Skill-based competitions with practice
- Asynchronous competitions across time zones

### 6.2. Tournament Lifecycle

```
DRAFT → OPENED → CLOSED → COMPLETED → REWARDED
          ↓         ↓
      CANCELLED → REFUNDED
```

- **DRAFT**: Tournament created, can be edited or deleted
- **OPENED**: Accepting entries, players can compete
- **CLOSED**: No new entries, existing players complete attempts
- **COMPLETED**: Competition ended, processing winners
- **REWARDED**: Prizes distributed to winners
- **CANCELLED/REFUNDED**: Tournament cancelled with automatic refunds

### 6.3. Tournament Configuration Options

| Parameter | Description | Default |
|-----------|-------------|---------|
| `name` | Tournament name (max 200 chars) | Optional |
| `description` | Tournament description | Optional |
| `gameplayModeId` | Gameplay mode to use | Required |
| `startAt` | Tournament start time | Required |
| `endAt` | Tournament end time | Required |
| `entryWindowMinutes` | Minutes to complete attempts after starting | 0 (unlimited) |
| `entryFeeType` | FIXED or VARIABLE | FIXED |
| `baseEntryFee` | Entry fee amount | 0 |
| `maxEntriesPerPlayer` | Max entries per player | null (unlimited) |
| `maxAttemptsPerEntry` | Max attempts per entry | null (unlimited) |
| `maxParticipants` | Max total participants | null (unlimited) |
| `minParticipants` | Minimum participants required (auto-cancels + refunds if not met) | 2 |
| `publicAccess` | Publicly accessible | true |
| `variableFees` | Discounted fees for subsequent entries | Optional |
| `prizes` | Prize structure (must total 100%) | Required |

### 6.4. Example: Running a Weekly Tournament

Here's a complete example of creating and managing a weekly high score tournament:

#### Step 1: Create the Tournament
```bash
curl -X POST https://api.gamers.dev/tournament \
  -H "Content-Type: application/json" \
  -H "X-API-KEY: YOUR_API_KEY" \
  -d '{
        "name": "Weekly High Score Challenge",
        "description": "Compete for the highest score this week!",
        "gameplayModeId": "gm_001",
        "startAt": "2025-01-15T00:00:00Z",
        "endAt": "2025-01-22T00:00:00Z",
        "entryWindowMinutes": 60,
        "entryFeeType": "FIXED",
        "baseEntryFee": 5,
        "maxEntriesPerPlayer": 3,
        "maxAttemptsPerEntry": 5,
        "maxParticipants": 100,
        "minParticipants": 10,
        "publicAccess": true,
        "prizes": [
            { "prizeType": "FIXED", "position": 1, "metricId": "metric_score", "amount": 50, "description": "1st Place" },
            { "prizeType": "FIXED", "position": 2, "metricId": "metric_score", "amount": 30, "description": "2nd Place" },
            { "prizeType": "FIXED", "position": 3, "metricId": "metric_score", "amount": 20, "description": "3rd Place" }
        ]
      }'
```

> **Note:** If the tournament does not reach `minParticipants` when completed, it is automatically cancelled and all entry fees are refunded to participants.

#### Step 2: Open the Tournament
```bash
curl -X PATCH https://api.gamers.dev/tournament/tourn_12345/open \
  -H "X-API-KEY: YOUR_API_KEY"
```

#### Step 3: Create an Entry for a Player
```bash
curl -X POST https://api.gamers.dev/tournament/tourn_12345/entries \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "X-API-KEY: YOUR_API_KEY"
```

#### Step 4: Start the Player's Entry (begins the entry window timer)
```bash
curl -X POST https://api.gamers.dev/tournament/tourn_12345/entries/entry_001/start \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "X-API-KEY: YOUR_API_KEY"
```

#### Step 5: Create an Attempt
```bash
curl -X POST https://api.gamers.dev/tournament/tourn_12345/entries/entry_001/attempts \
  -H "X-API-KEY: YOUR_API_KEY"
```

> ⚠️ **Breaking change, since 0.5.0:** `maxAttemptsPerEntry` is enforced atomically. Two attempts created at the same
> instant for one entry could previously both succeed and exceed the limit; the later one is now
> rejected with `400 MAX_ATTEMPTS_REACHED`. Create attempts **one at a time** — wait for each
> response before requesting the next — and treat `MAX_ATTEMPTS_REACHED` as authoritative.
> Sequential callers are unaffected.

#### Step 6: Submit Scores for the Attempt
```bash
curl -X POST https://api.gamers.dev/tournament/tourn_12345/entries/entry_001/attempts/attempt_001/scores \
  -H "Content-Type: application/json" \
  -H "X-API-KEY: YOUR_API_KEY" \
  -d '{
        "scores": [
            { "metricId": "metric_score", "value": 15000 }
        ]
      }'
```

#### Step 7: Get Leaderboard
```bash
curl -X GET https://api.gamers.dev/tournament/tourn_12345/leaderboard \
  -H "X-API-KEY: YOUR_API_KEY"
```

**Response:**
```json
{
  "tournamentId": "tourn_12345",
  "metricId": "metric_score",
  "metricName": "Score",
  "sortDirection": "desc",
  "prizePool": {
    "estimatedTotalPrizePool": 200.00,
    "estimatedWinnerPool": 180.00,
    "rake": 0.10,
    "playerCount": 20
  },
  "entries": [
    { "rank": 1, "userId": "user_abc123", "username": "ProGamer", "bestScore": 15000, "attemptCount": 1, "estimatedPrize": 90.00 },
    { "rank": 2, "userId": "user_def456", "username": "Challenger", "bestScore": 12500, "attemptCount": 3, "estimatedPrize": 54.00 }
  ],
  "total": 2
}
```

> **Tip — Estimated Prizes**: The leaderboard response now includes `estimatedPrize` on each entry and a `prizePool` summary. These values show what each player would win if the tournament ended now, based on current standings, entry fees, and prize configuration (net of rake). For PERCENTAGE prizes, the distribution uses the same cascading logic as the actual payout. Use `GET /tournament/{id}/prize-pool` separately if you only need the gross prize pool total.

#### Multi-Metric Leaderboards (New)

For tournaments with multiple prize metrics (e.g., "High Score" + "Best Lap Time"), the leaderboard supports two view modes:

**By-Player View (Default):**
Aggregates player performance across all metrics. Players are ranked by total estimated prize. The `sumOfRanks` field provides a tiebreaker when prizes are equal.

```bash
# Default view - aggregates across all metrics
curl -X GET https://api.gamers.dev/tournament/tourn_12345/leaderboard \
  -H "X-API-KEY: YOUR_API_KEY"
```

**Response (by-player):**
```json
{
  "tournamentId": "tourn_12345",
  "view": "by-player",
  "prizePool": { "estimatedTotalPrizePool": 300, "estimatedWinnerPool": 270, "rake": 0.10, "playerCount": 30 },
  "entries": [
    {
      "rank": 1,
      "userId": "user_abc123",
      "username": "ProGamer",
      "metrics": [
        { "metricId": "metric_score", "metricName": "High Score", "sortDirection": "desc", "metricRank": 1, "bestScore": 15000, "estimatedPrize": 90 },
        { "metricId": "metric_time", "metricName": "Best Lap Time", "sortDirection": "asc", "metricRank": 3, "bestScore": 45.2, "estimatedPrize": 18 }
      ],
      "totalEstimatedPrize": 108,
      "sumOfRanks": 4,
      "totalAttemptCount": 5
    }
  ],
  "total": 30
}
```

**By-Metric View (with metricId parameter):**
Returns the classic single-metric leaderboard for backward compatibility.

```bash
# Single metric view - for backward compatibility
curl -X GET "https://api.gamers.dev/tournament/tourn_12345/leaderboard?metricId=metric_score" \
  -H "X-API-KEY: YOUR_API_KEY"
```

**Sorting & Tiebreakers:**
- **Primary sort**: Total estimated prize (highest first)
- **Tiebreaker**: Sum of metric ranks (lowest first). Example: A player ranked 1st in Score and 3rd in Time has sumOfRanks=4.

Use the by-player view to show overall standings in multi-metric tournaments. Use the by-metric view to display category-specific leaderboards.

#### Step 8: Complete the Tournament (at end date)
```bash
curl -X PATCH https://api.gamers.dev/tournament/tourn_12345/complete \
  -H "X-API-KEY: YOUR_API_KEY"
```

> ⚠️ **Breaking change, since 0.5.0:** calling this on a tournament that is already `COMPLETED` returns `200` with the
> tournament's current state instead of `400 TOURNAMENT_CANNOT_CHANGE_STATUS`, so a retry after a
> partial failure converges instead of failing permanently. Transitions from any other status still
> return `400`.
>
> Because of this, **do not trigger post-completion work — payouts, notifications, results screens —
> solely on this call returning `200`**. A retry now returns `200` again where it previously stopped
> at the `400`, so gate that work on your own record of whether you have already handled the
> tournament. If you need the authoritative status, read it back with
> `GET /tournament/{tournamentId}`.

### 6.5. Variable Entry Fees

Encourage multiple entries with discounted fees for subsequent entries:

```bash
curl -X POST https://api.gamers.dev/tournament \
  -H "Content-Type: application/json" \
  -H "X-API-KEY: YOUR_API_KEY" \
  -d '{
        "name": "Multi-Entry Challenge",
        "gameplayModeId": "gm_001",
        "startAt": "2025-01-15T00:00:00Z",
        "endAt": "2025-01-22T00:00:00Z",
        "entryFeeType": "VARIABLE",
        "baseEntryFee": 10,
        "maxEntriesPerPlayer": 5,
        "variableFees": [
            { "entryNumber": 2, "fee": 8 },
            { "entryNumber": 3, "fee": 5 },
            { "entryNumber": 4, "fee": 3 },
            { "entryNumber": 5, "fee": 1 }
        ],
        "prizes": [
            { "prizeType": "FIXED", "position": 1, "metricId": "metric_score", "amount": 100 }
        ]
      }'
```

This configuration charges:
- 1st entry: $10
- 2nd entry: $8
- 3rd entry: $5
- 4th entry: $3
- 5th entry: $1

### 6.6. Tournament API Endpoints Summary

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/tournament` | GET | List all tournaments |
| `/tournament` | POST | Create a tournament |
| `/tournament/{id}` | GET | Get tournament details |
| `/tournament/{id}` | PATCH | Update a DRAFT tournament |
| `/tournament/{id}` | DELETE | Delete a DRAFT tournament |
| `/tournament/{id}/open` | PATCH | Open for entries |
| `/tournament/{id}/close` | PATCH | Close to new entries |
| `/tournament/{id}/complete` | PATCH | Complete and process winners |
| `/tournament/{id}/cancel` | PATCH | Cancel with refunds |
| `/tournament/{id}/prize-pool` | GET | Get estimated gross prize pool (for display) |
| `/tournament/{id}/leaderboard` | GET | Get current standings |
| `/tournament/{id}/entries` | GET | List entries (optionally filter by `userId`) |
| `/tournament/{id}/entries` | POST | Create an entry |
| `/tournament/{id}/entries/{entryId}` | GET | Get entry details |
| `/tournament/{id}/entries/{entryId}/start` | POST | Start entry window |
| `/tournament/{id}/entries/{entryId}/attempts` | POST | Create attempt |
| `/tournament/{id}/entries/{entryId}/attempts/{attemptId}/scores` | POST | Submit scores |

#### Missing or deleted tournaments

Every tournament endpoint above resolves the tournament ID **within your game**. When no tournament with that ID
belongs to your game — it was never created, it has been deleted, or it belongs to another game — the API answers:

```
HTTP/1.1 404 Not Found
```
```json
{
  "success": false,
  "error": {
    "code": "TOURNAMENT_NOT_FOUND",
    "message": "Tournament not found"
  },
  "timestamp": "2026-09-14T04:35:00.000Z"
}
```

A `404 TOURNAMENT_NOT_FOUND` is **permanent**. Retrying the same ID will never succeed, so treat it as a signal to
**drop the tournament from your polling set** (prize-pool, leaderboard and entry polls included) and to refresh your
tournament list with `GET /tournament`. This matters most after an environment reset: tournaments created before the
reset no longer exist, and a client that retries them indefinitely is doing nothing but burning its rate limit.

The API does not validate the format of a tournament ID: a mistyped ID is simply one that does not resolve, so it
answers `404 TOURNAMENT_NOT_FOUND` as well. `400 INVALID_TOURNAMENT_ID` is no longer returned by any endpoint; the code
stays in the `Error.error.code` enum only so that existing clients keep deserializing error bodies. `400 VALIDATION_ERROR`
is still returned for a request that fails schema validation (for example a tournament ID longer than 40 characters).

> **Changed in the current release.** Tournament reads previously answered `400 INVALID_TOURNAMENT_ID` for a missing
> tournament. If your integration branches on that status or code, handle `404` / `TOURNAMENT_NOT_FOUND` instead and
> regenerate your server SDK so the new code deserializes.

### 6.7. Integration Example: Game Server Flow

```javascript
// Tournament integration in your game server
class TournamentIntegration {
  constructor(apiKey) {
    this.apiKey = apiKey;
    this.baseUrl = 'https://api.gamers.dev';
  }

  // When a player wants to join a tournament
  async joinTournament(tournamentId, userId) {
    // Create entry
    const entry = await fetch(`${this.baseUrl}/tournament/${tournamentId}/entries`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-API-KEY': this.apiKey
      },
      body: JSON.stringify({ userId })
    }).then(r => r.json());
    
    return entry;
  }

  // When player starts playing
  async startPlayerSession(tournamentId, entryId) {
    // Start the entry (begins the entry window countdown)
    await fetch(`${this.baseUrl}/tournament/${tournamentId}/entries/${entryId}/start`, {
      method: 'POST',
      headers: { 'X-API-KEY': this.apiKey }
    });

    // Create a new attempt
    const attempt = await fetch(`${this.baseUrl}/tournament/${tournamentId}/entries/${entryId}/attempts`, {
      method: 'POST',
      headers: { 'X-API-KEY': this.apiKey }
    }).then(r => r.json());

    return attempt;
  }

  // When player finishes a game session
  async submitGameResult(tournamentId, entryId, attemptId, score) {
    const result = await fetch(
      `${this.baseUrl}/tournament/${tournamentId}/entries/${entryId}/attempts/${attemptId}/scores`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-API-KEY': this.apiKey
        },
        body: JSON.stringify({
          scores: [{ metricId: 'metric_score', value: score }]
        })
      }
    ).then(r => r.json());

    return result;
  }

  // Get current leaderboard to display in-game
  async getLeaderboard(tournamentId, limit = 10) {
    const leaderboard = await fetch(
      `${this.baseUrl}/tournament/${tournamentId}/leaderboard?limit=${limit}`,
      {
        headers: { 'X-API-KEY': this.apiKey }
      }
    ).then(r => r.json());

    return leaderboard;
  }
}

// Usage
const tournament = new TournamentIntegration('YOUR_API_KEY');

// Player joins tournament
const entry = await tournament.joinTournament('tourn_12345', 'user_abc123');

// Player starts playing
const attempt = await tournament.startPlayerSession('tourn_12345', entry.id);

// After game ends, submit score
await tournament.submitGameResult('tourn_12345', entry.id, attempt.id, 15000);

// Show leaderboard with estimated prizes
const leaderboard = await tournament.getLeaderboard('tourn_12345');
console.log('Top players:', leaderboard.entries);
console.log('Prize pool:', leaderboard.prizePool);
// Each entry includes estimatedPrize — the net amount the player would win
```

### 6.8. Tournament Use Cases

#### Daily Challenges
```json
{
  "name": "Daily Speed Run",
  "startAt": "2025-01-15T00:00:00Z",
  "endAt": "2025-01-15T23:59:59Z",
  "entryWindowMinutes": 30,
  "maxEntriesPerPlayer": 1,
  "maxAttemptsPerEntry": 3,
  "baseEntryFee": 1
}
```

#### Seasonal Championships
```json
{
  "name": "Season 1 Championship",
  "startAt": "2025-01-01T00:00:00Z",
  "endAt": "2025-03-31T23:59:59Z",
  "entryWindowMinutes": 0,
  "maxEntriesPerPlayer": null,
  "maxAttemptsPerEntry": null,
  "baseEntryFee": 25
}
```

#### Free Practice Tournaments
```json
{
  "name": "Free Practice Tournament",
  "startAt": "2025-01-15T00:00:00Z",
  "endAt": "2025-01-22T00:00:00Z",
  "entryFeeType": "FIXED",
  "baseEntryFee": 0,
  "maxEntriesPerPlayer": null,
  "maxAttemptsPerEntry": null
}
```

### 6.9. Tournament Templates

Tournament templates allow you to pre-define tournament configurations for reuse. This is ideal for recurring tournaments like daily challenges or weekly competitions.

#### Template Management

Tournament templates can be managed through the **Developer Portal** web interface or the Game API (`/tournament-template` endpoints).

##### Creating a Template
```bash
curl -X POST https://api.gamers.dev/tournament-template \
  -H "Content-Type: application/json" \
  -H "X-API-KEY: YOUR_API_KEY" \
  -d '{
        "name": "Weekly High Score",
        "description": "Weekly tournament for high scores",
        "gameModeId": "gm_001",
        "durationValue": 7,
        "durationUnit": "DAYS",
        "entryFeeType": "FIXED",
        "baseEntryFee": 5,
        "entryWindowMinutes": 60,
        "maxEntriesPerPlayer": 3,
        "maxAttemptsPerEntry": 5,
        "maxParticipants": 100,
        "minParticipants": 10,
        "publicAccess": true,
        "prizes": [
            { "prizeType": "FIXED", "position": 1, "metricId": "metric_score", "amount": 50, "description": "1st Place" },
            { "prizeType": "FIXED", "position": 2, "metricId": "metric_score", "amount": 30, "description": "2nd Place" },
            { "prizeType": "FIXED", "position": 3, "metricId": "metric_score", "amount": 20, "description": "3rd Place" }
        ]
      }'
```

**Required fields**: `name`, `gameModeId`, `prizes`

##### Updating a Template
```bash
curl -X PUT https://api.gamers.dev/tournament-template/tpl_12345 \
  -H "Content-Type: application/json" \
  -H "X-API-KEY: YOUR_API_KEY" \
  -d '{
        "description": "Updated description",
        "maxParticipants": 200
      }'
```

All fields are optional when updating.

##### Deleting a Template
```bash
curl -X DELETE https://api.gamers.dev/tournament-template/tpl_12345 \
  -H "X-API-KEY: YOUR_API_KEY"
```

This performs a soft-delete (sets status to DELETED).

#### Template Duration Configuration

Templates can include default duration settings that are applied when creating tournaments:

**Duration Fields:**
- `durationValue`: Numeric value (1-525600)
- `durationUnit`: Time unit - "MINUTES", "HOURS", or "DAYS"

**Examples:**
- `durationValue: 30, durationUnit: "MINUTES"` = 30-minute tournament
- `durationValue: 2, durationUnit: "HOURS"` = 2-hour tournament  
- `durationValue: 7, durationUnit: "DAYS"` = 7-day tournament

When creating a tournament from a template, you must provide:
- `startAt` (required) - Tournament start time
- Either `endAt` or `durationMinutes` (one is required) - Tournament end time or duration

#### Prize Types

Templates support two prize types:

- **FIXED**: Award prizes to specific positions (1st, 2nd, 3rd)
- **PERCENTAGE**: Award prizes to top X% of participants

**Fixed Position Prizes:**
```json
{
  "prizes": [
    { "metricId": "metric_score", "prizeType": "FIXED", "position": 1, "amount": 60, "description": "1st Place" },
    { "metricId": "metric_score", "prizeType": "FIXED", "position": 2, "amount": 30, "description": "2nd Place" },
    { "metricId": "metric_score", "prizeType": "FIXED", "position": 3, "amount": 10, "description": "3rd Place" }
  ]
}
```

**Percentage Prizes:**
```json
{
  "prizes": [
    { "metricId": "metric_score", "prizeType": "PERCENTAGE", "percentage": 10, "amount": 60, "cascadingPercentage": 0 },
    { "metricId": "metric_score", "prizeType": "PERCENTAGE", "percentage": 20, "amount": 40, "cascadingPercentage": 50 }
  ]
}
```

**Percentage Prize Examples:**

1. **Equal Distribution** (`cascadingPercentage: 0`):
   - Top 10% share 60% equally
   - Next 20% share 40% equally

2. **Weighted Distribution** (`cascadingPercentage: 50`):
   - Top performers get larger share
   - Distribution curve favors higher ranks

3. **Heavy Top Weight** (`cascadingPercentage: 100`):
   - #1 gets significantly more than #2
   - Sharp drop-off in prize amounts

The `cascadingPercentage` controls distribution curve:
- **0**: Equal split among all qualifying participants
- **50**: Moderate weighting toward top performers  
- **100**: Heavily weighted to top positions

#### List Templates

You can retrieve available templates via the API:

```bash
curl -X GET https://api.gamers.dev/tournament-template \
  -H "X-API-KEY: YOUR_API_KEY"
```

#### Create Tournament from Template

Use `templateId` or `templateName` to reference the template:

**Using durationMinutes:**
```bash
curl -X POST https://api.gamers.dev/tournament-template/create-tournament \
  -H "Content-Type: application/json" \
  -H "X-API-KEY: YOUR_API_KEY" \
  -d '{
        "templateName": "Weekly High Score",
        "startAt": "2025-01-15T00:00:00Z",
        "durationMinutes": 10080
      }'
```

**Specific End Time:**
```bash
curl -X POST https://api.gamers.dev/tournament-template/create-tournament \
  -H "Content-Type: application/json" \
  -H "X-API-KEY: YOUR_API_KEY" \
  -d '{
        "templateName": "Weekly High Score",
        "startAt": "2025-01-15T00:00:00Z",
        "endAt": "2025-01-17T00:00:00Z"
      }'
```

**Overriding Multiple Settings:**
```bash
curl -X POST https://api.gamers.dev/tournament-template/create-tournament \
  -H "Content-Type: application/json" \
  -H "X-API-KEY: YOUR_API_KEY" \
  -d '{
        "templateName": "Weekly High Score",
        "name": "Special Weekend Tournament",
        "startAt": "2025-01-15T00:00:00Z",
        "endAt": "2025-01-17T00:00:00Z",
        "baseEntryFee": 10,
        "maxEntriesPerPlayer": 5
      }'
```

#### Template API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/tournament-template` | GET | List all templates |
| `/tournament-template` | POST | Create a new template |
| `/tournament-template/{id}` | GET | Get template details |
| `/tournament-template/{id}` | PUT | Update a template |
| `/tournament-template/{id}` | DELETE | Delete a template (soft-delete) |
| `/tournament-template/create-tournament` | POST | Create tournament from template |

#### Creating Templates via Developer Portal or API

Templates can be created and managed through the [Developer Portal](https://developer.gamers.dev/) which provides a visual interface for configuration, or programmatically using the `POST /tournament-template`, `PUT /tournament-template/{id}`, and `DELETE /tournament-template/{id}` endpoints.

## 7. Testing & Integration

### 7.1. Development Best Practices

#### Error Handling

All API endpoints return a structured error envelope:

```json
{
  "success": false,
  "error": {
    "code": "INVALID_EVENT_ID",
    "message": "The event ID is invalid, please check and try again",
    "details": { }
  },
  "timestamp": "2026-06-18T17:28:00.000Z"
}
```

- `code` — machine-readable error identifier
- `message` — human-readable description
- `details` — optional additional context (e.g., validation error field breakdowns)
- `timestamp` — ISO 8601 time when the error occurred

#### API Versioning

Error responses support an `X-API-Version` request header for backward compatibility during migration:

| Header Value | Behavior |
|--------------|----------|
| missing or `1` | Returns the **legacy** flat format (pre-0.1.17) |
| `2` | Returns the **standardized** envelope shown above |

**Migration recommendation:** Update your integration to send `X-API-Version: 2` and parse the new envelope. The legacy format will be removed in a future version.

#### Idempotency

The Game API supports an optional `Idempotency-Key` header on `POST`, `PUT`, `PATCH`, and `DELETE` requests. When the header is included, the server:

1. **Authenticates before replaying.** Cached responses are only returned after re-validating the API key (and player JWT, if the original request was player-scoped).
2. **Fingerprints the request.** The key is bound to the request's method, route, query, and body. Reusing the same key with a different payload returns `409 Conflict`.
3. **Reserves the key in the same transaction.** The idempotency record is created inside the same database transaction as the business write, so a failed write does not block retries.
4. **Caches successful responses only.** A `2xx` is stored and replayed for 24 hours, so retries with the same key receive the cached response without re-executing the business logic. A `4xx` or `5xx` is **not** cached — the key is released so the same key can be retried.
5. **Fails closed on storage errors.** If the idempotency store cannot be reached, the server returns `503 Service Unavailable` to avoid accidental duplicate writes.

- The header is **optional**: existing integrations that do not send it continue to work unchanged.
- The .NET Server SDK sends an `Idempotency-Key` automatically on every write request and reuses the same key across retries.
- Use a unique key for each distinct operation, typically a UUID. Do not reuse a key for a different request.
- If an operation returns `503` due to storage unavailability, you may retry with the same key after the storage recovers.
- If a write returns a non-2xx status, retrying with the same key re-executes the request rather than replaying the failure. Only a `2xx` is replayable.
- While a request with a given key is still in flight, reusing that key returns `409 Conflict` (`IDEMPOTENCY_KEY_CONFLICT`). A reservation left behind by a request that never returned is released after 5 minutes.
- A replayed response carries an `Idempotency-Key` response header echoing the key it was replayed from. A freshly executed response does not, so that header is how you tell a replay from a first execution.
- `POST /auth/request` and `POST /auth/verify` accept the header but deliberately ignore it: re-requesting a verification code is the intended behavior. Every other mutating operation honours it.

```bash
curl -X POST https://api.gamers.dev/entry/evt_12345 \
  -H "X-API-KEY: YOUR_API_KEY" \
  -H "Idempotency-Key: 2f8d9c4e-5b3a-4e1d-9f6c-7a8b9c0d1e2f" \
  -H "Content-Type: application/json" \
  -d '{"userToken": "player-jwt"}'
```

##### Rate Limiting (v1 vs v2)

**v1 (legacy):**
```json
{ "error": "Too many requests from this IP, please try again later." }
```

**v2 (standardized):**
```json
{
  "success": false,
  "error": {
    "code": "RATE_LIMIT_EXCEEDED",
    "message": "Too many requests from this IP, please try again later."
  },
  "timestamp": "2026-06-18T19:00:00.000Z"
}
```

##### Validation Errors (v1 vs v2)

**v1 (legacy):**
```json
{
  "status": "error",
  "message": "Validation failed",
  "details": { "body": [{ "message": "\"name\" is required" }] }
}
```

**v2 (standardized):**
```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Validation failed",
    "details": { "body": [{ "message": "\"name\" is required" }] }
  },
  "timestamp": "2026-06-18T17:28:00.000Z"
}
```

Common HTTP status codes:

- **401 Unauthorized**: Invalid or missing API key or JWT
- **403 Forbidden**: Valid credentials but operation not permitted (e.g., `UNAUTHORIZED_ENTRY`)
- **400 Bad Request**: Invalid request payload, missing required fields, or business-rule violation
- **404 Not Found**: Resource does not exist or does not belong to the authenticated game (e.g. `TOURNAMENT_NOT_FOUND`, `TOURNAMENT_TEMPLATE_NOT_FOUND`). A `404` is permanent — stop retrying the ID.
- **409 Conflict**: Operation not allowed in current state (e.g., duplicate team name)
- **429 Too Many Requests**: Rate limit exceeded
- **503 Service Unavailable**: Blockchain temporarily disabled (`BLOCKCHAIN_DISABLED`)

##### Validation Errors

When request validation fails, the response uses the `VALIDATION_ERROR` code with field-level details:

```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Validation failed",
    "details": {
      "body": [
        { "message": "\"name\" is required", "path": ["name"], "type": "any.required" }
      ]
    }
  },
  "timestamp": "2026-06-18T17:28:00.000Z"
}
```

#### Request ID Tracking

You may optionally send an `X-Request-ID` header on any request (UUID recommended). The server echoes it back in the response `X-Request-ID` header. If omitted, the server generates a UUID for you. Include this value in support requests for faster debugging.

#### Rate Limiting
The API enforces a rate limit of **1,000 requests per minute per IP address**. If you exceed this limit, you will receive a `429 Too Many Requests` response:

```json
{
  "success": false,
  "error": {
    "code": "RATE_LIMIT_EXCEEDED",
    "message": "Too many requests from this IP, please try again later."
  },
  "timestamp": "2026-06-18T19:00:00.000Z"
}
```

Rate limit information is returned in the response headers:
- `RateLimit-Limit` — Maximum requests allowed per window
- `RateLimit-Remaining` — Requests remaining in the current window
- `RateLimit-Reset` — Seconds until the rate limit window resets

Implement appropriate retry logic with exponential backoff for production integrations.

#### Security Considerations
- Never expose API keys in client-side code
- Store JWT tokens securely and respect expiration times
- Use HTTPS for all API communications
- Validate all user inputs before sending to the API

## 8. API Reference

For a complete and interactive list of all available endpoints, data models, and examples, please see our OpenAPI documentation, which is available in the `docs` section of the developer portal or can be accessed directly.

[**View Full API Reference**](https://developer.gamers.dev/docs)

## 9. Support and Resources

### Getting Help
- **Developer Portal**: Create and manage templates, access your dashboard, manage API keys, and view documentation
- **API Documentation**: Interactive OpenAPI specification with live examples

### Useful Links
- [Developer Portal](https://developer.gamers.dev/)
- [Interactive API Docs](https://developer.gamers.dev/docs)

### Template Management
- **Event templates**: Management (create, update, delete) is available through the **Developer Portal** or the Game API. The API provides full template CRUD plus event creation from templates.
- **Tournament templates**: Management (create, update, delete) is available through the **Developer Portal** or the Game API. The API provides full template CRUD plus tournament creation from templates.

## 10. Version History

> For detailed endpoint-level API changes, see the [API Changelog](https://developer.gamers.dev/docs/changelog).

### 0.5.0 — 2026-08-06
- ⚠️ **BREAKING CHANGE**: `PATCH /tournament/{tournamentId}/complete` on a tournament that is already `COMPLETED` returns `200` with the tournament's current state instead of `400 TOURNAMENT_CANNOT_CHANGE_STATUS`, so a retry converges. Other invalid transitions still return `400`. Do not trigger post-completion work solely on this call returning `200` — see [Step 8](#step-8-complete-the-tournament-at-end-date).
- ⚠️ **BREAKING CHANGE**: `POST /tournament/{tournamentId}/entries/{entryId}/attempts` enforces `maxAttemptsPerEntry` atomically. Concurrent attempt creation for one entry could previously exceed the limit; the later request is now rejected with `400 MAX_ATTEMPTS_REACHED`. Sequential callers are unaffected.
- Idempotency (`Idempotency-Key`) is now durable and fail-closed, adding `409 IDEMPOTENCY_KEY_CONFLICT` and `503 IDEMPOTENCY_STORAGE_UNAVAILABLE`. The header remains **optional** — a request without one is routed, authenticated and answered exactly as before, so integrations that do not send it are unaffected. See [Idempotency](#idempotency).

### 0.1.19 — 2026-08-01
- `POST /entry/{eventId}` now returns the created `EventEntry` object (including `id`, `eventId`, `userId`, `status`, and `createdAt`) in the response body. This is a non-breaking, additive change for clients that previously ignored the empty body.
- `POST /tournament/{tournamentId}/entries` and `POST /tournament/{tournamentId}/entries/{entryId}/start` require the user's JWT in the `Authorization` header.
- `POST /tournament/{tournamentId}/entries/{entryId}/attempts` and `POST /tournament/{tournamentId}/entries/{entryId}/attempts/{attemptId}/scores` are game-server calls and require only the `X-API-KEY` header.

### 0.1.17 — 2026-06-18
- ⚠️ **BREAKING CHANGE**: Global error response envelope updated. All error responses now use `{ success: false, error: { code, message, details? }, timestamp }` instead of the previous flat `{ error, message }` format. Update your error-parsing logic before deploying to production.
- Added `X-Request-ID` request/response header support for request tracing.
- Added new error codes: `VALIDATION_ERROR`, `EVENT_NOT_OPEN`, `INVALID_TEAM_ID`, `INVALID_EVENT_TYPE`, `DUPLICATE_TEAM_NAME`, `INVALID_ENTRY_EVENT`, `ENTRY_ALREADY_IN_TEAM`, `BLOCKCHAIN_DISABLED`, `TOURNAMENT_TEMPLATE_NOT_FOUND`, `TOURNAMENT_TEMPLATE_NAME_EXISTS`, `INVALID_TOURNAMENT_ID`, `TOURNAMENT_NOT_DRAFT`, `TOURNAMENT_CANNOT_CHANGE_STATUS`, `TOURNAMENT_NO_PRIZES`, `TOURNAMENT_EXPIRED`, `TOURNAMENT_NOT_OPEN`, `TOURNAMENT_FULL`, `TOURNAMENT_NOT_ACCEPTING_ATTEMPTS`, `INVALID_ENTRY_ID`, `MAX_ENTRIES_REACHED`, `ENTRY_NOT_ACTIVE`, `ENTRY_ALREADY_STARTED`, `ENTRY_WINDOW_EXPIRED`, `MAX_ATTEMPTS_REACHED`, `INVALID_ATTEMPT_ID`, `ATTEMPT_ALREADY_COMPLETED`, `UNAUTHORIZED_ENTRY`, `PAYOUT_CALCULATION_FAILED`, `RATE_LIMIT_EXCEEDED`, `USER_NOT_ACTIVE`.

### 0.1.16 — 2026-06-02
- `GET /tournament/{id}/entries` now supports an optional `userId` query parameter to filter entries by gamer bet account ID. This enables games to look up all entries for a specific player in a tournament. Combines with existing `limit` and `offset` parameters for paginated results. Example: `GET /tournament/tour_123/entries?userId=user_abc&limit=10`. This is an additive, non-breaking change.

### 0.1.15 — 2026-04-20
- `GET /tournament/{id}/leaderboard` now includes estimated prize information: a `prizePool` summary (total pool, winner pool, rake, player count) and `estimatedPrize` on each leaderboard entry showing the net payout if the tournament ended now. Supports FIXED and PERCENTAGE prize types with cascading distribution. This is an additive, non-breaking change.

### 0.1.14 — 2026-04-01
- ⚠️ **BREAKING CHANGE**: Team management endpoints now enforce API key authentication (`X-API-KEY` header required). Affected endpoints: `POST /team/events/{eventId}/teams`, `GET /team/events/{eventId}/teams`, `GET /team/teams/{teamId}`, `POST /team/teams/{teamId}/members`.
- Added rate limiting: 1,000 requests per minute per IP. Exceeding returns `429 Too Many Requests`.

### 0.1.13 — 2026-02-19
- ⚠️ **BREAKING CHANGE**: `POST /tournament/{tournamentId}/entries` now requires the user's JWT in the `Authorization` header instead of accepting `userId` in the request body. This standardizes the authentication model across all entry endpoints.

### 0.1.12 — 2026-02-18
- Added prize pool display tip (Section 6.4) and `GET /tournament/{id}/prize-pool` to the endpoints summary table (Section 6.6)

### 0.1.11 — 2026-02-17
- added a note to the dev-guide mentioning that auth tokens are only valid for the game they were issued for.

### 0.1.10 — 2026-02-16
- No changes to the developer guide

### 0.1.9 — 2026-02-12
- Added tournament minimum participants documentation and auto-cancel behavior
- Added link to API Changelog for endpoint-level change tracking

### 0.1.8 — 2026-02-11
- Updated all URLs to use custom domains
- Added test environment URLs table (Section 1.1)

### 0.1.7 — 2026-02-10
- Added event template section with batch creation workflow, team event examples, and validation error reference
- Added team management documentation and payload examples
- Expanded event results documentation with multi-metric format

### 0.1.6 — 2026-01-11
- Added Tournaments section with lifecycle, configuration, variable entry fees, leaderboards, and integration examples
- Added Tournament Templates section with duration configuration and prize types
- Added Game Configuration Management section with gameplay mode and metric examples
- Added Developer Portal overview (Section 1.2)
- Restructured document sections and numbering
- Expanded authentication documentation with JWT validity details

### 0.1.5 — 2025-12-04
- Updated event creation examples with default values and minimal configuration
- Added game configuration and event template endpoint references

### 0.1.4 — 2025-09-23
- Core event lifecycle walkthrough with examples
- Authentication flow documentation
- Event requirements and validation rules
- Testing & integration best practices
