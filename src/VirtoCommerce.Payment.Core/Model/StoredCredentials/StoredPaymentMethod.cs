using System;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.PaymentModule.Core.Model.StoredCredentials
{
    /// <summary>
    /// A payment method saved at a payment provider for future use. Virto never stores the card number or CVV:
    /// only the provider's references, display data and the network transaction ID of the first transaction.
    /// </summary>
    public class StoredPaymentMethod : AuditableEntity, ICloneable
    {
        public string StoreId { get; set; }
        public string CustomerId { get; set; }
        public string OrganizationId { get; set; }

        /// <summary>Code of the payment method (connector) that owns the token.</summary>
        public string PaymentMethodCode { get; set; }

        /// <summary>Provider customer reference (e.g. Authorize.Net customer profile, CyberSource TMS customer).</summary>
        public string ProviderCustomerRef { get; set; }

        /// <summary>Provider payment method token (e.g. payment profile id, instrument id, alias).</summary>
        public string Token { get; set; }

        public string Brand { get; set; }
        public string Last4 { get; set; }
        public int? ExpirationMonth { get; set; }
        public int? ExpirationYear { get; set; }
        public string CardholderName { get; set; }
        public string Fingerprint { get; set; }

        /// <summary>Network transaction ID returned by the first (customer-initiated) transaction. Sent with every merchant-initiated charge.</summary>
        public string NetworkTransactionId { get; set; }

        /// <summary>Provider reference of the first transaction (e.g. previous payment id).</summary>
        public string InitialTransactionRef { get; set; }

        /// <summary>Order and payment that created this stored method, if any.</summary>
        public string InitialOrderId { get; set; }
        public string InitialPaymentId { get; set; }

        public StoredCredentialUsage Usage { get; set; }

        /// <summary>True when the first transaction was authenticated with 3-D Secure.</summary>
        public bool IsAuthenticated { get; set; }

        public string Status { get; set; } = StoredPaymentMethodStatus.Active;
        public bool IsDefault { get; set; }
        public DateTime? LastUsedDate { get; set; }
        public DateTime? UpdatedByProviderDate { get; set; }

        public string ConsentId { get; set; }

        public virtual bool IsExpired(DateTime utcNow)
        {
            if (ExpirationYear == null || ExpirationMonth == null || ExpirationMonth < 1 || ExpirationMonth > 12)
            {
                return false;
            }

            var firstDayAfterExpiration = new DateTime(ExpirationYear.Value, ExpirationMonth.Value, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
            return utcNow >= firstDayAfterExpiration;
        }

        public virtual object Clone()
        {
            return MemberwiseClone();
        }
    }
}
