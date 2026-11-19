namespace VirtoCommerce.PaymentModule.Core.Model.StoredCredentials
{
    /// <summary>
    /// Who pays and for what, when a payment saves a card or pays with a saved one.
    /// </summary>
    public class StoredPaymentContext
    {
        public string StoreId { get; set; }

        /// <summary>The customer the card belongs to: the order's customer.</summary>
        public string CustomerId { get; set; }

        public string OrganizationId { get; set; }

        /// <summary>The payment method (connector) code of the payment.</summary>
        public string PaymentMethodCode { get; set; }

        public string OrderId { get; set; }

        public string PaymentId { get; set; }
    }
}
