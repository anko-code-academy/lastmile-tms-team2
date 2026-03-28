using HotChocolate.Types;

namespace LastMile.TMS.Api.GraphQL.Common;

public abstract class EntityObjectType<TEntity> : ObjectType<TEntity>
    where TEntity : class
{
    protected override void Configure(IObjectTypeDescriptor<TEntity> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        ConfigureFields(descriptor);
    }

    protected abstract void ConfigureFields(IObjectTypeDescriptor<TEntity> descriptor);
}
