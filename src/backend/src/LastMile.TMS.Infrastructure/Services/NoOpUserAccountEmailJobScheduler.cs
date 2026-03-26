using LastMile.TMS.Application.Common.Interfaces;

namespace LastMile.TMS.Infrastructure.Services;

public sealed class NoOpUserAccountEmailJobScheduler(
    UserAccountEmailBackgroundJob backgroundJob,
    FrontendBaseUrlResolver frontendBaseUrlResolver) : IUserAccountEmailJobScheduler
{
    public async Task SchedulePasswordSetupEmailAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await backgroundJob.SendPasswordSetupEmailAsync(userId, frontendBaseUrlResolver.Resolve());
    }

    public async Task SchedulePasswordResetEmailAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await backgroundJob.SendPasswordResetEmailAsync(userId, frontendBaseUrlResolver.Resolve());
    }
}
