using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.PaymentModule.Core.Model.Search;
using VirtoCommerce.PaymentModule.Core.Model.StoredCredentials;
using VirtoCommerce.PaymentModule.Core.Services;
using VirtoCommerce.PaymentModule.Data.Services;
using VirtoCommerce.PaymentModule.Model.Requests;
using Xunit;

namespace VirtoCommerce.Payment.Tests.StoredCredentials;

public class StoredPaymentMethodManagerTests
{
    private const string StoreId = "store-1";
    private const string CustomerId = "customer-1";
    private const string MethodCode = "TestStoredCard";

    private readonly List<StoredPaymentMethod> _cards = [];
    private readonly List<StoredPaymentMethod> _saved = [];
    private readonly List<IStoredPaymentMethodUsageProvider> _usageProviders = [];
    private readonly TestStoredCardPaymentMethod _connector = new(MethodCode);
    private readonly Mock<IStoredPaymentMethodService> _storedPaymentMethodService = new();
    private readonly Mock<IStoredPaymentMethodSearchService> _storedPaymentMethodSearchService = new();
    private readonly Mock<IPaymentMethodsSearchService> _paymentMethodsSearchService = new();

    public StoredPaymentMethodManagerTests()
    {
        _storedPaymentMethodService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync((IList<string> ids, string _, bool _) => _cards.Where(x => ids.Contains(x.Id)).ToList());
        _storedPaymentMethodService
            .Setup(x => x.SaveChangesAsync(It.IsAny<IList<StoredPaymentMethod>>()))
            .Callback<IList<StoredPaymentMethod>>(_saved.AddRange)
            .Returns(Task.CompletedTask);
        _storedPaymentMethodSearchService
            .Setup(x => x.SearchAsync(It.IsAny<StoredPaymentMethodSearchCriteria>(), It.IsAny<bool>()))
            .ReturnsAsync((StoredPaymentMethodSearchCriteria criteria, bool _) => new StoredPaymentMethodSearchResult
            {
                Results = _cards.Where(x => x.CustomerId == criteria.CustomerId && x.StoreId == criteria.StoreId).ToList(),
            });
        _paymentMethodsSearchService
            .Setup(x => x.SearchAsync(It.IsAny<PaymentMethodsSearchCriteria>(), It.IsAny<bool>()))
            .ReturnsAsync((PaymentMethodsSearchCriteria criteria, bool _) => new PaymentMethodsSearchResult
            {
                Results = criteria.Codes.Contains(MethodCode) ? [_connector] : [],
            });
    }

    private StoredPaymentMethodManager Manager => new(
        _storedPaymentMethodService.Object,
        _storedPaymentMethodSearchService.Object,
        _paymentMethodsSearchService.Object,
        _usageProviders);

    private static StoredPaymentContext Context => new()
    {
        StoreId = StoreId,
        CustomerId = CustomerId,
        PaymentMethodCode = MethodCode,
        OrderId = "order-1",
        PaymentId = "payment-1",
    };

    private StoredPaymentMethod Card(string id, string customerId = CustomerId, string status = StoredPaymentMethodStatus.Active, bool isDefault = false, string token = null)
    {
        var card = TestStoredCardPaymentMethod.ProviderCard(token ?? $"token-{id}");
        card.Id = id;
        card.StoreId = StoreId;
        card.CustomerId = customerId;
        card.PaymentMethodCode = MethodCode;
        card.Status = status;
        card.IsDefault = isDefault;
        card.CreatedDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        _cards.Add(card);
        return card;
    }

    private static PostProcessPaymentRequest PaymentRequest(params (string Key, string Value)[] parameters)
    {
        var values = new NameValueCollection();
        foreach (var (key, value) in parameters)
        {
            values[key] = value;
        }

        return new PostProcessPaymentRequest { Parameters = values };
    }

    // Preparing a payment

    [Fact]
    public async Task Prepare_ReadsSaveCardRequestAndConsent()
    {
        var request = PaymentRequest(
            (StoredCredentialParameterNames.StoreForFutureUse, "true"),
            (StoredCredentialParameterNames.ConsentId, "consent-1"));

        var result = await Manager.PrepareAsync(request, Context);

        result.IsSuccess.Should().BeTrue();
        request.StoreForFutureUse.Should().BeTrue();
        request.ConsentId.Should().Be("consent-1");
        request.StoredCredentialUsage.Should().Be(StoredCredentialUsage.Recurring, "a card saved with a consent pays recurring orders");
        request.StoredPaymentMethod.Should().BeNull();
    }

