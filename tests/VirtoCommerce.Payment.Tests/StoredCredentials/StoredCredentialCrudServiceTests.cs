using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using VirtoCommerce.PaymentModule.Core.Events;
using VirtoCommerce.PaymentModule.Core.Model.StoredCredentials;
using VirtoCommerce.PaymentModule.Data.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using Xunit;

namespace VirtoCommerce.Payment.Tests.StoredCredentials;

public class StoredCredentialCrudServiceTests
{
    [Fact]
    public async Task SaveStoredPaymentMethod_New_AddsTheEntityCommitsAndPublishesBothEvents()
    {
        var fixture = new StoredCredentialServicesFixture();
        var card = StoredCredentialTestData.StoredPaymentMethod(null);

        await fixture.StoredPaymentMethodService.SaveChangesAsync([card]);

        var added = fixture.Added.Should().ContainSingle().Which.Should().BeOfType<StoredPaymentMethodEntity>().Subject;
        added.CustomerId.Should().Be(card.CustomerId);
        added.Token.Should().Be(card.Token);
        added.Usage.Should().Be(nameof(StoredCredentialUsage.Recurring));
        fixture.UnitOfWork.Verify(x => x.CommitAsync(), Times.Once);
        VerifyPublished<StoredPaymentMethodChangingEvent, StoredPaymentMethod>(fixture, EntryState.Added);
        VerifyPublished<StoredPaymentMethodChangedEvent, StoredPaymentMethod>(fixture, EntryState.Added);
    }

    [Fact]
    public async Task SaveStoredPaymentMethod_Existing_PatchesTheStoredEntityInsteadOfAddingOne()
    {
        var stored = new StoredPaymentMethodEntity().FromModel(StoredCredentialTestData.StoredPaymentMethod("spm-1"), new PrimaryKeyResolvingMap());
        var fixture = new StoredCredentialServicesFixture([stored]);
        var changed = StoredCredentialTestData.StoredPaymentMethod("spm-1");
        changed.Status = StoredPaymentMethodStatus.Expired;
        changed.IsDefault = false;
        changed.ExpirationYear = 2031;

        await fixture.StoredPaymentMethodService.SaveChangesAsync([changed]);

        fixture.Added.Should().BeEmpty();
        stored.Status.Should().Be(StoredPaymentMethodStatus.Expired);
        stored.IsDefault.Should().BeFalse();
        stored.ExpirationYear.Should().Be(2031);
        fixture.UnitOfWork.Verify(x => x.CommitAsync(), Times.Once);
        VerifyPublished<StoredPaymentMethodChangedEvent, StoredPaymentMethod>(fixture, EntryState.Modified);
    }

    [Fact]
    public async Task GetStoredPaymentMethods_LoadsThroughTheRepository()
    {
        var expected = StoredCredentialTestData.StoredPaymentMethod("spm-1");
        var fixture = new StoredCredentialServicesFixture([new StoredPaymentMethodEntity().FromModel(expected, new PrimaryKeyResolvingMap())]);

        var result = await fixture.StoredPaymentMethodService.GetAsync(["spm-1", "missing"]);

        result.Should().ContainSingle().Which.Should().BeEquivalentTo(expected, options => options.ComparingByMembers<StoredPaymentMethod>());
    }

    [Fact]
    public async Task SavePaymentConsent_New_AddsTheEntityAndPublishesBothEvents()
    {
        var fixture = new StoredCredentialServicesFixture();
        var consent = StoredCredentialTestData.PaymentConsent(null);

        await fixture.PaymentConsentService.SaveChangesAsync([consent]);

        var added = fixture.Added.Should().ContainSingle().Which.Should().BeOfType<PaymentConsentEntity>().Subject;
        added.TermsHash.Should().Be(consent.TermsHash);
        added.AcceptedDate.Should().Be(consent.AcceptedDate);
        fixture.UnitOfWork.Verify(x => x.CommitAsync(), Times.Once);
        VerifyPublished<PaymentConsentChangingEvent, PaymentConsent>(fixture, EntryState.Added);
        VerifyPublished<PaymentConsentChangedEvent, PaymentConsent>(fixture, EntryState.Added);
    }

    [Fact]
    public async Task SavePaymentConsent_Existing_RecordsTheRevocationButKeepsTheAcceptedTerms()
    {
        var stored = new PaymentConsentEntity().FromModel(StoredCredentialTestData.PaymentConsent("consent-1"), new PrimaryKeyResolvingMap());
        var fixture = new StoredCredentialServicesFixture(paymentConsents: [stored]);
        var revoked = StoredCredentialTestData.PaymentConsent("consent-1");
        revoked.RevokedDate = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc);
        revoked.TermsText = "Rewritten terms";

        await fixture.PaymentConsentService.SaveChangesAsync([revoked]);

        fixture.Added.Should().BeEmpty();
        stored.RevokedDate.Should().Be(revoked.RevokedDate);
        stored.TermsText.Should().Be("Auto-renewal terms");
    }

    // The platform publishes change events as GenericChangedEntryEvent<TModel>, so match on that and check the concrete type.
    private static void VerifyPublished<TEvent, TModel>(StoredCredentialServicesFixture fixture, EntryState state)
        where TEvent : GenericChangedEntryEvent<TModel>
    {
        fixture.EventPublisher.Verify(
            x => x.Publish(
                It.Is<GenericChangedEntryEvent<TModel>>(e => e is TEvent && e.ChangedEntries.Single().EntryState == state),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
