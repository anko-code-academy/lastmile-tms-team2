using LastMile.TMS.Application.Routes.DTOs;
using LastMile.TMS.Application.Routes.Services;
using MediatR;

namespace LastMile.TMS.Application.Routes.Queries;

public sealed record GetRoutePlanPreviewQuery(RoutePlanPreviewInputDto Input) : IRequest<RoutePlanPreviewDto>;

public sealed class GetRoutePlanPreviewQueryHandler(
    IRoutePlanningService routePlanningService)
    : IRequestHandler<GetRoutePlanPreviewQuery, RoutePlanPreviewDto>
{
    public async Task<RoutePlanPreviewDto> Handle(
        GetRoutePlanPreviewQuery request,
        CancellationToken cancellationToken)
    {
        var result = await routePlanningService.BuildPlanAsync(
            new RoutePlanRequest
            {
                ZoneId = request.Input.ZoneId,
                VehicleId = request.Input.VehicleId,
                DriverId = request.Input.DriverId,
                StartDate = request.Input.StartDate,
                AssignmentMode = request.Input.AssignmentMode,
                StopMode = request.Input.StopMode,
                ParcelIds = request.Input.ParcelIds,
                Stops = request.Input.Stops,
            },
            cancellationToken);

        return result.ToPreviewDto();
    }
}
