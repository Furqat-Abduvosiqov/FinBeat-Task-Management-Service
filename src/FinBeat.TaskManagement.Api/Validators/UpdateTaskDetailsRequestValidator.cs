using FluentValidation;

namespace FinBeat.TaskManagement.Api.Endpoints.Validation;

/// <summary>Checks an update request before it reaches the domain.</summary>
internal sealed class UpdateTaskDetailsRequestValidator : AbstractValidator<UpdateTaskDetailsRequest>
{
    /// <summary>Declares the rules.</summary>
    public UpdateTaskDetailsRequestValidator()
    {
        RuleFor(request => request.Title).MustBeATaskTitle();
        RuleFor(request => request.Description).MustBeATaskDescription();
    }
}