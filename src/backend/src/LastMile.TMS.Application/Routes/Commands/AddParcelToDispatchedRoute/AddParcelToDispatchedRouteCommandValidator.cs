using FluentValidation;

namespace LastMile.TMS.Application.Routes.Commands;

public sealed class AddParcelToDispatchedRouteCommandValidator
    : AbstractValidator<AddParcelToDispatchedRouteCommand>
{
    public AddParcelToDispatchedRouteCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Dto.ParcelId)
            .NotEmpty();

        RuleFor(x => x.Dto.Reason)
            .NotEmpty()
            .MaximumLength(1000);
    }
}
