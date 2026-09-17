using Newtonsoft.Json;
using System;

namespace Gamers.Client
{
    /// <summary>Sent by the client when the player enters the verification code they received.</summary>
    [Serializable]
    public class SubmitCodeMessage : GamersClientMessage
    {
        /// <summary>Gets the <c>auth:submit-code</c> wire-protocol discriminator.</summary>
        [JsonProperty("type")]
        public override string Type => "auth:submit-code";

        /// <summary>The email address the code was sent to.</summary>
        [JsonProperty("email")]
        public string Email { get; set; }

        /// <summary>The verification code entered by the player.</summary>
        [JsonProperty("code")]
        public string Code { get; set; }

        /// <summary>Initializes an empty message for deserialization.</summary>
        public SubmitCodeMessage() { }

        /// <summary>Initializes a verification-code submission.</summary>
        /// <param name="email">The email address that received the code.</param>
        /// <param name="code">The verification code entered by the player.</param>
        public SubmitCodeMessage(string email, string code)
        {
            Email = email;
            Code = code;
        }
    }
}
