using Gamers.Client.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Gamers.Client.Samples
{
    /// <summary>
    /// Mock transport that serializes messages as JSON and logs them to the console.
    /// In a real game this would send the JSON to your game-server over your chosen networking layer.
    /// </summary>
    public class ReferenceTransport : IGamersTransport
    {
        public event Action<GamersServerReply> ReplyReceived;

        public Task SendAsync(GamersClientMessage message, CancellationToken ct = default)
        {
            var json = JsonConvert.SerializeObject(message, Formatting.Indented);
            Debug.Log($"[ReferenceTransport] Sending to game-server:\n{json}");

            // Simulate a successful reply for demonstration.
            SimulateReply(message);
            return Task.CompletedTask;
        }

        private void SimulateReply(GamersClientMessage message)
        {
            switch (message)
            {
                case RequestAuthMessage request:
                    ReplyReceived?.Invoke(new AuthCodeRequestedReply
                    {
                        Email = request.Email,
                        Message = "A verification code has been sent.",
                        CorrelationId = message.CorrelationId
                    });
                    break;
                case SubmitCodeMessage submit:
                    ReplyReceived?.Invoke(new AuthResultReply
                    {
                        Success = true,
                        UserId = $"user-for-{submit.Email}",
                        CorrelationId = message.CorrelationId
                    });
                    break;
                case JoinTournamentMessage join:
                    ReplyReceived?.Invoke(new TournamentJoinResultReply
                    {
                        TournamentId = join.TournamentId,
                        EntryId = $"entry-{join.TournamentId}",
                        Success = true,
                        CorrelationId = message.CorrelationId
                    });
                    break;
                case JoinEventMessage joinEvent:
                    ReplyReceived?.Invoke(new EventJoinResultReply
                    {
                        EventId = joinEvent.EventId,
                        EntryId = $"entry-{joinEvent.EventId}",
                        Success = true,
                        CorrelationId = message.CorrelationId
                    });
                    break;
                case RequestLeaderboardMessage leaderboard:
                    ReplyReceived?.Invoke(new LeaderboardSnapshotReply
                    {
                        TournamentId = leaderboard.TournamentId,
                        MetricId = leaderboard.MetricId,
                        Limit = leaderboard.Limit,
                        Offset = leaderboard.Offset,
                        TotalEntries = 1,
                        Entries = new List<LeaderboardEntry>
                        {
                            new LeaderboardEntry
                            {
                                Rank = 1,
                                DisplayName = "PlayerOne",
                                PlayerId = "player-1",
                                Score = 100m
                            }
                        },
                        CorrelationId = message.CorrelationId
                    });
                    break;
            }
        }
    }
}
