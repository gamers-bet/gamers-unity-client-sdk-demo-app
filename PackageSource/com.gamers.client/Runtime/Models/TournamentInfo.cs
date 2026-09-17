using Gamers.Client.Models.Common;
using Newtonsoft.Json;
using System;

namespace Gamers.Client.Models
{
    /// <summary>Read-only tournament summary sent from the game-server for display.</summary>
    [Serializable]
    public class TournamentInfo
    {
        /// <summary>Gets or sets the tournament identifier.</summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>Gets or sets the tournament display name.</summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>Gets or sets the current tournament lifecycle status.</summary>
        [JsonProperty("status")]
        public TournamentStatus Status { get; set; }

        /// <summary>Gets or sets the ISO 8601 tournament start timestamp supplied by the game-server.</summary>
        [JsonProperty("startDate")]
        public string StartDate { get; set; }

        /// <summary>Gets or sets the ISO 8601 tournament end timestamp supplied by the game-server.</summary>
        [JsonProperty("endDate")]
        public string EndDate { get; set; }

        /// <summary>Gets or sets the tournament entry fee in <see cref="Currency"/>.</summary>
        [JsonProperty("entryFee")]
        public decimal EntryFee { get; set; }

        /// <summary>Gets or sets the currency code used for <see cref="EntryFee"/>.</summary>
        [JsonProperty("currency")]
        public string Currency { get; set; }
    }
}
