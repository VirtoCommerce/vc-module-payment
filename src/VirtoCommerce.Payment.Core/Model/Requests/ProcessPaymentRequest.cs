using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.PaymentModule.Core.Model.StoredCredentials;

namespace VirtoCommerce.PaymentModule.Model.Requests
{
    public class ProcessPaymentRequest : PaymentRequestBase, IStoredCredentialPaymentRequest
    {
        public BankCardInfo BankCardInfo { get; set; }

        /// <summary>
        /// Save the payment method at the provider for future payments (card on file).
        /// Only honored by payment methods that implement <see cref="ISupportStoredCredentialFlow"/>.
        /// </summary>
        public bool StoreForFutureUse { get; set; }

        public StoredCredentialUsage StoredCredentialUsage { get; set; } = StoredCredentialUsage.CardOnFile;

        /// <summary>Consent record that covers storing the payment method.</summary>
        public string ConsentId { get; set; }

        /// <inheritdoc />
        public StoredPaymentMethod StoredPaymentMethod { get; set; }
    }
}
