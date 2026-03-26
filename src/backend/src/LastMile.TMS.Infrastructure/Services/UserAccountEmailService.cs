using System.Text;
using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Infrastructure.Options;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace LastMile.TMS.Infrastructure.Services;

public sealed class UserAccountEmailService(
    IOptions<EmailOptions> emailOptions,
    IOptions<FrontendOptions> frontendOptions,
    IHostEnvironment environment,
    ILogger<UserAccountEmailService> logger) : IUserAccountEmailService
{
    public Task SendPasswordSetupEmailAsync(
        ApplicationUser user,
        string token,
        string? frontendBaseUrl,
        CancellationToken cancellationToken) =>
        SendAsync(
            user,
            token,
            frontendBaseUrl,
            "Set up your Last Mile TMS account",
            "Use the link below to set your password and activate your account.",
            cancellationToken);

    public Task SendPasswordResetEmailAsync(
        ApplicationUser user,
        string token,
        string? frontendBaseUrl,
        CancellationToken cancellationToken) =>
        SendAsync(
            user,
            token,
            frontendBaseUrl,
            "Reset your Last Mile TMS password",
            "Use the link below to choose a new password for your account.",
            cancellationToken);

    private async Task SendAsync(
        ApplicationUser user,
        string token,
        string? frontendBaseUrl,
        string subject,
        string intro,
        CancellationToken cancellationToken)
    {
        var email = user.Email;
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("The selected user does not have an email address.");
        }

        var resetLink = BuildResetLink(email, token, frontendBaseUrl);
        var plainTextContent = $"{intro}{Environment.NewLine}{Environment.NewLine}{resetLink}";
        var htmlContent =
            $"<p>{intro}</p><p><a href=\"{resetLink}\">Open password reset page</a></p><p>{resetLink}</p>";

        if (emailOptions.Value.DisableDelivery)
        {
            logger.LogInformation(
                "Password email delivery disabled for {Email} in {Environment}: {Link}",
                email,
                environment.EnvironmentName,
                resetLink);
            return;
        }

        if (string.IsNullOrWhiteSpace(emailOptions.Value.SendGridApiKey))
        {
            logger.LogInformation(
                "Password email fallback for {Email} in {Environment}: {Link}",
                email,
                environment.EnvironmentName,
                resetLink);
            return;
        }

        var client = new SendGridClient(emailOptions.Value.SendGridApiKey);
        var message = MailHelper.CreateSingleEmail(
            new EmailAddress(emailOptions.Value.FromEmail, emailOptions.Value.FromName),
            new EmailAddress(email, $"{user.FirstName} {user.LastName}".Trim()),
            subject,
            plainTextContent,
            htmlContent);

        var response = await client.SendEmailAsync(message, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var responseBody = await response.Body.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException(
            $"Failed to send the account email. SendGrid returned {(int)response.StatusCode}: {responseBody}");
    }

    private string BuildResetLink(string email, string token, string? frontendBaseUrl)
    {
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var baseUrl = string.IsNullOrWhiteSpace(frontendBaseUrl)
            ? frontendOptions.Value.BaseUrl.TrimEnd('/')
            : frontendBaseUrl.TrimEnd('/');

        return $"{baseUrl}/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(encodedToken)}";
    }
}
