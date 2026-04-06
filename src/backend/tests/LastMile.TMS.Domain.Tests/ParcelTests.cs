using FluentAssertions;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;

namespace LastMile.TMS.Domain.Tests;

public class ParcelTests
{
    [Fact]
    public void GenerateTrackingNumber_ShouldReturnUniqueTrackingNumber()
    {
        var trackingNumber1 = Parcel.GenerateTrackingNumber();
        var trackingNumber2 = Parcel.GenerateTrackingNumber();

        trackingNumber1.Should().NotBeEmpty();
        trackingNumber2.Should().NotBeEmpty();
        trackingNumber1.Should().NotBe(trackingNumber2);
    }

    [Fact]
    public void GenerateTrackingNumber_ShouldStartWithLM()
    {
        var trackingNumber = Parcel.GenerateTrackingNumber();

        trackingNumber.Should().StartWith("LM");
    }

    [Fact]
    public void GenerateTrackingNumber_ShouldHaveCorrectLength()
    {
        var trackingNumber = Parcel.GenerateTrackingNumber();

        trackingNumber.Length.Should().Be(18);
    }

    [Theory]
    [InlineData(ParcelStatus.Registered, ParcelStatus.ReceivedAtDepot, true)]
    [InlineData(ParcelStatus.Registered, ParcelStatus.Cancelled, true)]
    [InlineData(ParcelStatus.Registered, ParcelStatus.Delivered, false)]
    [InlineData(ParcelStatus.Registered, ParcelStatus.OutForDelivery, false)]
    [InlineData(ParcelStatus.ReceivedAtDepot, ParcelStatus.Sorted, true)]
    [InlineData(ParcelStatus.ReceivedAtDepot, ParcelStatus.Exception, true)]
    [InlineData(ParcelStatus.ReceivedAtDepot, ParcelStatus.Delivered, false)]
    [InlineData(ParcelStatus.Delivered, ParcelStatus.ReturnedToDepot, true)]
    [InlineData(ParcelStatus.Delivered, ParcelStatus.Sorted, false)]
    [InlineData(ParcelStatus.Delivered, ParcelStatus.OutForDelivery, false)]
    [InlineData(ParcelStatus.Cancelled, ParcelStatus.Registered, false)]
    [InlineData(ParcelStatus.Cancelled, ParcelStatus.Delivered, false)]
    [InlineData(ParcelStatus.Exception, ParcelStatus.Sorted, true)]
    [InlineData(ParcelStatus.Exception, ParcelStatus.OutForDelivery, true)]
    [InlineData(ParcelStatus.Exception, ParcelStatus.ReturnedToDepot, true)]
    [InlineData(ParcelStatus.Exception, ParcelStatus.Cancelled, true)]
    [InlineData(ParcelStatus.FailedAttempt, ParcelStatus.OutForDelivery, true)]
    [InlineData(ParcelStatus.FailedAttempt, ParcelStatus.ReturnedToDepot, true)]
    [InlineData(ParcelStatus.FailedAttempt, ParcelStatus.Delivered, false)]
    public void CanTransitionTo_ShouldReturnCorrectValue(
        ParcelStatus currentStatus, ParcelStatus newStatus, bool expected)
    {
        var parcel = new Parcel { Status = currentStatus };

        var result = parcel.CanTransitionTo(newStatus);

        result.Should().Be(expected);
    }

    [Fact]
    public void TransitionTo_ShouldChangeStatus_WhenValidTransition()
    {
        var parcel = new Parcel { Status = ParcelStatus.Registered };

        parcel.TransitionTo(ParcelStatus.ReceivedAtDepot);

        parcel.Status.Should().Be(ParcelStatus.ReceivedAtDepot);
    }

    [Fact]
    public void TransitionTo_ShouldThrowException_WhenInvalidTransition()
    {
        var parcel = new Parcel { Status = ParcelStatus.Registered };

        var act = () => parcel.TransitionTo(ParcelStatus.Delivered);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot transition*");
    }

    [Fact]
    public void TransitionTo_ShouldAllowMultipleValidTransitions()
    {
        var parcel = new Parcel { Status = ParcelStatus.Registered };

        parcel.TransitionTo(ParcelStatus.ReceivedAtDepot);
        parcel.Status.Should().Be(ParcelStatus.ReceivedAtDepot);

        parcel.TransitionTo(ParcelStatus.Sorted);
        parcel.Status.Should().Be(ParcelStatus.Sorted);

        parcel.TransitionTo(ParcelStatus.Staged);
        parcel.Status.Should().Be(ParcelStatus.Staged);

        parcel.TransitionTo(ParcelStatus.Loaded);
        parcel.Status.Should().Be(ParcelStatus.Loaded);

        parcel.TransitionTo(ParcelStatus.OutForDelivery);
        parcel.Status.Should().Be(ParcelStatus.OutForDelivery);

        parcel.TransitionTo(ParcelStatus.Delivered);
        parcel.Status.Should().Be(ParcelStatus.Delivered);
    }

    [Fact]
    public void FullDeliveryCycle_ShouldFollowValidTransitions()
    {
        var parcel = new Parcel
        {
            TrackingNumber = Parcel.GenerateTrackingNumber(),
            Status = ParcelStatus.Registered,
            Weight = 1.5m,
            WeightUnit = WeightUnit.Lb,
            DimensionUnit = DimensionUnit.In,
            ServiceType = ServiceType.Standard
        };

        parcel.TransitionTo(ParcelStatus.ReceivedAtDepot);
        parcel.TransitionTo(ParcelStatus.Sorted);
        parcel.TransitionTo(ParcelStatus.Staged);
        parcel.TransitionTo(ParcelStatus.Loaded);
        parcel.TransitionTo(ParcelStatus.OutForDelivery);
        parcel.TransitionTo(ParcelStatus.Delivered);

        parcel.Status.Should().Be(ParcelStatus.Delivered);
    }

    [Fact]
    public void GetValidNextStatuses_ShouldMatchTransitionRules()
    {
        var registered = new Parcel { Status = ParcelStatus.Registered };
        registered.GetValidNextStatuses().Should().BeEquivalentTo(
            [ParcelStatus.ReceivedAtDepot, ParcelStatus.Cancelled],
            options => options.WithStrictOrdering());

        var delivered = new Parcel { Status = ParcelStatus.Delivered };
        delivered.GetValidNextStatuses().Should().Equal(ParcelStatus.ReturnedToDepot);
    }
}