using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using VirtoCommerce.PaymentModule.Core.Model.StoredCredentials;
using VirtoCommerce.PaymentModule.Data.Model;
using VirtoCommerce.Platform.Core.Common;
using Xunit;

namespace VirtoCommerce.Payment.Tests.StoredCredentials;

public class StoredCredentialSearchServiceTests
{
    private static readonly DateTime Day = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    private static StoredCredentialServicesFixture CreateStoredPaymentMethods()
    {
        return new StoredCredentialServicesFixture(
        [
            new StoredPaymentMethodEntity { Id = "a", StoreId = "store-1", CustomerId = "customer-1", OrganizationId = "organization-1", PaymentMethodCode = "Gateway1", Status = StoredPaymentMethodStatus.Active, InitialOrderId = "order-1", InitialPaymentId = "payment-1", CreatedDate = Day },
            new StoredPaymentMethodEntity { Id = "b", StoreId = "store-1", CustomerId = "customer-1", PaymentMethodCode = "Gateway2", Status = StoredPaymentMethodStatus.Expired, IsDefault = true, CreatedDate = Day.AddDays(1) },
            new StoredPaymentMethodEntity { Id = "c", StoreId = "store-1", CustomerId = "customer-2", OrganizationId = "organization-1", PaymentMethodCode = "Gateway1", Status = StoredPaymentMethodStatus.Revoked, CreatedDate = Day.AddDays(2) },
            new StoredPaymentMethodEntity { Id = "d", StoreId = "store-2", CustomerId = "customer-1", PaymentMethodCode = "Gateway1", Status = StoredPaymentMethodStatus.NeedsUpdate, CreatedDate = Day.AddDays(3) },
        ]);
    }

    public static TheoryData<StoredPaymentMethodSearchCriteria, string[]> StoredPaymentMethodFilters => new()
    {
        { new StoredPaymentMethodSearchCriteria { StoreId = "store-2" }, ["d"] },
        { new StoredPaymentMethodSearchCriteria { CustomerId = "customer-2" }, ["c"] },
        { new StoredPaymentMethodSearchCriteria { OrganizationId = "organization-1" }, ["a", "c"] },
        { new StoredPaymentMethodSearchCriteria { PaymentMethodCode = "Gateway2" }, ["b"] },
        { new StoredPaymentMethodSearchCriteria { Statuses = [StoredPaymentMethodStatus.Expired, StoredPaymentMethodStatus.NeedsUpdate] }, ["b", "d"] },
        { new StoredPaymentMethodSearchCriteria { InitialOrderId = "order-1" }, ["a"] },
        { new StoredPaymentMethodSearchCriteria { InitialPaymentId = "payment-1" }, ["a"] },
        { new StoredPaymentMethodSearchCriteria { StoreId = "store-1", CustomerId = "customer-1" }, ["a", "b"] },
        { new StoredPaymentMethodSearchCriteria { StoreId = "store-1", CustomerId = "customer-1", PaymentMethodCode = "Gateway1", Statuses = [StoredPaymentMethodStatus.Active] }, ["a"] },
        { new StoredPaymentMethodSearchCriteria { CustomerId = "customer-3" }, [] },
    };

    [Theory]
    [MemberData(nameof(StoredPaymentMethodFilters))]
    public async Task SearchStoredPaymentMethods_AppliesEveryFilter(StoredPaymentMethodSearchCriteria criteria, string[] expectedIds)
    {
        var fixture = CreateStoredPaymentMethods();

        var result = await fixture.StoredPaymentMethodSearchService.SearchAsync(criteria);

        result.TotalCount.Should().Be(expectedIds.Length);
        result.Results.Select(x => x.Id).Should().BeEquivalentTo(expectedIds);
    }

    [Fact]
    public async Task SearchStoredPaymentMethods_ByDefault_ReturnsTheDefaultCardFirstThenTheNewest()
    {
        var fixture = CreateStoredPaymentMethods();

        var result = await fixture.StoredPaymentMethodSearchService.SearchAsync(new StoredPaymentMethodSearchCriteria());

        result.Results.Select(x => x.Id).Should().Equal("b", "d", "c", "a");
    }

