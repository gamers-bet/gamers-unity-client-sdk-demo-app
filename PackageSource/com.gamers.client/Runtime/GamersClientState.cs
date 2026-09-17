namespace Gamers.Client
{
    /// <summary>States of the <see cref="GamersClientFlow"/>.</summary>
    public enum GamersClientState
    {
        /// <summary>Initial state, no operation in progress.</summary>
        Idle,

        /// <summary>A verification code has been requested.</summary>
        RequestingAuth,

        /// <summary>A verification code has been requested and is awaited.</summary>
        AwaitingCode,

        /// <summary>The code has been submitted and a result is awaited.</summary>
        AwaitingAuthResult,

        /// <summary>Authentication succeeded.</summary>
        Authenticated,

        /// <summary>Attempting to join an event.</summary>
        JoiningEvent,

        /// <summary>Successfully joined an event.</summary>
        JoinedEvent,

        /// <summary>Attempting to join a tournament.</summary>
        JoiningTournament,

        /// <summary>Successfully joined a tournament.</summary>
        JoinedTournament,

        /// <summary>An error occurred and the flow is in an error state.</summary>
        Error
    }
}
