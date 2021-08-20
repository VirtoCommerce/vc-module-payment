using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.PaymentModule.Core.Model.Search;
using VirtoCommerce.PaymentModule.Core.Services;
using VirtoCommerce.PaymentModule.Data.Caching;
using VirtoCommerce.PaymentModule.Data.Model;
using VirtoCommerce.PaymentModule.Data.Repositories;
using VirtoCommerce.Platform.Caching;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Data.GenericCrud;
using VirtoCommerce.Platform.Data.Infrastructure;

namespace VirtoCommerce.PaymentModule.Data.Services
{
    public class PaymentMethodsSearchService : SearchService<PaymentMethodsSearchCriteria, PaymentMethodsSearchResult, PaymentMethod, StorePaymentMethodEntity>,  IPaymentMethodsSearchService
    {
        private readonly ISettingsManager _settingsManager;

        public PaymentMethodsSearchService(Func<IPaymentRepository> repositoryFactory, IPlatformMemoryCache platformMemoryCache,
            IPaymentMethodsService paymentMethodsService, ISettingsManager settingsManager)
           : base(repositoryFactory, platformMemoryCache, (ICrudService<PaymentMethod>)paymentMethodsService)
        {
            _settingsManager = settingsManager;
        }

        public async override Task<PaymentMethodsSearchResult> SearchAsync(PaymentMethodsSearchCriteria criteria)
        {
            return await SearchPaymentMethodsAsync(criteria);
        }


        protected override IQueryable<StorePaymentMethodEntity> BuildQuery(IRepository repository, PaymentMethodsSearchCriteria criteria)
        {
            var query = ((IPaymentRepository)repository).PaymentMethods;

            if (!string.IsNullOrEmpty(criteria.Keyword))
            {
                query = query.Where(x => x.Code.Contains(criteria.Keyword) || x.Id.Contains(criteria.Keyword));
            }

            if (!criteria.StoreId.IsNullOrEmpty())
            {
                query = query.Where(x => x.StoreId == criteria.StoreId);
            }

            if (!criteria.Codes.IsNullOrEmpty())
            {
                query = query.Where(x => criteria.Codes.Contains(x.Code));
            }

            if (criteria.IsActive.HasValue)
            {
                query = query.Where(x => x.IsActive == criteria.IsActive.Value);
            }

            return query;
        }

        protected override IList<SortInfo> BuildSortExpression(PaymentMethodsSearchCriteria criteria)
        {
            var sortInfos = criteria.SortInfos;
            if (sortInfos.IsNullOrEmpty())
            {
                sortInfos = new[]
                {
                    new SortInfo{ SortColumn = nameof(StorePaymentMethodEntity.Code) }
                };
            }

            return sortInfos;
        }

        private Task<PaymentMethodsSearchResult> SearchPaymentMethodsAsync(PaymentMethodsSearchCriteria criteria)
        {
            var cacheKey = CacheKey.With(GetType(), nameof(SearchAsync), criteria.GetCacheKey());
            return  _platformMemoryCache.GetOrCreateExclusiveAsync(cacheKey, async cacheEntry =>
            {
                var result = AbstractTypeFactory<PaymentMethodsSearchResult>.TryCreateInstance();
                cacheEntry.AddExpirationToken(GenericSearchCachingRegion<PaymentMethod>.CreateChangeToken());
                using (var repository = _repositoryFactory())
                {
                    //Optimize performance and CPU usage
                    repository.DisableChangesTracking();

                    var sortInfos = BuildSortExpression(criteria);
                    var query = BuildQuery(repository, criteria);

                    var needExecuteCount = criteria.Take == 0;

                    if (criteria.Take > 0)
                    {
                        var ids = await query.OrderBySortInfos(sortInfos).ThenBy(x => x.Id)
                                         .Select(x => x.Id)
                                         .Skip(criteria.Skip).Take(criteria.Take)
                                         .ToArrayAsync();

                        result.TotalCount = ids.Count();
                        if (criteria.Skip > 0 || result.TotalCount == criteria.Take)

                        {
                            needExecuteCount = true;
                        }
                        result.Results = (await _crudService.GetByIdsAsync(ids, criteria.ResponseGroup)).OrderBy(x => Array.IndexOf(ids, x.Id)).ToList();
                    }

                    if (needExecuteCount)
                    {
                        result.TotalCount = await query.CountAsync();
                    }
                }
                result = await ProcessPaymentMethodSearchResult(result, criteria);
                return result;
            });
        }

        private async Task<PaymentMethodsSearchResult> ProcessPaymentMethodSearchResult(PaymentMethodsSearchResult result, PaymentMethodsSearchCriteria criteria) {
            var tmpSkip = Math.Min(result.TotalCount, criteria.Skip);
            var tmpTake = Math.Min(criteria.Take, Math.Max(0, result.TotalCount - criteria.Skip));
            criteria.Skip -= tmpSkip;
            criteria.Take -= tmpTake;
            if (criteria.Take > 0 && !criteria.WithoutTransient)
            {
                var transientMethodsQuery = AbstractTypeFactory<PaymentMethod>.AllTypeInfos.Select(x => AbstractTypeFactory<PaymentMethod>.TryCreateInstance(x.Type.Name))
                                                                              .OfType<PaymentMethod>().AsQueryable();
                if (!string.IsNullOrEmpty(criteria.Keyword))
                {
                    transientMethodsQuery = transientMethodsQuery.Where(x => x.Code.Contains(criteria.Keyword));
                }

                if (criteria.IsActive.HasValue)
                {
                    transientMethodsQuery = transientMethodsQuery.Where(x => x.IsActive == criteria.IsActive.Value);
                }
                var allPersistentTypes = result.Results.Select(x => x.GetType()).Distinct();
                transientMethodsQuery = transientMethodsQuery.Where(x => !allPersistentTypes.Contains(x.GetType()));

                result.TotalCount += transientMethodsQuery.Count();
                var transientProviders = transientMethodsQuery.Skip(criteria.Skip).Take(criteria.Take).ToList();

                foreach (var transientProvider in transientProviders)
                {
                    await _settingsManager.DeepLoadSettingsAsync(transientProvider);
                }

                var sortInfos = BuildSortExpression(criteria);
                result.Results = result.Results.Concat(transientProviders).AsQueryable().OrderBySortInfos(sortInfos).ToList();
            }

            return result;
        }
    }
}
