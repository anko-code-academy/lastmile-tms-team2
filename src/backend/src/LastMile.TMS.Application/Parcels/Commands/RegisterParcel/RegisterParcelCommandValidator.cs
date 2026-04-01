using FluentValidation;

namespace LastMile.TMS.Application.Parcels.Commands;

public sealed class RegisterParcelCommandValidator : AbstractValidator<RegisterParcelCommand>
{
    public RegisterParcelCommandValidator()
    {
        RuleFor(x => x.Dto.ShipperAddressId)
            .NotEmpty().WithMessage("ShipperAddressId is required.");

        RuleFor(x => x.Dto.RecipientAddress.Street1)
            .NotEmpty().WithMessage("Recipient street address is required.");

        RuleFor(x => x.Dto.RecipientAddress.City)
            .NotEmpty().WithMessage("Recipient city is required.");

        RuleFor(x => x.Dto.RecipientAddress.State)
            .NotEmpty().WithMessage("Recipient state is required.");

        RuleFor(x => x.Dto.RecipientAddress.PostalCode)
            .NotEmpty().WithMessage("Recipient postal code is required.");

        RuleFor(x => x.Dto.RecipientAddress.CountryCode)
            .NotEmpty().WithMessage("Recipient country code is required.")
            .Length(2, 3).WithMessage("Country code must be 2 or 3 characters (e.g. US, UK).");

        RuleFor(x => x.Dto.Weight)
            .GreaterThan(0).WithMessage("Weight must be greater than 0.");

        RuleFor(x => x.Dto.Length)
            .GreaterThan(0).WithMessage("Length must be greater than 0.");

        RuleFor(x => x.Dto.Width)
            .GreaterThan(0).WithMessage("Width must be greater than 0.");

        RuleFor(x => x.Dto.Height)
            .GreaterThan(0).WithMessage("Height must be greater than 0.");

        RuleFor(x => x.Dto.DeclaredValue)
            .GreaterThanOrEqualTo(0).WithMessage("Declared value cannot be negative.");

        RuleFor(x => x.Dto.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be a 3-character ISO code (e.g. USD).");

        RuleFor(x => x.Dto.EstimatedDeliveryDate)
            .NotEmpty().WithMessage("Estimated delivery date is required.")
            .Must(d => d > DateTime.UtcNow)
            .WithMessage("Estimated delivery date must be in the future.");

        RuleFor(x => x.Dto.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

        RuleFor(x => x.Dto.ParcelType)
            .MaximumLength(50).WithMessage("Parcel type must not exceed 50 characters.");
    }
}
