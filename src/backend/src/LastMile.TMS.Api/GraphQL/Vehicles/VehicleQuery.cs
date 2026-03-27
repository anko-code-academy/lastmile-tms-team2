using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Data;
using LastMile.TMS.Application.Vehicles.DTOs;
using LastMile.TMS.Application.Vehicles.Reads;

namespace LastMile.TMS.Api.GraphQL.Vehicles;

[ExtendObjectType(OperationTypeNames.Query)]
public sealed class VehicleQuery
{
    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher" })]
    [UseProjection]
    [UseSorting]
    [UseFiltering]
    public IQueryable<VehicleDto> GetVehicles(
        [Service] IVehicleReadService readService = null!) =>
        readService.GetVehicles();
}
