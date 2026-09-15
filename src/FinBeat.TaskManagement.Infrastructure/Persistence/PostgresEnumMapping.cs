using System.Globalization;
using FinBeat.TaskManagement.Domain.Tasks;
using Npgsql;
using Npgsql.NameTranslation;

namespace FinBeat.TaskManagement.Infrastructure.Persistence;

/// <summary>The PostgreSQL enum types this model declares, named once for both halves of the mapping.</summary>
/// <remarks>
/// <para>
/// A native PostgreSQL enum has to be declared twice. <c>HasPostgresEnum</c> puts it on the EF model,
/// which is what writes <c>CREATE TYPE</c> into the migration. <c>MapEnum</c> puts it on the ADO.NET
/// data source, which is what lets Npgsql read and write the value on the wire. Neither knows about
/// the other.
/// </para>
/// <para>
/// Both default to <see cref="NpgsqlSnakeCaseNameTranslator"/>, so they would agree today by
/// coincidence. Sharing one name and one translator instance makes them agree by construction — a
/// mismatch is not a compile error, it is an exception on the first read.
/// </para>
/// </remarks>
public static class PostgresEnumMapping
{
    /// <summary>The PostgreSQL type name for <see cref="TaskItemStatus"/>.</summary>
    public const string TaskItemStatusTypeName = "task_item_status";

    /// <summary>The translator both halves of every enum mapping use.</summary>
    /// <remarks>
    /// Invariant culture explicitly. The translator lower-cases, and on a Turkish-locale machine the
    /// default rules turn <c>InProgress</c> into <c>ın_progress</c>.
    /// </remarks>
    public static readonly INpgsqlNameTranslator NameTranslator =
        new NpgsqlSnakeCaseNameTranslator(CultureInfo.InvariantCulture);
}
