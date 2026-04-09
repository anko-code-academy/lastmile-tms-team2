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

    public static StartInboundReceivingSessionCommand ToDto(
        this StartInboundReceivingSessionInput input)
        => new(input.ManifestId);

    public static ScanInboundParcelCommand ToDto(
        this ScanInboundParcelInput input)
        => new(input.SessionId, input.Barcode);

    public static ConfirmInboundReceivingSessionCommand ToDto(
        this ConfirmInboundReceivingSessionInput input)
        => new(input.SessionId);

    public static StageParcelForRouteCommand ToDto(
        this StageParcelForRouteInput input)
        => new(input.RouteId, input.Barcode);

    public static LoadParcelForRouteCommand ToDto(
        this LoadParcelForRouteInput input)
        => new(input.RouteId, input.Barcode);

    public static CompleteLoadOutCommand ToDto(
        this CompleteLoadOutInput input)
        => new(input.RouteId, input.Force);

    private static DateTime DateTimeOffsetToUtc(DateTimeOffset value) => value.UtcDateTime;
}
