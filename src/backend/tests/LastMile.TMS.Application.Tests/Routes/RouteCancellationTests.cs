using FluentAssertions;
using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.Services;
using LastMile.TMS.Application.Routes.Commands;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace LastMile.TMS.Application.Tests.Routes;

public class CancelRouteCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenRouteIsPlanned_CancelsRouteUpdatesAuditFieldsAndReleasesVehicle()
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
        vehicle.Status = VehicleStatus.InUse;
        db.Routes.Add(route);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserName.Returns("dispatcher@test");
        var parcelUpdateNotifier = Substitute.For<IParcelUpdateNotifier>();

        var handler = new CancelRouteCommandHandler(db, currentUser, parcelUpdateNotifier);

        var result = await handler.Handle(
            new CancelRouteCommand(
                route.Id,
                new()
                {
                    Reason = "Driver called in sick",
                }),
            CancellationToken.None);

        result.Should().NotBeNull();

        var persistedRoute = await db.Routes.SingleAsync(candidate => candidate.Id == route.Id);
        var persistedVehicle = await db.Vehicles.SingleAsync(candidate => candidate.Id == data.Vehicle1.Id);

        persistedRoute.Status.Should().Be(RouteStatus.Cancelled);
        persistedRoute.CancellationReason.Should().Be("Driver called in sick");
        persistedRoute.LastModifiedAt.Should().NotBeNull();
        persistedRoute.LastModifiedBy.Should().Be("dispatcher@test");
        persistedVehicle.Status.Should().Be(VehicleStatus.Available);
        await parcelUpdateNotifier.DidNotReceiveWithAnyArgs()
            .NotifyParcelUpdatedAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenRouteHasStagedParcels_ReturnsThemToSortedAndNotifies()
    {
        await using var db = RouteAssignmentTestData.CreateDbContext();
        var data = await RouteAssignmentTestData.SeedAsync(db);
        db.ChangeTracker.Clear();

        var vehicle = await db.Vehicles.SingleAsync(candidate => candidate.Id == data.Vehicle1.Id);
        var driver = await db.Drivers.SingleAsync(candidate => candidate.Id == data.Driver1.Id);
        var parcel = await db.Parcels
            .Include(candidate => candidate.TrackingEvents)
            .Include(candidate => candidate.ChangeHistory)
            .SingleAsync(candidate => candidate.Id == data.Parcel1.Id);
        parcel.Status = ParcelStatus.Staged;

        var route = RouteAssignmentTestData.CreateRoute(
            vehicle,
            driver,
            data.ServiceDate,
            RouteStatus.Draft,
            parcel);
        vehicle.Status = VehicleStatus.InUse;
        db.Routes.Add(route);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserName.Returns("dispatcher@test");
        var parcelUpdateNotifier = Substitute.For<IParcelUpdateNotifier>();

        var handler = new CancelRouteCommandHandler(db, currentUser, parcelUpdateNotifier);

        await handler.Handle(
            new CancelRouteCommand(
                route.Id,
                new()
                {
                    Reason = "Weather closure",
                }),
            CancellationToken.None);

        var persistedParcel = await db.Parcels
            .Include(candidate => candidate.ChangeHistory)
            .Include(candidate => candidate.TrackingEvents)
            .SingleAsync(candidate => candidate.Id == parcel.Id);

        persistedParcel.Status.Should().Be(ParcelStatus.Sorted);
        persistedParcel.ChangeHistory.Should().ContainSingle(entry =>
            entry.Action == ParcelChangeAction.Updated
            && entry.FieldName == "Status"
            && entry.BeforeValue == "Staged"
            && entry.AfterValue == "Sorted");
        persistedParcel.TrackingEvents.Should().Contain(entry =>
            entry.Description.Contains("route")
            && entry.Description.Contains("Weather closure"));
        await parcelUpdateNotifier.Received(1).NotifyParcelUpdatedAsync(
            Arg.Is<ParcelUpdateNotification>(notification =>
                notification.TrackingNumber == persistedParcel.TrackingNumber
                && notification.Status == ParcelStatus.Sorted.ToString()),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenVehicleHasOtherActiveRoutes_KeepsVehicleInUse()
    {
        await using var db = RouteAssignmentTestData.CreateDbContext();
        var data = await RouteAssignmentTestData.SeedAsync(db);
        db.ChangeTracker.Clear();

        var vehicle = await db.Vehicles.SingleAsync(candidate => candidate.Id == data.Vehicle1.Id);
        var driver1 = await db.Drivers.SingleAsync(candidate => candidate.Id == data.Driver1.Id);
        var driver2 = await db.Drivers.SingleAsync(candidate => candidate.Id == data.Driver2.Id);
        var routeToCancel = RouteAssignmentTestData.CreateRoute(
            vehicle,
            driver1,
            data.ServiceDate,
            RouteStatus.Draft);
        var siblingActiveRoute = RouteAssignmentTestData.CreateRoute(
            vehicle,
            driver2,
            data.ServiceDate.AddDays(1),
            RouteStatus.Draft);
        vehicle.Status = VehicleStatus.InUse;
        db.Routes.AddRange(routeToCancel, siblingActiveRoute);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var handler = new CancelRouteCommandHandler(
            db,
            Substitute.For<ICurrentUserService>(),
            Substitute.For<IParcelUpdateNotifier>());

        await handler.Handle(
            new CancelRouteCommand(
                routeToCancel.Id,
                new()
                {
                    Reason = "Capacity merged into another route",
                }),
            CancellationToken.None);

        var persistedVehicle = await db.Vehicles.SingleAsync(candidate => candidate.Id == data.Vehicle1.Id);
        persistedVehicle.Status.Should().Be(VehicleStatus.InUse);
    }

    [Fact]
    public async Task Handle_WhenRouteIsNotCancellable_Throws()
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
            RouteStatus.InProgress);
        vehicle.Status = VehicleStatus.InUse;
        db.Routes.Add(route);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var handler = new CancelRouteCommandHandler(
            db,
            Substitute.For<ICurrentUserService>(),
            Substitute.For<IParcelUpdateNotifier>());

        var act = () => handler.Handle(
            new CancelRouteCommand(
                route.Id,
                new()
                {
                    Reason = "Too late to dispatch",
                }),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*Only draft or dispatched routes can be cancelled before route start*");
    }

    [Fact]
    public async Task Handle_WhenReasonIsEmpty_Throws()
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
        db.ChangeTracker.Clear();

        var handler = new CancelRouteCommandHandler(
            db,
            Substitute.For<ICurrentUserService>(),
            Substitute.For<IParcelUpdateNotifier>());

        var act = () => handler.Handle(
            new CancelRouteCommand(
                route.Id,
                new()
                {
                    Reason = "   ",
                }),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cancellation reason is required*");
    }
}
