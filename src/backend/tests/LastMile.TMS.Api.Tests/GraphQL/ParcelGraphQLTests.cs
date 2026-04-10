using System.Net;
using System.Text.Json;
using FluentAssertions;
using LastMile.TMS.Domain.Entities;
using LastMile.TMS.Domain.Enums;
using LastMile.TMS.Persistence;
using Microsoft.AspNetCore.Identity;
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

    #region parcelByTrackingNumber query

    [Fact]
    public async Task GetParcelByTrackingNumber_ReturnsAggregateDetailAndMatchesLegacyIdQuery()
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
                        street1 = "77 Aggregate Detail Street",
                        city = "Sydney",
                        state = "NSW",
                        postalCode = "2000",
                        countryCode = "AU",
                        isResidential = true,
                        contactName = "Aggregate Test",
                        phone = "+61000000001",
                        email = "aggregate@example.com"
                    },
                    description = "Aggregate parcel",
                    parcelType = "Box",
                    serviceType = "STANDARD",
                    weight = 1.75,
                    weightUnit = "KG",
                    length = 20.0,
                    width = 10.0,
                    height = 5.0,
                    dimensionUnit = "CM",
                    declaredValue = 150.0,
                    currency = "AUD",
                    estimatedDeliveryDate = DateTimeOffset.UtcNow.AddDays(3).ToString("o")
                }
            },
            accessToken: token);

        var parcelId = registerDoc.RootElement
            .GetProperty("data")
            .GetProperty("registerParcel")
            .GetProperty("id")
            .GetString()!;

        var trackingNumber = registerDoc.RootElement
            .GetProperty("data")
            .GetProperty("registerParcel")
            .GetProperty("trackingNumber")
            .GetString()!;

        await SeedRouteAndProofOfDeliveryAsync(Guid.Parse(parcelId));

        await TransitionParcelStatusAsync(parcelId, token, "RECEIVED_AT_DEPOT", "Sydney Depot", "Received");
        await TransitionParcelStatusAsync(parcelId, token, "SORTED", "Sydney Depot", "Sorted");
        await TransitionParcelStatusAsync(parcelId, token, "STAGED", "Sydney Depot", "Staged");
        await TransitionParcelStatusAsync(parcelId, token, "LOADED", "Sydney Dock", "Loaded");
        await TransitionParcelStatusAsync(parcelId, token, "OUT_FOR_DELIVERY", "Sydney Dock", "Out on route");

        using var aggregateDoc = await PostGraphQLAsync(
            """
            query GetParcelByTrackingNumber($trackingNumber: String!) {
              parcelByTrackingNumber(trackingNumber: $trackingNumber) {
                id
                trackingNumber
                senderAddress {
                  street1
                  city
                  postalCode
                }
                recipientAddress {
                  contactName
                  street1
                }
                statusTimeline {
                  eventType
                  timestamp
                  location
                  operator
                }
                routeAssignment {
                  routeId
                  routeStatus
                  driverName
                  vehiclePlate
                }
                proofOfDelivery {
                  receivedBy
                  deliveryLocation
                  deliveredAt
                  hasSignatureImage
                  hasPhoto
                }
              }
            }
            """,
            variables: new { trackingNumber },
            accessToken: token);

        aggregateDoc.RootElement.TryGetProperty("errors", out var aggregateErrors)
            .Should().BeFalse("parcelByTrackingNumber should not return errors: {0}", aggregateErrors.ToString());

        var aggregateParcel = aggregateDoc.RootElement
            .GetProperty("data")
            .GetProperty("parcelByTrackingNumber");

        aggregateParcel.GetProperty("id").GetString().Should().Be(parcelId);
        aggregateParcel.GetProperty("trackingNumber").GetString().Should().Be(trackingNumber);
        aggregateParcel.GetProperty("senderAddress").GetProperty("street1").GetString().Should().Be("388 George Street");
        aggregateParcel.GetProperty("recipientAddress").GetProperty("contactName").GetString().Should().Be("Aggregate Test");
        aggregateParcel.GetProperty("statusTimeline").EnumerateArray().Should().NotBeEmpty();
        aggregateParcel.GetProperty("routeAssignment").GetProperty("driverName").GetString().Should().Be("Test Driver");
        aggregateParcel.GetProperty("proofOfDelivery").GetProperty("receivedBy").GetString().Should().Be("Front Desk");
        aggregateParcel.GetProperty("proofOfDelivery").GetProperty("hasSignatureImage").GetBoolean().Should().BeTrue();

        using var legacyDoc = await PostGraphQLAsync(
            """
            query GetParcel($id: UUID!) {
              parcel(id: $id) {
                id
                trackingNumber
                senderAddress {
                  street1
                }
                routeAssignment {
                  routeId
                }
                proofOfDelivery {
                  receivedBy
                }
              }
            }
            """,
            variables: new { id = parcelId },
            accessToken: token);

        legacyDoc.RootElement.TryGetProperty("errors", out var legacyErrors)
            .Should().BeFalse("legacy parcel query should not return errors: {0}", legacyErrors.ToString());

        var legacyParcel = legacyDoc.RootElement
            .GetProperty("data")
            .GetProperty("parcel");

        legacyParcel.GetProperty("id").GetString().Should().Be(aggregateParcel.GetProperty("id").GetString());
        legacyParcel.GetProperty("trackingNumber").GetString().Should().Be(trackingNumber);
        legacyParcel.GetProperty("senderAddress").GetProperty("street1").GetString().Should().Be(
            aggregateParcel.GetProperty("senderAddress").GetProperty("street1").GetString());
        legacyParcel.GetProperty("proofOfDelivery").GetProperty("receivedBy").GetString().Should().Be("Front Desk");
    }

    [Fact]
    public async Task GetParcelByTrackingNumber_WhenRouteAndPodAreMissing_ReturnsNullOptionalFields()
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
                        street1 = "88 Nullable Lane",
                        city = "Sydney",
                        state = "NSW",
                        postalCode = "2000",
                        countryCode = "AU",
                        isResidential = true,
                        contactName = "Nullable Test",
                        phone = "+61000000002",
                        email = "nullable@example.com"
                    },
                    serviceType = "STANDARD",
                    weight = 1.25,
                    weightUnit = "KG",
                    length = 20.0,
                    width = 10.0,
                    height = 5.0,
                    dimensionUnit = "CM",
                    declaredValue = 75.0,
                    currency = "AUD",
                    estimatedDeliveryDate = DateTimeOffset.UtcNow.AddDays(4).ToString("o")
                }
            },
            accessToken: token);

        var trackingNumber = registerDoc.RootElement
            .GetProperty("data")
            .GetProperty("registerParcel")
            .GetProperty("trackingNumber")
            .GetString()!;

        using var aggregateDoc = await PostGraphQLAsync(
            """
            query GetParcelByTrackingNumber($trackingNumber: String!) {
              parcelByTrackingNumber(trackingNumber: $trackingNumber) {
                trackingNumber
                routeAssignment {
                  routeId
                }
                proofOfDelivery {
                  receivedBy
                }
              }
            }
            """,
            variables: new { trackingNumber },
            accessToken: token);

        aggregateDoc.RootElement.TryGetProperty("errors", out var errors)
            .Should().BeFalse("parcelByTrackingNumber should not return errors: {0}", errors.ToString());

        var parcel = aggregateDoc.RootElement
            .GetProperty("data")
            .GetProperty("parcelByTrackingNumber");

        parcel.GetProperty("trackingNumber").GetString().Should().Be(trackingNumber);
        parcel.GetProperty("routeAssignment").ValueKind.Should().Be(JsonValueKind.Null);
        parcel.GetProperty("proofOfDelivery").ValueKind.Should().Be(JsonValueKind.Null);
    }

    #endregion

    #region route staging

    [Fact]
    public async Task GetStagingRoutes_ForWarehouseOperator_ReturnsOnlyActiveRoutesWithCounts()
    {
        var operatorEmail = await SeedWarehouseOperatorAsync();
        var plannedSortedParcelId = await SeedParcelAsync(DbSeeder.TestZoneId, "LMSTAGEAPI0001", ParcelStatus.Sorted);
        var plannedStagedParcelId = await SeedParcelAsync(DbSeeder.TestZoneId, "LMSTAGEAPI0002", ParcelStatus.Staged);
        var completedParcelId = await SeedParcelAsync(DbSeeder.TestZoneId, "LMSTAGEAPI0003", ParcelStatus.Sorted);

        var activeRouteId = await SeedRouteAsync(
            DbSeeder.TestVehicleId,
            DbSeeder.TestDriverId,
            RouteStatus.Draft,
            StagingArea.A,
            plannedSortedParcelId,
            plannedStagedParcelId);
        await SeedRouteAsync(
            DbSeeder.TestVehicleId,
            DbSeeder.TestDriver2Id,
            RouteStatus.Completed,
            StagingArea.B,
            completedParcelId);

        var token = await GetAccessTokenAsync(operatorEmail, "Warehouse@12345");

        using var document = await PostGraphQLAsync(
            """
            query GetStagingRoutes {
              stagingRoutes {
                id
                stagingArea
                status
                expectedParcelCount
                stagedParcelCount
                remainingParcelCount
              }
            }
            """,
            accessToken: token);

        document.RootElement.TryGetProperty("errors", out var errors)
            .Should().BeFalse("stagingRoutes should not return errors: {0}", errors.ToString());

        var routes = document.RootElement
            .GetProperty("data")
            .GetProperty("stagingRoutes")
            .EnumerateArray()
            .ToList();

        routes.Should().ContainSingle(route => route.GetProperty("id").GetString() == activeRouteId.ToString());

        var route = routes.Single();
        route.GetProperty("stagingArea").GetString().Should().Be("A");
        route.GetProperty("status").GetString().Should().Be("DRAFT");
        route.GetProperty("expectedParcelCount").GetInt32().Should().Be(2);
        route.GetProperty("stagedParcelCount").GetInt32().Should().Be(1);
        route.GetProperty("remainingParcelCount").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task StageParcelForRoute_ForExpectedSortedParcel_TransitionsToStagedAndReturnsUpdatedBoard()
    {
        var operatorEmail = await SeedWarehouseOperatorAsync();
        var parcelId = await SeedParcelAsync(DbSeeder.TestZoneId, "LMSTAGEAPI0101", ParcelStatus.Sorted);
        var routeId = await SeedRouteAsync(
            DbSeeder.TestVehicleId,
            DbSeeder.TestDriverId,
            RouteStatus.Draft,
            StagingArea.A,
            parcelId);

        var token = await GetAccessTokenAsync(operatorEmail, "Warehouse@12345");

        using var document = await PostGraphQLAsync(
            """
            mutation StageParcelForRoute($input: StageParcelForRouteInput!) {
              stageParcelForRoute(input: $input) {
                outcome
                trackingNumber
                message
                board {
                  id
                  stagingArea
                  expectedParcelCount
                  stagedParcelCount
                  remainingParcelCount
                  expectedParcels {
                    parcelId
                    trackingNumber
                    status
                    isStaged
                  }
                }
              }
            }
            """,
            variables: new
            {
                input = new
                {
                    routeId,
                    barcode = "LMSTAGEAPI0101",
                }
            },
            accessToken: token);

        document.RootElement.TryGetProperty("errors", out var errors)
            .Should().BeFalse("stageParcelForRoute should not return errors: {0}", errors.ToString());

        var result = document.RootElement
            .GetProperty("data")
            .GetProperty("stageParcelForRoute");

        result.GetProperty("outcome").GetString().Should().Be("STAGED");
        result.GetProperty("trackingNumber").GetString().Should().Be("LMSTAGEAPI0101");
        result.GetProperty("board").GetProperty("id").GetString().Should().Be(routeId.ToString());
        result.GetProperty("board").GetProperty("stagingArea").GetString().Should().Be("A");
        result.GetProperty("board").GetProperty("expectedParcelCount").GetInt32().Should().Be(1);
        result.GetProperty("board").GetProperty("stagedParcelCount").GetInt32().Should().Be(1);
        result.GetProperty("board").GetProperty("remainingParcelCount").GetInt32().Should().Be(0);
        result.GetProperty("board").GetProperty("expectedParcels").EnumerateArray()
            .Should().Contain(entry =>
                entry.GetProperty("parcelId").GetString() == parcelId.ToString()
                && entry.GetProperty("trackingNumber").GetString() == "LMSTAGEAPI0101"
                && entry.GetProperty("status").GetString() == "Staged"
                && entry.GetProperty("isStaged").GetBoolean());

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var parcel = await dbContext.Parcels
            .Include(candidate => candidate.TrackingEvents)
            .SingleAsync(candidate => candidate.Id == parcelId);

        parcel.Status.Should().Be(ParcelStatus.Staged);
        parcel.TrackingEvents.Should().ContainSingle();
        parcel.TrackingEvents.Single().Description.Should().Contain(routeId.ToString());
        parcel.TrackingEvents.Single().Description.Should().Contain("A");
    }

    [Fact]
    public async Task StageParcelForRoute_ForAlreadyStagedParcel_ReturnsAlreadyStagedWithoutDuplicateTrackingEvent()
    {
        var operatorEmail = await SeedWarehouseOperatorAsync();
        var parcelId = await SeedParcelAsync(DbSeeder.TestZoneId, "LMSTAGEAPI0201", ParcelStatus.Staged);
        var routeId = await SeedRouteAsync(
            DbSeeder.TestVehicleId,
            DbSeeder.TestDriverId,
            RouteStatus.Draft,
            StagingArea.A,
            parcelId);

        var token = await GetAccessTokenAsync(operatorEmail, "Warehouse@12345");

        using var document = await PostGraphQLAsync(
            """
            mutation StageParcelForRoute($input: StageParcelForRouteInput!) {
              stageParcelForRoute(input: $input) {
                outcome
                board {
                  stagedParcelCount
                  remainingParcelCount
                }
              }
            }
            """,
            variables: new
            {
                input = new
                {
                    routeId,
                    barcode = "LMSTAGEAPI0201",
                }
            },
            accessToken: token);

        document.RootElement.TryGetProperty("errors", out var errors)
            .Should().BeFalse("stageParcelForRoute should not return errors: {0}", errors.ToString());

        var result = document.RootElement
            .GetProperty("data")
            .GetProperty("stageParcelForRoute");

        result.GetProperty("outcome").GetString().Should().Be("ALREADY_STAGED");
        result.GetProperty("board").GetProperty("stagedParcelCount").GetInt32().Should().Be(1);
        result.GetProperty("board").GetProperty("remainingParcelCount").GetInt32().Should().Be(0);

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var parcel = await dbContext.Parcels
            .Include(candidate => candidate.TrackingEvents)
            .SingleAsync(candidate => candidate.Id == parcelId);

        parcel.Status.Should().Be(ParcelStatus.Staged);
        parcel.TrackingEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task StageParcelForRoute_ForParcelAssignedToDifferentRoute_ReturnsWrongRouteWithoutMutation()
    {
        var operatorEmail = await SeedWarehouseOperatorAsync();
        var selectedRouteParcelId = await SeedParcelAsync(DbSeeder.TestZoneId, "LMSTAGEAPI0301", ParcelStatus.Sorted);
        var conflictingParcelId = await SeedParcelAsync(DbSeeder.TestZoneId, "LMSTAGEAPI0302", ParcelStatus.Sorted);
        var selectedRouteId = await SeedRouteAsync(
            DbSeeder.TestVehicleId,
            DbSeeder.TestDriverId,
            RouteStatus.Draft,
            StagingArea.A,
            selectedRouteParcelId);
        var conflictingRouteId = await SeedRouteAsync(
            DbSeeder.TestVehicleId,
            DbSeeder.TestDriver2Id,
            RouteStatus.Draft,
            StagingArea.B,
            conflictingParcelId);

        var token = await GetAccessTokenAsync(operatorEmail, "Warehouse@12345");

        using var document = await PostGraphQLAsync(
            """
            mutation StageParcelForRoute($input: StageParcelForRouteInput!) {
              stageParcelForRoute(input: $input) {
                outcome
                trackingNumber
                conflictingRouteId
                conflictingStagingArea
                board {
                  expectedParcelCount
                  stagedParcelCount
                  remainingParcelCount
                }
              }
            }
            """,
            variables: new
            {
                input = new
                {
                    routeId = selectedRouteId,
                    barcode = "LMSTAGEAPI0302",
                }
            },
            accessToken: token);

        document.RootElement.TryGetProperty("errors", out var errors)
            .Should().BeFalse("stageParcelForRoute should not return errors: {0}", errors.ToString());

        var result = document.RootElement
            .GetProperty("data")
            .GetProperty("stageParcelForRoute");

        result.GetProperty("outcome").GetString().Should().Be("WRONG_ROUTE");
        result.GetProperty("trackingNumber").GetString().Should().Be("LMSTAGEAPI0302");
        result.GetProperty("conflictingRouteId").GetString().Should().Be(conflictingRouteId.ToString());
        result.GetProperty("conflictingStagingArea").GetString().Should().Be("B");
        result.GetProperty("board").GetProperty("expectedParcelCount").GetInt32().Should().Be(1);
        result.GetProperty("board").GetProperty("stagedParcelCount").GetInt32().Should().Be(0);
        result.GetProperty("board").GetProperty("remainingParcelCount").GetInt32().Should().Be(1);

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var conflictingParcel = await dbContext.Parcels.FindAsync(conflictingParcelId);
        conflictingParcel.Should().NotBeNull();
        conflictingParcel!.Status.Should().Be(ParcelStatus.Sorted);
    }

    [Fact]
    public async Task StageParcelForRoute_ForUnassignedParcel_ReturnsNotExpectedWithoutMutation()
    {
        var operatorEmail = await SeedWarehouseOperatorAsync();
        var expectedParcelId = await SeedParcelAsync(DbSeeder.TestZoneId, "LMSTAGEAPI0401", ParcelStatus.Sorted);
        var unassignedParcelId = await SeedParcelAsync(DbSeeder.TestZoneId, "LMSTAGEAPI0402", ParcelStatus.Sorted);
        var routeId = await SeedRouteAsync(
            DbSeeder.TestVehicleId,
            DbSeeder.TestDriverId,
            RouteStatus.Draft,
            StagingArea.A,
            expectedParcelId);

        var token = await GetAccessTokenAsync(operatorEmail, "Warehouse@12345");

        using var document = await PostGraphQLAsync(
            """
            mutation StageParcelForRoute($input: StageParcelForRouteInput!) {
              stageParcelForRoute(input: $input) {
                outcome
                trackingNumber
                board {
                  expectedParcelCount
                  stagedParcelCount
                  remainingParcelCount
                }
              }
            }
            """,
            variables: new
            {
                input = new
                {
                    routeId,
                    barcode = "LMSTAGEAPI0402",
                }
            },
            accessToken: token);

        document.RootElement.TryGetProperty("errors", out var errors)
            .Should().BeFalse("stageParcelForRoute should not return errors: {0}", errors.ToString());

        var result = document.RootElement
            .GetProperty("data")
            .GetProperty("stageParcelForRoute");

        result.GetProperty("outcome").GetString().Should().Be("NOT_EXPECTED");
        result.GetProperty("trackingNumber").GetString().Should().Be("LMSTAGEAPI0402");
        result.GetProperty("board").GetProperty("expectedParcelCount").GetInt32().Should().Be(1);
        result.GetProperty("board").GetProperty("stagedParcelCount").GetInt32().Should().Be(0);
        result.GetProperty("board").GetProperty("remainingParcelCount").GetInt32().Should().Be(1);

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var unassignedParcel = await dbContext.Parcels.FindAsync(unassignedParcelId);
        unassignedParcel.Should().NotBeNull();
        unassignedParcel!.Status.Should().Be(ParcelStatus.Sorted);
    }

    #endregion

    #region inbound receiving

    [Fact]
    public async Task GetOpenInboundManifests_ForAssignedDepot_ReturnsOpenManifest()
    {
        var operatorEmail = await SeedWarehouseOperatorAsync();
        var expectedParcelId = await SeedParcelAsync(
            DbSeeder.TestZoneId,
            "LMINBOUNDAPI0001",
            ParcelStatus.Registered);
        var missingParcelId = await SeedParcelAsync(
            DbSeeder.TestZoneId,
            "LMINBOUNDAPI0002",
            ParcelStatus.Registered);
        await SeedInboundManifestAsync("MAN-API-001", "TRUCK-API-1", expectedParcelId, missingParcelId);

        var token = await GetAccessTokenAsync(operatorEmail, "Warehouse@12345");

        using var document = await PostGraphQLAsync(
            """
            query GetOpenInboundManifests {
              openInboundManifests {
                manifestNumber
                truckIdentifier
                depotName
                expectedParcelCount
                openSessionId
                status
              }
            }
            """,
            accessToken: token);

        document.RootElement.TryGetProperty("errors", out var errors)
            .Should().BeFalse("openInboundManifests should not return errors: {0}", errors.ToString());

        var manifest = document.RootElement
            .GetProperty("data")
            .GetProperty("openInboundManifests")
            .EnumerateArray()
            .Single();

        manifest.GetProperty("manifestNumber").GetString().Should().Be("MAN-API-001");
        manifest.GetProperty("truckIdentifier").GetString().Should().Be("TRUCK-API-1");
        manifest.GetProperty("depotName").GetString().Should().Be("Test Depot");
        manifest.GetProperty("expectedParcelCount").GetInt32().Should().Be(2);
        manifest.GetProperty("openSessionId").ValueKind.Should().Be(JsonValueKind.Null);
        manifest.GetProperty("status").GetString().Should().Be("Open");
    }

    [Fact]
    public async Task InboundReceivingFlow_StartScanQueryAndConfirm_ReturnsExpectedSessionState()
    {
        var operatorEmail = await SeedWarehouseOperatorAsync();
        var expectedParcelId = await SeedParcelAsync(
            DbSeeder.TestZoneId,
            "LMINBOUNDAPI0101",
            ParcelStatus.Registered);
        var missingParcelId = await SeedParcelAsync(
            DbSeeder.TestZoneId,
            "LMINBOUNDAPI0102",
            ParcelStatus.Registered);
        await SeedParcelAsync(
            DbSeeder.TestZoneId,
            "LMINBOUNDAPI0199",
            ParcelStatus.Registered);

        var manifestId = await SeedInboundManifestAsync(
            "MAN-API-010",
            "TRUCK-API-10",
            expectedParcelId,
            missingParcelId);

        var token = await GetAccessTokenAsync(operatorEmail, "Warehouse@12345");

        using var startDoc = await PostGraphQLAsync(
            """
            mutation StartInboundReceivingSession($input: StartInboundReceivingSessionInput!) {
              startInboundReceivingSession(input: $input) {
                id
                manifestId
                status
                expectedParcelCount
                scannedExpectedCount
                scannedUnexpectedCount
                remainingExpectedCount
              }
            }
            """,
            variables: new
            {
                input = new
                {
                    manifestId = manifestId.ToString(),
                }
            },
            accessToken: token);

        startDoc.RootElement.TryGetProperty("errors", out var startErrors)
            .Should().BeFalse("startInboundReceivingSession should not return errors: {0}", startErrors.ToString());

        var session = startDoc.RootElement
            .GetProperty("data")
            .GetProperty("startInboundReceivingSession");

        var sessionId = session.GetProperty("id").GetString()!;
        session.GetProperty("manifestId").GetString().Should().Be(manifestId.ToString());
        session.GetProperty("status").GetString().Should().Be("Open");
        session.GetProperty("expectedParcelCount").GetInt32().Should().Be(2);
        session.GetProperty("remainingExpectedCount").GetInt32().Should().Be(2);

        using var expectedScanDoc = await PostGraphQLAsync(
            """
            mutation ScanInboundParcel($input: ScanInboundParcelInput!) {
              scanInboundParcel(input: $input) {
                isExpected
                scannedParcel {
                  trackingNumber
                  matchType
                }
                session {
                  scannedExpectedCount
                  scannedUnexpectedCount
                  remainingExpectedCount
                }
              }
            }
            """,
            variables: new
            {
                input = new
                {
                    sessionId,
                    barcode = "LMINBOUNDAPI0101",
                }
            },
            accessToken: token);

        expectedScanDoc.RootElement.TryGetProperty("errors", out var expectedScanErrors)
            .Should().BeFalse("expected scan should not return errors: {0}", expectedScanErrors.ToString());

        var expectedScan = expectedScanDoc.RootElement
            .GetProperty("data")
            .GetProperty("scanInboundParcel");

        expectedScan.GetProperty("isExpected").GetBoolean().Should().BeTrue();
        expectedScan.GetProperty("scannedParcel").GetProperty("trackingNumber").GetString().Should().Be("LMINBOUNDAPI0101");
        expectedScan.GetProperty("scannedParcel").GetProperty("matchType").GetString().Should().Be("Expected");
        expectedScan.GetProperty("session").GetProperty("scannedExpectedCount").GetInt32().Should().Be(1);
        expectedScan.GetProperty("session").GetProperty("remainingExpectedCount").GetInt32().Should().Be(1);

        using var unexpectedScanDoc = await PostGraphQLAsync(
            """
            mutation ScanInboundParcel($input: ScanInboundParcelInput!) {
              scanInboundParcel(input: $input) {
                isExpected
                scannedParcel {
                  trackingNumber
                  matchType
                }
                session {
                  scannedExpectedCount
                  scannedUnexpectedCount
                  exceptions {
                    exceptionType
                    trackingNumber
                  }
                }
              }
            }
            """,
            variables: new
            {
                input = new
                {
                    sessionId,
                    barcode = "LMINBOUNDAPI0199",
                }
            },
            accessToken: token);

        unexpectedScanDoc.RootElement.TryGetProperty("errors", out var unexpectedErrors)
            .Should().BeFalse("unexpected scan should not return errors: {0}", unexpectedErrors.ToString());

        var unexpectedScan = unexpectedScanDoc.RootElement
            .GetProperty("data")
            .GetProperty("scanInboundParcel");

        unexpectedScan.GetProperty("isExpected").GetBoolean().Should().BeFalse();
        unexpectedScan.GetProperty("scannedParcel").GetProperty("matchType").GetString().Should().Be("Unexpected");
        unexpectedScan.GetProperty("session").GetProperty("scannedUnexpectedCount").GetInt32().Should().Be(1);
        unexpectedScan.GetProperty("session").GetProperty("exceptions").EnumerateArray()
            .Should().Contain(entry =>
                entry.GetProperty("exceptionType").GetString() == "Unexpected"
                && entry.GetProperty("trackingNumber").GetString() == "LMINBOUNDAPI0199");

        using var sessionDoc = await PostGraphQLAsync(
            """
            query GetInboundReceivingSession($sessionId: UUID!) {
              inboundReceivingSession(sessionId: $sessionId) {
                id
                status
                remainingExpectedCount
                expectedParcels {
                  trackingNumber
                  isScanned
                }
              }
            }
            """,
            variables: new { sessionId },
            accessToken: token);

        sessionDoc.RootElement.TryGetProperty("errors", out var sessionErrors)
            .Should().BeFalse("inboundReceivingSession should not return errors: {0}", sessionErrors.ToString());

        var sessionState = sessionDoc.RootElement
            .GetProperty("data")
            .GetProperty("inboundReceivingSession");

        sessionState.GetProperty("status").GetString().Should().Be("Open");
        sessionState.GetProperty("remainingExpectedCount").GetInt32().Should().Be(1);
        sessionState.GetProperty("expectedParcels").EnumerateArray()
            .Should().Contain(entry =>
                entry.GetProperty("trackingNumber").GetString() == "LMINBOUNDAPI0102"
                && entry.GetProperty("isScanned").GetBoolean() == false);

        using var confirmDoc = await PostGraphQLAsync(
            """
            mutation ConfirmInboundReceivingSession($input: ConfirmInboundReceivingSessionInput!) {
              confirmInboundReceivingSession(input: $input) {
                status
                remainingExpectedCount
                exceptions {
                  exceptionType
                  trackingNumber
                }
              }
            }
            """,
            variables: new
            {
                input = new
                {
                    sessionId,
                }
            },
            accessToken: token);

        confirmDoc.RootElement.TryGetProperty("errors", out var confirmErrors)
            .Should().BeFalse("confirmInboundReceivingSession should not return errors: {0}", confirmErrors.ToString());

        var confirmed = confirmDoc.RootElement
            .GetProperty("data")
            .GetProperty("confirmInboundReceivingSession");

        confirmed.GetProperty("status").GetString().Should().Be("Confirmed");
        confirmed.GetProperty("remainingExpectedCount").GetInt32().Should().Be(0);
        var exceptions = confirmed.GetProperty("exceptions").EnumerateArray().ToList();
        exceptions.Should().Contain(entry =>
            entry.GetProperty("exceptionType").GetString() == "Unexpected"
            && entry.GetProperty("trackingNumber").GetString() == "LMINBOUNDAPI0199");
        exceptions.Should().Contain(entry =>
            entry.GetProperty("exceptionType").GetString() == "Missing"
            && entry.GetProperty("trackingNumber").GetString() == "LMINBOUNDAPI0102");
    }

    [Fact]
    public async Task InboundReceivingQueries_RejectDriverRole()
    {
        var token = await GetAccessTokenAsync("driver.test@lastmile.local", "Driver@12345");

        using var document = await PostGraphQLAsync(
            """
            query GetOpenInboundManifests {
              openInboundManifests {
                id
              }
            }
            """,
            accessToken: token);

        document.RootElement.TryGetProperty("errors", out var errors)
            .Should().BeTrue("drivers should not be authorized to access inbound receiving queries");
    }

    #endregion

    public Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<Guid> SeedVehicleAsync(string plate)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var vehicle = new Vehicle
        {
            RegistrationPlate = plate,
            Type = VehicleType.Van,
            ParcelCapacity = 25,
            WeightCapacity = 250m,
            Status = VehicleStatus.Available,
            DepotId = DbSeeder.TestDepotId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests"
        };

        dbContext.Vehicles.Add(vehicle);
        await dbContext.SaveChangesAsync();
        return vehicle.Id;
    }

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

    private async Task<string> SeedWarehouseOperatorAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var email = $"warehouse.operator.{Guid.NewGuid():N}@lastmile.local";

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FirstName = "Warehouse",
            LastName = "Operator",
            DepotId = DbSeeder.TestDepotId,
            ZoneId = DbSeeder.TestZoneId,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests",
        };

        var createResult = await userManager.CreateAsync(user, "Warehouse@12345");
        createResult.Succeeded.Should().BeTrue(string.Join(", ", createResult.Errors.Select(error => error.Description)));

        var roleResult = await userManager.AddToRoleAsync(user, PredefinedRole.WarehouseOperator.ToString());
        roleResult.Succeeded.Should().BeTrue(string.Join(", ", roleResult.Errors.Select(error => error.Description)));

        return email;
    }

    private async Task<Guid> SeedInboundManifestAsync(
        string manifestNumber,
        string truckIdentifier,
        params Guid[] parcelIds)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var manifest = new InboundManifest
        {
            Id = Guid.NewGuid(),
            ManifestNumber = manifestNumber,
            TruckIdentifier = truckIdentifier,
            DepotId = DbSeeder.TestDepotId,
            Status = InboundManifestStatus.Open,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests",
            Lines = parcelIds.Select(parcelId => new InboundManifestLine
            {
                Id = Guid.NewGuid(),
                ParcelId = parcelId,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "tests",
            }).ToList(),
        };

        dbContext.Add(manifest);
        await dbContext.SaveChangesAsync();
        return manifest.Id;
    }

    private async Task<Guid> SeedRouteAsync(
        Guid vehicleId,
        Guid driverId,
        RouteStatus status,
        StagingArea stagingArea,
        params Guid[] parcelIds)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var parcels = await dbContext.Parcels
            .Where(parcel => parcelIds.Contains(parcel.Id))
            .ToListAsync();
        var routeZoneId = parcels.FirstOrDefault()?.ZoneId
            ?? await dbContext.Drivers
                .Where(driver => driver.Id == driverId)
                .Select(driver => driver.ZoneId)
                .SingleAsync();

        var route = new Route
        {
            Id = Guid.NewGuid(),
            ZoneId = routeZoneId,
            VehicleId = vehicleId,
            DriverId = driverId,
            StartDate = DateTimeOffset.UtcNow.AddHours(-1),
            EndDate = status == RouteStatus.Completed ? DateTimeOffset.UtcNow : null,
            StartMileage = 100,
            EndMileage = status == RouteStatus.Completed ? 120 : 0,
            Status = status,
            StagingArea = stagingArea,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "tests",
            Parcels = parcels
        };

        dbContext.Routes.Add(route);
        await dbContext.SaveChangesAsync();
        return route.Id;
    }

    private async Task SeedRouteAndProofOfDeliveryAsync(Guid parcelId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var parcel = await dbContext.Parcels
            .Include(p => p.TrackingEvents)
            .SingleAsync(p => p.Id == parcelId);

        var route = new Route
        {
            Id = Guid.NewGuid(),
            ZoneId = parcel.ZoneId,
            VehicleId = DbSeeder.TestVehicleId,
            DriverId = DbSeeder.TestDriverId,
            StartDate = DateTimeOffset.UtcNow.AddHours(-1),
            EndDate = null,
            StartMileage = 1250,
            EndMileage = 0,
            Status = RouteStatus.InProgress,
            StagingArea = StagingArea.A,
            Parcels = [parcel]
        };

        var proofOfDelivery = new DeliveryConfirmation
        {
            Id = Guid.NewGuid(),
            ParcelId = parcel.Id,
            ReceivedBy = "Front Desk",
            DeliveryLocation = "Lobby",
            DeliveredAt = DateTimeOffset.UtcNow,
            SignatureImage = [1, 2, 3],
            Photo = [4, 5, 6]
        };

        dbContext.Routes.Add(route);
        dbContext.Add(proofOfDelivery);
        await dbContext.SaveChangesAsync();
    }

    private async Task TransitionParcelStatusAsync(
        string parcelId,
        string token,
        string newStatus,
        string location,
        string description)
    {
        using var transitionDoc = await PostGraphQLAsync(
            """
            mutation TransitionParcelStatus($input: TransitionParcelStatusInput!) {
              transitionParcelStatus(input: $input) {
                id
              }
            }
            """,
            variables: new
            {
                input = new
                {
                    parcelId,
                    newStatus,
                    location,
                    description
                }
            },
            accessToken: token);

        transitionDoc.RootElement.TryGetProperty("errors", out var transitionErrors)
            .Should().BeFalse("status transition should not return errors: {0}", transitionErrors.ToString());
    }
}
