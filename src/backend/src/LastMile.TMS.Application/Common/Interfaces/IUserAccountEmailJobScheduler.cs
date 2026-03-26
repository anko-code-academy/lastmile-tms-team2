namespace LastMile.TMS.Application.Common.Interfaces;

public interface IUserAccountEmailJobScheduler
{
    Task SchedulePasswordSetupEmailAsync(Guid userId, CancellationToken cancellationToken);

    Task SchedulePasswordResetEmailAsync(Guid userId, CancellationToken cancellationToken);
}