    [Fact]
    public async Task SearchStoredPaymentMethods_HonoursTheRequestedSortAndPaging()
    {
        var fixture = CreateStoredPaymentMethods();

        var result = await fixture.StoredPaymentMethodSearchService.SearchAsync(new StoredPaymentMethodSearchCriteria
        {
            Sort = "CreatedDate:asc",
            Skip = 1,
            Take = 2,
        });

        result.TotalCount.Should().Be(4);
        result.Results.Select(x => x.Id).Should().Equal("b", "c");
    }

    [Fact]
    public async Task SearchStoredPaymentMethods_ReturnsFullyMappedModels()
    {
        var expected = StoredCredentialTestData.StoredPaymentMethod("spm-1");
        var fixture = new StoredCredentialServicesFixture([new StoredPaymentMethodEntity().FromModel(expected, new PrimaryKeyResolvingMap())]);

        var result = await fixture.StoredPaymentMethodSearchService.SearchAsync(new StoredPaymentMethodSearchCriteria { CustomerId = expected.CustomerId });

        result.Results.Should().ContainSingle().Which.Should().BeEquivalentTo(expected, options => options.ComparingByMembers<StoredPaymentMethod>());
    }

    private static StoredCredentialServicesFixture CreatePaymentConsents()
    {
        return new StoredCredentialServicesFixture(paymentConsents:
        [
            new PaymentConsentEntity { Id = "a", StoreId = "store-1", CustomerId = "customer-1", StoredPaymentMethodId = "spm-1", Purpose = "Subscription", ObjectType = "Subscription", ObjectId = "subscription-1", AcceptedDate = Day },
            new PaymentConsentEntity { Id = "b", StoreId = "store-1", CustomerId = "customer-1", StoredPaymentMethodId = "spm-2", Purpose = "CardOnFile", AcceptedDate = Day.AddDays(1) },
            new PaymentConsentEntity { Id = "c", StoreId = "store-2", CustomerId = "customer-2", Purpose = "Subscription", ObjectType = "Subscription", ObjectId = "subscription-2", AcceptedDate = Day.AddDays(2) },
        ]);
    }

    public static TheoryData<PaymentConsentSearchCriteria, string[]> PaymentConsentFilters => new()
    {
        { new PaymentConsentSearchCriteria { StoreId = "store-2" }, ["c"] },
        { new PaymentConsentSearchCriteria { CustomerId = "customer-1" }, ["a", "b"] },
        { new PaymentConsentSearchCriteria { StoredPaymentMethodId = "spm-2" }, ["b"] },
        { new PaymentConsentSearchCriteria { Purpose = "Subscription" }, ["a", "c"] },
        { new PaymentConsentSearchCriteria { RelatedObjectType = "Subscription" }, ["a", "c"] },
        { new PaymentConsentSearchCriteria { RelatedObjectType = "Subscription", RelatedObjectId = "subscription-2" }, ["c"] },
        { new PaymentConsentSearchCriteria { CustomerId = "customer-3" }, [] },
    };

    [Theory]
    [MemberData(nameof(PaymentConsentFilters))]
    public async Task SearchPaymentConsents_AppliesEveryFilter(PaymentConsentSearchCriteria criteria, string[] expectedIds)
    {
        var fixture = CreatePaymentConsents();

        var result = await fixture.PaymentConsentSearchService.SearchAsync(criteria);

        result.TotalCount.Should().Be(expectedIds.Length);
        result.Results.Select(x => x.Id).Should().BeEquivalentTo(expectedIds);
    }

    [Fact]
    public async Task SearchPaymentConsents_ByDefault_ReturnsTheNewestFirst()
    {
        var fixture = CreatePaymentConsents();

        var result = await fixture.PaymentConsentSearchService.SearchAsync(new PaymentConsentSearchCriteria());

        result.Results.Select(x => x.Id).Should().Equal("c", "b", "a");
    }

    [Fact]
    public async Task SearchPaymentConsents_ReturnsFullyMappedModels()
    {
        var expected = StoredCredentialTestData.PaymentConsent("consent-1");
        var fixture = new StoredCredentialServicesFixture(paymentConsents: [new PaymentConsentEntity().FromModel(expected, new PrimaryKeyResolvingMap())]);

        var result = await fixture.PaymentConsentSearchService.SearchAsync(new PaymentConsentSearchCriteria { CustomerId = expected.CustomerId });

        result.Results.Should().ContainSingle().Which.Should().BeEquivalentTo(expected, options => options.ComparingByMembers<PaymentConsent>());
    }
}
