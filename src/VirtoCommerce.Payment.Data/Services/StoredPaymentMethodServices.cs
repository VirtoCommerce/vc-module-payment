using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using VirtoCommerce.PaymentModule.Core.Events;
using VirtoCommerce.PaymentModule.Core.Model.StoredCredentials;
using VirtoCommerce.PaymentModule.Core.Services;
using VirtoCommerce.PaymentModule.Data.Model;
using VirtoCommerce.PaymentModule.Data.Repositories;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Data.GenericCrud;

namespace VirtoCommerce.PaymentModule.Data.Services
{
    public class StoredPaymentMethodService : CrudService<StoredPaymentMethod, StoredPaymentMethodEntity, StoredPaymentMethodChangingEvent, StoredPaymentMethodChangedEvent>, IStoredPaymentMethodService
    {
        public StoredPaymentMethodService(Func<IPaymentRepository> repositoryFactory, IPlatformMemoryCache platformMemoryCache, IEventPublisher eventPublisher)
            : base(repositoryFactory, platformMemoryCache, eventPublisher)
        {
        }

        protected override Task<IList<StoredPaymentMethodEntity>> LoadEntities(IRepository repository, IList<string> ids, string responseGroup)
        {
            return ((IPaymentRepository)repository).GetStoredPaymentMethodsByIdsAsync(ids, responseGroup);
        }
    }

    public class StoredPaymentMethodSearchService : SearchService<StoredPaymentMethodSearchCriteria, StoredPaymentMethodSearchResult, StoredPaymentMethod, StoredPaymentMethodEntity>, IStoredPaymentMethodSearchService
    {
        public StoredPaymentMethodSearchService(Func<IPaymentRepository> repositoryFactory, IPlatformMemoryCache platformMemoryCache, IStoredPaymentMethodService crudService, IOptions<CrudOptions> crudOptions)
            : base(repositoryFactory, platformMemoryCache, crudService, crudOptions)
        {
        }

        protected override IQueryable<StoredPaymentMethodEntity> BuildQuery(IRepository repository, StoredPaymentMethodSearchCriteria criteria)
        {
            var query = ((IPaymentRepository)repository).StoredPaymentMethods;

            if (!string.IsNullOrEmpty(criteria.StoreId))
            {
                query = query.Where(x => x.StoreId == criteria.StoreId);
            }

            if (!string.IsNullOrEmpty(criteria.CustomerId))
            {
                query = query.Where(x => x.CustomerId == criteria.CustomerId);
            }

            if (!string.IsNullOrEmpty(criteria.OrganizationId))
            {
                query = query.Where(x => x.OrganizationId == criteria.OrganizationId);
            }

            if (!string.IsNullOrEmpty(criteria.PaymentMethodCode))
            {
                query = query.Where(x => x.PaymentMethodCode == criteria.PaymentMethodCode);
            }

            if (!criteria.Statuses.IsNullOrEmpty())
            {
                query = query.Where(x => criteria.Statuses.Contains(x.Status));
            }

            if (!string.IsNullOrEmpty(criteria.InitialOrderId))
            {
                query = query.Where(x => x.InitialOrderId == criteria.InitialOrderId);
            }

            if (!string.IsNullOrEmpty(criteria.InitialPaymentId))
            {
                query = query.Where(x => x.InitialPaymentId == criteria.InitialPaymentId);
            }

            return query;
        }

        protected override IList<SortInfo> BuildSortExpression(StoredPaymentMethodSearchCriteria criteria)
        {
            var sortInfos = criteria.SortInfos;

            if (sortInfos.IsNullOrEmpty())
            {
                sortInfos =
                [
                    new SortInfo { SortColumn = nameof(StoredPaymentMethodEntity.IsDefault), SortDirection = SortDirection.Descending },
                    new SortInfo { SortColumn = nameof(StoredPaymentMethodEntity.CreatedDate), SortDirection = SortDirection.Descending },
                    new SortInfo { SortColumn = nameof(StoredPaymentMethodEntity.Id) },
                ];
            }

            return sortInfos;
        }
    }

    public class PaymentConsentService : CrudService<PaymentConsent, PaymentConsentEntity, PaymentConsentChangingEvent, PaymentConsentChangedEvent>, IPaymentConsentService
    {
        public PaymentConsentService(Func<IPaymentRepository> repositoryFactory, IPlatformMemoryCache platformMemoryCache, IEventPublisher eventPublisher)
            : base(repositoryFactory, platformMemoryCache, eventPublisher)
        {
        }

        protected override Task<IList<PaymentConsentEntity>> LoadEntities(IRepository repository, IList<string> ids, string responseGroup)
        {
            return ((IPaymentRepository)repository).GetPaymentConsentsByIdsAsync(ids, responseGroup);
        }
    }

    public class PaymentConsentSearchService : SearchService<PaymentConsentSearchCriteria, PaymentConsentSearchResult, PaymentConsent, PaymentConsentEntity>, IPaymentConsentSearchService
    {
        public PaymentConsentSearchService(Func<IPaymentRepository> repositoryFactory, IPlatformMemoryCache platformMemoryCache, IPaymentConsentService crudService, IOptions<CrudOptions> crudOptions)
            : base(repositoryFactory, platformMemoryCache, crudService, crudOptions)
        {
        }

        protected override IQueryable<PaymentConsentEntity> BuildQuery(IRepository repository, PaymentConsentSearchCriteria criteria)
        {
            var query = ((IPaymentRepository)repository).PaymentConsents;

            if (!string.IsNullOrEmpty(criteria.StoreId))
            {
                query = query.Where(x => x.StoreId == criteria.StoreId);
            }

            if (!string.IsNullOrEmpty(criteria.CustomerId))
            {
                query = query.Where(x => x.CustomerId == criteria.CustomerId);
            }

            if (!string.IsNullOrEmpty(criteria.StoredPaymentMethodId))
            {
                query = query.Where(x => x.StoredPaymentMethodId == criteria.StoredPaymentMethodId);
            }

            if (!string.IsNullOrEmpty(criteria.Purpose))
            {
                query = query.Where(x => x.Purpose == criteria.Purpose);
            }

            if (!string.IsNullOrEmpty(criteria.RelatedObjectType))
            {
                query = query.Where(x => x.ObjectType == criteria.RelatedObjectType);
            }

            if (!string.IsNullOrEmpty(criteria.RelatedObjectId))
            {
                query = query.Where(x => x.ObjectId == criteria.RelatedObjectId);
            }

            return query;
        }

        protected override IList<SortInfo> BuildSortExpression(PaymentConsentSearchCriteria criteria)
        {
            var sortInfos = criteria.SortInfos;

            if (sortInfos.IsNullOrEmpty())
            {
                sortInfos =
                [
                    new SortInfo { SortColumn = nameof(PaymentConsentEntity.AcceptedDate), SortDirection = SortDirection.Descending },
                    new SortInfo { SortColumn = nameof(PaymentConsentEntity.Id) },
                ];
            }

            return sortInfos;
        }
    }
}
