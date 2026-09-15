using System.Diagnostics.CodeAnalysis;

namespace FinBeat.TaskManagement.Application.Results;

/// <summary>The outcome of a use case: success, or one <see cref="Results.Error"/> saying why not.</summary>
/// <remarks>Expected failures are return values; exceptions stay for what nobody can handle.</remarks>
public class Result
{
    private protected Result(Error? error) => Error = error;

    /// <summary>Whether the operation succeeded.</summary>
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    /// <summary>Whether the operation failed.</summary>
    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFailure => Error is not null;

    /// <summary>Why the operation failed, or null when it succeeded.</summary>
    public Error? Error { get; }

    /// <summary>A success carrying no value.</summary>
    public static Result Success() => new(null);

    /// <summary>A success carrying <paramref name="value"/>.</summary>
    /// <typeparam name="TValue">The type of the value produced.</typeparam>
    /// <param name="value">The value the operation produced.</param>
    public static Result<TValue> Success<TValue>(TValue value) => new(value);

    /// <summary>A failure.</summary>
    /// <param name="error">Why the operation failed.</param>
    public static Result Failure(Error error) => new(error);

    /// <summary>A failure of an operation that would otherwise have produced a value.</summary>
    /// <typeparam name="TValue">The type the operation would have produced.</typeparam>
    /// <param name="error">Why the operation failed.</param>
    public static Result<TValue> Failure<TValue>(Error error) => new(error);
}

/// <summary>The outcome of a use case that produces a value.</summary>
/// <typeparam name="TValue">The type of the value produced on success.</typeparam>
public sealed class Result<TValue> : Result
{
    private readonly TValue _value;

    internal Result(TValue value)
        : base(null) => _value = value;

    internal Result(Error error)
        : base(error) => _value = default!;

    /// <summary>The value produced.</summary>
    /// <exception cref="InvalidOperationException">The result is a failure, so there is no value.</exception>
    public TValue Value => IsSuccess
        ? _value
        : throw new InvalidOperationException($"A failed result has no value. Error: {Error.Code}.");
}
