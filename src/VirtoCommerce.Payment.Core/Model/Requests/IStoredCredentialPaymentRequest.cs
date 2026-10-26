using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.PaymentModule.Core.Model.StoredCredentials;

namespace VirtoCommerce.PaymentModule.Model.Requests
{
    /// <summary>
    /// A customer-present payment request that can save the card or pay with a saved one.
    /// Only honored by payment methods that implement <see cref="ISupportStoredCredentialFlow"/>.
    /// </summary>
    public interface IStoredCredentialPaymentRequest
    {
        /// <summary>Save the card at the provider for future payments (card on file).</summary>
        bool StoreForFutureUse { get; set; }

        /// <summary>How the saved card will be used: recurring, unscheduled or customer-initiated card on file.</summary>
        StoredCredentialUsage StoredCredentialUsage { get; set; }

        /// <summary>Consent record that covers storing the card.</summary>
        string ConsentId { get; set; }

        /// <summary>
        /// The saved card to pay with, instead of new card data. Resolved and checked by the caller
        /// (see <c>IStoredPaymentMethodManager.PrepareAsync</c>); the payment method only charges it.
        /// </summary>
        StoredPaymentMethod StoredPaymentMethod { get; set; }
    }
}
