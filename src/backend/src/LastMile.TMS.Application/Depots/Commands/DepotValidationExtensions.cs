using System.Linq.Expressions;
using FluentValidation;
using LastMile.TMS.Application.Depots.DTOs;

namespace LastMile.TMS.Application.Depots.Commands;

internal static class DepotValidationExtensions
{
    public static void AddOperatingHoursRules<T>(
        this AbstractValidator<T> validator,
        Expression<Func<T, IEnumerable<OperatingHoursDto>>> selector)
    {
        var selectorFunc = selector.Compile();

        validator.When(x => selectorFunc(x) is not null, () =>
        {
            validator.RuleFor(selector)
                .Must(hours => hours.Count() <= 7)
                .WithMessage("Cannot have more than 7 operating hours entries (one per day).")
                .Must(HaveUniqueOperatingHoursDays)
                .WithMessage("Operating hours cannot contain duplicate days.");

            validator.RuleForEach(selector)
                .SetValidator(new DepotOperatingHoursEntryValidator());
        });
    }

    private static bool HaveUniqueOperatingHoursDays(IEnumerable<OperatingHoursDto>? hours)
    {
        if (hours is null)
            return true;

        var operatingHours = hours.ToList();
        return operatingHours.Select(h => h.DayOfWeek).Distinct().Count() == operatingHours.Count;
    }

    private sealed class DepotOperatingHoursEntryValidator : AbstractValidator<OperatingHoursDto>
    {
        public DepotOperatingHoursEntryValidator()
        {
            RuleFor(h => h.OpenTime)
                .NotNull().WithMessage("Open time is required when not closed.")
                .When(h => !h.IsClosed);

            RuleFor(h => h.ClosedTime)
                .NotNull().WithMessage("Closed time is required when not closed.")
                .When(h => !h.IsClosed);

            RuleFor(h => h.ClosedTime)
                .GreaterThan(h => h.OpenTime)
                .WithMessage("Closed time must be after open time.")
                .When(h => !h.IsClosed && h.OpenTime is not null && h.ClosedTime is not null);
        }
    }
}
