using System;

namespace Gamers.Client
{
    /// <summary>
    /// Thrown when a <see cref="GamersClientFlow"/> operation fails.
    /// </summary>
    /// <remarks>
    /// Raised when the game-server answers a request with an <see cref="ErrorReply"/>, when it
    /// answers with a reply of the wrong type, or when the flow itself rejects the exchange
    /// (unsupported protocol version). A request that receives no reply within
    /// <see cref="GamersClientFlow.RequestTimeout"/> throws <see cref="TimeoutException"/>
    /// instead, and caller cancellation throws <see cref="OperationCanceledException"/>.
    /// </remarks>
    public class GamersClientException : Exception
    {
        /// <summary>Machine-readable error code, as sent by the game-server.</summary>
        public string Code { get; }

        /// <summary>Whether the client may safely retry the same request.</summary>
        public bool Retryable { get; }

        /// <summary>Correlation id of the request that failed, when known.</summary>
        public string CorrelationId { get; }

        /// <summary>Initializes a new instance of the <see cref="GamersClientException"/> class.</summary>
        /// <param name="code">The machine-readable error code.</param>
        /// <param name="message">The player-safe error message.</param>
        /// <param name="retryable">Whether retrying the same request is safe.</param>
        /// <param name="correlationId">The originating request's correlation identifier, when known.</param>
        public GamersClientException(string code, string message, bool retryable = false, string correlationId = null)
            : base(message)
        {
            Code = code;
            Retryable = retryable;
            CorrelationId = correlationId;
        }

        /// <summary>Builds an exception from a server <see cref="ErrorReply"/>.</summary>
        /// <param name="reply">The non-null server error reply.</param>
        /// <returns>An exception containing the reply's code, message, retry flag, and correlation identifier.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="reply"/> is <see langword="null"/>.</exception>
        public static GamersClientException FromReply(ErrorReply reply)
        {
            if (reply == null) throw new ArgumentNullException(nameof(reply));

            return new GamersClientException(
                reply.Code,
                reply.Message,
                reply.Retryable,
                reply.CorrelationId);
        }
    }
}
