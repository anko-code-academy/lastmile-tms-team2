namespace LastMile.TMS.Application.Common.Models;

public static class StorageObjectKeys
{
    public const string ParcelImportPrefix = "parcel-imports/";
    public const string DeliveryConfirmationPrefix = "delivery-confirmations/";

    public static string BuildParcelImportSourceFileKey(Guid parcelImportId, string fileName)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty).Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(extension)
            ? $"{ParcelImportPrefix}{parcelImportId:N}"
            : $"{ParcelImportPrefix}{parcelImportId:N}{extension}";
    }

    public static string BuildDeliveryConfirmationPhotoKey(Guid deliveryConfirmationId) =>
        $"{DeliveryConfirmationPrefix}{deliveryConfirmationId:N}/photo";

    public static string BuildDeliveryConfirmationSignatureKey(Guid deliveryConfirmationId) =>
        $"{DeliveryConfirmationPrefix}{deliveryConfirmationId:N}/signature";
}
