using System;
using System.Collections.Specialized;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.PaymentModule.Model.Requests;

namespace VirtoCommerce.Payment.Tests;

public class TestPaymentMethod : PaymentMethod
{
    public TestPaymentMethod(string code) : base(code)
    {
    }

    public override PaymentMethodType PaymentMethodType { get; }
    public override PaymentMethodGroupType PaymentMethodGroupType { get; }

    public override ProcessPaymentRequestResult ProcessPayment(ProcessPaymentRequest request)
    {
        throw new NotImplementedException();
    }

    public override PostProcessPaymentRequestResult PostProcessPayment(PostProcessPaymentRequest request)
    {
        throw new NotImplementedException();
    }

    public override VoidPaymentRequestResult VoidProcessPayment(VoidPaymentRequest request)
    {
        throw new NotImplementedException();
    }

    public override CapturePaymentRequestResult CaptureProcessPayment(CapturePaymentRequest context)
    {
        throw new NotImplementedException();
    }

    public override RefundPaymentRequestResult RefundProcessPayment(RefundPaymentRequest context)
    {
        throw new NotImplementedException();
    }

    public override ValidatePostProcessRequestResult ValidatePostProcessRequest(NameValueCollection queryString)
    {
        throw new NotImplementedException();
    }
}
