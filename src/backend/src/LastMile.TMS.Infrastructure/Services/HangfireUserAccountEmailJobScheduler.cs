using Hangfire;
using LastMile.TMS.Application.Common.Interfaces;

namespace LastMile.TMS.Infrastructure.Services;

public sealed class HangfireUserAccountEmailJobScheduler(
    IBackgroundJobClient backgroundJobClient,
    FrontendBaseUrlResolver frontendBaseUrlResolver) : IUserAccountEmailJobScheduler
{
    public Task SchedulePasswordSetupEmailAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var frontendBaseUrl = frontendBaseUrlResolver.Resolve();
        backgroundJobClient.Enqueue<UserAccountEmailBackgroundJob>(
            job => job.SendPasswordSetupEmailAsync(userId, frontendBaseUrl));
        return Task.CompletedTask;
    }

    public Task SchedulePasswordResetEmailAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var frontendBaseUrl = frontendBaseUrlResolver.Resolve();
        backgroundJobClient.Enqueue<UserAccountEmailBackgroundJob>(
            job => job.SendPasswordResetEmailAsync(userId, frontendBaseUrl));
        return Task.CompletedTask;
    }
}
