using Gamers.Client.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Gamers.Client
{
    /// <summary>High-level client-side flow for the Gamers server-authoritative integration.</summary>
    /// <remarks>
    /// This class sends messages to the developer's game-server via <see cref="IGamersTransport"/> and reacts to replies.
    /// It does not communicate directly with the Gamers Game API, hold API keys, or store JWTs.
    ///
    /// Every outgoing message is stamped with a unique <see cref="GamersClientMessage.CorrelationId"/>, and the
    /// returned <see cref="Task{TResult}"/> completes when the game-server's matching reply arrives — not when the
    /// transport accepts the outbound message. A reply is delivered to the operation that created its correlation id
    /// and to no other, so a late or duplicate reply can never drive the state of a newer request.
    ///
    /// Replies that carry no correlation id, or one that is not outstanding, are treated as unsolicited server pushes
    /// and raised through the corresponding event. Requests that receive no reply within <see cref="RequestTimeout"/>
    /// fail with <see cref="TimeoutException"/>.
    ///
    /// Call the flow and raise <see cref="IGamersTransport.ReplyReceived"/> on Unity's main thread. Exceptions from
    /// <see cref="IGamersTransport.SendAsync"/> propagate to the returned task unchanged.
    /// </remarks>
    public class GamersClientFlow : IDisposable
    {
        /// <summary>Version of the client↔game-server envelope this client speaks.</summary>
        public const int ProtocolVersion = 1;

        private readonly IGamersTransport _transport;
        private readonly Dictionary<string, PendingOperation> _pending = new Dictionary<string, PendingOperation>();
        private readonly object _correlationLock = new object();
        private bool _disposed;

        /// <summary>Gets or sets how long to wait for a game-server reply before failing the operation.</summary>
        /// <remarks>
        /// Defaults to 30 seconds, matching the server SDKs. A game-server that never answers — because it
        /// crashed, dropped the message, or failed to deserialize it — would otherwise leave the operation
        /// pending forever. Set this to <see cref="TimeSpan.Zero"/> or a negative value to disable the timeout.
        /// </remarks>
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>Current state of the client flow.</summary>
        public GamersClientState State { get; private set; } = GamersClientState.Idle;

        /// <summary>Email address currently being authenticated, if any.</summary>
        public string Email { get; private set; }

        /// <summary>Current event the player is attempting to join or has joined, if any.</summary>
        public string CurrentEventId { get; private set; }

        /// <summary>Current tournament the player is attempting to join or has joined, if any.</summary>
        public string CurrentTournamentId { get; private set; }

        /// <summary>Raised whenever <see cref="State"/> changes.</summary>
        public event Action<GamersClientState> OnStateChanged;

        /// <summary>Raised when the game-server confirms a verification code has been requested.</summary>
        public event Action<AuthCodeRequestedReply> OnAuthCodeRequested;

        /// <summary>Raised when authentication succeeds.</summary>
        public event Action<AuthResultReply> OnAuthenticated;

        /// <summary>Raised when the player successfully joins an event.</summary>
        public event Action<EventJoinResultReply> OnEventJoined;

        /// <summary>Raised when the player successfully joins a tournament.</summary>
        public event Action<TournamentJoinResultReply> OnTournamentJoined;

        /// <summary>Raised when a leaderboard snapshot arrives, whether requested or pushed by the game-server.</summary>
        public event Action<LeaderboardSnapshotReply> OnLeaderboardUpdated;

        /// <summary>Raised when an operation fails or the game-server pushes an uncorrelated error.</summary>
        /// <remarks>
        /// Awaiting the task returned by an operation is the authoritative way to observe its failure; this
        /// event is a notification for code elsewhere in the scene that is not awaiting the call.
        /// </remarks>
        public event Action<ErrorReply> OnError;

        private Action<GamersServerReply> _replyHandler;

        /// <summary>Initializes a new instance of the <see cref="GamersClientFlow"/> class.</summary>
        /// <param name="transport">The transport used to reach the developer's game-server.</param>
        /// <exception cref="ArgumentNullException"><paramref name="transport"/> is <see langword="null"/>.</exception>
        public GamersClientFlow(IGamersTransport transport)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _replyHandler = HandleReply;
            _transport.ReplyReceived += _replyHandler;
        }

        /// <summary>Unsubscribes from the transport and cancels any operations still in flight.</summary>
        /// <remarks>
        /// This method is idempotent. It does not dispose the caller-owned <see cref="IGamersTransport"/>.
        /// </remarks>
        public void Dispose()
        {
            if (_replyHandler == null) return;
            _transport.ReplyReceived -= _replyHandler;
            _replyHandler = null;

            List<PendingOperation> outstanding;
            lock (_correlationLock)
            {
                _disposed = true;
                outstanding = new List<PendingOperation>(_pending.Values);
                _pending.Clear();
            }

            // Awaiting callers must not hang when the scene unloads.
            foreach (var operation in outstanding)
            {
                operation.Cleanup();
                operation.Completion.TrySetCanceled();
            }
        }

        /// <summary>Requests the game-server to start authentication for the provided email.</summary>
        /// <param name="email">The nonempty player email address that should receive the verification code.</param>
        /// <param name="ct">A token that cancels the pending operation.</param>
        /// <returns>A task that completes with the game-server's code-request confirmation.</returns>
        /// <exception cref="ArgumentException"><paramref name="email"/> is empty or whitespace.</exception>
        /// <exception cref="ObjectDisposedException">The client flow has been disposed.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="ct"/> is cancelled or the client flow is disposed.</exception>
        /// <exception cref="TimeoutException">The game-server does not reply within <see cref="RequestTimeout"/>.</exception>
        /// <exception cref="GamersClientException">The game-server returns an error or an incompatible reply.</exception>
        public Task<AuthCodeRequestedReply> RequestAuthAsync(string email, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required", nameof(email));

            Email = email;
            TransitionTo(GamersClientState.RequestingAuth);

            return SendAndAwaitAsync<AuthCodeRequestedReply>(
                new RequestAuthMessage(email),
                reply =>
                {
                    TransitionTo(GamersClientState.AwaitingCode);
                    OnAuthCodeRequested?.Invoke(reply);
                },
                ct);
        }

        /// <summary>Submits the verification code the player received.</summary>
        /// <param name="email">The nonempty player email address that received the code.</param>
        /// <param name="code">The nonempty verification code entered by the player.</param>
        /// <param name="ct">A token that cancels the pending operation.</param>
        /// <returns>A task that completes with the game-server's authentication result.</returns>
        /// <exception cref="ArgumentException"><paramref name="email"/> or <paramref name="code"/> is empty or whitespace.</exception>
        /// <exception cref="ObjectDisposedException">The client flow has been disposed.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="ct"/> is cancelled or the client flow is disposed.</exception>
        /// <exception cref="TimeoutException">The game-server does not reply within <see cref="RequestTimeout"/>.</exception>
        /// <exception cref="GamersClientException">Authentication fails, or the game-server returns an error or incompatible reply.</exception>
        public Task<AuthResultReply> SubmitCodeAsync(string email, string code, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required", nameof(email));
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Code is required", nameof(code));

            Email = email;
            TransitionTo(GamersClientState.AwaitingAuthResult);

            return SendAndAwaitAsync<AuthResultReply>(
                new SubmitCodeMessage(email, code),
                reply =>
                {
                    if (!reply.Success)
                        throw new GamersClientException("AUTH_FAILED", reply.Message, false, reply.CorrelationId);

                    TransitionTo(GamersClientState.Authenticated);
                    OnAuthenticated?.Invoke(reply);
                },
                ct);
        }

        /// <summary>Requests the game-server to join an event on behalf of the player.</summary>
        /// <param name="eventId">The nonempty event identifier.</param>
        /// <param name="ct">A token that cancels the pending operation.</param>
        /// <returns>A task that completes with the game-server's event join result.</returns>
        /// <exception cref="ArgumentException"><paramref name="eventId"/> is empty or whitespace.</exception>
        /// <exception cref="ObjectDisposedException">The client flow has been disposed.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="ct"/> is cancelled or the client flow is disposed.</exception>
        /// <exception cref="TimeoutException">The game-server does not reply within <see cref="RequestTimeout"/>.</exception>
        /// <exception cref="GamersClientException">The join fails, or the game-server returns an error or incompatible reply.</exception>
        public Task<EventJoinResultReply> JoinEventAsync(string eventId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(eventId))
                throw new ArgumentException("Event id is required", nameof(eventId));

            TransitionTo(GamersClientState.JoiningEvent);

            return SendAndAwaitAsync<EventJoinResultReply>(
                new JoinEventMessage(eventId),
                reply =>
                {
                    if (!reply.Success)
                        throw new GamersClientException("JOIN_EVENT_FAILED", reply.Message, false, reply.CorrelationId);

                    // Set from the reply, not from the request: only the operation whose reply
                    // actually arrived may publish state.
                    CurrentEventId = reply.EventId ?? eventId;
                    TransitionTo(GamersClientState.JoinedEvent);
                    OnEventJoined?.Invoke(reply);
                },
                ct);
        }

        /// <summary>Requests the game-server to join a tournament on behalf of the player.</summary>
        /// <param name="tournamentId">The nonempty tournament identifier.</param>
        /// <param name="ct">A token that cancels the pending operation.</param>
        /// <returns>A task that completes with the game-server's tournament join result.</returns>
        /// <exception cref="ArgumentException"><paramref name="tournamentId"/> is empty or whitespace.</exception>
        /// <exception cref="ObjectDisposedException">The client flow has been disposed.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="ct"/> is cancelled or the client flow is disposed.</exception>
        /// <exception cref="TimeoutException">The game-server does not reply within <see cref="RequestTimeout"/>.</exception>
        /// <exception cref="GamersClientException">The join fails, or the game-server returns an error or incompatible reply.</exception>
        public Task<TournamentJoinResultReply> JoinTournamentAsync(string tournamentId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tournamentId))
                throw new ArgumentException("Tournament id is required", nameof(tournamentId));

            TransitionTo(GamersClientState.JoiningTournament);

            return SendAndAwaitAsync<TournamentJoinResultReply>(
                new JoinTournamentMessage(tournamentId),
                reply =>
                {
                    if (!reply.Success)
                        throw new GamersClientException("JOIN_TOURNAMENT_FAILED", reply.Message, false, reply.CorrelationId);

                    CurrentTournamentId = reply.TournamentId ?? tournamentId;
                    TransitionTo(GamersClientState.JoinedTournament);
                    OnTournamentJoined?.Invoke(reply);
                },
                ct);
        }

        /// <summary>Requests a leaderboard snapshot for a tournament.</summary>
        /// <param name="tournamentId">The nonempty tournament identifier.</param>
        /// <param name="metricId">An optional metric identifier. Omit it for the default by-player view.</param>
        /// <param name="offset">The zero-based pagination offset sent to the game-server.</param>
        /// <param name="limit">The maximum number of entries requested from the game-server.</param>
        /// <param name="ct">A token that cancels the pending operation.</param>
        /// <returns>A task that completes with the snapshot the game-server returns.</returns>
        /// <exception cref="ArgumentException"><paramref name="tournamentId"/> is empty or whitespace.</exception>
        /// <exception cref="ObjectDisposedException">The client flow has been disposed.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="ct"/> is cancelled or the client flow is disposed.</exception>
        /// <exception cref="TimeoutException">The game-server does not reply within <see cref="RequestTimeout"/>.</exception>
        /// <exception cref="GamersClientException">The game-server returns an error or an incompatible reply.</exception>
        public Task<LeaderboardSnapshotReply> RequestLeaderboardAsync(
            string tournamentId,
            string metricId = null,
            int offset = 0,
            int limit = 50,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tournamentId))
                throw new ArgumentException("Tournament id is required", nameof(tournamentId));

            return SendAndAwaitAsync<LeaderboardSnapshotReply>(
                new RequestLeaderboardMessage(tournamentId, metricId, offset, limit),
                reply => OnLeaderboardUpdated?.Invoke(reply),
                ct);
        }

        /// <summary>Number of operations awaiting a reply. Exposed for diagnostics and tests.</summary>
        public int PendingOperationCount
        {
            get { lock (_correlationLock) { return _pending.Count; } }
        }

        private async Task<TReply> SendAndAwaitAsync<TReply>(
            GamersClientMessage message,
            Action<TReply> onSuccess,
            CancellationToken ct)
            where TReply : GamersServerReply
        {
            if (_replyHandler == null)
                throw new ObjectDisposedException(nameof(GamersClientFlow));

            message.ProtocolVersion = ProtocolVersion;
            var correlationId = Guid.NewGuid().ToString("N");
            message.CorrelationId = correlationId;

            var operation = new PendingOperation
            {
                ExpectedReplyType = typeof(TReply),
                // Without RunContinuationsAsynchronously the awaiting caller's continuation runs
                // inline on the transport's receive thread, which in Unity means game logic
                // executing off the main thread.
                Completion = new TaskCompletionSource<GamersServerReply>(TaskCreationOptions.RunContinuationsAsynchronously)
            };

            // Register BEFORE sending: a fast transport can deliver the reply before SendAsync returns.
            lock (_correlationLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(GamersClientFlow));

                _pending[correlationId] = operation;
            }

            ArmTimeoutAndCancellation(correlationId, operation, ct);

            try
            {
                // No ConfigureAwait(false) anywhere in this method: Unity callers await from the
                // main thread, and their SynchronizationContext is what carries the continuation -
                // and the events raised from it - back to the main thread.
                await _transport.SendAsync(message, ct);
            }
            catch
            {
                // The message never left; nothing can ever resolve this operation.
                Discard(correlationId)?.Cleanup();
                throw;
            }

            GamersServerReply reply;
            try
            {
                reply = await operation.Completion.Task;
            }
            catch (GamersClientException ex)
            {
                RaiseError(ex);
                throw;
            }

            var typed = (TReply)reply;

            try
            {
                onSuccess?.Invoke(typed);
            }
            catch (GamersClientException ex)
            {
                // The reply arrived but reported failure (e.g. Success == false).
                RaiseError(ex);
                throw;
            }

            return typed;
        }

        private void ArmTimeoutAndCancellation(string correlationId, PendingOperation operation, CancellationToken ct)
        {
            // Created unarmed: CancelAfter is called at the end, once TimeoutRegistration is
            // assigned. Arming first would let a very short RequestTimeout fire during Register()
            // and leave the registration undisposed.
            var timeoutCts = new CancellationTokenSource();
            operation.TimeoutSource = timeoutCts;

            operation.TimeoutRegistration = timeoutCts.Token.Register(() =>
            {
                var pending = Discard(correlationId);
                if (pending == null) return;

                pending.Cleanup();
                pending.Completion.TrySetException(new TimeoutException(
                    $"The game-server did not reply within {RequestTimeout}."));
            });

            if (ct.CanBeCanceled)
            {
                operation.CancellationRegistration = ct.Register(() =>
                {
                    var pending = Discard(correlationId);
                    if (pending == null) return;

                    pending.Cleanup();
                    pending.Completion.TrySetCanceled(ct);
                });
            }

            if (RequestTimeout > TimeSpan.Zero)
                timeoutCts.CancelAfter(RequestTimeout);
        }

        private PendingOperation Discard(string correlationId)
        {
            lock (_correlationLock)
            {
                if (!_pending.TryGetValue(correlationId, out var operation))
                    return null;

                _pending.Remove(correlationId);
                return operation;
            }
        }

        private void HandleReply(GamersServerReply reply)
        {
            if (reply == null) return;

            if (reply.ProtocolVersion != ProtocolVersion)
            {
                HandleUnsupportedProtocolVersion(reply);
                return;
            }

            PendingOperation operation = null;
            if (!string.IsNullOrEmpty(reply.CorrelationId))
                operation = Discard(reply.CorrelationId);

            if (operation == null)
            {
                // No correlation id, or one we are not waiting on: an unsolicited server push, or a
                // late duplicate of a reply we already consumed. Either way it must not touch the
                // state of any pending request.
                HandleServerPush(reply);
                return;
            }

            operation.Cleanup();

            if (reply is ErrorReply error)
            {
                operation.Completion.TrySetException(GamersClientException.FromReply(error));
                return;
            }

            if (!operation.ExpectedReplyType.IsInstanceOfType(reply))
            {
                operation.Completion.TrySetException(new GamersClientException(
                    "PROTOCOL_MISMATCH",
                    $"Expected {operation.ExpectedReplyType.Name} but the game-server sent {reply.GetType().Name}.",
                    false,
                    reply.CorrelationId));
                return;
            }

            operation.Completion.TrySetResult(reply);
        }

        private void HandleUnsupportedProtocolVersion(GamersServerReply reply)
        {
            var message =
                $"The game-server replied with protocol version {reply.ProtocolVersion}; this client speaks {ProtocolVersion}.";

            var operation = string.IsNullOrEmpty(reply.CorrelationId) ? null : Discard(reply.CorrelationId);

            if (operation != null)
            {
                operation.Cleanup();
                operation.Completion.TrySetException(new GamersClientException(
                    "UNSUPPORTED_PROTOCOL_VERSION", message, false, reply.CorrelationId));
                return;
            }

            RaiseError(new GamersClientException(
                "UNSUPPORTED_PROTOCOL_VERSION", message, false, reply.CorrelationId));
        }

        /// <summary>
        /// Handles a reply that is not the answer to a pending request: an unsolicited push from
        /// the game-server, or a duplicate of a reply already consumed.
        /// </summary>
        private void HandleServerPush(GamersServerReply reply)
        {
            switch (reply)
            {
                case LeaderboardSnapshotReply leaderboard:
                    OnLeaderboardUpdated?.Invoke(leaderboard);
                    break;
                case ErrorReply error:
                    TransitionTo(GamersClientState.Error);
                    OnError?.Invoke(error);
                    break;
                case AuthCodeRequestedReply codeRequested:
                    OnAuthCodeRequested?.Invoke(codeRequested);
                    break;
                case AuthResultReply auth when auth.Success:
                    TransitionTo(GamersClientState.Authenticated);
                    OnAuthenticated?.Invoke(auth);
                    break;
                case EventJoinResultReply eventJoin when eventJoin.Success:
                    CurrentEventId = eventJoin.EventId;
                    TransitionTo(GamersClientState.JoinedEvent);
                    OnEventJoined?.Invoke(eventJoin);
                    break;
                case TournamentJoinResultReply tournamentJoin when tournamentJoin.Success:
                    CurrentTournamentId = tournamentJoin.TournamentId;
                    TransitionTo(GamersClientState.JoinedTournament);
                    OnTournamentJoined?.Invoke(tournamentJoin);
                    break;
            }
        }

        private void RaiseError(GamersClientException ex)
        {
            TransitionTo(GamersClientState.Error);
            OnError?.Invoke(new ErrorReply
            {
                CorrelationId = ex.CorrelationId,
                Code = ex.Code,
                Message = ex.Message,
                Retryable = ex.Retryable
            });
        }

        private void TransitionTo(GamersClientState newState)
        {
            if (State == newState) return;
            State = newState;
            OnStateChanged?.Invoke(newState);
        }

        private sealed class PendingOperation
        {
            public Type ExpectedReplyType;
            public TaskCompletionSource<GamersServerReply> Completion;
            public CancellationTokenSource TimeoutSource;
            public CancellationTokenRegistration TimeoutRegistration;
            public CancellationTokenRegistration CancellationRegistration;

            public void Cleanup()
            {
                TimeoutRegistration.Dispose();
                CancellationRegistration.Dispose();
                TimeoutSource?.Dispose();
            }
        }
    }
}
