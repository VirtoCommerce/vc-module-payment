using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
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
        /// Method that contains logic of registration payment in external payment system
        /// </summary>
        /// <returns>Result of registration payment in external payment system</returns>
        public virtual ProcessPaymentRequestResult ProcessPayment(ProcessPaymentRequest request)
        {
            ThrowIfSyncAndAsyncMethodsAreNotOverridden(nameof(ProcessPayment), nameof(ProcessPaymentAsync));

            return ProcessPaymentAsync(request).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Method that contains logic of checking payment status of payment in external payment system
        /// </summary>
        /// <returns>Result of checking payment in external payment system</returns>
        public virtual PostProcessPaymentRequestResult PostProcessPayment(PostProcessPaymentRequest request)
        {
            ThrowIfSyncAndAsyncMethodsAreNotOverridden(nameof(PostProcessPayment), nameof(PostProcessPaymentAsync));

            return PostProcessPaymentAsync(request).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Voids the payment
        /// </summary>
        /// <returns>Result of voiding payment in external payment system</returns>
        public virtual VoidPaymentRequestResult VoidProcessPayment(VoidPaymentRequest request)
        {
            ThrowIfSyncAndAsyncMethodsAreNotOverridden(nameof(VoidProcessPayment), nameof(VoidProcessPaymentAsync));

            return VoidProcessPaymentAsync(request).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Capture authorized payment
        /// </summary>
        /// <param name="context"></param>
        /// <returns>Result of capturing payment in external system</returns>
        public virtual CapturePaymentRequestResult CaptureProcessPayment(CapturePaymentRequest request)
        {
            ThrowIfSyncAndAsyncMethodsAreNotOverridden(nameof(CaptureProcessPayment), nameof(CaptureProcessPaymentAsync));

            return CaptureProcessPaymentAsync(request).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Refund payment
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public virtual RefundPaymentRequestResult RefundProcessPayment(RefundPaymentRequest request)
        {
            ThrowIfSyncAndAsyncMethodsAreNotOverridden(nameof(RefundProcessPayment), nameof(RefundProcessPaymentAsync));

            return RefundProcessPaymentAsync(request).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Method that validates parameters in querystring of request to push URL
        /// </summary>
        /// <param name="queryString">Query string of payment push request (external payment system or storefront)</param>
        /// <returns>Validation result</returns>
        public virtual ValidatePostProcessRequestResult ValidatePostProcessRequest(NameValueCollection queryString)
        {
            ThrowIfSyncAndAsyncMethodsAreNotOverridden(nameof(ValidatePostProcessRequest), nameof(ValidatePostProcessRequestAsync));

            return ValidatePostProcessRequestAsync(queryString).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Processes the payment asynchronously.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual Task<ProcessPaymentRequestResult> ProcessPaymentAsync(ProcessPaymentRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ProcessPayment(request));
        }

        /// <summary>
        /// Post processes the payment asynchronously.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual Task<PostProcessPaymentRequestResult> PostProcessPaymentAsync(PostProcessPaymentRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PostProcessPayment(request));
        }

        public virtual Task<VoidPaymentRequestResult> VoidProcessPaymentAsync(VoidPaymentRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(VoidProcessPayment(request));
        }

        public virtual Task<CapturePaymentRequestResult> CaptureProcessPaymentAsync(CapturePaymentRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CaptureProcessPayment(request));
        }

        public virtual Task<RefundPaymentRequestResult> RefundProcessPaymentAsync(RefundPaymentRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(RefundProcessPayment(request));
        }

        public virtual Task<ValidatePostProcessRequestResult> ValidatePostProcessRequestAsync(NameValueCollection queryString, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ValidatePostProcessRequest(queryString));
        }

        protected void ThrowIfSyncAndAsyncMethodsAreNotOverridden(string syncMethodName, string asyncMethodName)
        {
            var derivedType = GetType();
            var syncMethod = derivedType.GetMethod(syncMethodName, BindingFlags.Public | BindingFlags.Instance);
            var asyncMethod = derivedType.GetMethod(asyncMethodName, BindingFlags.Public | BindingFlags.Instance);

            if (syncMethod?.DeclaringType == typeof(PaymentMethod) && asyncMethod?.DeclaringType == typeof(PaymentMethod))
            {
                throw new NotImplementedException($"Override {syncMethodName} or {asyncMethodName} in your payment method.");
            }
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
