using System;
using FluentAssertions;
using VirtoCommerce.PaymentModule.Core.Model.StoredCredentials;
using VirtoCommerce.PaymentModule.Data.Model;
using VirtoCommerce.Platform.Core.Common;
using Xunit;

namespace VirtoCommerce.Payment.Tests.StoredCredentials;

public class StoredCredentialEntityMappingTests
{
    [Fact]
    public void StoredPaymentMethod_FromModelThenToModel_KeepsEveryProperty()
    {
        var model = StoredCredentialTestData.StoredPaymentMethod("spm-1");

        var entity = new StoredPaymentMethodEntity().FromModel(model, new PrimaryKeyResolvingMap());
        var result = entity.ToModel(new StoredPaymentMethod());

        result.Should().BeEquivalentTo(model, options => options.ComparingByMembers<StoredPaymentMethod>());
    }

    [Fact]
    public void StoredPaymentMethod_FromModel_StoresUsageAsTextAndDefaultsStatusToActive()
    {
        var model = StoredCredentialTestData.StoredPaymentMethod("spm-1");
        model.Usage = StoredCredentialUsage.Unscheduled;
        model.Status = null;

        var entity = new StoredPaymentMethodEntity().FromModel(model, new PrimaryKeyResolvingMap());

        entity.Usage.Should().Be("Unscheduled");
        entity.Status.Should().Be(StoredPaymentMethodStatus.Active);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("NotAUsage")]
    public void StoredPaymentMethod_ToModel_UnknownUsageFallsBackToCardOnFile(string usage)
    {
        var entity = new StoredPaymentMethodEntity { Usage = usage };

        var model = entity.ToModel(new StoredPaymentMethod());

        model.Usage.Should().Be(StoredCredentialUsage.CardOnFile);
    }

    [Fact]
    public void StoredPaymentMethod_Patch_CopiesEveryMutableProperty()
    {
        var source = new StoredPaymentMethodEntity().FromModel(StoredCredentialTestData.StoredPaymentMethod("spm-1"), new PrimaryKeyResolvingMap());
        var target = new StoredPaymentMethodEntity { Id = "spm-1", CreatedBy = "original", CreatedDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc) };

        source.Patch(target);

        target.Should().BeEquivalentTo(source, options => options
            .ComparingByMembers<StoredPaymentMethodEntity>()
            .Excluding(x => x.CreatedBy)
            .Excluding(x => x.CreatedDate)
            .Excluding(x => x.ModifiedBy)
            .Excluding(x => x.ModifiedDate));
        target.CreatedBy.Should().Be("original");
    }

    [Fact]
    public void PaymentConsent_FromModelThenToModel_KeepsEveryProperty()
    {
        var model = StoredCredentialTestData.PaymentConsent("consent-1");

        var entity = new PaymentConsentEntity().FromModel(model, new PrimaryKeyResolvingMap());
        var result = entity.ToModel(new PaymentConsent());

        result.Should().BeEquivalentTo(model, options => options.ComparingByMembers<PaymentConsent>());
    }

    [Fact]
    public void PaymentConsent_Patch_ChangesOnlyTheLinkAndTheRevocation()
    {
        var target = new PaymentConsentEntity().FromModel(StoredCredentialTestData.PaymentConsent("consent-1"), new PrimaryKeyResolvingMap());
        var original = new PaymentConsentEntity().FromModel(StoredCredentialTestData.PaymentConsent("consent-1"), new PrimaryKeyResolvingMap());

        var changed = StoredCredentialTestData.PaymentConsent("consent-1");
        changed.StoredPaymentMethodId = "spm-2";
        changed.RevokedDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        changed.TermsText = "Edited terms";
        changed.TermsHash = "edited-hash";
        changed.AcceptedDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        changed.IpAddress = "203.0.113.99";
        var source = new PaymentConsentEntity().FromModel(changed, new PrimaryKeyResolvingMap());

        source.Patch(target);

        target.StoredPaymentMethodId.Should().Be("spm-2");
        target.RevokedDate.Should().Be(changed.RevokedDate);
        target.Should().BeEquivalentTo(original, options => options
            .ComparingByMembers<PaymentConsentEntity>()
            .Excluding(x => x.StoredPaymentMethodId)
            .Excluding(x => x.RevokedDate));
    }
}
