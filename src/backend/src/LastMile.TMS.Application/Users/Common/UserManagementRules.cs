using FluentValidation;
using FluentValidation.Results;
using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LastMile.TMS.Application.Users.Common;

internal static class UserManagementRules
{
    public const string ProtectedSystemAdminMessage =
        "The seeded system admin account is protected and cannot be modified.";

    public static async Task EnsureValidAssignmentsAsync(
        IAppDbContext dbContext,
        Guid? depotId,
        Guid? zoneId,
        CancellationToken cancellationToken)
    {
        if (zoneId.HasValue && depotId is null)
        {
            throw CreateValidationException("A depot must be selected when a zone is assigned.");
        }

        if (depotId.HasValue)
        {
            var depotExists = await dbContext.Depots
                .AsNoTracking()
                .AnyAsync(x => x.Id == depotId.Value && x.IsActive, cancellationToken);

            if (!depotExists)
            {
                throw CreateValidationException("The selected depot does not exist or is inactive.");
            }
        }

        if (zoneId.HasValue)
        {
            var zone = await dbContext.Zones
                .AsNoTracking()
                .Where(x => x.Id == zoneId.Value)
                .Select(x => new { x.DepotId, x.IsActive })
                .SingleOrDefaultAsync(cancellationToken);

            if (zone is null || !zone.IsActive)
            {
                throw CreateValidationException("The selected zone does not exist or is inactive.");
            }

            if (zone.DepotId != depotId)
            {
                throw CreateValidationException("The selected zone does not belong to the selected depot.");
            }
        }
    }

    public static void EnsureUserCanBeManaged(ApplicationUser user)
    {
        if (user.IsSystemAdmin)
        {
            throw CreateValidationException(ProtectedSystemAdminMessage);
        }
    }

    public static ValidationException CreateValidationException(params string[] messages) =>
        new(messages.Select(message => new ValidationFailure(string.Empty, message)));
}
