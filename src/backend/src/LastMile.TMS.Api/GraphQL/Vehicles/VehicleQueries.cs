using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Data;
using LastMile.TMS.Application.Vehicles.Reads;
using LastMile.TMS.Domain.Entities;

namespace LastMile.TMS.Api.GraphQL.Vehicles;

[ExtendObjectType(OperationTypeNames.Query)]
public sealed class VehicleQueries
{
    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher" })]
    [UseProjection]
    [UseFiltering(typeof(VehicleFilterInputType))]
    [UseSorting(typeof(VehicleSortInputType))]
    public IQueryable<Vehicle> GetVehicles(
        [Service] IVehicleReadService readService = null!) =>
        readService.GetVehicles();
}
