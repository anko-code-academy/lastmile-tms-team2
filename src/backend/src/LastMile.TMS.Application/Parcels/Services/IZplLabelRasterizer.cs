namespace LastMile.TMS.Application.Parcels.Services;

public interface IZplLabelRasterizer
{
    byte[] Rasterize(string zpl, CancellationToken cancellationToken = default);
}
