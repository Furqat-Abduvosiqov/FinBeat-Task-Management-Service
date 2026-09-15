using FluentValidation;

namespace FinBeat.TaskManagement.Api.Endpoints.Validation;

/// <summary>Checks a create request before it reaches the domain.</summary>
internal sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    /// <summary>Declares the rules.</summary>
    public CreateTaskRequestValidator()
    {
        RuleFor(request => request.Title).MustBeATaskTitle();
        RuleFor(request => request.Description).MustBeATaskDescription();
    }
}