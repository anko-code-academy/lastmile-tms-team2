using LastMile.TMS.Application.Common;
using LastMile.TMS.Application.Depots.DTOs;
using LastMile.TMS.Application.Depots.Queries;
using LastMile.TMS.Application.Drivers.DTOs;
using LastMile.TMS.Application.Drivers.Queries;
using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Application.Parcels.Queries;
using LastMile.TMS.Application.Routes.DTOs;
using LastMile.TMS.Application.Routes.Queries;
using LastMile.TMS.Application.Vehicles.DTOs;
using LastMile.TMS.Application.Vehicles.Queries;
using LastMile.TMS.Domain.Enums;
using HotChocolate.Authorization;
using MediatR;

namespace LastMile.TMS.Api.GraphQL;

public class Query
{
    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher" })]
    public async Task<IReadOnlyList<DepotDto>> GetDepots(
        [Service] ISender mediator = null!,
        CancellationToken cancellationToken = default)
    {
        return await mediator.Send(new GetAllDepotsQuery(), cancellationToken);
    }

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher" })]
    public async Task<PaginatedResult<VehicleDto>> GetVehicles(
        int page = 1,
        int pageSize = 20,
        VehicleStatus? status = null,
        Guid? depotId = null,
        [Service] ISender mediator = null!,
        CancellationToken cancellationToken = default)
    {
        var query = new GetVehiclesQuery(page, pageSize, status, depotId);
        return await mediator.Send(query, cancellationToken);
    }

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher" })]
    public async Task<VehicleDto?> GetVehicle(
        Guid id,
        [Service] ISender mediator = null!,
        CancellationToken cancellationToken = default)
    {
        var query = new GetVehicleByIdQuery(id);
        return await mediator.Send(query, cancellationToken);
    }

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher" })]
    public async Task<PaginatedResult<RouteDto>> GetRoutes(
        Guid? vehicleId = null,
        RouteStatus? status = null,
        int page = 1,
        int pageSize = 20,
        [Service] ISender mediator = null!,
        CancellationToken cancellationToken = default)
    {
        var query = new GetRoutesQuery(vehicleId, status, page, pageSize);
        return await mediator.Send(query, cancellationToken);
    }

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher" })]
    public async Task<RouteDto?> GetRoute(
        Guid id,
        [Service] ISender mediator = null!,
        CancellationToken cancellationToken = default)
    {
        var query = new GetRouteByIdQuery(id);
        return await mediator.Send(query, cancellationToken);
    }

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher" })]
    public async Task<PaginatedResult<RouteDto>> GetVehicleHistory(
        Guid vehicleId,
        RouteStatus? status = null,
        int page = 1,
        int pageSize = 10,
        [Service] ISender mediator = null!,
        CancellationToken cancellationToken = default)
    {
        var query = new GetRoutesQuery(vehicleId, status, page, pageSize);
        return await mediator.Send(query, cancellationToken);
    }

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher" })]
    public async Task<IReadOnlyList<ParcelOptionDto>> GetParcelsForRouteCreation(
        [Service] ISender mediator = null!,
        CancellationToken cancellationToken = default)
    {
        return await mediator.Send(new GetParcelsForRouteCreationQuery(), cancellationToken);
    }

    [Authorize(Roles = new[] { "OperationsManager", "Admin", "Dispatcher" })]
    public async Task<IReadOnlyList<DriverListItemDto>> GetDrivers(
        Guid? depotId = null,
        [Service] ISender mediator = null!,
        CancellationToken cancellationToken = default)
    {
        return await mediator.Send(new GetDriversQuery(depotId), cancellationToken);
    }
}
