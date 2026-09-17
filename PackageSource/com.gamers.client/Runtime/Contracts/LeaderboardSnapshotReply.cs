using Gamers.Client.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Gamers.Client
{
    /// <summary>Sent by the game-server to push a leaderboard snapshot to the client.</summary>
    [Serializable]
    public class LeaderboardSnapshotReply : GamersServerReply
    {
        /// <summary>Gets the <c>tournament:leaderboard</c> wire-protocol discriminator.</summary>
        [JsonProperty("type")]
        public override string Type => "tournament:leaderboard";

        /// <summary>The tournament identifier.</summary>
        [JsonProperty("tournamentId")]
        public string TournamentId { get; set; }

        /// <summary>The metric identifier, if filtered by metric.</summary>
        [JsonProperty("metricId")]
        public string MetricId { get; set; }

        /// <summary>Metric display name, if filtered by metric.</summary>
        [JsonProperty("metricName")]
        public string MetricName { get; set; }

        /// <summary>Sort direction of the returned entries.</summary>
        [JsonProperty("sortDirection")]
        public string SortDirection { get; set; }

        /// <summary>Pagination offset of this snapshot.</summary>
        [JsonProperty("offset")]
        public int Offset { get; set; }

        /// <summary>Maximum number of entries requested.</summary>
        [JsonProperty("limit")]
        public int Limit { get; set; }

        /// <summary>Total number of entries available for this view.</summary>
        [JsonProperty("totalEntries")]
        public int TotalEntries { get; set; }

        /// <summary>Leaderboard entries, ordered by rank.</summary>
        [JsonProperty("entries")]
        public List<LeaderboardEntry> Entries { get; set; } = new List<LeaderboardEntry>();
    }
}
