using BinaryKits.Zpl.Viewer;
using LastMile.TMS.Application.Parcels.Services;

namespace LastMile.TMS.Infrastructure.Services;

public sealed class ZplLabelRasterizer : IZplLabelRasterizer
{
    private const string RenderMutexName = @"Local\LastMileTms.ParcelLabelRender";
    private static readonly Mutex RenderMutex = new(false, RenderMutexName);

    public byte[] Rasterize(string zpl, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // BinaryKits.Zpl.Viewer uses BarcodeLib/System.Drawing underneath.
        // A process-local lock is not enough because `dotnet test` may run multiple
        // test host processes in parallel, so we coordinate with a named mutex.
        var ownsMutex = false;

        try
        {
            try
            {
                ownsMutex = RenderMutex.WaitOne(TimeSpan.FromMinutes(1));
            }
            catch (AbandonedMutexException)
            {
                ownsMutex = true;
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (!ownsMutex)
            {
                throw new TimeoutException("Timed out waiting for the parcel label render mutex.");
            }

            IPrinterStorage printerStorage = new PrinterStorage();
            var analyzer = new ZplAnalyzer(printerStorage);
            var drawer = new ZplElementDrawer(printerStorage);
            var analysis = analyzer.Analyze(zpl);
            var labelInfo = analysis.LabelInfos.FirstOrDefault();

            if (labelInfo is null)
            {
                throw new InvalidOperationException("Could not render the generated ZPL label.");
            }

            return drawer.Draw(labelInfo.ZplElements);
        }
        finally
        {
            if (ownsMutex)
            {
                RenderMutex.ReleaseMutex();
            }
        }
    }
}
