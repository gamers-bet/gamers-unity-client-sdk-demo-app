using Newtonsoft.Json;
using System;

namespace Gamers.Client
{
    /// <summary>Base class for all replies sent from the game-server to the Unity client.</summary>
    [Serializable]
    public abstract class GamersServerReply
    {
        /// <summary>Discriminator used by the client to route the reply.</summary>
        [JsonProperty("type")]
        public abstract string Type { get; }

        /// <summary>
        /// Protocol version of the client↔game-server envelope. Starts at <c>1</c>.
        /// </summary>
        [JsonProperty("protocolVersion")]
        public int ProtocolVersion { get; set; } = 1;

        /// <summary>
        /// Optional correlation identifier echoed from the originating <see cref="GamersClientMessage.CorrelationId"/>.
        /// </summary>
        [JsonProperty("correlationId")]
        public string CorrelationId { get; set; }
    }
}
