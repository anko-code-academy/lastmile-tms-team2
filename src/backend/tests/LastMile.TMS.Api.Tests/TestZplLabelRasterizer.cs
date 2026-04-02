using LastMile.TMS.Application.Parcels.Services;

namespace LastMile.TMS.Api.Tests;

public sealed class TestZplLabelRasterizer : IZplLabelRasterizer
{
    private static readonly byte[] TestPngBytes = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR4nGNgYAAAAAMAASsJTYQAAAAASUVORK5CYII=");

    public byte[] Rasterize(string zpl, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return TestPngBytes;
    }
}
