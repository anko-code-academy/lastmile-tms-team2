using HotChocolate.Types;
using LastMile.TMS.Application.BinLocations.DTOs;

namespace LastMile.TMS.Api.GraphQL.BinLocations;

public sealed class DepotStorageLayoutType : ObjectType<DepotStorageLayoutDto>
{
    protected override void Configure(IObjectTypeDescriptor<DepotStorageLayoutDto> descriptor)
    {
        descriptor.Name("DepotStorageLayout");
        descriptor.BindFieldsExplicitly();
        descriptor.Field(x => x.DepotId);
        descriptor.Field(x => x.DepotName);
        descriptor.Field(x => x.StorageZones)
            .Type<NonNullType<ListType<NonNullType<StorageZoneType>>>>();
    }
}

public sealed class StorageZoneType : ObjectType<StorageZoneResultDto>
{
    protected override void Configure(IObjectTypeDescriptor<StorageZoneResultDto> descriptor)
    {
        descriptor.Name("StorageZone");
        descriptor.BindFieldsExplicitly();
        descriptor.Field(x => x.Id);
        descriptor.Field(x => x.Name);
        descriptor.Field(x => x.DepotId);
        descriptor.Field(x => x.StorageAisles)
            .Type<NonNullType<ListType<NonNullType<StorageAisleType>>>>();
    }
}

public sealed class StorageAisleType : ObjectType<StorageAisleResultDto>
{
    protected override void Configure(IObjectTypeDescriptor<StorageAisleResultDto> descriptor)
    {
        descriptor.Name("StorageAisle");
        descriptor.BindFieldsExplicitly();
        descriptor.Field(x => x.Id);
        descriptor.Field(x => x.Name);
        descriptor.Field(x => x.StorageZoneId);
        descriptor.Field(x => x.BinLocations)
            .Type<NonNullType<ListType<NonNullType<BinLocationType>>>>();
    }
}

public sealed class BinLocationType : ObjectType<BinLocationResultDto>
{
    protected override void Configure(IObjectTypeDescriptor<BinLocationResultDto> descriptor)
    {
        descriptor.Name("BinLocation");
        descriptor.BindFieldsExplicitly();
        descriptor.Field(x => x.Id);
        descriptor.Field(x => x.Name);
        descriptor.Field(x => x.IsActive);
        descriptor.Field(x => x.StorageAisleId);
    }
}
