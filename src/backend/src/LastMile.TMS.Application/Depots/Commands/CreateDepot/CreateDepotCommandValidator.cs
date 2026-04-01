using FluentValidation;

namespace LastMile.TMS.Application.Depots.Commands;

public sealed class CreateDepotCommandValidator : AbstractValidator<CreateDepotCommand>
{
    public CreateDepotCommandValidator()
    {
        RuleFor(x => x.Dto.Name)
            .NotEmpty().WithMessage("Depot name is required.")
            .MaximumLength(200).WithMessage("Depot name must not exceed 200 characters.");

        RuleFor(x => x.Dto.Address)
            .NotNull().WithMessage("Address is required.");

        RuleFor(x => x.Dto.Address.Street1)
            .NotEmpty().WithMessage("Street address is required.")
            .MaximumLength(200);

        RuleFor(x => x.Dto.Address.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(100);

        RuleFor(x => x.Dto.Address.State)
            .NotEmpty().WithMessage("State is required.")
            .MaximumLength(100);

        RuleFor(x => x.Dto.Address.PostalCode)
            .NotEmpty().WithMessage("Postal code is required.")
            .MaximumLength(20);

        RuleFor(x => x.Dto.Address.CountryCode)
            .NotEmpty().WithMessage("Country code is required.")
            .Length(2, 3).WithMessage("Country code must be 2 or 3 characters.");

        this.AddOperatingHoursRules(x => x.Dto.OperatingHours!);
    }
}
