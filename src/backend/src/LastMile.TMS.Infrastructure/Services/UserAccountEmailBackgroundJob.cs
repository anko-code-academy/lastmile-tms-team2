using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace LastMile.TMS.Infrastructure.Services;

public sealed class UserAccountEmailBackgroundJob(
    UserManager<ApplicationUser> userManager,
    IUserAccountEmailService emailService,
    ILogger<UserAccountEmailBackgroundJob> logger)
{
    public async Task SendPasswordSetupEmailAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            logger.LogWarning("Skipped password setup email for missing user {UserId}.", userId);
            return;
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        await emailService.SendPasswordSetupEmailAsync(user, token, CancellationToken.None);
    }

    public async Task SendPasswordResetEmailAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            logger.LogWarning("Skipped password reset email for missing user {UserId}.", userId);
            return;
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        await emailService.SendPasswordResetEmailAsync(user, token, CancellationToken.None);
    }
}
