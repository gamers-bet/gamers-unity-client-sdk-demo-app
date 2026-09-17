using Newtonsoft.Json;
using System;

namespace Gamers.Client
{
    /// <summary>Sent by the client when it wants the game-server to push a leaderboard snapshot.</summary>
    [Serializable]
    public class RequestLeaderboardMessage : GamersClientMessage
    {
        /// <summary>Gets the <c>tournament:leaderboard</c> wire-protocol discriminator.</summary>
        [JsonProperty("type")]
        public override string Type => "tournament:leaderboard";

        /// <summary>The tournament identifier.</summary>
        [JsonProperty("tournamentId")]
        public string TournamentId { get; set; }

        /// <summary>Optional metric identifier to filter by.</summary>
        [JsonProperty("metricId")]
        public string MetricId { get; set; }

        /// <summary>Pagination offset of the first entry to return.</summary>
        [JsonProperty("offset")]
        public int Offset { get; set; }

        /// <summary>Maximum number of entries to return.</summary>
        [JsonProperty("limit")]
        public int Limit { get; set; } = 50;

        /// <summary>Initializes an empty message for deserialization.</summary>
        public RequestLeaderboardMessage() { }

        /// <summary>Initializes a leaderboard request.</summary>
        /// <param name="tournamentId">The tournament identifier.</param>
        /// <param name="metricId">An optional metric identifier for a by-metric view.</param>
        /// <param name="offset">The zero-based pagination offset.</param>
        /// <param name="limit">The maximum number of entries to return.</param>
        public RequestLeaderboardMessage(string tournamentId, string metricId = null, int offset = 0, int limit = 50)
        {
            TournamentId = tournamentId;
            MetricId = metricId;
            Offset = offset;
            Limit = limit;
        }
    }
}
