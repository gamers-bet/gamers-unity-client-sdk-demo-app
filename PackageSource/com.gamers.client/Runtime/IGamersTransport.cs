using System;
using System.Threading;
using System.Threading.Tasks;

namespace Gamers.Client
{
    /// <summary>
    /// Transport abstraction implemented by the game developer.
    /// Carries serialized <see cref="GamersClientMessage"/> objects from the Unity client to the developer's game-server,
    /// and delivers <see cref="GamersServerReply"/> objects back.
    /// </summary>
    /// <remarks>
    /// Implementations are free to use any networking stack (Netcode for GameObjects, Mirror, Photon, WebSockets, REST, etc.).
    /// The implementation is responsible for serializing messages, routing them to the game-server, and raising
    /// <see cref="ReplyReceived"/> when a server reply arrives.
    ///
    /// Server replies must echo the <see cref="GamersClientMessage.CorrelationId"/> from the originating message
    /// in <see cref="GamersServerReply.CorrelationId"/> so that <see cref="GamersClientFlow"/> can match late or
    /// duplicate replies to the correct pending request.
    /// </remarks>
    public interface IGamersTransport
    {
        /// <summary>Sends a message to the game-server.</summary>
        /// <param name="message">The non-null message to serialize and send.</param>
        /// <param name="ct">A token that cancels the send operation.</param>
        /// <returns>A task that completes when the transport accepts the message, not when the server replies.</returns>
        /// <exception cref="OperationCanceledException"><paramref name="ct"/> is cancelled.</exception>
        Task SendAsync(GamersClientMessage message, CancellationToken ct = default);

        /// <summary>Raised when a reply arrives from the game-server.</summary>
        /// <remarks>
        /// This event must be raised on Unity's main thread.
        /// If the transport receives replies on a background thread, marshal the callback to the main thread
        /// (for example via <see cref="UnityEngine.UnitySynchronizationContext"/>) before invoking
        /// <see cref="ReplyReceived"/>.
        /// </remarks>
        event Action<GamersServerReply> ReplyReceived;
    }
}
