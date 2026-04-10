using Riok.Mapperly.Abstractions;

namespace LastMile.TMS.Api.GraphQL.Routes;

[Mapper]
public static partial class RouteInputMapper
{
    public static partial LastMile.TMS.Application.Routes.DTOs.RoutePlanPreviewInputDto ToDto(this RoutePlanPreviewInput input);
    public static partial LastMile.TMS.Application.Routes.DTOs.CreateRouteDto ToDto(this CreateRouteInput input);
    public static partial LastMile.TMS.Application.Routes.DTOs.UpdateRouteAssignmentDto ToDto(this UpdateRouteAssignmentInput input);
    public static partial LastMile.TMS.Application.Routes.DTOs.CancelRouteDto ToDto(this CancelRouteInput input);
    public static partial LastMile.TMS.Application.Routes.DTOs.CompleteRouteDto ToDto(this CompleteRouteInput input);
}
