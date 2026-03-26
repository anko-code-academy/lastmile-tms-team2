using FluentValidation;
using LastMile.TMS.Application.Common.Interfaces;
using LastMile.TMS.Application.Users.Common;
using LastMile.TMS.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace LastMile.TMS.Application.Users.Commands;

public sealed record SendPasswordResetEmailCommand(Guid UserId) : IRequest<UserActionResultDto>;

public sealed class SendPasswordResetEmailCommandValidator : AbstractValidator<SendPasswordResetEmailCommand>
{
    public SendPasswordResetEmailCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class SendPasswordResetEmailCommandHandler(
    UserManager<ApplicationUser> userManager,
    IUserAccountEmailJobScheduler emailJobScheduler)
    : IRequestHandler<SendPasswordResetEmailCommand, UserActionResultDto>
{
    public async Task<UserActionResultDto> Handle(
        SendPasswordResetEmailCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        UserManagementRules.EnsureUserCanBeManaged(user);

        await emailJobScheduler.SchedulePasswordResetEmailAsync(user.Id, cancellationToken);

        return new UserActionResultDto(true, "Password reset email queued.");
    }
}
