namespace VirtoCommerce.PaymentModule.Core.Model.StoredCredentials
{
    /// <summary>
    /// The payment parameters a storefront posts for stored credentials, e.g. with x-order's <c>authorizePayment</c>.
    /// <c>IStoredPaymentMethodManager.PrepareAsync</c> turns them into the typed fields of <c>IStoredCredentialPaymentRequest</c>.
    /// </summary>
    public static class StoredCredentialParameterNames
    {
        /// <summary>The ID of the saved card to pay with.</summary>
        public const string StoredPaymentMethodId = "storedPaymentMethodId";

        /// <summary>"true" to save the card when the payment is approved.</summary>
        public const string StoreForFutureUse = "storeForFutureUse";

        /// <summary>The consent record that covers saving the card.</summary>
        public const string ConsentId = "consentId";

        /// <summary>Optional <see cref="StoredCredentialUsage"/> name; defaults to Recurring with a consent and CardOnFile without.</summary>
        public const string StoredCredentialUsage = "storedCredentialUsage";
    }
}
