using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.PaymentModule.Core.Model.Search;
using VirtoCommerce.Platform.Core.GenericCrud;

namespace VirtoCommerce.PaymentModule.Core.Services
{
    public interface IPaymentMethodsSearchService : ISearchService<PaymentMethodsSearchCriteria, PaymentMethodsSearchResult, PaymentMethod>
    {
    }
}
