using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Domain;

namespace VirtoCommerce.PaymentModule.Data.Model
{
    public class StorePaymentMethodEntity : Entity, IDataEntity<StorePaymentMethodEntity, PaymentMethod>
    {
        [Required]
        [StringLength(128)]
        public string Code { get; set; }

        public int Priority { get; set; }

        [StringLength(2048)]
        public string LogoUrl { get; set; }

        public bool IsActive { get; set; }

        public bool IsAvailableForPartial { get; set; }

        public bool AllowDeferredPayment { get; set; }

        public string TypeName { get; set; }

        public string StoreId { get; set; }

        public string Description { get; set; }

        public ObservableCollection<PaymentMethodLocalizedNameEntity> LocalizedNames { get; set; }
            = new NullCollection<PaymentMethodLocalizedNameEntity>();

        public virtual PaymentMethod ToModel(PaymentMethod paymentMethod)
        {
            if (paymentMethod == null)
            {
                throw new ArgumentNullException(nameof(paymentMethod));
            }

            paymentMethod.Id = Id;
            paymentMethod.IsActive = IsActive;
            paymentMethod.Code = Code;
            paymentMethod.IsAvailableForPartial = IsAvailableForPartial;
            paymentMethod.AllowDeferredPayment = AllowDeferredPayment;
            paymentMethod.LogoUrl = LogoUrl;
            paymentMethod.Priority = Priority;
            paymentMethod.StoreId = StoreId;
            paymentMethod.Description = Description;

            if (LocalizedNames != null)
            {
                paymentMethod.LocalizedName = new LocalizedString();
                foreach (var localizedName in LocalizedNames)
                {
                    paymentMethod.LocalizedName.SetValue(localizedName.LanguageCode, localizedName.Value);
                }
            }

            return paymentMethod;
        }

        public virtual StorePaymentMethodEntity FromModel(PaymentMethod paymentMethod, PrimaryKeyResolvingMap pkMap)
        {
            if (paymentMethod == null)
            {
                throw new ArgumentNullException(nameof(paymentMethod));
            }

            pkMap.AddPair(paymentMethod, this);

            Id = paymentMethod.Id;
            IsActive = paymentMethod.IsActive;
            Code = paymentMethod.Code;
            IsAvailableForPartial = paymentMethod.IsAvailableForPartial;
            AllowDeferredPayment = paymentMethod.AllowDeferredPayment;
            LogoUrl = paymentMethod.LogoUrl;
            Priority = paymentMethod.Priority;
            StoreId = paymentMethod.StoreId;
            TypeName = paymentMethod.TypeName;
            Description = paymentMethod.Description;

            if (paymentMethod.LocalizedName != null)
            {
                LocalizedNames = new ObservableCollection<PaymentMethodLocalizedNameEntity>(paymentMethod.LocalizedName.Values
                    .Select(x =>
                    {
                        var entity = AbstractTypeFactory<PaymentMethodLocalizedNameEntity>.TryCreateInstance();
                        entity.LanguageCode = x.Key;
                        entity.Value = x.Value;
                        return entity;
                    }));
            }

            return this;
        }

        public virtual void Patch(StorePaymentMethodEntity target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            target.IsActive = IsActive;
            target.Code = Code;
            target.IsAvailableForPartial = IsAvailableForPartial;
            target.AllowDeferredPayment = AllowDeferredPayment;
            target.LogoUrl = LogoUrl;
            target.Priority = Priority;
            target.StoreId = StoreId;
            target.Description = Description;

            if (!LocalizedNames.IsNullCollection())
            {
                var localizedNameComparer = AnonymousComparer.Create((PaymentMethodLocalizedNameEntity x) => $"{x.Value}-{x.LanguageCode}");
                LocalizedNames.Patch(target.LocalizedNames, localizedNameComparer, (sourceValue, targetValue) => { });
            }
        }
    }
}
