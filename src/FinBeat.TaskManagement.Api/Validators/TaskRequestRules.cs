using FinBeat.TaskManagement.Domain.Tasks.ValueObjects;
using FluentValidation;

namespace FinBeat.TaskManagement.Api.Endpoints.Validation;

/// <summary>The rules a task title and description obey, so create and update cannot drift apart.</summary>
/// <remarks>Restates the value objects' limits as field-level answers. The domain stays the authority.</remarks>
internal static class TaskRequestRules
{
    internal static IRuleBuilderOptions<TRequest, string?> MustBeATaskTitle<TRequest>(
        this IRuleBuilder<TRequest, string?> rule) =>
        rule.NotEmpty()
            .WithMessage("A task title is required.")
            .Must(WithinLength(TaskTitle.MaxLength))
            .WithMessage($"A task title cannot exceed {TaskTitle.MaxLength} characters once trimmed.");

    internal static IRuleBuilderOptions<TRequest, string?> MustBeATaskDescription<TRequest>(
        this IRuleBuilder<TRequest, string?> rule) =>
        rule.Must(WithinLength(TaskDescription.MaxLength))
            .WithMessage($"A task description cannot exceed {TaskDescription.MaxLength} characters once trimmed.");

    // Trimmed, because that is the length the value objects measure - an untrimmed check would
    // reject a title the domain would have accepted.
    private static Func<string?, bool> WithinLength(int maximum) =>
        value => value is null || value.Trim().Length <= maximum;
}
