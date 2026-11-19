using VirtoCommerce.PaymentModule.Core.Model.StoredCredentials;

namespace VirtoCommerce.PaymentModule.Model.Requests
{
    public class PostProcessPaymentRequestResult : PaymentRequestResultBase, IStoredCredentialPaymentResult
    {
        public string ReturnUrl { get; set; }

        public string OrderId { get; set; }

        public string OuterId { get; set; }

        /// <inheritdoc />
        public StoredPaymentMethod StoredPaymentMethod { get; set; }
    }
}
