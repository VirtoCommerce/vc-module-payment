namespace VirtoCommerce.PaymentModule.Core.Model
{
    /// <summary>
    /// Marks a payment method that can save a payment method for future use (card on file)
    /// and charge it later without the customer present (merchant-initiated transactions).
    /// Implementations override <see cref="PaymentMethod.StorePaymentMethodAsync"/>,
    /// <see cref="PaymentMethod.ProcessStoredPaymentAsync"/> and <see cref="PaymentMethod.DeleteStoredPaymentMethodAsync"/>.
    /// </summary>
    public interface ISupportStoredCredentialFlow
    {
    }
}
