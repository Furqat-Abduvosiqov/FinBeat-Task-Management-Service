namespace FinBeat.TaskManagement.Application.Results;

/// <summary>One page of results, and enough for a caller to ask for the next.</summary>
/// <typeparam name="TItem">What the page holds.</typeparam>
/// <param name="Items">The items on this page.</param>
/// <param name="Number">Which page this is, counting from one.</param>
/// <param name="Size">How many items a full page holds.</param>
/// <param name="TotalItems">How many items match in total, across every page.</param>
public sealed record Page<TItem>(IReadOnlyList<TItem> Items, int Number, int Size, long TotalItems)
{
    /// <summary>How many pages the total divides into.</summary>
    public int TotalPages => Size <= 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)Size);

    /// <summary>Whether another page follows this one.</summary>
    public bool HasNext => Number < TotalPages;
}
