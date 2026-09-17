using Gamers.Client.Models.Common;
using Newtonsoft.Json;
using System;

namespace Gamers.Client.Models
{
    /// <summary>Read-only event summary sent from the game-server for display.</summary>
    [Serializable]
    public class EventInfo
    {
        /// <summary>Gets or sets the event identifier.</summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>Gets or sets the event display name. A game-server may use the identifier as a fallback.</summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>Gets or sets the current event lifecycle status.</summary>
        [JsonProperty("status")]
        public EventStatus Status { get; set; }

        /// <summary>Gets or sets the ISO 8601 event start timestamp supplied by the game-server.</summary>
        [JsonProperty("startDate")]
        public string StartDate { get; set; }

        /// <summary>Gets or sets the ISO 8601 event end timestamp supplied by the game-server.</summary>
        [JsonProperty("endDate")]
        public string EndDate { get; set; }
    }
}
