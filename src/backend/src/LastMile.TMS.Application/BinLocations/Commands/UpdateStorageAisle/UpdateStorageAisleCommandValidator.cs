using FluentValidation;

namespace LastMile.TMS.Application.BinLocations.Commands;

public sealed class UpdateStorageAisleCommandValidator : AbstractValidator<UpdateStorageAisleCommand>
{
    public UpdateStorageAisleCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Dto)
            .NotNull()
            .WithMessage("Storage aisle update payload is required.");

        When(x => x.Dto is not null, () =>
        {
            RuleFor(x => x.Dto.Name)
                .NotEmpty().WithMessage("Storage aisle name is required.")
                .MaximumLength(200).WithMessage("Storage aisle name must not exceed 200 characters.");
        });
    }
}
