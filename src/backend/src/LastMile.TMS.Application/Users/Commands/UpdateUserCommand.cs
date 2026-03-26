using FluentValidation;
using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Users.Common;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace LastMile.TMS.Application.Users.Commands;

public sealed record UpdateUserCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    PredefinedRole Role,
    Guid? DepotId,
    Guid? ZoneId,
    bool IsActive) : IRequest<UserManagementUserDto>;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Phone).MaximumLength(20);
    }
}

public sealed class UpdateUserCommandHandler(
    IAppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    ICurrentUserService currentUserService)
    : IRequestHandler<UpdateUserCommand, UserManagementUserDto>
{
    public async Task<UserManagementUserDto> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        await UserManagementRules.EnsureValidAssignmentsAsync(
            dbContext,
            request.DepotId,
            request.ZoneId,
            cancellationToken);

        var user = await userManager.FindByIdAsync(request.Id.ToString());
        if (user is null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        UserManagementRules.EnsureUserCanBeManaged(user);

        var email = request.Email.Trim();
        var emailOwner = await userManager.FindByEmailAsync(email);
        if (emailOwner is not null && emailOwner.Id != user.Id)
        {
            throw UserManagementRules.CreateValidationException("A user with this email already exists.");
        }

        var roleName = UserManagementRoleHelper.ToIdentityRoleName(request.Role);
        var role = await roleManager.FindByNameAsync(roleName);
        if (role is null)
        {
            throw UserManagementRules.CreateValidationException("The selected role does not exist.");
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = email;
        user.UserName = email;
        user.PhoneNumber = NormalizeOptional(request.Phone);
        user.IsActive = request.IsActive;
        user.DepotId = request.DepotId;
        user.ZoneId = request.ZoneId;
        user.LastModifiedAt = DateTimeOffset.UtcNow;
        user.LastModifiedBy = currentUserService.UserId ?? "system";

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw CreateIdentityValidationException(updateResult);
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Count != 1 || !currentRoles.Contains(roleName))
        {
            if (currentRoles.Count > 0)
            {
                var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    throw CreateIdentityValidationException(removeResult);
                }
            }

            var addResult = await userManager.AddToRoleAsync(user, roleName);
            if (!addResult.Succeeded)
            {
                throw CreateIdentityValidationException(addResult);
            }
        }

        return await UserManagementReadModel.GetUserAsync(dbContext, user.Id, cancellationToken);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ValidationException CreateIdentityValidationException(IdentityResult result) =>
        UserManagementRules.CreateValidationException(result.Errors.Select(x => x.Description).ToArray());
}
