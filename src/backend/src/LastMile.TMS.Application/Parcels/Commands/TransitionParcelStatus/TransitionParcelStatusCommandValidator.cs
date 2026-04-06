using FluentValidation;

namespace LastMile.TMS.Application.Parcels.Commands;

public sealed class TransitionParcelStatusCommandValidator : AbstractValidator<TransitionParcelStatusCommand>
{
    public TransitionParcelStatusCommandValidator()
    {
        RuleFor(x => x.ParcelId)
            .NotEmpty().WithMessage("ParcelId is required.");

        RuleFor(x => x.NewStatus)
            .IsInEnum().WithMessage("Invalid parcel status.");

        RuleFor(x => x.Location)
            .MaximumLength(200).WithMessage("Location must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");
    }
}
