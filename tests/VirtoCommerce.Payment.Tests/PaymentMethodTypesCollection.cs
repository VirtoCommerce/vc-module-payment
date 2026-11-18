using Xunit;

namespace VirtoCommerce.Payment.Tests;

// For tests that swap the static AbstractTypeFactory<PaymentMethod> registrations: never runs in parallel.
[CollectionDefinition(Name, DisableParallelization = true)]
public class PaymentMethodTypesCollection
{
    public const string Name = "Payment method types";
}
