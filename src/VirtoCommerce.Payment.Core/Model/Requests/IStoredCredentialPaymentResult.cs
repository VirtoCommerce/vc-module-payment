using VirtoCommerce.PaymentModule.Core.Model.StoredCredentials;

namespace VirtoCommerce.PaymentModule.Model.Requests
{
    /// <summary>
    /// A payment result that can carry the card the provider saved.
    /// </summary>
    public interface IStoredCredentialPaymentResult : IPaymenRequestResult
    {
        /// <summary>
        /// The card the provider saved for <see cref="IStoredCredentialPaymentRequest.StoreForFutureUse"/>: token, customer reference,
        /// brand, last 4 digits, expiry and network transaction ID. Not persisted; the caller saves it
        /// (see <c>IStoredPaymentMethodManager.SaveFromPaymentAsync</c>).
        /// </summary>
        StoredPaymentMethod StoredPaymentMethod { get; set; }
    }
}
