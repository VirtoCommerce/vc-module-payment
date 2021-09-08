using System;
using System.Threading.Tasks;
using VirtoCommerce.PaymentModule.Core.Model;

namespace VirtoCommerce.PaymentModule.Core.Services
{
    public interface IPaymentMethodsService
    {
        [Obsolete(@"Need to remove after inheriting IPaymentMethodService from ICrudService<PaymentMethod>.")]
        Task<PaymentMethod[]> GetByIdsAsync(string[] ids, string responseGroup);
        [Obsolete(@"Need to remove after inheriting IPaymentMethodService from ICrudService<PaymentMethod>.")]
        Task<PaymentMethod> GetByIdAsync(string id, string responseGroup);
        [Obsolete(@"Need to remove after inheriting IPaymentMethodService from ICrudService<PaymentMethod>.")]
        Task SaveChangesAsync(PaymentMethod[] paymentMethods);
        [Obsolete(@"Need to remove after inheriting IPaymentMethodService from ICrudService<PaymentMethod>.")]
        Task DeleteAsync(string[] ids);
    }
}
