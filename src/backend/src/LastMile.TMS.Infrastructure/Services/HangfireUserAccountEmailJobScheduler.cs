using Hangfire;
using LastMile.TMS.Application.Common.Interfaces;

namespace LastMile.TMS.Infrastructure.Services;

public sealed class HangfireUserAccountEmailJobScheduler(
    IBackgroundJobClient backgroundJobClient) : IUserAccountEmailJobScheduler
{
    public Task SchedulePasswordSetupEmailAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        backgroundJobClient.Enqueue<UserAccountEmailBackgroundJob>(
            job => job.SendPasswordSetupEmailAsync(userId));
        return Task.CompletedTask;
    }

    public Task SchedulePasswordResetEmailAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        backgroundJobClient.Enqueue<UserAccountEmailBackgroundJob>(
            job => job.SendPasswordResetEmailAsync(userId));
        return Task.CompletedTask;
    }
}