    [Fact]
    public async Task Prepare_SaveCardWithoutConsent_IsCardOnFile()
    {
        var request = PaymentRequest((StoredCredentialParameterNames.StoreForFutureUse, "true"));

        await Manager.PrepareAsync(request, Context);

        request.StoredCredentialUsage.Should().Be(StoredCredentialUsage.CardOnFile);
    }

    [Fact]
    public async Task Prepare_GuestPayment_NeverSavesTheCard()
    {
        var request = PaymentRequest((StoredCredentialParameterNames.StoreForFutureUse, "true"));
        var context = Context;
        context.CustomerId = null;

        await Manager.PrepareAsync(request, context);

        request.StoreForFutureUse.Should().BeFalse();
    }

    [Fact]
    public async Task Prepare_SavedCard_IsResolvedOntoTheRequest()
    {
        var card = Card("card-1");
        var request = PaymentRequest((StoredCredentialParameterNames.StoredPaymentMethodId, "card-1"));

        var result = await Manager.PrepareAsync(request, Context);

        result.IsSuccess.Should().BeTrue();
        request.StoredPaymentMethod.Should().BeSameAs(card);
        request.StoreForFutureUse.Should().BeFalse("a saved card is not saved again");
    }

    [Theory]
    [InlineData("someone-else", StoredPaymentMethodStatus.Active, MethodCode)]
    [InlineData(CustomerId, StoredPaymentMethodStatus.Revoked, MethodCode)]
    [InlineData(CustomerId, StoredPaymentMethodStatus.NeedsUpdate, MethodCode)]
    [InlineData(CustomerId, StoredPaymentMethodStatus.Active, "OtherConnector")]
    public async Task Prepare_CardThatCannotPay_IsRefused(string owner, string status, string paymentMethodCode)
    {
        var card = Card("card-1", owner, status);
        card.PaymentMethodCode = paymentMethodCode;
        var request = PaymentRequest((StoredCredentialParameterNames.StoredPaymentMethodId, "card-1"));

        var result = await Manager.PrepareAsync(request, Context);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        request.StoredPaymentMethod.Should().BeNull();
    }

    [Fact]
    public async Task Prepare_ExpiredCard_IsRefused()
    {
        var card = Card("card-1");
        card.ExpirationYear = 2020;
        var request = PaymentRequest((StoredCredentialParameterNames.StoredPaymentMethodId, "card-1"));

        var result = await Manager.PrepareAsync(request, Context);

        result.IsSuccess.Should().BeFalse();
    }

    // Recording a card after a payment

