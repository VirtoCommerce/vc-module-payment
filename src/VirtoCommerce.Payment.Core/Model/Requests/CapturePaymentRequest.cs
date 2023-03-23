namespace VirtoCommerce.PaymentModule.Model.Requests
{
    public class CapturePaymentRequest : PaymentRequestBase
    {
        public decimal? CaptureAmount { get; set; }
    }
}
