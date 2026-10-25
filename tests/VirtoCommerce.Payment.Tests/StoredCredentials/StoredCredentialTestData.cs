using System;
using VirtoCommerce.PaymentModule.Core.Model.StoredCredentials;

namespace VirtoCommerce.Payment.Tests.StoredCredentials;

internal static class StoredCredentialTestData
{
    public static StoredPaymentMethod StoredPaymentMethod(string id)
    {
        return new StoredPaymentMethod
        {
            Id = id,
            CreatedBy = "creator",
            CreatedDate = new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc),
            ModifiedBy = "modifier",
            ModifiedDate = new DateTime(2026, 10, 2, 8, 0, 0, DateTimeKind.Utc),
            StoreId = "store-1",
            CustomerId = "customer-1",
            OrganizationId = "organization-1",
            PaymentMethodCode = "TestPaymentMethod",
            ProviderCustomerRef = "provider-customer-1",
            Token = "provider-token-1",
            Brand = "Visa",
            Last4 = "4242",
            ExpirationMonth = 12,
            ExpirationYear = 2030,
            CardholderName = "Test Cardholder",
            Fingerprint = "fingerprint-1",
            NetworkTransactionId = "network-transaction-1",
            InitialTransactionRef = "transaction-1",
            InitialOrderId = "order-1",
            InitialPaymentId = "payment-1",
            Usage = StoredCredentialUsage.Recurring,
            IsAuthenticated = true,
            Status = StoredPaymentMethodStatus.Active,
            IsDefault = true,
            LastUsedDate = new DateTime(2026, 10, 3, 8, 0, 0, DateTimeKind.Utc),
            UpdatedByProviderDate = new DateTime(2026, 10, 4, 8, 0, 0, DateTimeKind.Utc),
            ConsentId = "consent-1",
        };
    }

    public static PaymentConsent PaymentConsent(string id)
    {
        return new PaymentConsent
        {
            Id = id,
            CreatedBy = "creator",
            CreatedDate = new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc),
            ModifiedBy = "modifier",
            ModifiedDate = new DateTime(2026, 10, 2, 8, 0, 0, DateTimeKind.Utc),
            StoreId = "store-1",
            CustomerId = "customer-1",
            StoredPaymentMethodId = "spm-1",
            Purpose = "Subscription",
            TermsVersion = "2026-10",
            TermsText = "Auto-renewal terms",
            TermsHash = "terms-hash",
            Channel = "Storefront",
            IpAddress = "203.0.113.10",
            UserAgent = "Test agent",
            AcceptedDate = new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc),
            RevokedDate = null,
            ObjectType = "Subscription",
            ObjectId = "subscription-1",
        };
    }
}
