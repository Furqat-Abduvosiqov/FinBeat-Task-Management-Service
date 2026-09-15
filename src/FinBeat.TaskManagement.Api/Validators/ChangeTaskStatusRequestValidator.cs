using FinBeat.TaskManagement.Domain.Tasks;
using FluentValidation;

namespace FinBeat.TaskManagement.Api.Endpoints.Validation;

/// <summary>Checks a status-change request before it reaches the domain.</summary>
internal sealed class ChangeTaskStatusRequestValidator : AbstractValidator<ChangeTaskStatusRequest>
{
    /// <summary>Declares the rules.</summary>
    public ChangeTaskStatusRequestValidator() =>
        RuleFor(request => request.Status)
            .IsInEnum()
            .WithMessage($"A task status must be one of: {string.Join(", ", Enum.GetNames<TaskItemStatus>())}.");
}