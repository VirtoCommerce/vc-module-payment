using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.PaymentModule.Core.Events;
using VirtoCommerce.PaymentModule.Core.Services;
using VirtoCommerce.PaymentModule.Data.Model;
using VirtoCommerce.PaymentModule.Data.Repositories;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Data.GenericCrud;
using System.Linq;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.PaymentModule.Data.Services
{

    public class PaymentMethodsService : CrudService<PaymentMethod, StorePaymentMethodEntity, PaymentMethodChangeEvent, PaymentMethodChangedEvent>, IPaymentMethodsService, IPaymentMethodsRegistrar
    {
        private readonly ISettingsManager _settingManager;

        public PaymentMethodsService(Func<IPaymentRepository> repositoryFactory, IPlatformMemoryCache platformMemoryCache, IEventPublisher eventPublisher, ISettingsManager settingManager)
            : base(repositoryFactory, platformMemoryCache, eventPublisher)
        {
            _settingManager = settingManager;
        }

        public void RegisterPaymentMethod<T>(Func<T> factory = null) where T : PaymentMethod
        {
            if (AbstractTypeFactory<PaymentMethod>.AllTypeInfos.All(t => t.Type != typeof(T)))
            {
                var typeInfo = AbstractTypeFactory<PaymentMethod>.RegisterType<T>();
                if (factory != null)
                {
                    typeInfo.WithFactory(factory);
                }
            }
        }

        public Task<PaymentMethod[]> GetRegisteredPaymentMethods()
        {
            var result = Task.FromResult(
                AbstractTypeFactory<PaymentMethod>.AllTypeInfos
                .Select(x => AbstractTypeFactory<PaymentMethod>.TryCreateInstance(x.Type.Name))
                .ToArray());
            return result;
        }


        protected override Task<IEnumerable<StorePaymentMethodEntity>> LoadEntities(IRepository repository, IEnumerable<string> ids, string responseGroup)
        {
            return ((IPaymentRepository)repository).GetByIdsAsync(ids, responseGroup);
        }

        protected override PaymentMethod ProcessModel(string responseGroup, StorePaymentMethodEntity entity, PaymentMethod model)
        {
            var paymentMethod = AbstractTypeFactory<PaymentMethod>.TryCreateInstance(string.IsNullOrEmpty(entity.TypeName) ? entity.Code : entity.TypeName);
            if (paymentMethod != null)
            {
                entity.ToModel(paymentMethod);
                _settingManager.DeepLoadSettingsAsync(paymentMethod).GetAwaiter().GetResult();
                return paymentMethod;
            }
            return null;
        }

        protected override Task AfterSaveChangesAsync(IEnumerable<PaymentMethod> models, IEnumerable<GenericChangedEntry<PaymentMethod>> changedEntries)
        {
            return _settingManager.DeepSaveSettingsAsync(models);
        }

        protected override Task AfterDeleteAsync(IEnumerable<PaymentMethod> models, IEnumerable<GenericChangedEntry<PaymentMethod>> changedEntries)
        {
            return _settingManager.DeepRemoveSettingsAsync(models);
        }


        #region IPaymentMethodsService compatibility
        public async Task<PaymentMethod[]> GetByIdsAsync(string[] ids, string responseGroup)
        {
            return (await GetByIdsAsync((IEnumerable<string>)ids, responseGroup)).ToArray();
        }

        public Task SaveChangesAsync(PaymentMethod[] paymentMethods)
        {
            return SaveChangesAsync((IEnumerable<PaymentMethod>)paymentMethods);
        }

        public Task DeleteAsync(string[] ids)
        {
            return DeleteAsync((IEnumerable<string>)ids);
        }
        #endregion
    }
}