    [Fact]
    public async Task SaveFromPayment_NewCard_IsRecordedForTheCustomerAndOrder()
    {
        var request = new PostProcessPaymentRequest { StoreForFutureUse = true, ConsentId = "consent-1", StoredCredentialUsage = StoredCredentialUsage.Recurring };
        var result = new PostProcessPaymentRequestResult { IsSuccess = true, StoredPaymentMethod = TestStoredCardPaymentMethod.ProviderCard() };

        var card = await Manager.SaveFromPaymentAsync(request, result, Context);

        card.Should().NotBeNull();
        _saved.Should().ContainSingle().Which.Should().BeSameAs(card);
        card.CustomerId.Should().Be(CustomerId);
        card.StoreId.Should().Be(StoreId);
        card.PaymentMethodCode.Should().Be(MethodCode);
        card.InitialOrderId.Should().Be("order-1");
        card.InitialPaymentId.Should().Be("payment-1");
        card.ConsentId.Should().Be("consent-1");
        card.Usage.Should().Be(StoredCredentialUsage.Recurring);
        card.Status.Should().Be(StoredPaymentMethodStatus.Active);
        card.IsDefault.Should().BeTrue("it is the customer's first card");
        card.LastUsedDate.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveFromPayment_CardReturnedUnderAnotherCode_BelongsToThePaymentMethodPaidWith()
    {
        // e.g. a vault method that forwards the payment to a gateway connector, which reports its own code
        var providerCard = TestStoredCardPaymentMethod.ProviderCard();
        providerCard.PaymentMethodCode = "GatewayPaymentMethod";
        var result = new PostProcessPaymentRequestResult { IsSuccess = true, StoredPaymentMethod = providerCard };

        var card = await Manager.SaveFromPaymentAsync(new PostProcessPaymentRequest { StoreForFutureUse = true }, result, Context);

        card.PaymentMethodCode.Should().Be(MethodCode);
    }

    [Fact]
    public async Task SaveFromPayment_SecondCard_IsNotTheDefault()
    {
        Card("card-1", isDefault: true);
        var result = new PostProcessPaymentRequestResult { IsSuccess = true, StoredPaymentMethod = TestStoredCardPaymentMethod.ProviderCard("token-new") };

        var card = await Manager.SaveFromPaymentAsync(new PostProcessPaymentRequest(), result, Context);

        card.IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task SaveFromPayment_SameCardAgain_UpdatesTheExistingRecord()
    {
        var existing = Card("card-1", status: StoredPaymentMethodStatus.Revoked, token: "token-1");
        var result = new PostProcessPaymentRequestResult { IsSuccess = true, StoredPaymentMethod = TestStoredCardPaymentMethod.ProviderCard("token-1") };

        var card = await Manager.SaveFromPaymentAsync(new PostProcessPaymentRequest { ConsentId = "consent-2" }, result, Context);

        card.Should().BeSameAs(existing);
        card.Status.Should().Be(StoredPaymentMethodStatus.Active);
        card.ConsentId.Should().Be("consent-2");
        card.LastUsedDate.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveFromPayment_PaidWithSavedCard_MarksItUsed()
    {
        var card = Card("card-1");
        var request = new PostProcessPaymentRequest { StoredPaymentMethod = card };

        var recorded = await Manager.SaveFromPaymentAsync(request, new PostProcessPaymentRequestResult { IsSuccess = true }, Context);

        recorded.Should().BeSameAs(card);
        card.LastUsedDate.Should().NotBeNull();
        _saved.Should().ContainSingle();
    }

    [Fact]
    public async Task SaveFromPayment_FailedPayment_RecordsNothing()
    {
        var result = new PostProcessPaymentRequestResult { IsSuccess = false, StoredPaymentMethod = TestStoredCardPaymentMethod.ProviderCard() };

        var card = await Manager.SaveFromPaymentAsync(new PostProcessPaymentRequest { StoreForFutureUse = true }, result, Context);

        card.Should().BeNull();
        _saved.Should().BeEmpty();
    }

    [Fact]
    public async Task MarkUsed_KeepsTheFirstNetworkTransactionId()
    {
        var card = Card("card-1");

        await Manager.MarkUsedAsync(card, "later-ntid");

        card.NetworkTransactionId.Should().Be("ntid-1");
        card.LastUsedDate.Should().NotBeNull();
    }

    // Adding a card without a payment

    [Fact]
    public async Task Add_SavesTheCardTheConnectorStoredAsTheFirstDefault()
    {
        _connector.StoreResult = new StorePaymentMethodRequestResult { IsSuccess = true, Outcome = ChargeOutcome.Succeeded, StoredPaymentMethod = TestStoredCardPaymentMethod.ProviderCard() };
        var parameters = new NameValueCollection { ["dataValue"] = "nonce" };

        var result = await Manager.AddAsync(new AddStoredPaymentMethodRequest { StoreId = StoreId, CustomerId = CustomerId, PaymentMethodCode = MethodCode, Parameters = parameters });

        result.IsSuccess.Should().BeTrue();
        var card = result.StoredPaymentMethod;
        card.CustomerId.Should().Be(CustomerId);
        card.PaymentMethodCode.Should().Be(MethodCode);
        card.IsDefault.Should().BeTrue();
        _saved.Should().Contain(card);
        _connector.StoreRequests.Should().ContainSingle().Which.Parameters["dataValue"].Should().Be("nonce");
        _connector.StoreRequests[0].Verify.Should().BeTrue();
    }

    [Fact]
    public async Task Add_WithSetDefault_MovesTheDefault()
    {
        var previous = Card("card-1", isDefault: true);
        _connector.StoreResult = new StorePaymentMethodRequestResult { IsSuccess = true, StoredPaymentMethod = TestStoredCardPaymentMethod.ProviderCard("token-new") };

        var result = await Manager.AddAsync(new AddStoredPaymentMethodRequest { StoreId = StoreId, CustomerId = CustomerId, PaymentMethodCode = MethodCode, SetDefault = true });

        result.StoredPaymentMethod.IsDefault.Should().BeTrue();
        previous.IsDefault.Should().BeFalse();
        _saved.Should().Contain(previous);
    }

    [Fact]
    public async Task Add_DeclinedByTheConnector_KeepsTheOutcomeAndSavesNothing()
    {
        _connector.StoreResult = new StorePaymentMethodRequestResult { IsSuccess = false, Outcome = ChargeOutcome.RequiresAction, ErrorMessage = "Confirm with your bank.", RawCode = "authentication_required" };

        var result = await Manager.AddAsync(new AddStoredPaymentMethodRequest { StoreId = StoreId, CustomerId = CustomerId, PaymentMethodCode = MethodCode });

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(ChargeOutcome.RequiresAction);
        result.ErrorMessage.Should().Be("Confirm with your bank.");
        _saved.Should().BeEmpty();
    }

    [Fact]
    public async Task Add_PaymentMethodThatCannotSaveCards_IsRefused()
    {
        var result = await Manager.AddAsync(new AddStoredPaymentMethodRequest { StoreId = StoreId, CustomerId = CustomerId, PaymentMethodCode = "Unknown" });

        result.IsSuccess.Should().BeFalse();
        _connector.StoreRequests.Should().BeEmpty();
    }

    // Removing and the default card

    [Fact]
    public async Task Remove_DeletesAtTheProviderAndPassesTheDefaultOn()
    {
        var card = Card("card-1", isDefault: true);
        var other = Card("card-2");

        var result = await Manager.RemoveAsync(CustomerId, "card-1");

        result.IsSuccess.Should().BeTrue();
        _connector.DeleteRequests.Should().ContainSingle().Which.StoredPaymentMethod.Should().BeSameAs(card);
        card.Status.Should().Be(StoredPaymentMethodStatus.Revoked);
        card.IsDefault.Should().BeFalse();
        other.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Remove_CardInUse_IsRefusedWithTheReason()
    {
        Card("card-1");
        var usage = new Mock<IStoredPaymentMethodUsageProvider>();
        usage.Setup(x => x.GetUsageAsync(It.IsAny<StoredPaymentMethod>())).ReturnsAsync(StoredPaymentMethodUsage.InUse("This card pays for 2 subscriptions."));
        _usageProviders.Add(usage.Object);

        var result = await Manager.RemoveAsync(CustomerId, "card-1");

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("This card pays for 2 subscriptions.");
        _connector.DeleteRequests.Should().BeEmpty();
        _saved.Should().BeEmpty();
    }

    [Fact]
    public async Task Remove_ProviderFails_KeepsTheCard()
    {
        var card = Card("card-1");
        _connector.DeleteResult = new DeleteStoredPaymentMethodRequestResult { IsSuccess = false, ErrorMessage = "Provider unavailable." };

        var result = await Manager.RemoveAsync(CustomerId, "card-1");

        result.IsSuccess.Should().BeFalse();
        card.Status.Should().Be(StoredPaymentMethodStatus.Active);
        _saved.Should().BeEmpty();
    }

    [Fact]
    public async Task Remove_SomeoneElsesCard_IsNotFound()
    {
        Card("card-1", customerId: "someone-else");

        var result = await Manager.RemoveAsync(CustomerId, "card-1");

        result.IsSuccess.Should().BeFalse();
        _connector.DeleteRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task SetDefault_LeavesOneDefault()
    {
        var previous = Card("card-1", isDefault: true);
        var card = Card("card-2");

        var result = await Manager.SetDefaultAsync(CustomerId, "card-2");

        result.IsSuccess.Should().BeTrue();
        card.IsDefault.Should().BeTrue();
        previous.IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task SetDefault_InactiveCard_IsRefused()
    {
        Card("card-1", status: StoredPaymentMethodStatus.Revoked);

        var result = await Manager.SetDefaultAsync(CustomerId, "card-1");

        result.IsSuccess.Should().BeFalse();
    }
}
