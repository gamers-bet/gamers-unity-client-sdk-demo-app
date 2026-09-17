using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace Gamers.Client.Models.Common
{
    /// <summary>Lifecycle status of an event, for display purposes.</summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EventStatus
    {
        /// <summary>The event is open.</summary>
        [EnumMember(Value = "OPEN")] Open,

        /// <summary>The event has started.</summary>
        [EnumMember(Value = "STARTED")] Started,

        /// <summary>The event is closed and results are being processed.</summary>
        [EnumMember(Value = "COMPLETED")] Completed,

        /// <summary>The event is closed and winners have been rewarded.</summary>
        [EnumMember(Value = "REWARDED")] Rewarded,

        /// <summary>The event was cancelled and any required refunds are pending.</summary>
        [EnumMember(Value = "CANCELLED")] Cancelled,

        /// <summary>The event was cancelled and applicable entry fees were refunded.</summary>
        [EnumMember(Value = "REFUNDED")] Refunded
    }

    /// <summary>Lifecycle status of a tournament, for display purposes.</summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TournamentStatus
    {
        /// <summary>The tournament is being configured and is not open for entry.</summary>
        [EnumMember(Value = "DRAFT")] Draft,

        /// <summary>The tournament is scheduled but is not yet open for entry.</summary>
        [EnumMember(Value = "PENDING")] Pending,

        /// <summary>The tournament is open for player entry.</summary>
        [EnumMember(Value = "OPENED")] Opened,

        /// <summary>The tournament is closed to new entries.</summary>
        [EnumMember(Value = "CLOSED")] Closed,

        /// <summary>The tournament window is closed and results are being processed.</summary>
        [EnumMember(Value = "COMPLETED")] Completed,

        /// <summary>The tournament was cancelled before completion.</summary>
        [EnumMember(Value = "CANCELLED")] Cancelled,

        /// <summary>The tournament was cancelled and applicable entry fees were refunded.</summary>
        [EnumMember(Value = "REFUNDED")] Refunded
    }
}
