using System.Runtime.CompilerServices;

// Infrastructure publishes this topology and Listener binds queues from it - both need these
// constants internally, without them becoming part of Contracts' public (and published) surface.
[assembly: InternalsVisibleTo("FinBeat.TaskManagement.Infrastructure")]
[assembly: InternalsVisibleTo("FinBeat.TaskManagement.Listener")]

namespace FinBeat.TaskManagement.Contracts.Messaging;

/// <summary>Broker names Infrastructure (publishing) and Listener (consuming) must declare identically.</summary>
/// <remarks>A mismatch is caught by neither the compiler nor a test - it surfaces as <c>PRECONDITION_FAILED
/// - inequivalent arg 'alternate-exchange'</c> at broker-declare time, so the names live in the one
/// assembly both sides reference. Internal because both sides publish every type this assembly exports, and plain
/// constants only because Contracts carries zero package or project references.</remarks>
internal static class BrokerTopology
{
    /// <summary>The configuration section both sides bind their RabbitMQ transport options from.</summary>
    internal const string RabbitMqSectionName = "RabbitMq";

    /// <summary>The exchange and queue that collect events no consumer queue was bound to receive.</summary>
    internal const string UnroutableName = "unroutable";
}
