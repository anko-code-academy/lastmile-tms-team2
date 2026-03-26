using System.Text;
using FluentValidation;
using LastMile.TMS.Application.Users.Common;
using LastMile.TMS.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace LastMile.TMS.Application.Users.Commands;

public sealed record CompletePasswordResetCommand(
    string Email,
    string Token,
    string NewPassword) : IRequest<UserActionResultDto>;

public sealed class CompletePasswordResetCommandValidator : AbstractValidator<CompletePasswordResetCommand>
{
    public CompletePasswordResetCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
    }
}

public sealed class CompletePasswordResetCommandHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<CompletePasswordResetCommand, UserActionResultDto>
{
    public async Task<UserActionResultDto> Handle(
        CompletePasswordResetCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            throw UserManagementRules.CreateValidationException("The password reset link is invalid.");
        }

        string decodedToken;
        try
        {
            decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
        }
        catch (Exception)
        {
            throw UserManagementRules.CreateValidationException("The password reset link is invalid.");
        }

        var result = await userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);
        if (!result.Succeeded)
        {
            throw UserManagementRules.CreateValidationException(result.Errors.Select(x => x.Description).ToArray());
        }

        return new UserActionResultDto(true, "Password has been reset.");
    }
}
