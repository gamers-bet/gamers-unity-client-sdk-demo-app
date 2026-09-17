using Newtonsoft.Json;
using System;

namespace Gamers.Client
{
    /// <summary>Sent by the client when the player wants to join an event.</summary>
    [Serializable]
    public class JoinEventMessage : GamersClientMessage
    {
        /// <summary>Gets the <c>event:join</c> wire-protocol discriminator.</summary>
        [JsonProperty("type")]
        public override string Type => "event:join";

        /// <summary>The event identifier.</summary>
        [JsonProperty("eventId")]
        public string EventId { get; set; }

        /// <summary>Initializes an empty message for deserialization.</summary>
        public JoinEventMessage() { }

        /// <summary>Initializes an event join request.</summary>
        /// <param name="eventId">The event identifier.</param>
        public JoinEventMessage(string eventId)
        {
            EventId = eventId;
        }
    }
}
