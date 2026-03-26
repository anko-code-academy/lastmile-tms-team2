using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Domain.Entities;

namespace LastMile.TMS.Api.Tests;

public sealed record SentUserAccountEmail(
    Guid UserId,
    string Email,
    string Token,
    string Kind);

public sealed class TestUserAccountEmailService : IUserAccountEmailService
{
    private readonly List<SentUserAccountEmail> _emails = [];

    public IReadOnlyList<SentUserAccountEmail> Emails => _emails;

    public void Clear() => _emails.Clear();

    public Task SendPasswordSetupEmailAsync(
        ApplicationUser user,
        string token,
        CancellationToken cancellationToken)
    {
        _emails.Add(new SentUserAccountEmail(user.Id, user.Email!, token, "setup"));
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(
        ApplicationUser user,
        string token,
        CancellationToken cancellationToken)
    {
        _emails.Add(new SentUserAccountEmail(user.Id, user.Email!, token, "reset"));
        return Task.CompletedTask;
    }
}
