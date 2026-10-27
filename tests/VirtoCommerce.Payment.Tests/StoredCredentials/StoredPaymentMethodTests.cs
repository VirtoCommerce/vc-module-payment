using System;
using FluentAssertions;
using VirtoCommerce.PaymentModule.Core.Model.StoredCredentials;
using Xunit;

namespace VirtoCommerce.Payment.Tests.StoredCredentials;

public class StoredPaymentMethodTests
{
    [Theory]
    [InlineData(2026, 10, "2026-10-31T23:59:59", false)]
    [InlineData(2026, 10, "2026-11-01T00:00:00", true)]
    [InlineData(2026, 12, "2026-12-31T23:59:59", false)]
    [InlineData(2026, 12, "2027-01-01T00:00:00", true)]
    [InlineData(2030, 1, "2026-10-25T00:00:00", false)]
    public void IsExpired_CardIsValidThroughTheLastDayOfItsExpiryMonth(int year, int month, string utcNow, bool expected)
    {
        var card = new StoredPaymentMethod { ExpirationYear = year, ExpirationMonth = month };

        card.IsExpired(DateTime.SpecifyKind(DateTime.Parse(utcNow), DateTimeKind.Utc)).Should().Be(expected);
    }

    [Theory]
    [InlineData(null, 10)]
    [InlineData(2020, null)]
    [InlineData(2020, 0)]
    [InlineData(2020, 13)]
    public void IsExpired_WithoutAValidExpiryDate_ReturnsFalse(int? year, int? month)
    {
        var card = new StoredPaymentMethod { ExpirationYear = year, ExpirationMonth = month };

        card.IsExpired(new DateTime(2026, 10, 25, 0, 0, 0, DateTimeKind.Utc)).Should().BeFalse();
    }

    [Fact]
    public void Clone_ReturnsAnEqualButSeparateInstance()
    {
        var card = StoredCredentialTestData.StoredPaymentMethod("spm-1");

        var clone = (StoredPaymentMethod)card.Clone();

        clone.Should().NotBeSameAs(card);
        clone.Should().BeEquivalentTo(card, options => options.ComparingByMembers<StoredPaymentMethod>());
    }

    [Fact]
    public void NewStoredPaymentMethod_IsActive()
    {
        new StoredPaymentMethod().Status.Should().Be(StoredPaymentMethodStatus.Active);
    }
}
