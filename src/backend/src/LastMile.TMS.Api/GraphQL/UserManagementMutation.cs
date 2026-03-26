using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.AspNetCore.Authorization;
using LastMile.TMS.Application.Users.Commands;
using LastMile.TMS.Application.Users.Common;
using LastMile.TMS.Api.GraphQL.Inputs;
using LastMile.TMS.Domain.Enums;
using MediatR;

namespace LastMile.TMS.Api.GraphQL;

[ExtendObjectType(OperationTypeNames.Mutation)]
[Authorize(Roles = new[] { nameof(PredefinedRole.Admin) })]
public sealed class UserManagementMutation
{
    public Task<UserManagementUserDto> CreateUser(
        CreateUserInput input,
        [Service] ISender sender,
        CancellationToken cancellationToken) =>
        sender.Send(
            new CreateUserCommand(
                input.FirstName,
                input.LastName,
                input.Email,
                input.Phone,
                input.Role,
                input.DepotId,
                input.ZoneId),
            cancellationToken);

    public Task<UserManagementUserDto> UpdateUser(
        UpdateUserInput input,
        [Service] ISender sender,
        CancellationToken cancellationToken) =>
        sender.Send(
            new UpdateUserCommand(
                input.Id,
                input.FirstName,
                input.LastName,
                input.Email,
                input.Phone,
                input.Role,
                input.DepotId,
                input.ZoneId,
                input.IsActive),
            cancellationToken);

    public Task<UserManagementUserDto> DeactivateUser(
        Guid userId,
        [Service] ISender sender,
        CancellationToken cancellationToken) =>
        sender.Send(new DeactivateUserCommand(userId), cancellationToken);

    public Task<UserActionResultDto> SendPasswordResetEmail(
        Guid userId,
        [Service] ISender sender,
        CancellationToken cancellationToken) =>
        sender.Send(new SendPasswordResetEmailCommand(userId), cancellationToken);

    [AllowAnonymous]
    public Task<UserActionResultDto> RequestPasswordReset(
        string email,
        [Service] ISender sender,
        CancellationToken cancellationToken) =>
        sender.Send(new RequestPasswordResetCommand(email), cancellationToken);

    [AllowAnonymous]
    public Task<UserActionResultDto> CompletePasswordReset(
        CompletePasswordResetInput input,
        [Service] ISender sender,
        CancellationToken cancellationToken) =>
        sender.Send(
            new CompletePasswordResetCommand(
                input.Email,
                input.Token,
                input.NewPassword),
            cancellationToken);
}
