using System.Runtime.CompilerServices;

// Infrastructure declares this topology when publishing and Listener declares it again when binding
// consumer queues; both need the literals below without Contracts' public surface growing to include
// them - GetExportedTypes() on this assembly is how both sides discover which types are integration
// events to publish, and a broker-topology type sitting in that list would get published as one.
[assembly: InternalsVisibleTo("FinBeat.TaskManagement.Infrastructure")]
[assembly: InternalsVisibleTo("FinBeat.TaskManagement.Listener")]

namespace FinBeat.TaskManagement.Contracts.Messaging;

/// <summary>
/// The broker names the publishing side (Infrastructure) and the consuming side (Listener) must
/// declare identically. A mismatch here is not a compile error or a failing test - it is
/// <c>PRECONDITION_FAILED - inequivalent arg 'alternate-exchange'</c> at broker-declare time, in
/// whichever deployable did not change. Contracts is the only assembly both of them reference, so
/// the names live here instead of as two hand-copied literals.
/// </summary>
/// <remarks>Plain constants only: Contracts has zero package and project references, so nothing here
/// may depend on MassTransit or anything else that would give it one. Internal, not public: the
/// event-publishing loops on both sides enumerate this assembly's exported types, so a public type
/// here would be mistaken for an integration event.</remarks>
internal static class BrokerTopology
{
    /// <summary>The configuration section both sides bind their RabbitMQ transport options from.</summary>
    internal const string RabbitMqSectionName = "RabbitMq";

    /// <summary>The exchange and queue that collect events no consumer queue was bound to receive.</summary>
    internal const string UnroutableName = "unroutable";
}
