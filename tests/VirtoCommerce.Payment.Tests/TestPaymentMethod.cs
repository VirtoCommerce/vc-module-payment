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
}
