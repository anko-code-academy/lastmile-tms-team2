using LastMile.TMS.Domain.Entities;

namespace LastMile.TMS.Application.Common.Interfaces;

public interface IUserAccountEmailService
{
    Task SendPasswordSetupEmailAsync(
        ApplicationUser user,
        string token,
        string? frontendBaseUrl,
        CancellationToken cancellationToken);

    Task SendPasswordResetEmailAsync(
        ApplicationUser user,
        string token,
        string? frontendBaseUrl,
        CancellationToken cancellationToken);
}
