using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using LastMile.TMS.Infrastructure.Services;
using LastMile.TMS.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LastMile.TMS.Api.Tests.Controllers;

[Collection(ApiTestCollection.Name)]
public class ParcelImportControllerTests(CustomWebApplicationFactory factory)
    : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
    });

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Upload_WithUnsupportedExtension_ReturnsBadRequest()
    {
        var request = await CreateUploadRequestAsync(
            fileName: "parcels.txt",
            fileContent: "not a supported file");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Only .csv and .xlsx files are supported.");
    }

    [Fact]
    public async Task Upload_WithOversizedFile_ReturnsPayloadTooLarge()
    {
        var request = await CreateUploadRequestAsync(
            fileName: "parcels.csv",
            fileBytes: new byte[(10 * 1024 * 1024) + 1]);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be((HttpStatusCode)413);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("10 MB");
    }

    [Fact]
    public async Task DownloadTemplateCsv_ReturnsCanonicalHeaderRow()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/parcel-imports/template.csv");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetAdminAccessTokenAsync());

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().StartWith(
            "recipient_street1,recipient_street2,recipient_city,recipient_state,recipient_postal_code,recipient_country_code,recipient_is_residential,recipient_contact_name,recipient_company_name,recipient_phone,recipient_email,description,parcel_type,service_type,weight,weight_unit,length,width,height,dimension_unit,declared_value,currency,estimated_delivery_date");
    }

    [Fact]
    public async Task DownloadTemplateXlsx_ReturnsFile()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/parcel-imports/template.xlsx");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetAdminAccessTokenAsync());

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        var content = await response.Content.ReadAsByteArrayAsync();
        content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Upload_MixedCsv_ReturnsAccepted_AndErrorsCsvCanBeDownloaded()
    {
        var request = await CreateUploadRequestAsync(
            fileName: "parcels.csv",
            fileContent:
                """
                recipient_street1,recipient_street2,recipient_city,recipient_state,recipient_postal_code,recipient_country_code,recipient_is_residential,recipient_contact_name,recipient_company_name,recipient_phone,recipient_email,description,parcel_type,service_type,weight,weight_unit,length,width,height,dimension_unit,declared_value,currency,estimated_delivery_date
                15 George Street,,Sydney,NSW,2000,AU,true,Taylor Smith,Acme,+61000000000,taylor@example.com,Box,Package,STANDARD,2.5,KG,20,10,5,CM,100,AUD,2030-01-15
                17 Pitt Street,,Sydney,NSW,2000,AU,true,Jordan Lee,Acme,+61000000001,jordan@example.com,Box,Package,STANDARD,abc,KG,20,10,5,CM,100,AUD,2030-01-15
                """);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var importId = json.RootElement.GetProperty("importId").GetGuid();

        var downloadRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/parcel-imports/{importId}/errors.csv");
        downloadRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", await GetAdminAccessTokenAsync());

        var downloadResponse = await _client.SendAsync(downloadRequest);

        downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        downloadResponse.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");

        var report = await downloadResponse.Content.ReadAsStringAsync();
        report.Should().Contain("row_number,error_message,recipient_street1");
        report.Should().Contain("17 Pitt Street");
        report.Should().Contain("weight must be a valid number.");
    }

    [Fact]
    public async Task Upload_PersistsSourceFileInObjectStorage_AndKeepsOnlyStorageKeyInDatabase()
    {
        var request = await CreateUploadRequestAsync(
            fileName: "parcels.csv",
            fileContent:
                """
                recipient_street1,recipient_street2,recipient_city,recipient_state,recipient_postal_code,recipient_country_code,recipient_is_residential,recipient_contact_name,recipient_company_name,recipient_phone,recipient_email,description,parcel_type,service_type,weight,weight_unit,length,width,height,dimension_unit,declared_value,currency,estimated_delivery_date
                15 George Street,,Sydney,NSW,2000,AU,true,Taylor Smith,Acme,+61000000000,taylor@example.com,Box,Package,STANDARD,2.5,KG,20,10,5,CM,100,AUD,2030-01-15
                """);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var importId = document.RootElement.GetProperty("importId").GetGuid();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<InMemoryFileStorageService>();

        var parcelImport = await db.ParcelImports.FindAsync(importId);
        parcelImport.Should().NotBeNull();
        parcelImport!.SourceFileKey.Should().NotBeNullOrWhiteSpace();
        parcelImport.SourceFile.Should().BeNull();

        var keys = await fileStorage.ListKeysAsync("parcel-imports/");
        keys.Should().Contain(parcelImport.SourceFileKey!);
    }

    private async Task<HttpRequestMessage> CreateUploadRequestAsync(
        string fileName,
        string fileContent)
        => await CreateUploadRequestAsync(fileName, Encoding.UTF8.GetBytes(fileContent));

    private async Task<HttpRequestMessage> CreateUploadRequestAsync(
        string fileName,
        byte[] fileBytes)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/parcel-imports");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetAdminAccessTokenAsync());

        var content = new MultipartFormDataContent();
        content.Add(
            new StringContent(DbSeeder.TestDepotAddressId.ToString()),
            "shipperAddressId");
        content.Add(
            new ByteArrayContent(fileBytes),
            "file",
            fileName);

        request.Content = content;
        return request;
    }

    private async Task<string> GetAdminAccessTokenAsync()
    {
        var response = await _client.PostAsync(
            "/connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"] = "admin@lastmile.com",
                ["password"] = "Admin@12345",
            }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("access_token").GetString()!;
    }
}
