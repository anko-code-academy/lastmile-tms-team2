using FluentValidation;

namespace LastMile.TMS.Application.Routes.Commands;

public sealed class StartRouteCommandValidator : AbstractValidator<StartRouteCommand>
{
    public StartRouteCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
