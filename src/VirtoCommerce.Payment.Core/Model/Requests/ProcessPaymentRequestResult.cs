using VirtoCommerce.PaymentModule.Core.Model.StoredCredentials;

namespace VirtoCommerce.PaymentModule.Model.Requests
{
    public class ProcessPaymentRequestResult : PaymentRequestResultBase, IStoredCredentialPaymentResult
    {
        public string RedirectUrl { get; set; }

        public string HtmlForm { get; set; }

        public string OuterId { get; set; }

        /// <inheritdoc />
        public StoredPaymentMethod StoredPaymentMethod { get; set; }
    }
}
