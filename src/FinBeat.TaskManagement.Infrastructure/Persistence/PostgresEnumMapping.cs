using FinBeat.TaskManagement.Domain.Tasks;

namespace FinBeat.TaskManagement.Infrastructure.Persistence;

/// <summary>The PostgreSQL enum types this model declares, named once for both halves of the mapping.</summary>
/// <remarks>
/// A native PostgreSQL enum has to be declared twice, and the two halves do different jobs.
/// <c>HasPostgresEnum</c> puts the type on the EF model, which is what writes <c>CREATE TYPE</c> into
/// the migration. <c>MapEnum</c> puts it on the ADO.NET data source, which is what the provider
/// resolves the column's store type from, as well as the wire format — configure a context without a
/// data source and the column silently falls back to <c>integer</c> while the type is still created.
/// Neither half knows about the other, so the name they have to agree on is spelled once, here.
/// </remarks>
public static class PostgresEnumMapping
{
    /// <summary>The PostgreSQL type name for <see cref="TaskItemStatus"/>.</summary>
    /// <remarks>
    /// Only the name is pinned. The labels come from the translator both halves already default to,
    /// which snake-cases member names under the invariant culture, so passing one explicitly would
    /// restate the default rather than defend against anything. What does defend against a changed
    /// default, or a renamed enum member, is the model test that asserts the four labels literally.
    /// </remarks>
    public const string TaskItemStatusTypeName = "task_item_status";
}
