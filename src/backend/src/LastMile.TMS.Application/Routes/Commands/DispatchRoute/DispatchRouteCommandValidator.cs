using FluentValidation;

namespace LastMile.TMS.Application.Routes.Commands;

public sealed class DispatchRouteCommandValidator : AbstractValidator<DispatchRouteCommand>
{
    public DispatchRouteCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
