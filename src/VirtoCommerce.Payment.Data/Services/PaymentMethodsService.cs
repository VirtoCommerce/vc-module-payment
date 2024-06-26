using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.PaymentModule.Core.Events;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.PaymentModule.Core.Services;
using VirtoCommerce.PaymentModule.Data.Model;
using VirtoCommerce.PaymentModule.Data.Repositories;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Data.GenericCrud;

namespace VirtoCommerce.PaymentModule.Data.Services
{
    public class PaymentMethodsService : CrudService<PaymentMethod, StorePaymentMethodEntity, PaymentMethodChangeEvent, PaymentMethodChangedEvent>, IPaymentMethodsService, IPaymentMethodsRegistrar
    {
        private readonly IEventPublisher _eventPublisher;
        private readonly ISettingsManager _settingManager;

        public PaymentMethodsService(
            Func<IPaymentRepository> repositoryFactory,
            IPlatformMemoryCache platformMemoryCache,
            IEventPublisher eventPublisher,
            ISettingsManager settingManager)
            : base(repositoryFactory, platformMemoryCache, eventPublisher)
        {
            _eventPublisher = eventPublisher;
            _settingManager = settingManager;
        }

        public void RegisterPaymentMethod<T>(Func<T> factory = null)
            where T : PaymentMethod
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
            var result = AbstractTypeFactory<PaymentMethod>.AllTypeInfos
                .Select(x => AbstractTypeFactory<PaymentMethod>.TryCreateInstance(x.Type.Name))
                .ToArray();

            return Task.FromResult(result);
        }


        protected override Task<IList<StorePaymentMethodEntity>> LoadEntities(IRepository repository, IList<string> ids, string responseGroup)
        {
            return ((IPaymentRepository)repository).GetByIdsAsync(ids, responseGroup);
        }

        protected override PaymentMethod ProcessModel(string responseGroup, StorePaymentMethodEntity entity, PaymentMethod model)
        {
            _settingManager.DeepLoadSettingsAsync(model).GetAwaiter().GetResult();
            return model;
        }

        protected override Task AfterSaveChangesAsync(IList<PaymentMethod> models, IList<GenericChangedEntry<PaymentMethod>> changedEntries)
        {
            return _settingManager.DeepSaveSettingsAsync(models);
        }

        protected override Task AfterDeleteAsync(IList<PaymentMethod> models, IList<GenericChangedEntry<PaymentMethod>> changedEntries)
        {
            return _settingManager.DeepRemoveSettingsAsync(models);
        }

        protected override PaymentMethod ToModel(StorePaymentMethodEntity entity)
        {
            // Publish this event in case there are modules that need some special work done before instancing a payment method.
            // For example, NativePaymentMethods registers its payment methods.
            _eventPublisher.Publish(new PaymentMethodInstancingEvent { PaymentMethodCodes = [entity.Code] }).GetAwaiter().GetResult();

            var typeName = entity.TypeName?.EmptyToNull() ?? entity.Code;
            var model = AbstractTypeFactory<PaymentMethod>.TryCreateInstance(typeName);
            return entity.ToModel(model);
        }
    }
}
