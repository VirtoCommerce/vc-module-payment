namespace VirtoCommerce.PaymentModule.Core.Model.StoredCredentials
{
    /// <summary>
    /// How a stored credential is used, as defined by the card network stored credential framework.
    /// </summary>
    public enum StoredCredentialUsage
    {
        /// <summary>Customer-initiated payments with a saved card (one-click checkout).</summary>
        CardOnFile,
        /// <summary>Merchant-initiated payments on a fixed schedule (subscriptions).</summary>
        Recurring,
        /// <summary>Merchant-initiated payments without a fixed schedule or amount.</summary>
        Unscheduled,
    }

    public enum StoredCredentialSequence
    {
        /// <summary>The first transaction that stores the credential (customer-initiated).</summary>
        First,
        /// <summary>A later transaction that uses the stored credential.</summary>
        Subsequent,
    }

    public enum MerchantInitiatedReason
    {
        Recurring,
        Unscheduled,
        Reauthorization,
        Resubmission,
        DelayedCharge,
    }

    public enum StoredPaymentCaptureMode
    {
        AuthorizeOnly,
        AuthorizeAndCapture,
    }

    /// <summary>
    /// Provider-independent result of a stored-credential operation.
    /// </summary>
    public enum ChargeOutcome
    {
        /// <summary>Authorized or captured.</summary>
        Succeeded,
        /// <summary>The issuer requires the customer to authenticate (3-D Secure). Never retried automatically.</summary>
        RequiresAction,
        /// <summary>Temporary decline (e.g. insufficient funds). Can be retried later.</summary>
        SoftDecline,
        /// <summary>Permanent decline (e.g. lost card, closed account). Never retried; a new payment method is needed.</summary>
        HardDecline,
        /// <summary>Network, timeout or configuration error. Safe to retry with the same idempotency key.</summary>
        ProviderError,
    }

    public static class StoredPaymentMethodStatus
    {
        public const string Active = "Active";
        public const string NeedsUpdate = "NeedsUpdate";
        public const string Expired = "Expired";
        public const string Revoked = "Revoked";
    }
}
