using LastMile.TMS.Application.Parcels.DTOs;
using LastMile.TMS.Application.Parcels.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LastMile.TMS.Api.Controllers;

[ApiController]
[Route("api/parcels")]
[Authorize(Roles = "Admin,OperationsManager,Dispatcher,WarehouseOperator")]
public sealed class ParcelLabelsController(ISender mediator) : ControllerBase
{
    [HttpGet("{id:guid}/labels/4x6.zpl")]
    public Task<FileContentResult> DownloadParcelLabelZpl(
        Guid id,
        CancellationToken cancellationToken) =>
        DownloadSingleAsync(id, LabelOutputFormat.Zpl, cancellationToken);

    [HttpGet("{id:guid}/labels/a4.pdf")]
    public Task<FileContentResult> DownloadParcelLabelPdf(
        Guid id,
        CancellationToken cancellationToken) =>
        DownloadSingleAsync(id, LabelOutputFormat.Pdf, cancellationToken);

    [HttpPost("labels/4x6.zpl")]
    public Task<FileContentResult> DownloadBulkParcelLabelsZpl(
        [FromBody] ParcelLabelDownloadRequestDto request,
        CancellationToken cancellationToken) =>
        DownloadBulkAsync(request, LabelOutputFormat.Zpl, cancellationToken);

    [HttpPost("labels/a4.pdf")]
    public Task<FileContentResult> DownloadBulkParcelLabelsPdf(
        [FromBody] ParcelLabelDownloadRequestDto request,
        CancellationToken cancellationToken) =>
        DownloadBulkAsync(request, LabelOutputFormat.Pdf, cancellationToken);

    private async Task<FileContentResult> DownloadSingleAsync(
        Guid id,
        LabelOutputFormat format,
        CancellationToken cancellationToken)
    {
        var file = await mediator.Send(new GenerateParcelLabelsQuery([id], format), cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    private async Task<FileContentResult> DownloadBulkAsync(
        ParcelLabelDownloadRequestDto request,
        LabelOutputFormat format,
        CancellationToken cancellationToken)
    {
        if (request.ParcelIds.Count == 0)
        {
            throw new ArgumentException("At least one parcel id is required.", nameof(request.ParcelIds));
        }

        var file = await mediator.Send(new GenerateParcelLabelsQuery(request.ParcelIds, format), cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
