using FluentValidation;

namespace LastMile.TMS.Application.Parcels.Commands;

public sealed class ConfirmParcelSortCommandValidator : AbstractValidator<ConfirmParcelSortCommand>
{
    public ConfirmParcelSortCommandValidator()
    {
        RuleFor(x => x.ParcelId).NotEmpty();
        RuleFor(x => x.BinLocationId).NotEmpty();
    }
}
