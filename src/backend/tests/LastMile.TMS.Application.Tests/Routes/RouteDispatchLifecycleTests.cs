using FluentAssertions;
using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.Services;
using LastMile.TMS.Application.Routes.Commands;
using LastMile.TMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace LastMile.TMS.Application.Tests.Routes;

public class DispatchRouteCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenRouteIsReady_DispatchesRouteAndTransitionsLoadedParcels()
    {
        await using var db = RouteAssignmentTestData.CreateDbContext();
        var data = await RouteAssignmentTestData.SeedAsync(db);
        db.ChangeTracker.Clear();

        var vehicle = await db.Vehicles.SingleAsync(candidate => candidate.Id == data.Vehicle1.Id);
        var driver = await db.Drivers.SingleAsync(candidate => candidate.Id == data.Driver1.Id);
        var parcel1 = await db.Parcels
            .Include(candidate => candidate.TrackingEvents)
            .Include(candidate => candidate.ChangeHistory)
            .SingleAsync(candidate => candidate.Id == data.Parcel1.Id);
        var parcel2 = await db.Parcels
            .Include(candidate => candidate.TrackingEvents)
            .Include(candidate => candidate.ChangeHistory)
            .SingleAsync(candidate => candidate.Id == data.Parcel2.Id);
        parcel1.Status = ParcelStatus.Loaded;
        parcel2.Status = ParcelStatus.Loaded;

        var route = RouteAssignmentTestData.CreateRoute(
            vehicle,
            driver,
            data.ServiceDate,
            RouteStatus.Draft,
            parcel1,
            parcel2);
        db.Routes.Add(route);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserName.Returns("dispatcher@test");
        var parcelUpdateNotifier = Substitute.For<IParcelUpdateNotifier>();

        var handler = new DispatchRouteCommandHandler(db, currentUser, parcelUpdateNotifier);

        await handler.Handle(new DispatchRouteCommand(route.Id), CancellationToken.None);

        var persistedRoute = await db.Routes.SingleAsync(candidate => candidate.Id == route.Id);
        var persistedParcels = await db.Parcels
            .Where(candidate => candidate.Id == parcel1.Id || candidate.Id == parcel2.Id)
            .ToListAsync();

        persistedRoute.Status.Should().Be(RouteStatus.Dispatched);
        persistedRoute.DispatchedAt.Should().NotBeNull();
        persistedRoute.LastModifiedBy.Should().Be("dispatcher@test");
        persistedParcels.Should().OnlyContain(candidate => candidate.Status == ParcelStatus.OutForDelivery);
        await parcelUpdateNotifier.Received(2).NotifyParcelUpdatedAsync(
            Arg.Is<ParcelUpdateNotification>(notification =>
                notification.Status == ParcelStatus.OutForDelivery.ToString()),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRouteHasNoAssignedParcels_Throws()
    {
        await using var db = RouteAssignmentTestData.CreateDbContext();
        var data = await RouteAssignmentTestData.SeedAsync(db);
        db.ChangeTracker.Clear();

        var vehicle = await db.Vehicles.SingleAsync(candidate => candidate.Id == data.Vehicle1.Id);
        var driver = await db.Drivers.SingleAsync(candidate => candidate.Id == data.Driver1.Id);
        var route = RouteAssignmentTestData.CreateRoute(
            vehicle,
            driver,
            data.ServiceDate,
            RouteStatus.Draft);
        db.Routes.Add(route);
        await db.SaveChangesAsync();

        var handler = new DispatchRouteCommandHandler(
            db,
            Substitute.For<ICurrentUserService>(),
            Substitute.For<IParcelUpdateNotifier>());

        var act = () => handler.Handle(new DispatchRouteCommand(route.Id), CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*At least one parcel*");
    }

    [Fact]
    public async Task Handle_WhenAnyParcelIsNotLoaded_Throws()
    {
        await using var db = RouteAssignmentTestData.CreateDbContext();
        var data = await RouteAssignmentTestData.SeedAsync(db);
        db.ChangeTracker.Clear();

        var vehicle = await db.Vehicles.SingleAsync(candidate => candidate.Id == data.Vehicle1.Id);
        var driver = await db.Drivers.SingleAsync(candidate => candidate.Id == data.Driver1.Id);
        var parcel = await db.Parcels.SingleAsync(candidate => candidate.Id == data.Parcel1.Id);
        parcel.Status = ParcelStatus.Staged;

        var route = RouteAssignmentTestData.CreateRoute(
            vehicle,
            driver,
            data.ServiceDate,
            RouteStatus.Draft,
            parcel);
        db.Routes.Add(route);
        await db.SaveChangesAsync();

        var handler = new DispatchRouteCommandHandler(
            db,
            Substitute.For<ICurrentUserService>(),
            Substitute.For<IParcelUpdateNotifier>());

        var act = () => handler.Handle(new DispatchRouteCommand(route.Id), CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*must be loaded*");
    }
}

public class StartRouteCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenRouteIsDispatched_TransitionsOnlyRouteStatus()
    {
        await using var db = RouteAssignmentTestData.CreateDbContext();
        var data = await RouteAssignmentTestData.SeedAsync(db);
        db.ChangeTracker.Clear();

        var vehicle = await db.Vehicles.SingleAsync(candidate => candidate.Id == data.Vehicle1.Id);
        var driver = await db.Drivers.SingleAsync(candidate => candidate.Id == data.Driver1.Id);
        var parcel = await db.Parcels
            .Include(candidate => candidate.ChangeHistory)
            .SingleAsync(candidate => candidate.Id == data.Parcel1.Id);
        parcel.Status = ParcelStatus.OutForDelivery;

        var route = RouteAssignmentTestData.CreateRoute(
            vehicle,
            driver,
            data.ServiceDate,
            RouteStatus.Dispatched,
            parcel);
        db.Routes.Add(route);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserName.Returns("dispatcher@test");
        var handler = new StartRouteCommandHandler(db, currentUser);

        await handler.Handle(new StartRouteCommand(route.Id), CancellationToken.None);

        var persistedRoute = await db.Routes.SingleAsync(candidate => candidate.Id == route.Id);
        var persistedParcel = await db.Parcels
            .Include(candidate => candidate.ChangeHistory)
            .SingleAsync(candidate => candidate.Id == parcel.Id);

        persistedRoute.Status.Should().Be(RouteStatus.InProgress);
        persistedRoute.LastModifiedBy.Should().Be("dispatcher@test");
        persistedParcel.Status.Should().Be(ParcelStatus.OutForDelivery);
        persistedParcel.ChangeHistory.Should().BeEmpty();
    }
}
