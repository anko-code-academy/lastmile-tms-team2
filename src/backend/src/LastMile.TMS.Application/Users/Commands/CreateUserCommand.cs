using System.Security.Cryptography;
using FluentValidation;
using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Users.Common;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace LastMile.TMS.Application.Users.Commands;

public sealed record CreateUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    PredefinedRole Role,
    Guid? DepotId,
    Guid? ZoneId) : IRequest<UserManagementUserDto>;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Phone).MaximumLength(20);
    }
}

public sealed class CreateUserCommandHandler(
    IAppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IUserAccountEmailJobScheduler emailJobScheduler,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateUserCommand, UserManagementUserDto>
{
    public async Task<UserManagementUserDto> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        await UserManagementRules.EnsureValidAssignmentsAsync(
            dbContext,
            request.DepotId,
            request.ZoneId,
            cancellationToken);

        var email = request.Email.Trim();
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            throw UserManagementRules.CreateValidationException("A user with this email already exists.");
        }

        var roleName = UserManagementRoleHelper.ToIdentityRoleName(request.Role);
        var role = await roleManager.FindByNameAsync(roleName);
        if (role is null)
        {
            throw UserManagementRules.CreateValidationException("The selected role does not exist.");
        }

        var temporaryPassword = GenerateTemporaryPassword();
        var user = new ApplicationUser
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            UserName = email,
            PhoneNumber = NormalizeOptional(request.Phone),
            IsActive = true,
            DepotId = request.DepotId,
            ZoneId = request.ZoneId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = currentUserService.UserId ?? "system"
        };

        var createResult = await userManager.CreateAsync(user, temporaryPassword);
        if (!createResult.Succeeded)
        {
            throw CreateIdentityValidationException(createResult);
        }

        var roleResult = await userManager.AddToRoleAsync(user, roleName);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            throw CreateIdentityValidationException(roleResult);
        }

        await emailJobScheduler.SchedulePasswordSetupEmailAsync(user.Id, cancellationToken);

        return await UserManagementReadModel.GetUserAsync(dbContext, user.Id, cancellationToken);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ValidationException CreateIdentityValidationException(IdentityResult result) =>
        UserManagementRules.CreateValidationException(result.Errors.Select(x => x.Description).ToArray());

    private static string GenerateTemporaryPassword()
    {
        const string uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lowercase = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string allCharacters = uppercase + lowercase + digits;

        Span<char> password = stackalloc char[16];
        password[0] = uppercase[RandomNumberGenerator.GetInt32(uppercase.Length)];
        password[1] = lowercase[RandomNumberGenerator.GetInt32(lowercase.Length)];
        password[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];

        for (var index = 3; index < password.Length; index++)
        {
            password[index] = allCharacters[RandomNumberGenerator.GetInt32(allCharacters.Length)];
        }

        for (var index = password.Length - 1; index > 0; index--)
        {
            var swapIndex = RandomNumberGenerator.GetInt32(index + 1);
            (password[index], password[swapIndex]) = (password[swapIndex], password[index]);
        }

        return new string(password);
    }
}
