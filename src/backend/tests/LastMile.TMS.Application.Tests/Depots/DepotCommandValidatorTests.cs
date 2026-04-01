using FluentAssertions;
using LastMile.TMS.Application.Depots.Commands;
using LastMile.TMS.Application.Depots.DTOs;

namespace LastMile.TMS.Application.Tests.Depots;

public class DepotCommandValidatorTests
{
    [Fact]
    public void CreateDepotCommandValidator_ShouldRejectDuplicateOperatingHoursDays()
    {
        var validator = new CreateDepotCommandValidator();
        var command = new CreateDepotCommand(new CreateDepotDto
        {
            Name = "Test Depot",
            Address = ValidAddress(),
            OperatingHours =
            [
                ValidOperatingHours(DayOfWeek.Monday, 8, 0, 17, 0),
                ValidOperatingHours(DayOfWeek.Monday, 9, 0, 18, 0)
            ]
        });

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Dto.OperatingHours");
        result.Errors.Should().Contain(x => x.ErrorMessage.Contains("duplicate days"));
    }

    [Fact]
    public void UpdateDepotCommandValidator_ShouldRejectDuplicateOperatingHoursDays()
    {
        var validator = new UpdateDepotCommandValidator();
        var command = ValidUpdateCommand() with
        {
            Dto = new UpdateDepotDto
            {
                Name = "Updated Depot",
                IsActive = true,
                OperatingHours =
                [
                    ValidOperatingHours(DayOfWeek.Tuesday, 8, 0, 17, 0),
                    ValidOperatingHours(DayOfWeek.Tuesday, 10, 0, 19, 0)
                ]
            }
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Dto.OperatingHours");
        result.Errors.Should().Contain(x => x.ErrorMessage.Contains("duplicate days"));
    }

    [Fact]
    public void UpdateDepotCommandValidator_ShouldRejectMoreThanSevenOperatingHoursEntries()
    {
        var validator = new UpdateDepotCommandValidator();
        var command = ValidUpdateCommand() with
        {
            Dto = new UpdateDepotDto
            {
                Name = "Updated Depot",
                IsActive = true,
                OperatingHours =
                [
                    ValidOperatingHours(DayOfWeek.Sunday, 8, 0, 17, 0),
                    ValidOperatingHours(DayOfWeek.Monday, 8, 0, 17, 0),
                    ValidOperatingHours(DayOfWeek.Tuesday, 8, 0, 17, 0),
                    ValidOperatingHours(DayOfWeek.Wednesday, 8, 0, 17, 0),
                    ValidOperatingHours(DayOfWeek.Thursday, 8, 0, 17, 0),
                    ValidOperatingHours(DayOfWeek.Friday, 8, 0, 17, 0),
                    ValidOperatingHours(DayOfWeek.Saturday, 8, 0, 17, 0),
                    new OperatingHoursDto
                    {
                        DayOfWeek = DayOfWeek.Monday,
                        OpenTime = new TimeOnly(18, 0),
                        ClosedTime = new TimeOnly(19, 0),
                        IsClosed = false
                    }
                ]
            }
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Dto.OperatingHours");
        result.Errors.Should().Contain(x => x.ErrorMessage.Contains("more than 7 operating hours"));
    }

    [Fact]
    public void UpdateDepotCommandValidator_ShouldRequireOpenAndCloseTimesForOpenDays()
    {
        var validator = new UpdateDepotCommandValidator();
        var command = ValidUpdateCommand() with
        {
            Dto = new UpdateDepotDto
            {
                Name = "Updated Depot",
                IsActive = true,
                OperatingHours =
                [
                    new OperatingHoursDto
                    {
                        DayOfWeek = DayOfWeek.Wednesday,
                        OpenTime = null,
                        ClosedTime = null,
                        IsClosed = false
                    }
                ]
            }
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Dto.OperatingHours[0].OpenTime");
        result.Errors.Should().Contain(x => x.PropertyName == "Dto.OperatingHours[0].ClosedTime");
    }

    [Fact]
    public void UpdateDepotCommandValidator_ShouldRejectClosedTimeBeforeOpenTime()
    {
        var validator = new UpdateDepotCommandValidator();
        var command = ValidUpdateCommand() with
        {
            Dto = new UpdateDepotDto
            {
                Name = "Updated Depot",
                IsActive = true,
                OperatingHours =
                [
                    new OperatingHoursDto
                    {
                        DayOfWeek = DayOfWeek.Thursday,
                        OpenTime = new TimeOnly(18, 0),
                        ClosedTime = new TimeOnly(9, 0),
                        IsClosed = false
                    }
                ]
            }
        };

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Dto.OperatingHours[0].ClosedTime");
        result.Errors.Should().Contain(x => x.ErrorMessage.Contains("after open time"));
    }

    [Fact]
    public void UpdateDepotCommandValidator_ShouldAcceptValidOperatingHours()
    {
        var validator = new UpdateDepotCommandValidator();
        var command = ValidUpdateCommand();

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    private static UpdateDepotCommand ValidUpdateCommand() =>
        new(
            Guid.NewGuid(),
            new UpdateDepotDto
            {
                Name = "Updated Depot",
                IsActive = true,
                OperatingHours =
                [
                    ValidOperatingHours(DayOfWeek.Monday, 8, 0, 17, 0)
                ]
            });

    private static AddressDto ValidAddress() =>
        new()
        {
            Street1 = "123 Warehouse Rd",
            City = "Melbourne",
            State = "VIC",
            PostalCode = "3000",
            CountryCode = "AU"
        };

    private static OperatingHoursDto ValidOperatingHours(
        DayOfWeek dayOfWeek,
        int openHour,
        int openMinute,
        int closeHour,
        int closeMinute) =>
        new()
        {
            DayOfWeek = dayOfWeek,
            OpenTime = new TimeOnly(openHour, openMinute),
            ClosedTime = new TimeOnly(closeHour, closeMinute),
            IsClosed = false
        };
}
