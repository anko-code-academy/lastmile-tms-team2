using System.Net;
using FluentAssertions;
using LastMile.TMS.Persistence;

namespace LastMile.TMS.Api.Tests.GraphQL;

[Collection(ApiTestCollection.Name)]
public class ParcelGraphQLTests(CustomWebApplicationFactory factory)
    : GraphQLTestBase(factory), IAsyncLifetime
{
    // DbSeeder ships these as known-good test addresses
    private static readonly Guid TestParcelShipperAddressId =
        new("00000000-0000-0000-0000-000000000004");

    #region registerParcel mutation

    [Fact]
    public async Task RegisterParcel_ValidInput_ReturnsStatusRegistered()
    {
        var token = await GetAdminAccessTokenAsync();

        using var document = await PostGraphQLAsync(
            """
            mutation RegisterParcel($input: RegisterParcelInput!) {
              registerParcel(input: $input) {
                id
                trackingNumber
                status
              }
            }
            """,
            variables: new
            {
                input = new
                {
                    shipperAddressId = TestParcelShipperAddressId.ToString(),
                    recipientAddress = new
                    {
                        street1 = "15 El Tahrir St",
                        city = "Cairo",
                        state = "Cairo",
                        postalCode = "11511",
                        countryCode = "EG",
                        isResidential = true,
                        contactName = "Omar Farouk",
                        phone = "+201234567890",
                        email = "omar@example.com"
                    },
                    description = "Test electronics shipment",
                    serviceType = "STANDARD",
                    weight = 1.5,
                    weightUnit = "KG",
                    length = 20.0,
                    width = 15.0,
                    height = 10.0,
                    dimensionUnit = "CM",
                    declaredValue = 200.0,
                    currency = "USD",
                    estimatedDeliveryDate = DateTimeOffset.UtcNow.AddDays(3).ToString("o")
                }
            },
            accessToken: token);

        document.RootElement.TryGetProperty("errors", out var errors)
            .Should().BeFalse("GraphQL should not return errors: {0}", errors.ToString());

        var result = document.RootElement
            .GetProperty("data")
            .GetProperty("registerParcel");

        result.GetProperty("status").GetString().Should().Be("Registered");
        result.GetProperty("trackingNumber").GetString().Should().StartWith("LM");
        result.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RegisterParcel_BarcodeEqualsTrackingNumber()
    {
        var token = await GetAdminAccessTokenAsync();

        using var document = await PostGraphQLAsync(
            """
            mutation RegisterParcel($input: RegisterParcelInput!) {
              registerParcel(input: $input) {
                trackingNumber
                barcode
              }
            }
            """,
            variables: new
            {
                input = new
                {
                    shipperAddressId = TestParcelShipperAddressId.ToString(),
                    recipientAddress = new
                    {
                        street1 = "15 El Tahrir St",
                        city = "Cairo",
                        state = "Cairo",
                        postalCode = "11511",
                        countryCode = "EG",
                        isResidential = true,
                        contactName = "Omar Farouk",
                        phone = "+201234567890",
                        email = "omar@example.com"
                    },
                    serviceType = "EXPRESS",
                    weight = 0.5,
                    weightUnit = "KG",
                    length = 10.0,
                    width = 10.0,
                    height = 5.0,
                    dimensionUnit = "CM",
                    declaredValue = 50.0,
                    currency = "USD",
                    estimatedDeliveryDate = DateTimeOffset.UtcNow.AddDays(1).ToString("o")
                }
            },
            accessToken: token);

        document.RootElement.TryGetProperty("errors", out var errors)
            .Should().BeFalse("GraphQL should not return errors: {0}", errors.ToString());

        var result = document.RootElement
            .GetProperty("data")
            .GetProperty("registerParcel");

        var tracking = result.GetProperty("trackingNumber").GetString();
        var barcode = result.GetProperty("barcode").GetString();

        barcode.Should().Be(tracking, "barcode must equal trackingNumber per AC #2");
    }

    [Fact]
    public async Task RegisterParcel_TrackingNumberHasCorrectPrefixAndLength()
    {
        var token = await GetAdminAccessTokenAsync();

        using var document = await PostGraphQLAsync(
            """
            mutation RegisterParcel($input: RegisterParcelInput!) {
              registerParcel(input: $input) {
                trackingNumber
              }
            }
            """,
            variables: new
            {
                input = new
                {
                    shipperAddressId = TestParcelShipperAddressId.ToString(),
                    recipientAddress = new
                    {
                        street1 = "15 El Tahrir St",
                        city = "Cairo",
                        state = "Cairo",
                        postalCode = "11511",
                        countryCode = "EG",
                        isResidential = true,
                        contactName = "Omar Farouk",
                        phone = "+201234567890",
                        email = "omar@example.com"
                    },
                    serviceType = "OVERNIGHT",
                    weight = 3.0,
                    weightUnit = "KG",
                    length = 30.0,
                    width = 20.0,
                    height = 15.0,
                    dimensionUnit = "CM",
                    declaredValue = 500.0,
                    currency = "USD",
                    estimatedDeliveryDate = DateTimeOffset.UtcNow.AddDays(1).ToString("o")
                }
            },
            accessToken: token);

        document.RootElement.TryGetProperty("errors", out var errors)
            .Should().BeFalse("GraphQL should not return errors: {0}", errors.ToString());

        var tracking = document.RootElement
            .GetProperty("data")
            .GetProperty("registerParcel")
            .GetProperty("trackingNumber")
            .GetString();

        tracking.Should().StartWith("LM");
        tracking.Length.Should().Be(18, "tracking number must be 18 characters per Parcel.GenerateTrackingNumber");
    }

    #endregion

    #region registeredParcels query

    [Fact]
    public async Task GetRegisteredParcels_AfterRegisteringParcel_ReturnsNewParcel()
    {
        var token = await GetAdminAccessTokenAsync();

        // Register a parcel first (status must be Registered)
        using var registerDoc = await PostGraphQLAsync(
            """
            mutation RegisterParcel($input: RegisterParcelInput!) {
              registerParcel(input: $input) {
                id
                trackingNumber
              }
            }
            """,
            variables: new
            {
                input = new
                {
                    shipperAddressId = TestParcelShipperAddressId.ToString(),
                    recipientAddress = new
                    {
                        street1 = "5 Dokki St",
                        city = "Giza",
                        state = "Giza",
                        postalCode = "12612",
                        countryCode = "EG",
                        isResidential = false,
                        contactName = "Faris Hassan",
                        phone = "+20111222333",
                        email = "faris@example.com"
                    },
                    serviceType = "STANDARD",
                    weight = 2.0,
                    weightUnit = "KG",
                    length = 25.0,
                    width = 20.0,
                    height = 15.0,
                    dimensionUnit = "CM",
                    declaredValue = 300.0,
                    currency = "USD",
                    estimatedDeliveryDate = DateTimeOffset.UtcNow.AddDays(4).ToString("o")
                }
            },
            accessToken: token);

        registerDoc.RootElement.TryGetProperty("errors", out var registerErrors)
            .Should().BeFalse("registerParcel should not return errors");

        var registeredTracking = registerDoc.RootElement
            .GetProperty("data")
            .GetProperty("registerParcel")
            .GetProperty("trackingNumber")
            .GetString();

        // Query the intake queue — seeded parcels have Status=Sorted so only the newly
        // registered parcel (Status=Registered) should appear.
        using var queryDoc = await PostGraphQLAsync(
            """
            query GetRegisteredParcels {
              registeredParcels {
                trackingNumber
                status
                serviceType
                weight
                weightUnit
              }
            }
            """,
            accessToken: token);

        queryDoc.RootElement.TryGetProperty("errors", out var queryErrors)
            .Should().BeFalse("registeredParcels should not return errors");

        var parcels = queryDoc.RootElement
            .GetProperty("data")
            .GetProperty("registeredParcels")
            .EnumerateArray()
            .ToList();

        parcels.Should().NotBeEmpty("at least the registered parcel should appear in the queue");

        var registeredParcel = parcels.FirstOrDefault(p =>
            p.GetProperty("trackingNumber").GetString() == registeredTracking);

        registeredParcel.ValueKind.Should().NotBe(default, "the just-registered parcel should be in registeredParcels results");
        registeredParcel.GetProperty("status").GetString().Should().Be("Registered");
    }

    [Fact]
    public async Task GetRegisteredParcels_OnlyReturnsRegisteredStatus()
    {
        var token = await GetAdminAccessTokenAsync();

        // DbSeeder seeds 9 parcels with Status=Sorted — they should NOT appear here.
        using var document = await PostGraphQLAsync(
            """
            query GetRegisteredParcels {
              registeredParcels {
                trackingNumber
                status
              }
            }
            """,
            accessToken: token);

        document.RootElement.TryGetProperty("errors", out var errors)
            .Should().BeFalse("GraphQL should not return errors: {0}", errors.ToString());

        var parcels = document.RootElement
            .GetProperty("data")
            .GetProperty("registeredParcels")
            .EnumerateArray()
            .ToList();

        foreach (var parcel in parcels)
        {
            parcel.GetProperty("status").GetString().Should().Be("Registered",
                "only parcels with status Registered should be returned by registeredParcels");
        }
    }

    [Fact]
    public async Task GetParcel_AfterRegisteringParcel_ReturnsParcelDetail()
    {
        var token = await GetAdminAccessTokenAsync();

        using var registerDoc = await PostGraphQLAsync(
            """
            mutation RegisterParcel($input: RegisterParcelInput!) {
              registerParcel(input: $input) {
                id
                trackingNumber
              }
            }
            """,
            variables: new
            {
                input = new
                {
                    shipperAddressId = TestParcelShipperAddressId.ToString(),
                    recipientAddress = new
                    {
                        street1 = "42 Parcel Detail Ave",
                        city = "Cairo",
                        state = "Cairo",
                        postalCode = "11511",
                        countryCode = "EG",
                        isResidential = true,
                        contactName = "Mona Saleh",
                        phone = "+201000000000",
                        email = "mona@example.com"
                    },
                    description = "Parcel detail test",
                    parcelType = "Box",
                    serviceType = "STANDARD",
                    weight = 1.75,
                    weightUnit = "KG",
                    length = 20.0,
                    width = 15.0,
                    height = 10.0,
                    dimensionUnit = "CM",
                    declaredValue = 200.0,
                    currency = "USD",
                    estimatedDeliveryDate = DateTimeOffset.UtcNow.AddDays(3).ToString("o")
                }
            },
            accessToken: token);

        var registeredParcel = registerDoc.RootElement
            .GetProperty("data")
            .GetProperty("registerParcel");

        var parcelId = registeredParcel.GetProperty("id").GetString();
        var trackingNumber = registeredParcel.GetProperty("trackingNumber").GetString();

        using var parcelDoc = await PostGraphQLAsync(
            """
            query GetParcel($id: UUID!) {
              parcel(id: $id) {
                id
                trackingNumber
                parcelType
                zoneName
                recipientAddress {
                  contactName
                  street1
                  city
                  postalCode
                }
              }
            }
            """,
            variables: new { id = parcelId },
            accessToken: token);

        parcelDoc.RootElement.TryGetProperty("errors", out var errors)
            .Should().BeFalse("parcel query should not return errors: {0}", errors.ToString());

        var parcel = parcelDoc.RootElement
            .GetProperty("data")
            .GetProperty("parcel");

        parcel.GetProperty("id").GetString().Should().Be(parcelId);
        parcel.GetProperty("trackingNumber").GetString().Should().Be(trackingNumber);
        parcel.GetProperty("parcelType").GetString().Should().Be("Box");
        parcel.GetProperty("zoneName").GetString().Should().NotBeNullOrEmpty();
        parcel.GetProperty("recipientAddress").GetProperty("contactName").GetString().Should().Be("Mona Saleh");
        parcel.GetProperty("recipientAddress").GetProperty("street1").GetString().Should().Be("42 Parcel Detail Ave");
    }

    #endregion

    public Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}
