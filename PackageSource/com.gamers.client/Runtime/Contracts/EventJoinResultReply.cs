using Newtonsoft.Json;
using System;

namespace Gamers.Client
{
    /// <summary>Sent by the game-server after the player requests to join an event.</summary>
    [Serializable]
    public class EventJoinResultReply : GamersServerReply
    {
        /// <summary>Gets the <c>event:join-result</c> wire-protocol discriminator.</summary>
        [JsonProperty("type")]
        public override string Type => "event:join-result";

        /// <summary>The event identifier.</summary>
        [JsonProperty("eventId")]
        public string EventId { get; set; }

        /// <summary>The entry identifier if one was created.</summary>
        [JsonProperty("entryId")]
        public string EntryId { get; set; }

        /// <summary>Whether the join request was accepted.</summary>
        [JsonProperty("success")]
        public bool Success { get; set; }

        /// <summary>Human-readable error message if the join failed.</summary>
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
