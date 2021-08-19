using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.PaymentModule.Data.Model;
using VirtoCommerce.Platform.Data.Infrastructure;

namespace VirtoCommerce.PaymentModule.Data.Repositories
{
    public class PaymentRepository : DbContextRepositoryBase<PaymentDbContext>, IPaymentRepository
    {
        public PaymentRepository(PaymentDbContext dbContext) : base(dbContext)
        {
        }

        public IQueryable<StorePaymentMethodEntity> PaymentMethods => DbContext.Set<StorePaymentMethodEntity>();

        public async Task<IEnumerable<StorePaymentMethodEntity>> GetByIdsAsync(IEnumerable<string> ids, string responseGroup = null)
        {
            return await PaymentMethods.Where(x => ids.Contains(x.Id))
                                            .ToArrayAsync();
        }
    }
}
