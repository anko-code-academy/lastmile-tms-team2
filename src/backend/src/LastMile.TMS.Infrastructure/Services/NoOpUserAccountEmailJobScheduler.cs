using LastMile.TMS.Application.Common.Interfaces;

namespace LastMile.TMS.Infrastructure.Services;

public sealed class NoOpUserAccountEmailJobScheduler(
    UserAccountEmailBackgroundJob backgroundJob) : IUserAccountEmailJobScheduler
{
    public async Task SchedulePasswordSetupEmailAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await backgroundJob.SendPasswordSetupEmailAsync(userId);
    }

    public async Task SchedulePasswordResetEmailAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await backgroundJob.SendPasswordResetEmailAsync(userId);
    }
}
