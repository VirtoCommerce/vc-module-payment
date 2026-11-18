using Xunit;

namespace VirtoCommerce.Payment.Tests;

// Tests that swap the payment method types registered in the static AbstractTypeFactory<PaymentMethod> run in this
// collection, which never runs in parallel with other tests.
[CollectionDefinition(Name, DisableParallelization = true)]
public class PaymentMethodTypesCollection
{
    public const string Name = "Payment method types";
}
