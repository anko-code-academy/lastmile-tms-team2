using System.Net;
using System.Text;
using FluentAssertions;
using LastMile.TMS.Infrastructure.Options;
using LastMile.TMS.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace LastMile.TMS.Application.Tests;

public sealed class MapboxGeocodingServiceTests
{
    [Fact]
    public async Task GeocodeAsync_UsesConfiguredV6BaseUrlWithoutDuplicatingPath()
    {
        Uri? capturedUri = null;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            capturedUri = request.RequestUri;
            return JsonResponse(
                """
                {
                  "type": "FeatureCollection",
                  "features": [
                    {
                      "type": "Feature",
                      "geometry": {
                        "type": "Point",
                        "coordinates": [151.187102, -33.90753]
                      },
                      "properties": {
                        "feature_type": "address",
                        "full_address": "416 Sydney Park Road, Erskineville New South Wales 2015, Australia",
                        "coordinates": {
                          "accuracy": "interpolated",
                          "routable_points": [
                            {
                              "name": "default",
                              "longitude": 151.187102,
                              "latitude": -33.90753
                            }
                          ]
                        },
                        "context": {
                          "address": {
                            "address_number": "416",
                            "street_name": "Sydney Park Road"
                          }
                        }
                      }
                    }
                  ]
                }
                """);
        }));
        var options = Options.Create(new MapboxOptions
        {
            AccessToken = "pk.test-token",
            GeocodingBaseUrl = "https://api.mapbox.com/search/geocode/v6",
        });
        var service = new MapboxGeocodingService(httpClient, options);

        var point = await service.GeocodeAsync("416 Sydney Park Rd, Sydney, NSW, 2015, AU");

        point.Should().NotBeNull();
        capturedUri.Should().NotBeNull();
        capturedUri!.AbsoluteUri.Should().StartWith("https://api.mapbox.com/search/geocode/v6/forward?");
        capturedUri.AbsoluteUri.Should().NotContain("/search/geocode/v6/search/geocode/v6/");
    }

    [Fact]
    public async Task GeocodeAsync_PrefersAddressCandidateWithMatchingHouseNumberAndRoutablePoint()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ => JsonResponse(
            """
            {
              "type": "FeatureCollection",
              "features": [
                {
                  "type": "Feature",
                  "geometry": {
                    "type": "Point",
                    "coordinates": [151.180000, -33.900000]
                  },
                  "properties": {
                    "feature_type": "street",
                    "name": "Sydney Park Road",
                    "full_address": "Sydney Park Road, Sydney New South Wales 2015, Australia",
                    "coordinates": {
                      "accuracy": "street"
                    },
                    "match_code": {
                      "street": "matched",
                      "postcode": "matched",
                      "region": "matched",
                      "country": "matched",
                      "confidence": "high"
                    },
                    "context": {
                      "street": {
                        "name": "Sydney Park Road"
                      }
                    }
                  }
                },
                {
                  "type": "Feature",
                  "geometry": {
                    "type": "Point",
                    "coordinates": [151.187102, -33.90753]
                  },
                  "properties": {
                    "feature_type": "address",
                    "name": "416 Sydney Park Road",
                    "full_address": "416 Sydney Park Road, Erskineville New South Wales 2015, Australia",
                    "coordinates": {
                      "accuracy": "interpolated",
                      "routable_points": [
                        {
                          "name": "default",
                          "longitude": 151.187102,
                          "latitude": -33.90753
                        }
                      ]
                    },
                    "match_code": {
                      "address_number": "plausible",
                      "street": "matched",
                      "postcode": "matched",
                      "region": "matched",
                      "country": "matched",
                      "confidence": "low"
                    },
                    "context": {
                      "address": {
                        "address_number": "416",
                        "street_name": "Sydney Park Road"
                      }
                    }
                  }
                }
              ]
            }
            """)));
        var options = Options.Create(new MapboxOptions
        {
            AccessToken = "pk.test-token",
            GeocodingBaseUrl = "https://api.mapbox.com",
        });
        var service = new MapboxGeocodingService(httpClient, options);

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
            """
            {
              "type": "FeatureCollection",
              "features": []
            }
            """,
            """
            {
              "type": "FeatureCollection",
              "features": []
            }
            """,
            """
            {
              "type": "FeatureCollection",
              "features": [
                {
                  "type": "Feature",
                  "geometry": {
                    "type": "Point",
                    "coordinates": [151.2093, -33.8688]
                  },
                  "properties": {
                    "feature_type": "address",
                    "full_address": "123 Market Street, Sydney New South Wales 2000, Australia",
                    "coordinates": {
                      "accuracy": "rooftop",
                      "routable_points": [
                        {
                          "name": "default",
                          "longitude": 151.2093,
                          "latitude": -33.8688
                        }
                      ]
                    },
                    "context": {
                      "address": {
                        "address_number": "123",
                        "street_name": "Market Street"
                      }
                    }
                  }
                }
              ]
            }
            """,
        ]);

        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            requests.Add(request.RequestUri!);
            return JsonResponse(responses.Dequeue());
        }));
        var options = Options.Create(new MapboxOptions
        {
            AccessToken = "pk.test-token",
            GeocodingBaseUrl = "https://api.mapbox.com",
        });
        var service = new MapboxGeocodingService(httpClient, options);

        var point = await service.GeocodeAsync("123 Market St, Suite 500, Sydney, NSW, 2000, AU");

        point.Should().NotBeNull();
        requests.Should().HaveCount(3);
        Uri.UnescapeDataString(requests[0].Query).Should().Contain("123 Market St, Suite 500, Sydney, NSW, 2000, AU");
        Uri.UnescapeDataString(requests[1].Query).Should().Contain("123 Market St, Suite 500, Sydney, NSW, 2000, AU");
        Uri.UnescapeDataString(requests[1].Query).Should().NotContain("types=");
        Uri.UnescapeDataString(requests[2].Query).Should().Contain("123 Market St, Sydney, NSW, 2000, AU");
    }

    [Fact]
    public async Task GeocodeAsync_RetriesWithoutTypedFilterWhenMapboxRejectsTypedLookup()
    {
        var requests = new List<Uri>();
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            requests.Add(request.RequestUri!);

            return requests.Count == 1
                ? new HttpResponseMessage((HttpStatusCode)422)
                : JsonResponse(
                    """
                    {
                      "type": "FeatureCollection",
                      "features": [
                        {
                          "type": "Feature",
                          "geometry": {
                            "type": "Point",
                            "coordinates": [-73.691695, 42.728907]
                          },
                          "properties": {
                            "feature_type": "address",
                            "full_address": "1 House Avenue, Troy, New York 12180, United States",
                            "coordinates": {
                              "accuracy": "rooftop",
                              "routable_points": [
                                {
                                  "name": "default",
                                  "longitude": -73.691695,
                                  "latitude": 42.728907
                                }
                              ]
                            },
                            "context": {
                              "address": {
                                "address_number": "1",
                                "street_name": "House Avenue"
                              }
                            }
                          }
                        }
                      ]
                    }
                    """);
        }));
        var options = Options.Create(new MapboxOptions
        {
            AccessToken = "pk.test-token",
            GeocodingBaseUrl = "https://api.mapbox.com",
        });
        var service = new MapboxGeocodingService(httpClient, options);

        var point = await service.GeocodeAsync("1 House Avenue, Troy, NY, 12180, US");

        point.Should().NotBeNull();
        point!.X.Should().BeApproximately(-73.691695, 0.0000001);
        point.Y.Should().BeApproximately(42.728907, 0.0000001);
        requests.Should().HaveCount(2);
        Uri.UnescapeDataString(requests[0].Query).Should().Contain("types=address,street,secondary_address");
        Uri.UnescapeDataString(requests[1].Query).Should().NotContain("types=");
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
