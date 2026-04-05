using System.Net;
using FluentAssertions;
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
                status
                allowedNextStatuses
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
        parcel.GetProperty("status").GetString().Should().Be("Registered");
        parcel.GetProperty("allowedNextStatuses").EnumerateArray()
            .Select(e => e.GetString())
            .Should()
            .BeEquivalentTo(new[] { "RECEIVED_AT_DEPOT", "CANCELLED" });
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

    #region transitionParcelStatus mutation

    [Fact]
    public async Task TransitionParcelStatus_ValidTransition_ReturnsUpdatedStatus()
    {
        var token = await GetAdminAccessTokenAsync();

        // Register a parcel (starts as Registered)
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
                        street1 = "10 Transition St",
                        city = "Alexandria",
                        state = "Alexandria",
                        postalCode = "21500",
                        countryCode = "EG",
                        isResidential = false,
                        contactName = "Tarek Hassan",
                        phone = "+201000000001",
                        email = "tarek@example.com"
                    },
                    serviceType = "EXPRESS",
                    weight = 1.0,
                    weightUnit = "KG",
                    length = 15.0,
                    width = 10.0,
                    height = 5.0,
                    dimensionUnit = "CM",
                    declaredValue = 100.0,
                    currency = "USD",
                    estimatedDeliveryDate = DateTimeOffset.UtcNow.AddDays(2).ToString("o")
                }
            },
            accessToken: token);

        registerDoc.RootElement.TryGetProperty("errors", out var registerErrors)
            .Should().BeFalse("registerParcel should not return errors");

        var parcel = registerDoc.RootElement
            .GetProperty("data")
            .GetProperty("registerParcel");
        var parcelId = parcel.GetProperty("id").GetString();
        parcel.GetProperty("status").GetString().Should().Be("Registered");

        // Transition to ReceivedAtDepot (valid from Registered)
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
                    parcelId,
                    newStatus = "RECEIVED_AT_DEPOT",
                    location = "Main Depot",
                    description = "Package received at depot"
                }
            },
            accessToken: token);

        transitionDoc.RootElement.TryGetProperty("errors", out var transitionErrors)
            .Should().BeFalse("transitionParcelStatus should not return errors: {0}", transitionErrors.ToString());

        var updated = transitionDoc.RootElement
            .GetProperty("data")
            .GetProperty("transitionParcelStatus");

        updated.GetProperty("id").GetString().Should().Be(parcelId);
        updated.GetProperty("status").GetString().Should().Be("ReceivedAtDepot");
    }

    [Fact]
    public async Task TransitionParcelStatus_InvalidTransition_ReturnsError()
    {
        var token = await GetAdminAccessTokenAsync();

        // Register a parcel (starts as Registered)
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
                        street1 = "11 Invalid St",
                        city = "Cairo",
                        state = "Cairo",
                        postalCode = "11511",
                        countryCode = "EG",
                        isResidential = true,
                        contactName = "Sara Ali",
                        phone = "+201000000002",
                        email = "sara@example.com"
                    },
                    serviceType = "STANDARD",
                    weight = 0.5,
                    weightUnit = "KG",
                    length = 10.0,
                    width = 10.0,
                    height = 5.0,
                    dimensionUnit = "CM",
                    declaredValue = 50.0,
                    currency = "USD",
                    estimatedDeliveryDate = DateTimeOffset.UtcNow.AddDays(3).ToString("o")
                }
            },
            accessToken: token);

        var parcelId = registerDoc.RootElement
            .GetProperty("data")
            .GetProperty("registerParcel")
            .GetProperty("id").GetString();

        // Attempt to transition from Registered -> Delivered (invalid per ValidTransitions)
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
                    newStatus = "DELIVERED"
                }
            },
            accessToken: token);

        transitionDoc.RootElement.TryGetProperty("errors", out var errors)
            .Should().BeTrue("transitionParcelStatus should return an error for invalid transition");

        var error = errors.EnumerateArray().FirstOrDefault();
        error.ValueKind.Should().NotBe(default, "at least one error should be returned");
    }

    [Fact]
    public async Task TransitionParcelStatus_ParcelNotFound_ReturnsError()
    {
        var token = await GetAdminAccessTokenAsync();

        using var doc = await PostGraphQLAsync(
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
                    parcelId = Guid.Empty.ToString(),
                    newStatus = "RECEIVED_AT_DEPOT"
                }
            },
            accessToken: token);

        doc.RootElement.TryGetProperty("errors", out var errors)
            .Should().BeTrue("should return error for non-existent parcel");

        var error = errors.EnumerateArray().FirstOrDefault();
        error.ValueKind.Should().NotBe(default);
    }

    [Fact]
    public async Task TransitionParcelStatus_Unauthenticated_ReturnsError()
    {
        using var doc = await PostGraphQLAsync(
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
                    parcelId = Guid.NewGuid().ToString(),
                    newStatus = "RECEIVED_AT_DEPOT"
                }
            });

        doc.RootElement.TryGetProperty("errors", out var errors)
            .Should().BeTrue("unauthenticated request should return error");
    }

    #endregion

    #region parcelTrackingEvents query

    [Fact]
    public async Task GetParcelTrackingEvents_AfterTransition_ReturnsTrackingEvents()
    {
        var token = await GetAdminAccessTokenAsync();

        // Register a parcel
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
                        street1 = "12 Tracking St",
                        city = "Giza",
                        state = "Giza",
                        postalCode = "12612",
                        countryCode = "EG",
                        isResidential = true,
                        contactName = "Nadia Youssef",
                        phone = "+201000000003",
                        email = "nadia@example.com"
                    },
                    serviceType = "ECONOMY",
                    weight = 2.5,
                    weightUnit = "KG",
                    length = 30.0,
                    width = 20.0,
                    height = 15.0,
                    dimensionUnit = "CM",
                    declaredValue = 250.0,
                    currency = "USD",
                    estimatedDeliveryDate = DateTimeOffset.UtcNow.AddDays(5).ToString("o")
                }
            },
            accessToken: token);

        var parcelId = registerDoc.RootElement
            .GetProperty("data")
            .GetProperty("registerParcel")
            .GetProperty("id").GetString();

        // Transition to ReceivedAtDepot
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
                    parcelId,
                    newStatus = "RECEIVED_AT_DEPOT",
                    location = "Zone A",
                    description = "Arrived at main facility"
                }
            },
            accessToken: token);

        transitionDoc.RootElement.TryGetProperty("errors", out var transitionErrors)
            .Should().BeFalse("transition should succeed");

        // Query tracking events
        using var queryDoc = await PostGraphQLAsync(
            """
            query GetParcelTrackingEvents($parcelId: UUID!) {
              parcelTrackingEvents(parcelId: $parcelId) {
                id
                timestamp
                eventType
                description
                location
                operator
              }
            }
            """,
            variables: new { parcelId },
            accessToken: token);

        queryDoc.RootElement.TryGetProperty("errors", out var queryErrors)
            .Should().BeFalse("parcelTrackingEvents should not return errors: {0}", queryErrors.ToString());

        var events = queryDoc.RootElement
            .GetProperty("data")
            .GetProperty("parcelTrackingEvents")
            .EnumerateArray()
            .ToList();

        events.Should().NotBeEmpty("at least one tracking event should exist after a transition");
        events.First().GetProperty("eventType").GetString().Should().Be("ArrivedAtFacility");
        events.First().GetProperty("description").GetString().Should().Be("Arrived at main facility");
        events.First().GetProperty("location").GetString().Should().Be("Zone A");
    }

    #endregion

    public Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}
