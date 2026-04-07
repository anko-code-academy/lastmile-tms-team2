using FluentValidation;

namespace LastMile.TMS.Application.BinLocations.Commands;

public sealed class UpdateStorageZoneCommandValidator : AbstractValidator<UpdateStorageZoneCommand>
{
    public UpdateStorageZoneCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Dto)
            .NotNull()
            .WithMessage("Storage zone update payload is required.");

        When(x => x.Dto is not null, () =>
        {
            RuleFor(x => x.Dto.Name)
                .NotEmpty().WithMessage("Storage zone name is required.")
                .MaximumLength(200).WithMessage("Storage zone name must not exceed 200 characters.");
        });
    }
}
