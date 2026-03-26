using LastMile.TMS.Domain.Enums;

namespace LastMile.TMS.Api.GraphQL.Inputs;

public sealed record CreateUserInput(
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    PredefinedRole Role,
    Guid? DepotId,
    Guid? ZoneId);

public sealed record UpdateUserInput(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    PredefinedRole Role,
    Guid? DepotId,
    Guid? ZoneId,
    bool IsActive);

public sealed record CompletePasswordResetInput(
    string Email,
    string Token,
    string NewPassword);
