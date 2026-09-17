using Newtonsoft.Json;
using System;

namespace Gamers.Client
{
    /// <summary>Sent by the game-server when the verification code has been requested.</summary>
    [Serializable]
    public class AuthCodeRequestedReply : GamersServerReply
    {
        /// <summary>Gets the <c>auth:code-requested</c> wire-protocol discriminator.</summary>
        [JsonProperty("type")]
        public override string Type => "auth:code-requested";

        /// <summary>The email address the code was sent to.</summary>
        [JsonProperty("email")]
        public string Email { get; set; }

        /// <summary>Human-readable status message.</summary>
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
