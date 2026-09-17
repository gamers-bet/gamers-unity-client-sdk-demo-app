using Newtonsoft.Json;
using System;

namespace Gamers.Client.Models
{
    /// <summary>Read-only prize information sent from the game-server for display.</summary>
    [Serializable]
    public class PrizeInfo
    {
        /// <summary>Gets or sets the one-based finishing position awarded this prize.</summary>
        [JsonProperty("position")]
        public int Position { get; set; }

        /// <summary>Gets or sets the prize amount in <see cref="Currency"/>.</summary>
        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        /// <summary>Gets or sets the currency code used for <see cref="Amount"/>.</summary>
        [JsonProperty("currency")]
        public string Currency { get; set; }
    }
}
