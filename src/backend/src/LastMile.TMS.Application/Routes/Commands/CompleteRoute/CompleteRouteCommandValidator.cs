using FluentValidation;

namespace LastMile.TMS.Application.Routes.Commands;

public sealed class CompleteRouteCommandValidator : AbstractValidator<CompleteRouteCommand>
{
    public CompleteRouteCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Dto.EndMileage)
            .GreaterThanOrEqualTo(0);
    }
}
