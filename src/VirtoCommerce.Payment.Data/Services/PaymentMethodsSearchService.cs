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
        protected const string DefaultSortColumn = nameof(StorePaymentMethodEntity.Code);

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
                    new SortInfo{ SortColumn = DefaultSortColumn }
                };
            }

            return sortInfos;
        }

        protected static bool IsSingleAscendingDefaultSort(IList<SortInfo> sortInfos)
        {
            return sortInfos?.Count == 1
                && sortInfos[0].SortDirection == SortDirection.Ascending
                && DefaultSortColumn.EqualsIgnoreCase(sortInfos[0].SortColumn);
        }

        protected override async Task<PaymentMethodsSearchResult> ProcessSearchResultAsync(PaymentMethodsSearchResult result, PaymentMethodsSearchCriteria criteria)
        {
            // throw this event in case there are modules than need some special work done before instancing payment methods (NativePaymentMethods for example)
            await _eventPublisher.Publish(new PaymentMethodInstancingEvent());

            if (criteria.WithoutTransient)
            {
                return result;
            }

            // The saved methods first, then a new instance of each method that is not saved.
            var filteredTransientMethods = await GetTransientMethodsAsync(criteria);
            var savedCount = result.TotalCount;

            if (filteredTransientMethods.Count > 0)
            {
                var savedMethodTypes = await GetSavedMethodTypesAsync(result, criteria);

                filteredTransientMethods = filteredTransientMethods
                    .Where(x => !savedMethodTypes.Contains(x.GetType()))
                    .ToList();
            }

            // Counted on every page, so the total is the same on all pages.
            result.TotalCount += filteredTransientMethods.Count;

            // Locals, not the criteria: SearchBatchesAsync reuses the criteria for the next page.
            var skip = Math.Max(0, criteria.Skip - savedCount);
            var take = criteria.Take - Math.Max(0, savedCount - criteria.Skip);

            if (take <= 0)
            {
                return result;
            }

            var pagedTransientMethods = filteredTransientMethods
                .Skip(skip)
                .Take(take)
                .ToList();

            foreach (var transientMethod in pagedTransientMethods)
            {
                await _settingsManager.DeepLoadSettingsAsync(transientMethod);
            }

            var allMethods = result.Results.Concat(pagedTransientMethods);

            var sortInfos = BuildSortExpression(criteria);

            // Arbitrary sort columns (admin, cold) are worth OrderBySortInfos' compile; the default
            // order is not. Decided from what BuildSortExpression returned, so overriding that seam
            // still changes the sort.
            result.Results = IsSingleAscendingDefaultSort(sortInfos)
                ? allMethods.OrderBy(x => x.Code).ToList()
                : allMethods.AsQueryable().OrderBySortInfos(sortInfos).ToList();

            return result;
        }

        // A new instance of each registered method that matches the keyword and IsActive, saved or not.
        protected virtual Task<IList<PaymentMethod>> GetTransientMethodsAsync(PaymentMethodsSearchCriteria criteria)
        {
            // Not .AsQueryable().Where(...): that recompiles the expression tree per enumeration, a lock convoy under load.
            var transientMethods = AbstractTypeFactory<PaymentMethod>.AllTypeInfos
                .Select(x => AbstractTypeFactory<PaymentMethod>.TryCreateInstance(x.Type.Name));

            if (!string.IsNullOrEmpty(criteria.Keyword))
            {
                transientMethods = transientMethods.Where(x => x.Code.Contains(criteria.Keyword));
            }

            if (criteria.IsActive.HasValue)
            {
                transientMethods = transientMethods.Where(x => x.IsActive == criteria.IsActive.Value);
            }

            return Task.FromResult<IList<PaymentMethod>>(transientMethods.ToList());
        }

        // The types of all saved methods for the criteria. The result holds the page's saved rows, and its TotalCount is
        // still their count: a page without every saved row cannot tell from its own rows which methods are saved.
        protected virtual async Task<ISet<Type>> GetSavedMethodTypesAsync(PaymentMethodsSearchResult result, PaymentMethodsSearchCriteria criteria)
        {
            if (result.Results.Count == result.TotalCount)
            {
                return result.Results.Select(x => x.GetType()).ToHashSet();
            }

            var savedCriteria = criteria.CloneTyped();
            savedCriteria.Skip = 0;
            savedCriteria.Take = result.TotalCount;
            savedCriteria.WithoutTransient = true;

            var savedMethods = await base.SearchAsync(savedCriteria, clone: false);

            return savedMethods.Results.Select(x => x.GetType()).ToHashSet();
        }
    }
}
