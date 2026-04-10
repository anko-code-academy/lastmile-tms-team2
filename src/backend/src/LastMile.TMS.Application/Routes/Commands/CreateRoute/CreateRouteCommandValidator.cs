using FluentValidation;
using LastMile.TMS.Domain.Enums;

namespace LastMile.TMS.Application.Routes.Commands;

public sealed class CreateRouteCommandValidator : AbstractValidator<CreateRouteCommand>
{
    public CreateRouteCommandValidator()
    {
        RuleFor(x => x.Dto.VehicleId)
            .NotEmpty();

        RuleFor(x => x.Dto.DriverId)
            .NotEmpty();

        RuleFor(x => x.Dto.ZoneId)
            .NotEmpty();

        RuleFor(x => x.Dto.StagingArea)
            .IsInEnum();

        RuleFor(x => x.Dto.StartMileage)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Dto.AssignmentMode)
            .IsInEnum();

        RuleFor(x => x.Dto.StopMode)
            .IsInEnum();

        RuleFor(x => x.Dto.ParcelIds)
            .NotNull();

        RuleFor(x => x.Dto.Stops)
            .NotNull();

        When(
            x => x.Dto.AssignmentMode == RouteAssignmentMode.ManualParcels,
            () =>
            {
                RuleFor(x => x.Dto.ParcelIds)
                    .NotEmpty();
            });

        When(
            x => x.Dto.StopMode == RouteStopMode.Manual,
            () =>
            {
                RuleFor(x => x.Dto.Stops)
                    .NotEmpty();
            });
    }
}
