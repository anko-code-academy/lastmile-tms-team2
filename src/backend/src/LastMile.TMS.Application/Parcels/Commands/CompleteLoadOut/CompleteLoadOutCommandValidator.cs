using FluentValidation;

namespace LastMile.TMS.Application.Parcels.Commands;

public sealed class CompleteLoadOutCommandValidator : AbstractValidator<CompleteLoadOutCommand>
{
    public CompleteLoadOutCommandValidator()
    {
        RuleFor(x => x.RouteId).NotEmpty();
    }
}
