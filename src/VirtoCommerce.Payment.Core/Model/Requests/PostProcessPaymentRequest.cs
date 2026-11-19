using VirtoCommerce.PaymentModule.Core.Model.StoredCredentials;

namespace VirtoCommerce.PaymentModule.Model.Requests
{
    public class PostProcessPaymentRequest : PaymentRequestBase, IStoredCredentialPaymentRequest
    {
        /// <inheritdoc />
        public bool StoreForFutureUse { get; set; }

        /// <inheritdoc />
        public StoredCredentialUsage StoredCredentialUsage { get; set; } = StoredCredentialUsage.CardOnFile;

        /// <inheritdoc />
        public string ConsentId { get; set; }

        /// <inheritdoc />
        public StoredPaymentMethod StoredPaymentMethod { get; set; }
    }
}
