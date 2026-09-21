using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.CoreModule.Core.Tax;
using VirtoCommerce.PaymentModule.Model.Requests;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.PaymentModule.Core.Model
{
    public abstract class PaymentMethod : Entity, IHasSettings, IHasTaxDetalization, ITaxable, ICloneable
    {
        protected PaymentMethod(string code)
        {
            Code = Name = code;
            Settings = Array.Empty<ObjectSettingEntry>();
        }

        /// <summary>
        /// Method identity property (system name)
        /// </summary>
        public string Code { get; set; }
        public string Name { get; set; }
        public string LogoUrl { get; set; }
        public bool IsActive { get; set; }
        public int Priority { get; set; }

        public bool IsAvailableForPartial { get; set; }

        /// <summary>
        /// Allow the order to go to processing without payment being received
        /// </summary>
        public bool AllowDeferredPayment { get; set; }

        public string Currency { get; set; }

        public virtual decimal Price { get; set; }
        public virtual decimal PriceWithTax => Price + Price * TaxPercentRate;

        public virtual decimal Total => Price - DiscountAmount;

        public virtual decimal TotalWithTax => PriceWithTax - DiscountAmountWithTax;

        public virtual decimal DiscountAmount { get; set; }
        public virtual decimal DiscountAmountWithTax
        {
            get
            {
                return DiscountAmount + DiscountAmount * TaxPercentRate;
            }
        }

        public virtual bool AllowCartPayment => false;

        public string StoreId { get; set; }

        public string Description { get; set; }

        #region IHasSettings Members

        public virtual string TypeName => GetType().Name;

        /// <summary>
        /// Settings of payment method
        /// </summary>
        public ICollection<ObjectSettingEntry> Settings { get; set; }

        #endregion

        #region ITaxable Members

        /// <summary>
        /// Tax category or type
        /// </summary>
        public string TaxType { get; set; }

        public decimal TaxTotal => TotalWithTax - Total;

        public decimal TaxPercentRate { get; set; }

        #endregion

        #region ITaxDetailSupport Members

        public ICollection<TaxDetail> TaxDetails { get; set; }

        #endregion

        public LocalizedString LocalizedName { get; set; }

        /// <summary>
        /// Type of payment method
        /// </summary>
        public abstract PaymentMethodType PaymentMethodType { get; }

        /// <summary>
        /// Type of payment method group
        /// </summary>
        public abstract PaymentMethodGroupType PaymentMethodGroupType { get; }

        /// <summary>
        /// Processes the payment asynchronously.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual Task<ProcessPaymentRequestResult> ProcessPaymentAsync(ProcessPaymentRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Post processes the payment asynchronously.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual Task<PostProcessPaymentRequestResult> PostProcessPaymentAsync(PostProcessPaymentRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public virtual Task<VoidPaymentRequestResult> VoidProcessPaymentAsync(VoidPaymentRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public virtual Task<CapturePaymentRequestResult> CaptureProcessPaymentAsync(CapturePaymentRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public virtual Task<RefundPaymentRequestResult> RefundProcessPaymentAsync(RefundPaymentRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public virtual Task<ValidatePostProcessRequestResult> ValidatePostProcessRequestAsync(NameValueCollection queryString, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        
        #region ICloneable members

        public virtual object Clone()
        {
            var result = MemberwiseClone() as PaymentMethod;

            result.Settings = Settings?.Select(x => x.Clone()).OfType<ObjectSettingEntry>().ToList();
            result.TaxDetails = TaxDetails?.Select(x => x.Clone()).OfType<TaxDetail>().ToList();

            return result;
        }

        #endregion
    }
}
