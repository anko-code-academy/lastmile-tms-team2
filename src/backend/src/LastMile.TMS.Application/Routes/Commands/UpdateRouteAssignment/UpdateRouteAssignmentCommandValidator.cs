using FluentValidation;

namespace LastMile.TMS.Application.Routes.Commands;

public sealed class UpdateRouteAssignmentCommandValidator
    : AbstractValidator<UpdateRouteAssignmentCommand>
{
    public UpdateRouteAssignmentCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Dto.VehicleId)
            .NotEmpty();

        RuleFor(x => x.Dto.DriverId)
            .NotEmpty();
    }
}
