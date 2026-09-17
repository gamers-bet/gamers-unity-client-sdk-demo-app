using Newtonsoft.Json;
using System;

namespace Gamers.Client
{
    /// <summary>Sent by the client when the player wants to start Gamers authentication.</summary>
    [Serializable]
    public class RequestAuthMessage : GamersClientMessage
    {
        /// <summary>Gets the <c>auth:request</c> wire-protocol discriminator.</summary>
        [JsonProperty("type")]
        public override string Type => "auth:request";

        /// <summary>The email address to send the verification code to.</summary>
        [JsonProperty("email")]
        public string Email { get; set; }

        /// <summary>Initializes an empty message for deserialization.</summary>
        public RequestAuthMessage() { }

        /// <summary>Initializes a request for a verification code.</summary>
        /// <param name="email">The email address to authenticate.</param>
        public RequestAuthMessage(string email)
        {
            Email = email;
        }
    }
}
