using System;
using System.Threading.Tasks;
using VirtoCommerce.PaymentModule.Core.Model.Search;

namespace VirtoCommerce.PaymentModule.Core.Services
{
    public interface IPaymentMethodsSearchService
    {
        [Obsolete(@"Need to remove after inheriting IPaymentMethodsSearchService from ISearchService<PaymentMethodsSearchCriteria, PaymentMethodsSearchResult, PaymentMethod>.")]
        Task<PaymentMethodsSearchResult> SearchPaymentMethodsAsync(PaymentMethodsSearchCriteria criteria);
    }
}
