using System;
using System.Threading;
using UnityEngine;

namespace Gamers.Client.Samples
{
    /// <summary>Example MonoBehaviour showing the full client auth + join flow.</summary>
    /// <remarks>
    /// Each operation returns a <see cref="System.Threading.Tasks.Task{TResult}"/> that completes when the
    /// game-server's reply arrives, so the awaited result is the real outcome. Failures surface as
    /// <see cref="GamersClientException"/>; a game-server that never answers surfaces as
    /// <see cref="TimeoutException"/> after <see cref="GamersClientFlow.RequestTimeout"/>.
    ///
    /// The events are still available for code elsewhere in the scene that is not awaiting the call.
    /// </remarks>
    public class ReferenceIntegration : MonoBehaviour
    {
        private GamersClientFlow _flow;
        private CancellationTokenSource _lifetime;

        void Start()
        {
            var transport = GetComponent<WebSocketTransport>();
            _flow = new GamersClientFlow(transport)
            {
                RequestTimeout = TimeSpan.FromSeconds(30)
            };
            _lifetime = new CancellationTokenSource();

            _flow.OnStateChanged += state => Debug.Log($"[ReferenceIntegration] State: {state}");
            _flow.OnAuthCodeRequested += result => Debug.Log($"[ReferenceIntegration] Code requested for {result.Email}");
            _flow.OnAuthenticated += result => Debug.Log($"[ReferenceIntegration] Authenticated: {result.UserId}");
            _flow.OnTournamentJoined += result => Debug.Log($"[ReferenceIntegration] Joined tournament {result.TournamentId} with entry {result.EntryId}");
            _flow.OnEventJoined += result => Debug.Log($"[ReferenceIntegration] Joined event {result.EventId} with entry {result.EntryId}");
            _flow.OnLeaderboardUpdated += snapshot => Debug.Log($"[ReferenceIntegration] Leaderboard has {snapshot.Entries.Count} entries");
            _flow.OnError += error => Debug.LogError($"[ReferenceIntegration] Error {error.Code}: {error.Message}");
        }

        void OnDestroy()
        {
            // Cancelling first fails any in-flight operations promptly; Dispose then cancels
            // anything still outstanding so no awaiting handler is left hanging.
            _lifetime?.Cancel();
            _lifetime?.Dispose();
            _flow?.Dispose();
        }

        public async void OnRequestAuth(string email)
        {
            try
            {
                var result = await _flow.RequestAuthAsync(email, _lifetime.Token);
                Debug.Log($"[ReferenceIntegration] Code sent to {result.Email}");
            }
            catch (OperationCanceledException) { /* scene unloaded */ }
            catch (TimeoutException) { Debug.LogError("[ReferenceIntegration] The game-server did not respond."); }
            catch (GamersClientException ex)
            {
                Debug.LogError($"[ReferenceIntegration] RequestAuth failed: {ex.Code} - {ex.Message} (retryable: {ex.Retryable})");
            }
        }

        public async void OnSubmitCode(string email, string code)
        {
            try
            {
                var result = await _flow.SubmitCodeAsync(email, code, _lifetime.Token);
                Debug.Log($"[ReferenceIntegration] Signed in as {result.UserId}");
            }
            catch (OperationCanceledException) { }
            catch (TimeoutException) { Debug.LogError("[ReferenceIntegration] The game-server did not respond."); }
            catch (GamersClientException ex)
            {
                Debug.LogError($"[ReferenceIntegration] SubmitCode failed: {ex.Code} - {ex.Message}");
            }
        }

        public async void OnJoinTournament(string tournamentId)
        {
            try
            {
                // Completes when the join actually succeeded, not when the message was sent.
                var result = await _flow.JoinTournamentAsync(tournamentId, _lifetime.Token);
                Debug.Log($"[ReferenceIntegration] Entry {result.EntryId} created for {result.TournamentId}");
            }
            catch (OperationCanceledException) { }
            catch (TimeoutException) { Debug.LogError("[ReferenceIntegration] The game-server did not respond."); }
            catch (GamersClientException ex)
            {
                Debug.LogError($"[ReferenceIntegration] JoinTournament failed: {ex.Code} - {ex.Message}");
            }
        }

        public async void OnJoinEvent(string eventId)
        {
            try
            {
                var result = await _flow.JoinEventAsync(eventId, _lifetime.Token);
                Debug.Log($"[ReferenceIntegration] Entry {result.EntryId} created for {result.EventId}");
            }
            catch (OperationCanceledException) { }
            catch (TimeoutException) { Debug.LogError("[ReferenceIntegration] The game-server did not respond."); }
            catch (GamersClientException ex)
            {
                Debug.LogError($"[ReferenceIntegration] JoinEvent failed: {ex.Code} - {ex.Message}");
            }
        }

        public async void OnShowLeaderboard(string tournamentId)
        {
            try
            {
                var snapshot = await _flow.RequestLeaderboardAsync(tournamentId, ct: _lifetime.Token);
                Debug.Log($"[ReferenceIntegration] {snapshot.TotalEntries} entries, showing {snapshot.Entries.Count}");
            }
            catch (OperationCanceledException) { }
            catch (TimeoutException) { Debug.LogError("[ReferenceIntegration] The game-server did not respond."); }
            catch (GamersClientException ex)
            {
                Debug.LogError($"[ReferenceIntegration] Leaderboard failed: {ex.Code} - {ex.Message}");
            }
        }
    }
}
