using FluentValidation;
using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Users.Common;
using LastMile.TMS.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;

namespace LastMile.TMS.Application.Users.Commands;

public sealed record DeactivateUserCommand(Guid UserId) : IRequest<UserManagementUserDto>;

public sealed class DeactivateUserCommandValidator : AbstractValidator<DeactivateUserCommand>
{
    public DeactivateUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class DeactivateUserCommandHandler(
    IAppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUserService,
    IOpenIddictAuthorizationManager authorizationManager,
    IOpenIddictTokenManager tokenManager)
    : IRequestHandler<DeactivateUserCommand, UserManagementUserDto>
{
    public async Task<UserManagementUserDto> Handle(
        DeactivateUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        UserManagementRules.EnsureUserCanBeManaged(user);

        if (user.IsActive)
        {
            user.IsActive = false;
            user.LastModifiedAt = DateTimeOffset.UtcNow;
            user.LastModifiedBy = currentUserService.UserId ?? "system";

            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                throw UserManagementRules.CreateValidationException(updateResult.Errors.Select(x => x.Description).ToArray());
            }
        }

        var subject = user.Id.ToString();
        await RevokeAuthorizationsAsync(subject, cancellationToken);
        await RevokeTokensAsync(subject, cancellationToken);

        return await UserManagementReadModel.GetUserAsync(dbContext, user.Id, cancellationToken);
    }

    private async Task RevokeAuthorizationsAsync(string subject, CancellationToken cancellationToken)
    {
        try
        {
            await authorizationManager.RevokeBySubjectAsync(subject, cancellationToken);
        }
        catch (InvalidOperationException exception) when (UsesUnsupportedExecuteUpdate(exception))
        {
            await foreach (var authorization in authorizationManager.FindBySubjectAsync(subject, cancellationToken))
            {
                await authorizationManager.TryRevokeAsync(authorization, cancellationToken);
            }
        }
    }

    private async Task RevokeTokensAsync(string subject, CancellationToken cancellationToken)
    {
        try
        {
            await tokenManager.RevokeBySubjectAsync(subject, cancellationToken);
        }
        catch (InvalidOperationException exception) when (UsesUnsupportedExecuteUpdate(exception))
        {
            await foreach (var token in tokenManager.FindBySubjectAsync(subject, cancellationToken))
            {
                await tokenManager.TryRevokeAsync(token, cancellationToken);
            }
        }
    }

    private static bool UsesUnsupportedExecuteUpdate(InvalidOperationException exception) =>
        exception.Message.Contains("ExecuteUpdate", StringComparison.Ordinal);
}
