using HotChocolate;
using HotChocolate.Authorization;
using LastMile.TMS.Application.BinLocations.Commands;
using LastMile.TMS.Application.BinLocations.DTOs;
using MediatR;

namespace LastMile.TMS.Api.GraphQL.BinLocations;

[ExtendObjectType(OperationTypeNames.Mutation)]
public sealed class BinLocationMutations
{
    [Authorize(Roles = new[] { "OperationsManager", "Admin" })]
    public Task<StorageZoneResultDto> CreateStorageZone(
        CreateStorageZoneInput input,
        [Service] ISender sender,
        CancellationToken cancellationToken) =>
        sender.Send(new CreateStorageZoneCommand(input.ToDto()), cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin" })]
    public Task<StorageZoneResultDto?> UpdateStorageZone(
        Guid id,
        UpdateStorageZoneInput input,
        [Service] ISender sender,
        CancellationToken cancellationToken) =>
        sender.Send(new UpdateStorageZoneCommand(id, input.ToDto()), cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin" })]
    public Task<bool> DeleteStorageZone(
        Guid id,
        [Service] ISender sender,
        CancellationToken cancellationToken) =>
        sender.Send(new DeleteStorageZoneCommand(id), cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin" })]
    public Task<StorageAisleResultDto> CreateStorageAisle(
        CreateStorageAisleInput input,
        [Service] ISender sender,
        CancellationToken cancellationToken) =>
        sender.Send(new CreateStorageAisleCommand(input.ToDto()), cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin" })]
    public Task<StorageAisleResultDto?> UpdateStorageAisle(
        Guid id,
        UpdateStorageAisleInput input,
        [Service] ISender sender,
        CancellationToken cancellationToken) =>
        sender.Send(new UpdateStorageAisleCommand(id, input.ToDto()), cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin" })]
    public Task<bool> DeleteStorageAisle(
        Guid id,
        [Service] ISender sender,
        CancellationToken cancellationToken) =>
        sender.Send(new DeleteStorageAisleCommand(id), cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin" })]
    public Task<BinLocationResultDto> CreateBinLocation(
        CreateBinLocationInput input,
        [Service] ISender sender,
        CancellationToken cancellationToken) =>
        sender.Send(new CreateBinLocationCommand(input.ToDto()), cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin" })]
    public Task<BinLocationResultDto?> UpdateBinLocation(
        Guid id,
        UpdateBinLocationInput input,
        [Service] ISender sender,
        CancellationToken cancellationToken) =>
        sender.Send(new UpdateBinLocationCommand(id, input.ToDto()), cancellationToken);

    [Authorize(Roles = new[] { "OperationsManager", "Admin" })]
    public Task<bool> DeleteBinLocation(
        Guid id,
        [Service] ISender sender,
        CancellationToken cancellationToken) =>
        sender.Send(new DeleteBinLocationCommand(id), cancellationToken);
}
