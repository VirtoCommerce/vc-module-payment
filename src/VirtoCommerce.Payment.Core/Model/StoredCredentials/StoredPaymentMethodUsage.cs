namespace VirtoCommerce.PaymentModule.Core.Model.StoredCredentials
{
    /// <summary>
    /// Whether something still depends on a saved card, e.g. subscriptions it pays.
    /// </summary>
    public class StoredPaymentMethodUsage
    {
        public bool IsInUse { get; set; }

        /// <summary>Why the card can't be removed, for the customer, e.g. "This card pays for 2 subscriptions."</summary>
        public string Description { get; set; }

        public static StoredPaymentMethodUsage NotInUse { get; } = new();

        public static StoredPaymentMethodUsage InUse(string description) => new() { IsInUse = true, Description = description };
    }
}
