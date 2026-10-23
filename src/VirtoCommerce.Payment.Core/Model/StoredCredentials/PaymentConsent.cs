using System;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.PaymentModule.Core.Model.StoredCredentials
{
    /// <summary>
    /// Evidence that the customer agreed to store a payment method and have it charged later.
    /// Kept for disputes and compliance (card network rules, ROSCA, state auto-renewal laws).
    /// </summary>
    public class PaymentConsent : AuditableEntity, ICloneable
    {
        public string StoreId { get; set; }
        public string CustomerId { get; set; }
        public string StoredPaymentMethodId { get; set; }

        /// <summary>What the consent is for, e.g. "subscription", "card-on-file".</summary>
        public string Purpose { get; set; }

        public string TermsVersion { get; set; }
        public string TermsText { get; set; }
        public string TermsHash { get; set; }

        public string Channel { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }

        public DateTime AcceptedDate { get; set; }
        public DateTime? RevokedDate { get; set; }

        /// <summary>Reference to the object the consent belongs to (e.g. cart or order id).</summary>
        public string ObjectType { get; set; }
        public string ObjectId { get; set; }

        public virtual object Clone()
        {
            return MemberwiseClone();
        }
    }
}
