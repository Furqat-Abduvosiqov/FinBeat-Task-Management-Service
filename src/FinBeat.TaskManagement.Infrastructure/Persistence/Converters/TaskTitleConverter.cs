using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FinBeat.TaskManagement.Infrastructure.Persistence.Converters;

/// <summary>Converts <see cref="TaskTitle"/> to and from the <see cref="string"/> column that stores it.</summary>
/// <remarks>
/// The read direction goes through <see cref="TaskTitle.Create(string?)"/> rather than a constructor
/// because <see cref="TaskTitle"/> has none to call — it is private, precisely so <c>Create</c> is
/// the only way to obtain an instance. <c>Create</c> trims before validating, but a value already
/// stored by this same converter is already trimmed, so re-trimming it is a no-op and the round trip
/// from column to <see cref="TaskTitle"/> and back is lossless.
/// </remarks>
public sealed class TaskTitleConverter : ValueConverter<TaskTitle, string>
{
    /// <summary>Creates the converter.</summary>
    public TaskTitleConverter()
        : base(title => title.Value, value => TaskTitle.Create(value))
    {
    }
}
