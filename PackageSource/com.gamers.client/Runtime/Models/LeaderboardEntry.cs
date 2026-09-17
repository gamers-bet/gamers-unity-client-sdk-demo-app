using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Gamers.Client.Models
{
    /// <summary>Read-only leaderboard entry sent from the game-server for display.</summary>
    [Serializable]
    public class LeaderboardEntry
    {
        /// <summary>Rank position, 1-based.</summary>
        [JsonProperty("rank")]
        public int Rank { get; set; }

        /// <summary>Display name or alias of the player.</summary>
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        /// <summary>Player identifier used by the developer's game.</summary>
        [JsonProperty("playerId")]
        public string PlayerId { get; set; }

        /// <summary>Score value for this entry. Null when the leaderboard is viewed by-player, because no single score exists.</summary>
        [JsonProperty("score")]
        public decimal? Score { get; set; }

        /// <summary>Metric identifier, when the leaderboard is viewed by metric.</summary>
        [JsonProperty("metricId")]
        public string MetricId { get; set; }

        /// <summary>Best score for the requested metric view.</summary>
        [JsonProperty("bestScore")]
        public decimal? BestScore { get; set; }

        /// <summary>Number of attempts for the requested metric view.</summary>
        [JsonProperty("attemptCount")]
        public int? AttemptCount { get; set; }

        /// <summary>Estimated prize for the requested metric view.</summary>
        [JsonProperty("estimatedPrize")]
        public decimal? EstimatedPrize { get; set; }

        /// <summary>Per-metric aggregates when the default by-player view is returned.</summary>
        [JsonProperty("metrics")]
        public List<LeaderboardPlayerMetric> Metrics { get; set; } = new List<LeaderboardPlayerMetric>();
    }
}
