using System.Globalization;
using System.Net;
using System.Text;
using FluentAssertions;
using LastMile.TMS.Infrastructure.Services;

namespace LastMile.TMS.Application.Tests;

public sealed class NominatimGeocodingServiceTests
{
    [Fact]
    public async Task GeocodeAsync_ParsesCoordinatesUsingInvariantCulture()
    {
        using var cultureScope = new CultureScope("ru-RU");
        using var httpClient = new HttpClient(new StubHttpMessageHandler("""
            [
              {
                "lat": "40.7033938",
                "lon": "-74.0113353",
                "display_name": "Fraunces Tavern"
              }
            ]
            """));

        var service = new NominatimGeocodingService(httpClient);

        var point = await service.GeocodeAsync("Fraunces Tavern, New York, NY, 10004, US");

        point.Should().NotBeNull();
        point!.SRID.Should().Be(4326);
        point.X.Should().BeApproximately(-74.0113353, 0.0000001);
        point.Y.Should().BeApproximately(40.7033938, 0.0000001);
    }

    private sealed class StubHttpMessageHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;
        private readonly CultureInfo _originalUiCulture = CultureInfo.CurrentUICulture;

        public CultureScope(string cultureName)
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _originalCulture;
            CultureInfo.CurrentUICulture = _originalUiCulture;
        }
    }
}
