using LastMile.TMS.Application.Routes.DTOs;
using LastMile.TMS.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace LastMile.TMS.Application.Routes.Mappings;

[Mapper]
public static partial class RouteMappings
{
    [MapperIgnoreTarget(nameof(Route.Vehicle))]
    [MapperIgnoreTarget(nameof(Route.Driver))]
    [MapperIgnoreTarget(nameof(Route.DispatchedAt))]
    [MapperIgnoreTarget(nameof(Route.EndDate))]
    [MapperIgnoreTarget(nameof(Route.EndMileage))]
    [MapperIgnoreTarget(nameof(Route.Zone))]
    [MapperIgnoreTarget(nameof(Route.Status))]
    [MapperIgnoreTarget(nameof(Route.Parcels))]
    [MapperIgnoreTarget(nameof(Route.Stops))]
    [MapperIgnoreTarget(nameof(Route.AssignmentAuditTrail))]
    [MapperIgnoreTarget(nameof(Route.ParcelAdjustmentAuditTrail))]
    [MapperIgnoreTarget(nameof(Route.CancellationReason))]
    [MapperIgnoreTarget(nameof(Route.PlannedDistanceMeters))]
    [MapperIgnoreTarget(nameof(Route.PlannedDurationSeconds))]
    [MapperIgnoreTarget(nameof(Route.PlannedPath))]
    [MapperIgnoreTarget(nameof(Route.CreatedAt))]
    [MapperIgnoreTarget(nameof(Route.CreatedBy))]
    [MapperIgnoreTarget(nameof(Route.LastModifiedAt))]
    [MapperIgnoreTarget(nameof(Route.LastModifiedBy))]
    [MapperIgnoreTarget(nameof(Route.Id))]
    [MapperIgnoreSource(nameof(CreateRouteDto.AssignmentMode))]
    [MapperIgnoreSource(nameof(CreateRouteDto.ParcelIds))]
    [MapperIgnoreSource(nameof(CreateRouteDto.StopMode))]
    [MapperIgnoreSource(nameof(CreateRouteDto.Stops))]
    public static partial Route ToEntity(this CreateRouteDto dto);
}
