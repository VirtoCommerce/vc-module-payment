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
        [Obsolete("Use ProcessPaymentAsync instead.", DiagnosticId = "VC0012", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
        public virtual ProcessPaymentRequestResult ProcessPayment(ProcessPaymentRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Method that contains logic of checking payment status of payment in external payment system
        /// </summary>
        /// <returns>Result of checking payment in external payment system</returns>
        [Obsolete("Use PostProcessPaymentAsync instead.", DiagnosticId = "VC0012", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
        public virtual PostProcessPaymentRequestResult PostProcessPayment(PostProcessPaymentRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Voids the payment
        /// </summary>
        /// <returns>Result of voiding payment in external payment system</returns>
        [Obsolete("Use VoidProcessPaymentAsync instead.", DiagnosticId = "VC0012", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
        public virtual VoidPaymentRequestResult VoidProcessPayment(VoidPaymentRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Capture authorized payment
        /// </summary>
        /// <param name="context"></param>
        /// <returns>Result of capturing payment in external system</returns>
        [Obsolete("Use CaptureProcessPaymentAsync instead.", DiagnosticId = "VC0012", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
        public virtual CapturePaymentRequestResult CaptureProcessPayment(CapturePaymentRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Refund payment
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Obsolete("Use RefundProcessPaymentAsync instead.", DiagnosticId = "VC0012", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
        public virtual RefundPaymentRequestResult RefundProcessPayment(RefundPaymentRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Method that validates parameters in querystring of request to push URL
        /// </summary>
        /// <param name="queryString">Query string of payment push request (external payment system or storefront)</param>
        /// <returns>Validation result</returns>
        [Obsolete("Use ValidatePostProcessRequestAsync instead.", DiagnosticId = "VC0012", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]

        public virtual ValidatePostProcessRequestResult ValidatePostProcessRequest(NameValueCollection queryString)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Processes the payment asynchronously.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual Task<ProcessPaymentRequestResult> ProcessPaymentAsync(ProcessPaymentRequest request, CancellationToken cancellationToken = default)
        {
#pragma warning disable VC0012 // Type or member is obsolete
            return Task.FromResult(ProcessPayment(request));
#pragma warning restore VC0012 // Type or member is obsolete
        }

        /// <summary>
        /// Post processes the payment asynchronously.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual Task<PostProcessPaymentRequestResult> PostProcessPaymentAsync(PostProcessPaymentRequest request, CancellationToken cancellationToken = default)
        {
#pragma warning disable VC0012 // Type or member is obsolete
            return Task.FromResult(PostProcessPayment(request));
#pragma warning restore VC0012 // Type or member is obsolete
        }

        public virtual Task<VoidPaymentRequestResult> VoidProcessPaymentAsync(VoidPaymentRequest request, CancellationToken cancellationToken = default)
        {
#pragma warning disable VC0012 // Type or member is obsolete
            return Task.FromResult(VoidProcessPayment(request));
#pragma warning restore VC0012 // Type or member is obsolete
        }

        public virtual Task<CapturePaymentRequestResult> CaptureProcessPaymentAsync(CapturePaymentRequest request, CancellationToken cancellationToken = default)
        {
#pragma warning disable VC0012 // Type or member is obsolete
            return Task.FromResult(CaptureProcessPayment(request));
#pragma warning restore VC0012 // Type or member is obsolete
        }

        public virtual Task<RefundPaymentRequestResult> RefundProcessPaymentAsync(RefundPaymentRequest request, CancellationToken cancellationToken = default)
        {
#pragma warning disable VC0012 // Type or member is obsolete
            return Task.FromResult(RefundProcessPayment(request));
#pragma warning restore VC0012 // Type or member is obsolete
        }

        public virtual Task<ValidatePostProcessRequestResult> ValidatePostProcessRequestAsync(NameValueCollection queryString, CancellationToken cancellationToken = default)
        {
#pragma warning disable VC0012 // Type or member is obsolete
            return Task.FromResult(ValidatePostProcessRequest(queryString));
#pragma warning restore VC0012 // Type or member is obsolete
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
