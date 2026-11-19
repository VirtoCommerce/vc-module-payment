using System.Collections.Specialized;

namespace VirtoCommerce.PaymentModule.Core.Model.StoredCredentials
{
    /// <summary>
    /// Saves a card for a customer without a payment, e.g. from the account's payment methods page.
    /// </summary>
    public class AddStoredPaymentMethodRequest
    {
        public string StoreId { get; set; }

        public string CustomerId { get; set; }

        public string OrganizationId { get; set; }

        public string PaymentMethodCode { get; set; }

        /// <summary>What the provider's card form posted: tokens, nonces, billing address.</summary>
        public NameValueCollection Parameters { get; set; } = new();

        /// <summary>Make the card the customer's default. The first card always becomes the default.</summary>
        public bool SetDefault { get; set; }

        public string ConsentId { get; set; }

        public StoredCredentialUsage Usage { get; set; } = StoredCredentialUsage.CardOnFile;

        /// <summary>Verify the card with a $0 (or minimal) authorization.</summary>
        public bool Verify { get; set; } = true;
    }
}
