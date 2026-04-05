using LastMile.TMS.Application.Parcels.Commands;
using LastMile.TMS.Application.Parcels.DTOs;
using Riok.Mapperly.Abstractions;

namespace LastMile.TMS.Api.GraphQL.Parcels;

[Mapper]
public static partial class ParcelInputMapper
{
    public static partial RegisterParcelRecipientAddressDto ToDto(
        this RegisterParcelRecipientAddressInput input);

    public static partial RegisterParcelDto ToDto(this RegisterParcelInput input);

    public static partial UpdateParcelDto ToDto(this UpdateParcelInput input);

    public static TransitionParcelStatusCommand ToDto(
        this TransitionParcelStatusInput input)
        => new(input.ParcelId, input.NewStatus, input.Location, input.Description);

    private static DateTime DateTimeOffsetToUtc(DateTimeOffset value) => value.UtcDateTime;
}
