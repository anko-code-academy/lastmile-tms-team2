using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Domain.Entities;

namespace LastMile.TMS.Api.Tests;

public sealed record SentUserAccountEmail(
    Guid UserId,
    string Email,
    string Token,
    string Kind,
    string? FrontendBaseUrl);

public sealed class TestUserAccountEmailService : IUserAccountEmailService
{
    private readonly List<SentUserAccountEmail> _emails = [];

    public IReadOnlyList<SentUserAccountEmail> Emails => _emails;

    public void Clear() => _emails.Clear();

    public Task SendPasswordSetupEmailAsync(
        ApplicationUser user,
        string token,
        string? frontendBaseUrl,
        CancellationToken cancellationToken)
    {
        _emails.Add(new SentUserAccountEmail(user.Id, user.Email!, token, "setup", frontendBaseUrl));
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(
        ApplicationUser user,
        string token,
        string? frontendBaseUrl,
        CancellationToken cancellationToken)
    {
        _emails.Add(new SentUserAccountEmail(user.Id, user.Email!, token, "reset", frontendBaseUrl));
        return Task.CompletedTask;
    }
}
