using FluentAssertions;
using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Parcels.Services;
using LastMile.TMS.Application.Routes.Commands;
using LastMile.TMS.Application.Routes.DTOs;
using LastMile.TMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace LastMile.TMS.Application.Tests.Routes;

public class CompleteRouteCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenVehicleHasNoOtherActiveRoutes_ReturnsVehicleToRouteDepot()
    {
        await using var db = RouteAssignmentTestData.CreateDbContext();
        var data = await RouteAssignmentTestData.SeedAsync(db);
        db.ChangeTracker.Clear();

        var vehicle = await db.Vehicles.SingleAsync(candidate => candidate.Id == data.Vehicle1.Id);
        var driver = await db.Drivers.SingleAsync(candidate => candidate.Id == data.Driver1.Id);

        vehicle.DepotId = data.Depot2.Id;
        vehicle.Status = VehicleStatus.InUse;

        var route = RouteAssignmentTestData.CreateRoute(
            vehicle,
            driver,
            data.ServiceDate,
            RouteStatus.InProgress);

        db.Routes.Add(route);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserName.Returns("dispatcher@test");
        var parcelUpdateNotifier = Substitute.For<IParcelUpdateNotifier>();
        var handler = new CompleteRouteCommandHandler(db, currentUser, parcelUpdateNotifier);

        var result = await handler.Handle(
            new CompleteRouteCommand(
                route.Id,
                new CompleteRouteDto
                {
                    EndMileage = 175,
                }),
            CancellationToken.None);

        result.Should().NotBeNull();

        var persistedRoute = await db.Routes.SingleAsync(candidate => candidate.Id == route.Id);
        var persistedVehicle = await db.Vehicles.SingleAsync(candidate => candidate.Id == data.Vehicle1.Id);

        persistedRoute.Status.Should().Be(RouteStatus.Completed);
        persistedRoute.EndMileage.Should().Be(175);
        persistedRoute.LastModifiedBy.Should().Be("dispatcher@test");
        persistedVehicle.Status.Should().Be(VehicleStatus.Available);
        persistedVehicle.DepotId.Should().Be(data.Depot1.Id);
    }

    [Fact]
    public async Task Handle_WhenAssignedParcelsAreOutForDelivery_MarksThemDelivered()
    {
        await using var db = RouteAssignmentTestData.CreateDbContext();
        var data = await RouteAssignmentTestData.SeedAsync(db);
        db.ChangeTracker.Clear();

        var vehicle = await db.Vehicles.SingleAsync(candidate => candidate.Id == data.Vehicle1.Id);
        var driver = await db.Drivers.SingleAsync(candidate => candidate.Id == data.Driver1.Id);
        var parcel = await db.Parcels.SingleAsync(candidate => candidate.Id == data.Parcel1.Id);

        parcel.Status = ParcelStatus.OutForDelivery;
        vehicle.Status = VehicleStatus.InUse;

        var route = RouteAssignmentTestData.CreateRoute(
            vehicle,
            driver,
            data.ServiceDate,
            RouteStatus.InProgress,
            parcel);

        db.Routes.Add(route);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserName.Returns("dispatcher@test");
        var parcelUpdateNotifier = Substitute.For<IParcelUpdateNotifier>();
        var handler = new CompleteRouteCommandHandler(db, currentUser, parcelUpdateNotifier);

        await handler.Handle(
            new CompleteRouteCommand(
                route.Id,
                new CompleteRouteDto
                {
                    EndMileage = 175,
                }),
            CancellationToken.None);

        var persistedParcel = await db.Parcels
            .Include(candidate => candidate.ChangeHistory)
            .Include(candidate => candidate.TrackingEvents)
            .SingleAsync(candidate => candidate.Id == data.Parcel1.Id);

        persistedParcel.Status.Should().Be(ParcelStatus.Delivered);
        persistedParcel.ZoneId.Should().Be(data.Zone1.Id);
        persistedParcel.ActualDeliveryDate.Should().NotBeNull();
        persistedParcel.DeliveryAttempts.Should().Be(1);
        persistedParcel.ChangeHistory.Should().ContainSingle(entry =>
            entry.FieldName == "Status"
            && entry.BeforeValue == "Out For Delivery"
            && entry.AfterValue == "Delivered");
        persistedParcel.TrackingEvents.Should().Contain(entry => entry.EventType == EventType.Delivered);

        await parcelUpdateNotifier.Received(1).NotifyParcelUpdatedAsync(
            Arg.Is<ParcelUpdateNotification>(notification =>
                notification.TrackingNumber == persistedParcel.TrackingNumber
                && notification.Status == ParcelStatus.Delivered.ToString()),
            Arg.Any<CancellationToken>());
    }
}
