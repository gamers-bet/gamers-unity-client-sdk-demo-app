using Newtonsoft.Json;
using System;

namespace Gamers.Client
{
    /// <summary>Sent by the game-server after the player submits a verification code.</summary>
    [Serializable]
    public class AuthResultReply : GamersServerReply
    {
        /// <summary>Gets the <c>auth:result</c> wire-protocol discriminator.</summary>
        [JsonProperty("type")]
        public override string Type => "auth:result";

        /// <summary>Whether authentication succeeded.</summary>
        [JsonProperty("success")]
        public bool Success { get; set; }

        /// <summary>Gamers user id if authentication succeeded.</summary>
        [JsonProperty("userId")]
        public string UserId { get; set; }

        /// <summary>Human-readable error or status message if authentication failed.</summary>
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
