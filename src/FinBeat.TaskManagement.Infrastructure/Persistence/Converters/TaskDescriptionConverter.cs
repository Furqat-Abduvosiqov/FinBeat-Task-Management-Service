using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FinBeat.TaskManagement.Infrastructure.Persistence.Converters;

/// <summary>Converts <see cref="TaskDescription"/> to and from the <see cref="string"/> column that stores it.</summary>
/// <remarks>
/// <para>
/// The provider type is a non-nullable <see cref="string"/>, deliberately: the column is
/// <c>NOT NULL</c> and <see cref="TaskDescription.None"/> is stored as <c>''</c>, not <c>NULL</c>.
/// EF never passes a null value through a value converter, so if the column allowed <c>NULL</c> a
/// row with none would materialise <c>Description = null</c> — breaking the aggregate's contract
/// that absence is <see cref="TaskDescription.None"/>, never a null reference.
/// </para>
/// <para>
/// <see cref="TaskDescription.Create(string?)"/> maps <c>""</c> back to the <see cref="TaskDescription.None"/>
/// singleton, so the round trip from column to <see cref="TaskDescription"/> and back is not just
/// value-equal but reference-identical.
/// </para>
/// </remarks>
public sealed class TaskDescriptionConverter : ValueConverter<TaskDescription, string>
{
    /// <summary>Creates the converter.</summary>
    public TaskDescriptionConverter()
        : base(description => description.Value, value => TaskDescription.Create(value))
    {
    }
}
