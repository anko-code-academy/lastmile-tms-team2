using LastMile.TMS.Application.Routes.DTOs;
using LastMile.TMS.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace LastMile.TMS.Application.Routes.Mappings;

[Mapper]
public static partial class RouteMappings
{
    [MapperIgnoreTarget(nameof(Route.Vehicle))]
    [MapperIgnoreTarget(nameof(Route.Driver))]
    [MapperIgnoreTarget(nameof(Route.EndDate))]
    [MapperIgnoreTarget(nameof(Route.EndMileage))]
    [MapperIgnoreTarget(nameof(Route.Status))]
    [MapperIgnoreTarget(nameof(Route.Parcels))]
    [MapperIgnoreTarget(nameof(Route.AssignmentAuditTrail))]
    [MapperIgnoreTarget(nameof(Route.CreatedAt))]
    [MapperIgnoreTarget(nameof(Route.CreatedBy))]
    [MapperIgnoreTarget(nameof(Route.LastModifiedAt))]
    [MapperIgnoreTarget(nameof(Route.LastModifiedBy))]
    [MapperIgnoreTarget(nameof(Route.Id))]
    [MapperIgnoreSource(nameof(CreateRouteDto.ParcelIds))]
    public static partial Route ToEntity(this CreateRouteDto dto);
}
