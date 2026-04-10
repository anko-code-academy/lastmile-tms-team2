using FluentValidation;

namespace LastMile.TMS.Application.Parcels.Commands;

public sealed class LoadParcelForRouteCommandValidator : AbstractValidator<LoadParcelForRouteCommand>
{
    public LoadParcelForRouteCommandValidator()
    {
        RuleFor(x => x.RouteId)
            .NotEmpty();

        RuleFor(x => x.Barcode)
            .NotEmpty()
            .MaximumLength(100);
    }
}
