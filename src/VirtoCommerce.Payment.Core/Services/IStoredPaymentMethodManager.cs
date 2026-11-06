using System.Threading.Tasks;
using VirtoCommerce.PaymentModule.Core.Model.StoredCredentials;
using VirtoCommerce.PaymentModule.Model.Requests;

namespace VirtoCommerce.PaymentModule.Core.Services
{
    /// <summary>
    /// The rules for customers' saved cards, in one place: which card may pay, how a card a provider saved is recorded,
    /// one default card per customer and store, and when a card can be removed. Payment methods (connectors) only talk to
    /// the provider and never read or write saved cards; x-order, x-cart and other modules call this manager instead.
    /// </summary>
    public interface IStoredPaymentMethodManager
    {
        /// <summary>
        /// Reads the stored credential parameters of a payment (<see cref="StoredCredentialParameterNames"/>) into the typed
        /// fields of the request, and resolves the saved card to pay with. Fails when that card isn't the customer's, isn't
        /// active or expired, or belongs to another payment method.
        /// </summary>
        Task<StoredPaymentMethodOperationResult> PrepareAsync<TRequest>(TRequest request, StoredPaymentContext context)
            where TRequest : PaymentRequestBase, IStoredCredentialPaymentRequest;

        /// <summary>
        /// After a successful payment: records the card the provider saved (the same card saved again updates the existing
        /// record, the customer's first card becomes the default), or marks the saved card that paid as used.
        /// Returns the recorded card, or null when the payment neither saved nor used a card.
        /// </summary>
        Task<StoredPaymentMethod> SaveFromPaymentAsync(IStoredCredentialPaymentRequest request, IStoredCredentialPaymentResult result, StoredPaymentContext context);

        /// <summary>Records that a saved card was charged, and its network transaction ID if it had none.</summary>
        Task MarkUsedAsync(StoredPaymentMethod storedPaymentMethod, string networkTransactionId = null);

        /// <summary>Saves a card at the provider without a payment and records it for the customer.</summary>
        Task<StoredPaymentMethodOperationResult> AddAsync(AddStoredPaymentMethodRequest request);

        /// <summary>
        /// Removes a customer's card at the provider and revokes it, unless a <see cref="IStoredPaymentMethodUsageProvider"/>
        /// reports it in use. A removed default card passes the default to the customer's most recent active card.
        /// </summary>
        Task<StoredPaymentMethodOperationResult> RemoveAsync(string customerId, string storedPaymentMethodId);

        /// <summary>Makes an active card the customer's default for its store.</summary>
        Task<StoredPaymentMethodOperationResult> SetDefaultAsync(string customerId, string storedPaymentMethodId);
    }
}
