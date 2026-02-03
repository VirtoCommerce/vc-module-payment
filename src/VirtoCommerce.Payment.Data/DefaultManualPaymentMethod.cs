using System;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.PaymentModule.Model.Requests;

namespace VirtoCommerce.PaymentModule.Data
{
    public class DefaultManualPaymentMethod : PaymentMethod, ISupportCaptureFlow, ISupportRefundFlow
    {
        public DefaultManualPaymentMethod() : base("DefaultManualPaymentMethod")
        {
            Name = "Test payment method";
        }

        public override PaymentMethodType PaymentMethodType => PaymentMethodType.Unknown;

        public override PaymentMethodGroupType PaymentMethodGroupType => PaymentMethodGroupType.Manual;

        public override Task<ProcessPaymentRequestResult> ProcessPaymentAsync(ProcessPaymentRequest request, CancellationToken cancellationToken = default)
        {
            //context.Payment.PaymentStatus = PaymentStatus.Paid;
            //context.Payment.OuterId = context.Payment.Number;
            //context.Payment.CapturedDate = DateTime.UtcNow;
            //         context.Payment.IsApproved = true;
            var result = new ProcessPaymentRequestResult { IsSuccess = true, NewPaymentStatus = PaymentStatus.Paid };
            return Task.FromResult(result);
        }

        public override Task<PostProcessPaymentRequestResult> PostProcessPaymentAsync(PostProcessPaymentRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task<VoidPaymentRequestResult> VoidProcessPaymentAsync(VoidPaymentRequest request, CancellationToken cancellationToken = default)
        {
            //context.Payment.IsApproved = false;
            //context.Payment.PaymentStatus = PaymentStatus.Voided;
            //context.Payment.VoidedDate = DateTime.UtcNow;
            //context.Payment.IsCancelled = true;
            //context.Payment.CancelledDate = DateTime.UtcNow;
            var result = new VoidPaymentRequestResult { IsSuccess = true, NewPaymentStatus = PaymentStatus.Voided };
            return Task.FromResult(result);
        }

        public override Task<CapturePaymentRequestResult> CaptureProcessPaymentAsync(CapturePaymentRequest request, CancellationToken cancellationToken = default)
        {
            //context.Payment.IsApproved = true;
            //context.Payment.PaymentStatus = PaymentStatus.Paid;
            //context.Payment.VoidedDate = DateTime.UtcNow;
            var result = new CapturePaymentRequestResult { IsSuccess = true, NewPaymentStatus = PaymentStatus.Paid };
            return Task.FromResult(result);
        }

        public override Task<RefundPaymentRequestResult> RefundProcessPaymentAsync(RefundPaymentRequest request, CancellationToken cancellationToken = default)
        {
            var result = new RefundPaymentRequestResult { IsSuccess = true, NewRefundStatus = RefundStatus.Processed };
            return Task.FromResult(result);
        }

        public override Task<ValidatePostProcessRequestResult> ValidatePostProcessRequestAsync(NameValueCollection queryString, CancellationToken cancellationToken = default)
        {
            var result = new ValidatePostProcessRequestResult { IsSuccess = false };
            return Task.FromResult(result);
        }
    }
}
