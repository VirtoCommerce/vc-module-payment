using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.PaymentModule.Core.Model.StoredCredentials;
using VirtoCommerce.PaymentModule.Model.Requests;

namespace VirtoCommerce.Payment.Tests.StoredCredentials;

/// <summary>
/// A connector that saves and deletes cards with scripted results and records what it was asked.
/// </summary>
public class TestStoredCardPaymentMethod : PaymentMethod, ISupportStoredCredentialFlow
{
    public TestStoredCardPaymentMethod(string code = "TestStoredCard") : base(code)
    {
    }

    public override PaymentMethodType PaymentMethodType => PaymentMethodType.Standard;
    public override PaymentMethodGroupType PaymentMethodGroupType => PaymentMethodGroupType.BankCard;

    public StorePaymentMethodRequestResult StoreResult { get; set; }
    public DeleteStoredPaymentMethodRequestResult DeleteResult { get; set; } = new() { IsSuccess = true };

    public List<StorePaymentMethodRequest> StoreRequests { get; } = [];
    public List<DeleteStoredPaymentMethodRequest> DeleteRequests { get; } = [];

    public override Task<StorePaymentMethodRequestResult> StorePaymentMethodAsync(StorePaymentMethodRequest request, CancellationToken cancellationToken = default)
    {
        StoreRequests.Add(request);
        return Task.FromResult(StoreResult);
    }

    public override Task<DeleteStoredPaymentMethodRequestResult> DeleteStoredPaymentMethodAsync(DeleteStoredPaymentMethodRequest request, CancellationToken cancellationToken = default)
    {
        DeleteRequests.Add(request);
        return Task.FromResult(DeleteResult);
    }

    public override Task<ProcessStoredPaymentRequestResult> ProcessStoredPaymentAsync(ProcessStoredPaymentRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ProcessStoredPaymentRequestResult { IsSuccess = true, Outcome = ChargeOutcome.Succeeded });
    }

    public static StoredPaymentMethod ProviderCard(string token = "token-1", string customerRef = "provider-customer-1")
    {
        return new StoredPaymentMethod
        {
            ProviderCustomerRef = customerRef,
            Token = token,
            Brand = "Visa",
            Last4 = "1111",
            ExpirationMonth = 12,
            ExpirationYear = 2030,
            NetworkTransactionId = "ntid-1",
        };
    }
}
