namespace VirtoCommerce.PaymentModule.Core.Model.StoredCredentials
{
    /// <summary>
    /// The result of preparing, adding, removing or changing a saved card.
    /// </summary>
    public class StoredPaymentMethodOperationResult
    {
        public bool IsSuccess { get; set; }

        /// <summary>A message for the customer when the operation failed.</summary>
        public string ErrorMessage { get; set; }

        /// <summary>The provider's outcome when the operation reached the provider, e.g. RequiresAction for 3-D Secure.</summary>
        public ChargeOutcome? Outcome { get; set; }

        /// <summary>The provider's raw code, for logs and support. Never shown to customers.</summary>
        public string RawCode { get; set; }

        public StoredPaymentMethod StoredPaymentMethod { get; set; }

        public static StoredPaymentMethodOperationResult Success(StoredPaymentMethod storedPaymentMethod = null)
        {
            return new StoredPaymentMethodOperationResult { IsSuccess = true, StoredPaymentMethod = storedPaymentMethod };
        }

        public static StoredPaymentMethodOperationResult Fail(string errorMessage)
        {
            return new StoredPaymentMethodOperationResult { ErrorMessage = errorMessage };
        }
    }
}
