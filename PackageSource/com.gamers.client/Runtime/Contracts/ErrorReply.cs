using Newtonsoft.Json;
using System;

namespace Gamers.Client
{
    /// <summary>Sent by the game-server when an operation fails or an error occurs.</summary>
    [Serializable]
    public class ErrorReply : GamersServerReply
    {
        /// <summary>Gets the <c>error</c> wire-protocol discriminator.</summary>
        [JsonProperty("type")]
        public override string Type => "error";

        /// <summary>Short error code.</summary>
        [JsonProperty("code")]
        public string Code { get; set; }

        /// <summary>Human-readable error message safe to show to players.</summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>Whether the client may safely retry the same request.</summary>
        [JsonProperty("retryable")]
        public bool Retryable { get; set; }
    }
}
