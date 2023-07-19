using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.PaymentModule.Data.Model;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.PaymentModule.Data.Repositories
{
    public interface IPaymentRepository : IRepository
    {
        IQueryable<StorePaymentMethodEntity> PaymentMethods { get; }
        Task<IList<StorePaymentMethodEntity>> GetByIdsAsync(IList<string> ids, string responseGroup = null);
    }
}
