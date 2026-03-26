using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace LastMile.TMS.Api.Tests;

public sealed class ImmediateUserAccountEmailJobScheduler(
    UserManager<ApplicationUser> userManager,
    IUserAccountEmailService emailService) : IUserAccountEmailJobScheduler
{
    public async Task SchedulePasswordSetupEmailAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(userId);
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        await emailService.SendPasswordSetupEmailAsync(user, token, cancellationToken);
    }

    public async Task SchedulePasswordResetEmailAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await GetUserAsync(userId);
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        await emailService.SendPasswordResetEmailAsync(user, token, cancellationToken);
    }

    private async Task<ApplicationUser> GetUserAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user ?? throw new KeyNotFoundException("User not found.");
    }
}
