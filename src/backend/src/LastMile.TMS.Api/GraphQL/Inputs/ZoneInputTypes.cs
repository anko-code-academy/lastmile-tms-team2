namespace LastMile.TMS.Api.GraphQL.Inputs;

public class CreateZoneInputType : InputObjectType<CreateZoneInput>
{
    protected override void Configure(IInputObjectTypeDescriptor<CreateZoneInput> descriptor)
    {
        descriptor.Name("CreateZoneInput");
        descriptor.BindFieldsImplicitly();

        descriptor.Field(x => x.Name).Name("name");
        descriptor.Field(x => x.DepotId).Name("depotId");
        descriptor.Field(x => x.IsActive).Name("isActive");
        descriptor.Field(x => x.GeoJson).Name("geoJson");
        descriptor.Field(x => x.Coordinates).Name("coordinates");
        descriptor.Field(x => x.BoundaryWkt).Name("boundaryWkt");
    }
}

public class UpdateZoneInputType : InputObjectType<UpdateZoneInput>
{
    protected override void Configure(IInputObjectTypeDescriptor<UpdateZoneInput> descriptor)
    {
        descriptor.Name("UpdateZoneInput");
        descriptor.BindFieldsImplicitly();

        descriptor.Field(x => x.Name).Name("name");
        descriptor.Field(x => x.DepotId).Name("depotId");
        descriptor.Field(x => x.IsActive).Name("isActive");
        descriptor.Field(x => x.GeoJson).Name("geoJson");
        descriptor.Field(x => x.Coordinates).Name("coordinates");
        descriptor.Field(x => x.BoundaryWkt).Name("boundaryWkt");
    }
}
