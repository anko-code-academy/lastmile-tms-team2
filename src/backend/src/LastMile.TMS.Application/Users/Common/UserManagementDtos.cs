using LastMile.TMS.Domain.Enums;

namespace LastMile.TMS.Application.Users.Common;

public sealed record UserManagementUserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string? Phone,
    PredefinedRole? Role,
    bool IsActive,
    Guid? DepotId,
    string? DepotName,
    Guid? ZoneId,
    string? ZoneName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastModifiedAt);

public sealed record UserManagementUsersResultDto(
    int TotalCount,
    IReadOnlyList<UserManagementUserDto> Items);

public sealed record UserManagementRoleOptionDto(
    PredefinedRole Value,
    string Label);

public sealed record UserManagementDepotOptionDto(
    Guid Id,
    string Name);

public sealed record UserManagementZoneOptionDto(
    Guid Id,
    Guid DepotId,
    string Name);

public sealed record UserManagementLookupsDto(
    IReadOnlyList<UserManagementRoleOptionDto> Roles,
    IReadOnlyList<UserManagementDepotOptionDto> Depots,
    IReadOnlyList<UserManagementZoneOptionDto> Zones);

public sealed record UserActionResultDto(
    bool Success,
    string Message);
