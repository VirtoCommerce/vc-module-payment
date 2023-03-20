using VirtoCommerce.PaymentModule.Core.Model;

namespace VirtoCommerce.PaymentModule.Model.Requests
{
    public class RefundPaymentRequestResult : PaymentRequestResultBase
    {
        public string OuterId { get; set; }
        public RefundStatus NewRefundStatus { get; set; }
    }
}
