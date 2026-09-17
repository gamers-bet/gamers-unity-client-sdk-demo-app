using Newtonsoft.Json;
using System;

namespace Gamers.Client
{
    /// <summary>Sent by the client when the player wants to join a tournament.</summary>
    [Serializable]
    public class JoinTournamentMessage : GamersClientMessage
    {
        /// <summary>Gets the <c>tournament:join</c> wire-protocol discriminator.</summary>
        [JsonProperty("type")]
        public override string Type => "tournament:join";

        /// <summary>The tournament identifier.</summary>
        [JsonProperty("tournamentId")]
        public string TournamentId { get; set; }

        /// <summary>Initializes an empty message for deserialization.</summary>
        public JoinTournamentMessage() { }

        /// <summary>Initializes a tournament join request.</summary>
        /// <param name="tournamentId">The tournament identifier.</param>
        public JoinTournamentMessage(string tournamentId)
        {
            TournamentId = tournamentId;
        }
    }
}
