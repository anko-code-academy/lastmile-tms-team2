using System.Net;
using System.Text;
using FluentAssertions;
using LastMile.TMS.Infrastructure.Options;
using LastMile.TMS.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace LastMile.TMS.Application.Tests;

public sealed class NominatimGeocodingServiceTests
{
    [Fact]
    public async Task GeocodeAsync_UsesConfiguredSearchBaseUrlWithoutDuplicatingPath()
    {
        HttpRequestMessage? capturedRequest = null;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return JsonResponse(
                """
                [
                  {
                    "place_id": 1,
                    "lat": "-33.90753",
                    "lon": "151.187102",
                    "display_name": "416 Sydney Park Road, Erskineville, Sydney, New South Wales, 2015, Australia",
                    "category": "building",
                    "type": "house",
                    "place_rank": 30,
                    "importance": 0.8,
                    "address": {
                      "house_number": "416",
                      "road": "Sydney Park Road",
                      "postcode": "2015",
                      "country_code": "au"
                    }
                  }
                ]
                """);
        }));
        var options = Options.Create(new NominatimOptions
        {
            BaseUrl = "https://nominatim.openstreetmap.org/search",
            UserAgent = "LastMile.TMS.Tests/1.0",
        });
        var service = new NominatimGeocodingService(httpClient, options);

        var point = await service.GeocodeAsync("416 Sydney Park Rd, Sydney, NSW, 2015, AU");

        point.Should().NotBeNull();
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri.Should().NotBeNull();
        capturedRequest.RequestUri!.AbsoluteUri.Should().StartWith("https://nominatim.openstreetmap.org/search?");
        capturedRequest.RequestUri.AbsoluteUri.Should().NotContain("/search/search?");
        Uri.UnescapeDataString(capturedRequest.RequestUri.Query).Should().Contain("countrycodes=au");
        capturedRequest.Headers.UserAgent.ToString().Should().Be("LastMile.TMS.Tests/1.0");
    }

    [Fact]
    public async Task GeocodeAsync_PrefersAddressCandidateWithMatchingHouseNumber()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ => JsonResponse(
            """
            [
              {
                "place_id": 1,
                "lat": "-33.900000",
                "lon": "151.180000",
                "display_name": "Sydney Park Road, Sydney, New South Wales, 2015, Australia",
                "category": "highway",
                "type": "residential",
                "place_rank": 26,
                "importance": 0.7,
                "address": {
                  "road": "Sydney Park Road",
                  "postcode": "2015",
                  "country_code": "au"
                }
              },
              {
                "place_id": 2,
                "lat": "-33.90753",
                "lon": "151.187102",
                "display_name": "416 Sydney Park Road, Erskineville, Sydney, New South Wales, 2015, Australia",
                "category": "building",
                "type": "house",
                "place_rank": 30,
                "importance": 0.4,
                "address": {
                  "house_number": "416",
                  "road": "Sydney Park Road",
                  "postcode": "2015",
                  "country_code": "au"
                }
              }
            ]
            """)));
        var options = Options.Create(new NominatimOptions
        {
            BaseUrl = "https://nominatim.openstreetmap.org",
            UserAgent = "LastMile.TMS.Tests/1.0",
        });
        var service = new NominatimGeocodingService(httpClient, options);

        var point = await service.GeocodeAsync("416 Sydney Park Rd, Sydney, NSW, 2015, AU");

        point.Should().NotBeNull();
        point!.X.Should().BeApproximately(151.187102, 0.0000001);
        point.Y.Should().BeApproximately(-33.90753, 0.0000001);
    }

    [Fact]
    public async Task GeocodeAsync_RetriesWithoutUnitDetailsWhenFirstLookupReturnsNoResults()
    {
        var requests = new List<Uri>();
        var responses = new Queue<string>(
        [
            "[]",
            """
            [
              {
                "place_id": 1,
                "lat": "-33.8688",
                "lon": "151.2093",
                "display_name": "123 Market Street, Sydney, New South Wales, 2000, Australia",
                "category": "building",
                "type": "house",
                "place_rank": 30,
                "importance": 0.7,
                "address": {
                  "house_number": "123",
                  "road": "Market Street",
                  "postcode": "2000",
                  "country_code": "au"
                }
              }
            ]
            """,
        ]);

        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            requests.Add(request.RequestUri!);
            return JsonResponse(responses.Dequeue());
        }));
        var options = Options.Create(new NominatimOptions
        {
            BaseUrl = "https://nominatim.openstreetmap.org",
            UserAgent = "LastMile.TMS.Tests/1.0",
        });
        var service = new NominatimGeocodingService(httpClient, options);

        var point = await service.GeocodeAsync("123 Market St, Suite 500, Sydney, NSW, 2000, AU");

        point.Should().NotBeNull();
        requests.Should().HaveCount(2);
        Uri.UnescapeDataString(requests[0].Query).Should().Contain("123 Market St, Suite 500, Sydney, NSW, 2000, AU");
        Uri.UnescapeDataString(requests[1].Query).Should().Contain("123 Market St, Sydney, NSW, 2000, AU");
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }
}
