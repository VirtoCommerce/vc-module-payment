using VirtoCommerce.PaymentModule.Core.Model.StoredCredentials;
using VirtoCommerce.Platform.Core.GenericCrud;

namespace VirtoCommerce.PaymentModule.Core.Services
{
    public interface IStoredPaymentMethodService : ICrudService<StoredPaymentMethod>
    {
    }

    public interface IStoredPaymentMethodSearchService : ISearchService<StoredPaymentMethodSearchCriteria, StoredPaymentMethodSearchResult, StoredPaymentMethod>
    {
    }

    public interface IPaymentConsentService : ICrudService<PaymentConsent>
    {
    }

    public interface IPaymentConsentSearchService : ISearchService<PaymentConsentSearchCriteria, PaymentConsentSearchResult, PaymentConsent>
    {
    }
}
