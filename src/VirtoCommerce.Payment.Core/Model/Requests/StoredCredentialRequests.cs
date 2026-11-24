using System;
using VirtoCommerce.PaymentModule.Core.Model.StoredCredentials;

namespace VirtoCommerce.PaymentModule.Model.Requests
{
    /// <summary>
    /// Saves a payment method at the provider without charging it (or with a $0 / small verification),
    /// e.g. for a free trial or a card added from the customer's account.
    /// Provider-specific data (hosted-field tokens, nonces) is passed in <see cref="PaymentRequestBase.Parameters"/>.
    /// </summary>
    public class StorePaymentMethodRequest : PaymentRequestBase
    {
        public string CustomerId { get; set; }
        public string OrganizationId { get; set; }
        public StoredCredentialUsage Usage { get; set; } = StoredCredentialUsage.Recurring;

        /// <summary>Run a $0 (or minimal) authorization to verify the card.</summary>
        public bool Verify { get; set; } = true;

        public string ConsentId { get; set; }
    }

    /// <summary>
    /// Common result of stored-credential operations, normalized across providers.
    /// </summary>
    public class StoredCredentialResultBase : PaymentRequestResultBase
    {
        public ChargeOutcome Outcome { get; set; }

        /// <summary>Raw provider decline or error code. For logs only; never show to customers.</summary>
        public string RawCode { get; set; }

        /// <summary>Raw provider message. For logs only; never show to customers.</summary>
        public string RawMessage { get; set; }

        /// <summary>Network advice code (e.g. Mastercard Merchant Advice Code) when the provider returns it.</summary>
        public string AdviceCode { get; set; }

        /// <summary>For <see cref="ChargeOutcome.SoftDecline"/>: the earliest time a retry makes sense.</summary>
        public TimeSpan? RetryAfter { get; set; }

        /// <summary>For <see cref="ChargeOutcome.RequiresAction"/>: where the customer completes authentication.</summary>
        public string RecoveryUrl { get; set; }

        /// <summary>True when the provider will retry on its own (e.g. Adyen Auto Rescue); the platform must not retry.</summary>
        public bool IsProviderRetryScheduled { get; set; }
    }

    public class StorePaymentMethodRequestResult : StoredCredentialResultBase
    {
        /// <summary>The stored payment method built by the provider. Not yet persisted; the caller saves it.</summary>
        public StoredPaymentMethod StoredPaymentMethod { get; set; }
    }

    /// <summary>
    /// Charges a stored payment method from the server, without the customer present (merchant-initiated transaction).
    /// </summary>
    public class ProcessStoredPaymentRequest : PaymentRequestBase
    {
        public StoredPaymentMethod StoredPaymentMethod { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }

        /// <summary>Same key for retries of the same charge, so a replay never charges twice.</summary>
        public string IdempotencyKey { get; set; }

        public StoredCredentialUsage Usage { get; set; } = StoredCredentialUsage.Recurring;
        public StoredCredentialSequence Sequence { get; set; } = StoredCredentialSequence.Subsequent;
        public MerchantInitiatedReason Reason { get; set; } = MerchantInitiatedReason.Recurring;
        public StoredPaymentCaptureMode CaptureMode { get; set; } = StoredPaymentCaptureMode.AuthorizeOnly;
    }

    public class ProcessStoredPaymentRequestResult : StoredCredentialResultBase
    {
        public string TransactionId { get; set; }
        public string NetworkTransactionId { get; set; }
        public string AuthorizationCode { get; set; }
        public decimal? Amount { get; set; }
    }

    public class DeleteStoredPaymentMethodRequest : PaymentRequestBase
    {
        public StoredPaymentMethod StoredPaymentMethod { get; set; }
    }

    public class DeleteStoredPaymentMethodRequestResult : PaymentRequestResultBase
    {
    }
}
