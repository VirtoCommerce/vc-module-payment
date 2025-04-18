using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using VirtoCommerce.PaymentModule.Core.Events;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.PaymentModule.Core.Model.Search;
using VirtoCommerce.PaymentModule.Core.Services;
using VirtoCommerce.PaymentModule.Data.Model;
using VirtoCommerce.PaymentModule.Data.Repositories;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Data.GenericCrud;

namespace VirtoCommerce.PaymentModule.Data.Services
{
    public class PaymentMethodsSearchService : SearchService<PaymentMethodsSearchCriteria, PaymentMethodsSearchResult, PaymentMethod, StorePaymentMethodEntity>, IPaymentMethodsSearchService
    {
        private readonly ISettingsManager _settingsManager;
        private readonly IEventPublisher _eventPublisher;

        public PaymentMethodsSearchService(
            Func<IPaymentRepository> repositoryFactory,
            IPlatformMemoryCache platformMemoryCache,
            IPaymentMethodsService crudService,
            IOptions<CrudOptions> crudOptions,
            ISettingsManager settingsManager,
            IEventPublisher eventPublisher)
           : base(repositoryFactory, platformMemoryCache, crudService, crudOptions)
        {
            _settingsManager = settingsManager;
            _eventPublisher = eventPublisher;
        }

        protected override IQueryable<StorePaymentMethodEntity> BuildQuery(IRepository repository, PaymentMethodsSearchCriteria criteria)
        {
            var query = ((IPaymentRepository)repository).PaymentMethods;

            // Return only registered payment methods
            var registeredPaymentCodes = AbstractTypeFactory<PaymentMethod>.AllTypeInfos
                .Select(x => AbstractTypeFactory<PaymentMethod>.TryCreateInstance(x.TypeName))
                .Select(x => x.Code)
                .ToArray();

            query = query.Where(x => registeredPaymentCodes.Contains(x.Code));

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

        protected override async Task<PaymentMethodsSearchResult> ProcessSearchResultAsync(PaymentMethodsSearchResult result, PaymentMethodsSearchCriteria criteria)
        {
            // throw this event in case there are modules than need some special work done before instancing payment methods (NativePaymentMethods for example)
            await _eventPublisher.Publish(new PaymentMethodInstancingEvent());

            var tmpSkip = Math.Min(result.TotalCount, criteria.Skip);
            var tmpTake = Math.Min(criteria.Take, Math.Max(0, result.TotalCount - criteria.Skip));
            criteria.Skip -= tmpSkip;
            criteria.Take -= tmpTake;

            if (criteria.Take > 0 && !criteria.WithoutTransient)
            {
                var transientMethodsQuery = AbstractTypeFactory<PaymentMethod>.AllTypeInfos
                    .Select(x => AbstractTypeFactory<PaymentMethod>.TryCreateInstance(x.Type.Name))
                    .AsQueryable();

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
