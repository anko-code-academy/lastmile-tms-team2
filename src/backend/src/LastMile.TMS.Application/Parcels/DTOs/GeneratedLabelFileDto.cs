namespace LastMile.TMS.Application.Parcels.DTOs;

public sealed record GeneratedLabelFileDto(
    byte[] Content,
    string ContentType,
    string FileName);
