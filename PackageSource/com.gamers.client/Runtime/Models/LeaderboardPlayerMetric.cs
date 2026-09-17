using Newtonsoft.Json;
using System;

namespace Gamers.Client.Models
{
    /// <summary>Per-metric aggregate for a player in a by-player leaderboard view.</summary>
    [Serializable]
    public class LeaderboardPlayerMetric
    {
        /// <summary>Metric identifier.</summary>
        [JsonProperty("metricId")]
        public string MetricId { get; set; }

        /// <summary>Metric display name, if available.</summary>
        [JsonProperty("metricName")]
        public string MetricName { get; set; }

        /// <summary>Best score recorded for this metric.</summary>
        [JsonProperty("bestScore")]
        public decimal BestScore { get; set; }

        /// <summary>Number of attempts submitted for this metric.</summary>
        [JsonProperty("attemptCount")]
        public int AttemptCount { get; set; }

        /// <summary>Estimated prize for this metric, if available.</summary>
        [JsonProperty("estimatedPrize")]
        public decimal? EstimatedPrize { get; set; }
    }
}
