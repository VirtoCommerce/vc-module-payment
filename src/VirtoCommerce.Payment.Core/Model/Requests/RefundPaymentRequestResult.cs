using VirtoCommerce.PaymentModule.Core.Model;

namespace VirtoCommerce.PaymentModule.Model.Requests
{
    public class RefundPaymentRequestResult : PaymentRequestResultBase
    {
        public RefundStatus NewRefundStatus { get; set; }
    }
}
