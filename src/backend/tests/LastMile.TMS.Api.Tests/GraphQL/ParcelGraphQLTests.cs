using System.Net;
using FluentAssertions;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using LastMile.TMS.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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

    [Fact]
    public async Task GetPreLoadParcels_ReturnsPreLoadStatuses()
    {
        var token = await GetAdminAccessTokenAsync();

        using var registerDoc = await PostGraphQLAsync(
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
                        street1 = "20 Preload Avenue",
                        city = "Cairo",
                        state = "Cairo",
                        postalCode = "11511",
                        countryCode = "EG",
                        isResidential = true,
                        contactName = "Nour Fathi",
                        phone = "+201111111111",
                        email = "nour@example.com"
                    },
                    serviceType = "STANDARD",
                    weight = 2.0,
                    weightUnit = "KG",
                    length = 20.0,
                    width = 10.0,
                    height = 5.0,
                    dimensionUnit = "CM",
                    declaredValue = 100.0,
                    currency = "USD",
                    estimatedDeliveryDate = DateTimeOffset.UtcNow.AddDays(2).ToString("o")
                }
            },
            accessToken: token);

        var registeredTracking = registerDoc.RootElement
            .GetProperty("data")
            .GetProperty("registerParcel")
            .GetProperty("trackingNumber")
            .GetString();

        using var queryDoc = await PostGraphQLAsync(
            """
            query GetPreLoadParcels {
              preLoadParcels {
                trackingNumber
                status
              }
            }
            """,
            accessToken: token);

        queryDoc.RootElement.TryGetProperty("errors", out var errors)
            .Should().BeFalse("preLoadParcels should not return errors: {0}", errors.ToString());

        var parcels = queryDoc.RootElement
            .GetProperty("data")
            .GetProperty("preLoadParcels")
            .EnumerateArray()
            .ToList();

        parcels.Should().Contain(parcel =>
            parcel.GetProperty("trackingNumber").GetString() == registeredTracking
            && parcel.GetProperty("status").GetString() == "Registered");
        parcels.Should().Contain(parcel => parcel.GetProperty("status").GetString() == "Sorted");
    }

    [Fact]
    public async Task GetPreLoadParcels_WhenSelectingZoneName_ReturnsProjectedZone()
    {
        var token = await GetAdminAccessTokenAsync();

        using var queryDoc = await PostGraphQLAsync(
            """
            query GetPreLoadParcels {
              preLoadParcels(order: [{ trackingNumber: ASC }]) {
                trackingNumber
                zoneName
              }
            }
            """,
            accessToken: token);

        queryDoc.RootElement.TryGetProperty("errors", out var errors)
            .Should().BeFalse("preLoadParcels should not return errors: {0}", errors.ToString());

        var seededParcel = queryDoc.RootElement
            .GetProperty("data")
            .GetProperty("preLoadParcels")
            .EnumerateArray()
            .First(parcel => parcel.GetProperty("trackingNumber").GetString() == "LMTESTSEED0001");

        seededParcel.GetProperty("zoneName").GetString().Should().Be("Test Zone");
    }

    [Fact]
    public async Task GetPreLoadParcelsConnection_FirstPage_ReturnsNodesPageInfoAndTotalCount()
    {
        var token = await GetAdminAccessTokenAsync();

        using var queryDoc = await PostGraphQLAsync(
            """
            query GetPreLoadParcelsConnection($first: Int!) {
              preLoadParcelsConnection(first: $first, order: [{ trackingNumber: ASC }]) {
                totalCount
                pageInfo {
                  hasNextPage
                  endCursor
                }
                nodes {
                  trackingNumber
                  zoneName
                }
              }
            }
            """,
            variables: new
            {
                first = 2
            },
            accessToken: token);

        queryDoc.RootElement.TryGetProperty("errors", out var errors)
            .Should().BeFalse("preLoadParcelsConnection should not return errors: {0}", errors.ToString());

        var connection = queryDoc.RootElement
            .GetProperty("data")
            .GetProperty("preLoadParcelsConnection");

        connection.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(2);
        connection.GetProperty("pageInfo").GetProperty("hasNextPage").GetBoolean().Should().BeTrue();
        connection.GetProperty("pageInfo").GetProperty("endCursor").GetString().Should().NotBeNullOrWhiteSpace();

        var nodes = connection.GetProperty("nodes").EnumerateArray().ToList();
        nodes.Should().HaveCount(2);
        nodes.Should().OnlyContain(node => !string.IsNullOrWhiteSpace(node.GetProperty("zoneName").GetString()));
        nodes.Select(node => node.GetProperty("trackingNumber").GetString()).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetParcelsForRouteCreation_WithVehicleAndDriverFilters_ReturnsMatchingZoneParcelsOnly()
    {
        var token = await GetAdminAccessTokenAsync();
        var otherZoneId = await SeedZoneAsync("Alternate Test Zone");
        var otherZoneParcelTrackingNumber = $"LM-ALT-{Guid.NewGuid():N}"[..18].ToUpperInvariant();
        await SeedParcelAsync(otherZoneId, otherZoneParcelTrackingNumber, ParcelStatus.Sorted);

        using var queryDoc = await PostGraphQLAsync(
            """
            query GetParcelsForRouteCreation($vehicleId: UUID!, $driverId: UUID!) {
              parcelsForRouteCreation(vehicleId: $vehicleId, driverId: $driverId) {
                trackingNumber
                zoneName
              }
            }
            """,
            variables: new
            {
                vehicleId = DbSeeder.TestVehicleId,
                driverId = DbSeeder.TestDriverId
            },
            accessToken: token);

        queryDoc.RootElement.TryGetProperty("errors", out var errors)
            .Should().BeFalse("parcelsForRouteCreation should not return errors: {0}", errors.ToString());

        var parcels = queryDoc.RootElement
            .GetProperty("data")
            .GetProperty("parcelsForRouteCreation")
            .EnumerateArray()
            .ToList();

        parcels.Should().Contain(parcel => parcel.GetProperty("trackingNumber").GetString() == "LMTESTSEED0001");
        parcels.Should().NotContain(parcel => parcel.GetProperty("trackingNumber").GetString() == otherZoneParcelTrackingNumber);
        parcels.Should().OnlyContain(parcel => parcel.GetProperty("zoneName").GetString() == "Test Zone");
    }

    [Fact]
    public async Task UpdateParcel_ValidInput_ReturnsUpdatedDetailAndHistory()
    {
        var token = await GetAdminAccessTokenAsync();

        using var registerDoc = await PostGraphQLAsync(
            """
            mutation RegisterParcel($input: RegisterParcelInput!) {
              registerParcel(input: $input) {
                id
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
                        street1 = "10 Edit Street",
                        city = "Cairo",
                        state = "Cairo",
                        postalCode = "11511",
                        countryCode = "EG",
                        isResidential = true,
                        contactName = "Edit Me",
                        phone = "+201222222222",
                        email = "edit@example.com"
                    },
                    description = "Original description",
                    serviceType = "STANDARD",
                    weight = 1.2,
                    weightUnit = "KG",
                    length = 12.0,
                    width = 10.0,
                    height = 8.0,
                    dimensionUnit = "CM",
                    declaredValue = 55.0,
                    currency = "USD",
                    estimatedDeliveryDate = DateTimeOffset.UtcNow.AddDays(2).ToString("o")
                }
            },
            accessToken: token);

        var parcelId = registerDoc.RootElement
            .GetProperty("data")
            .GetProperty("registerParcel")
            .GetProperty("id")
            .GetString();

        using var updateDoc = await PostGraphQLAsync(
            """
            mutation UpdateParcel($input: UpdateParcelInput!) {
              updateParcel(input: $input) {
                id
                description
                recipientAddress {
                  street1
                }
                changeHistory {
                  fieldName
                  beforeValue
                  afterValue
                }
              }
            }
            """,
            variables: new
            {
                input = new
                {
                    id = parcelId,
                    shipperAddressId = TestParcelShipperAddressId.ToString(),
                    recipientAddress = new
                    {
                        street1 = "11 Updated Street",
                        city = "Cairo",
                        state = "Cairo",
                        postalCode = "11511",
                        countryCode = "EG",
                        isResidential = true,
                        contactName = "Edit Me",
                        phone = "+201222222222",
                        email = "edit@example.com"
                    },
                    description = "Updated description",
                    serviceType = "STANDARD",
                    weight = 1.2,
                    weightUnit = "KG",
                    length = 12.0,
                    width = 10.0,
                    height = 8.0,
                    dimensionUnit = "CM",
                    declaredValue = 55.0,
                    currency = "USD",
                    estimatedDeliveryDate = DateTimeOffset.UtcNow.AddDays(3).ToString("o")
                }
            },
            accessToken: token);

        updateDoc.RootElement.TryGetProperty("errors", out var errors)
            .Should().BeFalse("updateParcel should not return errors: {0}", errors.ToString());

        var updatedParcel = updateDoc.RootElement
            .GetProperty("data")
            .GetProperty("updateParcel");

        updatedParcel.GetProperty("description").GetString().Should().Be("Updated description");
        updatedParcel.GetProperty("recipientAddress").GetProperty("street1").GetString().Should().Be("11 Updated Street");
        updatedParcel.GetProperty("changeHistory").EnumerateArray().Should().Contain(entry =>
            entry.GetProperty("fieldName").GetString() == "Description"
            && entry.GetProperty("beforeValue").GetString() == "Original description"
            && entry.GetProperty("afterValue").GetString() == "Updated description");
    }

    [Fact]
    public async Task CancelParcel_ValidInput_CancelsParcelAndRemovesItFromPreLoadQuery()
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
                        street1 = "18 Cancel Street",
                        city = "Cairo",
                        state = "Cairo",
                        postalCode = "11511",
                        countryCode = "EG",
                        isResidential = true,
                        contactName = "Cancel Me",
                        phone = "+201333333333",
                        email = "cancel@example.com"
                    },
                    serviceType = "STANDARD",
                    weight = 1.0,
                    weightUnit = "KG",
                    length = 10.0,
                    width = 10.0,
                    height = 10.0,
                    dimensionUnit = "CM",
                    declaredValue = 40.0,
                    currency = "USD",
                    estimatedDeliveryDate = DateTimeOffset.UtcNow.AddDays(2).ToString("o")
                }
            },
            accessToken: token);

        var registeredParcel = registerDoc.RootElement
            .GetProperty("data")
            .GetProperty("registerParcel");

        var parcelId = registeredParcel.GetProperty("id").GetString();
        var trackingNumber = registeredParcel.GetProperty("trackingNumber").GetString();

        using var cancelDoc = await PostGraphQLAsync(
            """
            mutation CancelParcel($input: CancelParcelInput!) {
              cancelParcel(input: $input) {
                status
                cancellationReason
                canCancel
              }
            }
            """,
            variables: new
            {
                input = new
                {
                    id = parcelId,
                    reason = "Customer cancelled before dispatch"
                }
            },
            accessToken: token);

        cancelDoc.RootElement.TryGetProperty("errors", out var cancelErrors)
            .Should().BeFalse("cancelParcel should not return errors: {0}", cancelErrors.ToString());

        var cancelledParcel = cancelDoc.RootElement
            .GetProperty("data")
            .GetProperty("cancelParcel");

        cancelledParcel.GetProperty("status").GetString().Should().Be("Cancelled");
        cancelledParcel.GetProperty("cancellationReason").GetString().Should().Be("Customer cancelled before dispatch");
        cancelledParcel.GetProperty("canCancel").GetBoolean().Should().BeFalse();

        using var queryDoc = await PostGraphQLAsync(
            """
            query GetPreLoadParcels {
              preLoadParcels {
                trackingNumber
              }
            }
            """,
            accessToken: token);

        queryDoc.RootElement
            .GetProperty("data")
            .GetProperty("preLoadParcels")
            .EnumerateArray()
            .Should()
            .NotContain(parcel => parcel.GetProperty("trackingNumber").GetString() == trackingNumber);

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var parcel = await dbContext.Parcels.SingleAsync(p => p.Id == Guid.Parse(parcelId!));
        parcel.CancellationReason.Should().Be("Customer cancelled before dispatch");
    }

    #endregion

    #region registeredParcels / preLoadParcels — search, filter, sort

    [Fact]
    public async Task GetPreLoadParcels_SearchByTrackingNumber_ReturnsMatchingParcel()
    {
        var token = await GetAdminAccessTokenAsync();

        using var doc = await PostGraphQLAsync(
            """
            query GetPreLoadParcels($search: String!) {
              preLoadParcels(search: $search) {
                trackingNumber
              }
            }
            """,
            variables: new { search = "LMTESTSEED0001" },
            accessToken: token);

        doc.RootElement.TryGetProperty("errors", out var errors)
            .Should().BeFalse("preLoadParcels(search) should not return errors: {0}", errors.ToString());

        var parcels = doc.RootElement
            .GetProperty("data")
            .GetProperty("preLoadParcels")
            .EnumerateArray()
            .ToList();

        parcels.Should().Contain(p =>
            p.GetProperty("trackingNumber").GetString() == "LMTESTSEED0001");
        parcels.Should().OnlyContain(p =>
            p.GetProperty("trackingNumber").GetString()!.Contains("LMTESTSEED0001"),
            "all results should match the search term");
    }

    [Fact]
    public async Task GetPreLoadParcels_FilterByStatus_ReturnsOnlyMatchingStatus()
    {
        var token = await GetAdminAccessTokenAsync();

        using var doc = await PostGraphQLAsync(
            """
            query GetPreLoadParcels($where: ParcelFilterInput) {
              preLoadParcels(where: $where) {
                trackingNumber
                status
              }
            }
            """,
            variables: new { where = new { status = new { @in = new[] { "SORTED" } } } },
            accessToken: token);

        doc.RootElement.TryGetProperty("errors", out var errors)
            .Should().BeFalse("preLoadParcels(where) should not return errors: {0}", errors.ToString());

        var parcels = doc.RootElement
            .GetProperty("data")
            .GetProperty("preLoadParcels")
            .EnumerateArray()
            .ToList();

        parcels.Should().NotBeEmpty("seeded parcels have status SORTED");
        parcels.Should().OnlyContain(p =>
            p.GetProperty("status").GetString() == "Sorted",
            "all returned parcels should have status Sorted (backend response uses PascalCase)");
    }

    [Fact]
    public async Task GetPreLoadParcels_SortByTrackingNumberAsc_ReturnsOrderedResults()
    {
        var token = await GetAdminAccessTokenAsync();

        using var doc = await PostGraphQLAsync(
            """
            query GetPreLoadParcels($order: [ParcelSortInput!]) {
              preLoadParcels(order: $order) {
                trackingNumber
              }
            }
            """,
            variables: new { order = new[] { new { trackingNumber = "ASC" } } },
            accessToken: token);

        doc.RootElement.TryGetProperty("errors", out var errors)
            .Should().BeFalse("preLoadParcels(order) should not return errors: {0}", errors.ToString());

        var parcels = doc.RootElement
            .GetProperty("data")
            .GetProperty("preLoadParcels")
            .EnumerateArray()
            .Select(p => p.GetProperty("trackingNumber").GetString())
            .ToList();

        parcels.Should().BeInAscendingOrder("results should be sorted by TrackingNumber ASC");
    }

    [Fact]
    public async Task GetPreLoadParcels_SortByTrackingNumberDesc_ReturnsOrderedResults()
    {
        var token = await GetAdminAccessTokenAsync();

        using var doc = await PostGraphQLAsync(
            """
            query GetPreLoadParcels($order: [ParcelSortInput!]) {
              preLoadParcels(order: $order) {
                trackingNumber
              }
            }
            """,
            variables: new { order = new[] { new { trackingNumber = "DESC" } } },
            accessToken: token);

        doc.RootElement.TryGetProperty("errors", out var errors)
            .Should().BeFalse("preLoadParcels(order) should not return errors: {0}", errors.ToString());

        var parcels = doc.RootElement
            .GetProperty("data")
            .GetProperty("preLoadParcels")
            .EnumerateArray()
            .Select(p => p.GetProperty("trackingNumber").GetString())
            .ToList();

        parcels.Should().BeInDescendingOrder("results should be sorted by TrackingNumber DESC");
    }

    #endregion

    #region transitionParcelStatus mutation

    [Fact]
    public async Task TransitionParcelStatus_ValidTransition_ReturnsUpdatedStatus()
    {
        var token = await GetAdminAccessTokenAsync();

        // Register a parcel first (status will be Registered)
        using var registerDoc = await PostGraphQLAsync(
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
                        street1 = "20 Transition St",
                        city = "Cairo",
                        state = "Cairo",
                        postalCode = "11511",
                        countryCode = "EG",
                        isResidential = true,
                        contactName = "Transition Test",
                        phone = "+201000000001",
                        email = "transition@example.com"
                    },
                    serviceType = "STANDARD",
                    weight = 1.0,
                    weightUnit = "KG",
                    length = 10.0,
                    width = 10.0,
                    height = 5.0,
                    dimensionUnit = "CM",
                    declaredValue = 100.0,
                    currency = "USD",
                    estimatedDeliveryDate = DateTimeOffset.UtcNow.AddDays(3).ToString("o")
                }
            },
            accessToken: token);

        registerDoc.RootElement.TryGetProperty("errors", out var registerErrors)
            .Should().BeFalse("registerParcel should not return errors");

        var parcelId = registerDoc.RootElement
            .GetProperty("data")
            .GetProperty("registerParcel")
            .GetProperty("id")
            .GetString();

        // Transition from Registered -> ReceivedAtDepot (valid transition)
        using var transitionDoc = await PostGraphQLAsync(
            """
            mutation TransitionParcelStatus($input: TransitionParcelStatusInput!) {
              transitionParcelStatus(input: $input) {
                id
                status
              }
            }
            """,
            variables: new
            {
                input = new
                {
                    parcelId = parcelId,
                    newStatus = "RECEIVED_AT_DEPOT",
                    location = "Sydney Central Depot",
                    description = "Parcel received at depot"
                }
            },
            accessToken: token);

        transitionDoc.RootElement.TryGetProperty("errors", out var transitionErrors)
            .Should().BeFalse("transitionParcelStatus should not return errors: {0}", transitionErrors.ToString());

        var result = transitionDoc.RootElement
            .GetProperty("data")
            .GetProperty("transitionParcelStatus");

        result.GetProperty("id").GetString().Should().Be(parcelId);
        result.GetProperty("status").GetString().Should().Be("ReceivedAtDepot");
    }

    [Fact]
    public async Task TransitionParcelStatus_InvalidTransition_ReturnsError()
    {
        var token = await GetAdminAccessTokenAsync();

        // Register a parcel first (status will be Registered)
        using var registerDoc = await PostGraphQLAsync(
            """
            mutation RegisterParcel($input: RegisterParcelInput!) {
              registerParcel(input: $input) {
                id
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
                        street1 = "21 Invalid Transition St",
                        city = "Cairo",
                        state = "Cairo",
                        postalCode = "11511",
                        countryCode = "EG",
                        isResidential = true,
                        contactName = "Invalid Test",
                        phone = "+201000000002",
                        email = "invalid@example.com"
                    },
                    serviceType = "STANDARD",
                    weight = 1.0,
                    weightUnit = "KG",
                    length = 10.0,
                    width = 10.0,
                    height = 5.0,
                    dimensionUnit = "CM",
                    declaredValue = 100.0,
                    currency = "USD",
                    estimatedDeliveryDate = DateTimeOffset.UtcNow.AddDays(3).ToString("o")
                }
            },
            accessToken: token);

        var parcelId = registerDoc.RootElement
            .GetProperty("data")
            .GetProperty("registerParcel")
            .GetProperty("id")
            .GetString();

        // Try to transition from Registered -> Delivered (invalid transition)
        using var transitionDoc = await PostGraphQLAsync(
            """
            mutation TransitionParcelStatus($input: TransitionParcelStatusInput!) {
              transitionParcelStatus(input: $input) {
                id
                status
              }
            }
            """,
            variables: new
            {
                input = new
                {
                    parcelId = parcelId,
                    newStatus = "DELIVERED",
                    location = "Somewhere",
                    description = "Invalid attempt"
                }
            },
            accessToken: token);

        transitionDoc.RootElement.TryGetProperty("errors", out var errors)
            .Should().BeTrue("invalid transition should return errors");

        var errorMessage = transitionDoc.RootElement
            .GetProperty("errors")[0]
            .GetProperty("message")
            .GetString();

        errorMessage.Should().Contain("Cannot transition");
    }

    [Fact]
    public async Task TransitionParcelStatus_ParcelNotFound_ReturnsError()
    {
        var token = await GetAdminAccessTokenAsync();

        var nonExistentParcelId = Guid.NewGuid();

        using var transitionDoc = await PostGraphQLAsync(
            """
            mutation TransitionParcelStatus($input: TransitionParcelStatusInput!) {
              transitionParcelStatus(input: $input) {
                id
                status
              }
            }
            """,
            variables: new
            {
                input = new
                {
                    parcelId = nonExistentParcelId.ToString(),
                    newStatus = "RECEIVED_AT_DEPOT",
                    location = "Any Depot",
                    description = "Looking for non-existent parcel"
                }
            },
            accessToken: token);

        transitionDoc.RootElement.TryGetProperty("errors", out var errors)
            .Should().BeTrue("parcel not found should return errors");

        var errorMessage = transitionDoc.RootElement
            .GetProperty("errors")[0]
            .GetProperty("message")
            .GetString();

        errorMessage!.ToLowerInvariant().Should().Contain("not found");
    }

    [Fact]
    public async Task TransitionParcelStatus_Unauthenticated_ReturnsError()
    {
        // Attempt to transition without authentication
        using var transitionDoc = await PostGraphQLAsync(
            """
            mutation TransitionParcelStatus($input: TransitionParcelStatusInput!) {
              transitionParcelStatus(input: $input) {
                id
                status
              }
            }
            """,
            variables: new
            {
                input = new
                {
                    parcelId = Guid.NewGuid().ToString(),
                    newStatus = "RECEIVED_AT_DEPOT",
                    location = "Any Depot",
                    description = "Unauthenticated attempt"
                }
            },
            accessToken: null);

        // Should have errors due to authorization failure
        transitionDoc.RootElement.TryGetProperty("errors", out var errors)
            .Should().BeTrue("unauthenticated request should return errors");

        // The exact error message format depends on the GraphQL implementation
        // but it should indicate unauthorized access
        var errorMessage = transitionDoc.RootElement
            .GetProperty("errors")[0]
            .GetProperty("message")
            .GetString();

        errorMessage.Should().NotBeEmpty();
    }

    #endregion

    #region getParcelTrackingEvents query

    [Fact]
    public async Task GetParcelTrackingEvents_AfterTransition_ReturnsTrackingEvents()
    {
        var token = await GetAdminAccessTokenAsync();

        // Register a parcel first
        using var registerDoc = await PostGraphQLAsync(
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
                        street1 = "22 Tracking Events St",
                        city = "Cairo",
                        state = "Cairo",
                        postalCode = "11511",
                        countryCode = "EG",
                        isResidential = true,
                        contactName = "Tracking Test",
                        phone = "+201000000003",
                        email = "tracking@example.com"
                    },
                    serviceType = "STANDARD",
                    weight = 1.0,
                    weightUnit = "KG",
                    length = 10.0,
                    width = 10.0,
                    height = 5.0,
                    dimensionUnit = "CM",
                    declaredValue = 100.0,
                    currency = "USD",
                    estimatedDeliveryDate = DateTimeOffset.UtcNow.AddDays(3).ToString("o")
                }
            },
            accessToken: token);

        var parcelId = registerDoc.RootElement
            .GetProperty("data")
            .GetProperty("registerParcel")
            .GetProperty("id")
            .GetString();

        // Transition to ReceivedAtDepot
        using var transition1 = await PostGraphQLAsync(
            """
            mutation TransitionParcelStatus($input: TransitionParcelStatusInput!) {
              transitionParcelStatus(input: $input) {
                status
              }
            }
            """,
            variables: new
            {
                input = new
                {
                    parcelId = parcelId,
                    newStatus = "RECEIVED_AT_DEPOT",
                    location = "Sydney Central Depot",
                    description = "First scan at depot"
                }
            },
            accessToken: token);

        transition1.RootElement.TryGetProperty("errors", out var t1Errors)
            .Should().BeFalse("first transition should succeed");

        // Transition to Sorted
        using var transition2 = await PostGraphQLAsync(
            """
            mutation TransitionParcelStatus($input: TransitionParcelStatusInput!) {
              transitionParcelStatus(input: $input) {
                status
              }
            }
            """,
            variables: new
            {
                input = new
                {
                    parcelId = parcelId,
                    newStatus = "SORTED",
                    location = "Sydney Central Depot",
                    description = "Parcel sorted"
                }
            },
            accessToken: token);

        transition2.RootElement.TryGetProperty("errors", out var t2Errors)
            .Should().BeFalse("second transition should succeed");

        // Query tracking events
        using var queryDoc = await PostGraphQLAsync(
            """
            query GetParcelTrackingEvents($parcelId: UUID!) {
              parcelTrackingEvents(parcelId: $parcelId) {
                eventType
                description
                location
                timestamp
                operator
              }
            }
            """,
            variables: new { parcelId = parcelId },
            accessToken: token);

        queryDoc.RootElement.TryGetProperty("errors", out var queryErrors)
            .Should().BeFalse("parcelTrackingEvents query should not return errors: {0}", queryErrors.ToString());

        var trackingEvents = queryDoc.RootElement
            .GetProperty("data")
            .GetProperty("parcelTrackingEvents")
            .EnumerateArray()
            .ToList();

        trackingEvents.Count.Should().BeGreaterThanOrEqualTo(2,
            "tracking events should exist for the two transitions");

        // Verify the events are in reverse chronological order (most recent first)
        var timestamps = trackingEvents
            .Select(e => e.GetProperty("timestamp").GetDateTimeOffset())
            .ToList();

        timestamps.Should().BeInDescendingOrder("events are returned most-recent-first");

        // Verify the event descriptions contain our transition descriptions
        var descriptions = trackingEvents
            .Select(e => e.GetProperty("description").GetString())
            .ToList();

        descriptions.Should().Contain(d => d == "First scan at depot");
        descriptions.Should().Contain(d => d == "Parcel sorted");
    }

    #endregion

    public Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Guid> SeedZoneAsync(string name)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var templateZone = await dbContext.Zones
            .AsNoTracking()
            .SingleAsync(zone => zone.Id == DbSeeder.TestZoneId);

        var zone = new Zone
        {
            Name = name,
            Boundary = (NetTopologySuite.Geometries.Polygon)templateZone.Boundary.Copy(),
            DepotId = DbSeeder.TestDepotId,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests"
        };

        dbContext.Zones.Add(zone);
        await dbContext.SaveChangesAsync();
        return zone.Id;
    }

    private async Task<Guid> SeedParcelAsync(Guid zoneId, string trackingNumber, ParcelStatus status)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var parcel = new Parcel
        {
            TrackingNumber = trackingNumber,
            Description = "Seeded test parcel for route option filtering",
            ServiceType = ServiceType.Standard,
            Status = status,
            ShipperAddressId = DbSeeder.TestParcelShipperAddressId,
            RecipientAddressId = DbSeeder.TestParcelRecipientAddressId,
            Weight = 2.5m,
            WeightUnit = WeightUnit.Kg,
            Length = 30m,
            Width = 20m,
            Height = 10m,
            DimensionUnit = DimensionUnit.Cm,
            DeclaredValue = 100m,
            Currency = "USD",
            EstimatedDeliveryDate = DateTimeOffset.UtcNow.AddDays(2),
            DeliveryAttempts = 0,
            ZoneId = zoneId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests"
        };

        dbContext.Parcels.Add(parcel);
        await dbContext.SaveChangesAsync();
        return parcel.Id;
    }
}
