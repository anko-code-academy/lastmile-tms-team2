using LastMile.TMS.Application.Zones.DTOs;

namespace LastMile.TMS.Api.GraphQL.Types;

public class ZoneType : ObjectType<ZoneDto>
{
    protected override void Configure(IObjectTypeDescriptor<ZoneDto> descriptor)
    {
        descriptor.Name("Zone");
        descriptor.BindFieldsImplicitly();
    }
}
