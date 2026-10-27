using System.Threading.Tasks;
using VirtoCommerce.PaymentModule.Core.Model.StoredCredentials;

namespace VirtoCommerce.PaymentModule.Core.Services
{
    /// <summary>
    /// Tells whether something depends on a saved card, so it isn't removed while in use.
    /// Register an implementation in a module that charges saved cards (e.g. subscriptions);
    /// <see cref="IStoredPaymentMethodManager.RemoveAsync"/> asks every registered provider.
    /// </summary>
    public interface IStoredPaymentMethodUsageProvider
    {
        Task<StoredPaymentMethodUsage> GetUsageAsync(StoredPaymentMethod storedPaymentMethod);
    }
}
