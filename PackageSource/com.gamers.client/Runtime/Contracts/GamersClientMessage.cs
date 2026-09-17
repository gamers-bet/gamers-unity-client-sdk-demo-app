using Newtonsoft.Json;
using System;

namespace Gamers.Client
{
    /// <summary>Base class for all messages sent from the Unity client to the game-server.</summary>
    [Serializable]
    public abstract class GamersClientMessage
    {
        /// <summary>Discriminator used by the game-server to route the message.</summary>
        [JsonProperty("type")]
        public abstract string Type { get; }

        /// <summary>
        /// Protocol version of the client↔game-server envelope. Starts at <c>1</c>.
        /// </summary>
        [JsonProperty("protocolVersion")]
        public int ProtocolVersion { get; set; } = 1;

        /// <summary>
        /// Optional correlation identifier that the game-server can echo back in its reply.
        /// Implementations may set this automatically in <see cref="IGamersTransport.SendAsync"/>.
        /// </summary>
        [JsonProperty("correlationId")]
        public string CorrelationId { get; set; }
    }
}
