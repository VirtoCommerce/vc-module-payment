using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.PaymentModule.Core.MCP.StoreSetup;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.PaymentModule.Core.Model.Search;
using VirtoCommerce.PaymentModule.Core.Services;
using VirtoCommerce.PaymentModule.Data;
using VirtoCommerce.PaymentModule.Data.MCP.StoreSetup;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.StoreModule.Core.MCP.StoreSetup;
using VirtoCommerce.StoreModule.Core.Model;
using Xunit;

namespace VirtoCommerce.Payment.Tests.MCP.StoreSetup;

public class PaymentStoreSetupPartTests
{
    private const string StoreId = "eu-store";
    private const string CardCode = "TestCard";

    private static readonly SettingDescriptor _merchantName = new() { Name = "Test.Card.MerchantName", ValueType = SettingValueType.ShortText, IsRequired = true };
    private static readonly SettingDescriptor _mode = new() { Name = "Test.Card.Mode", ValueType = SettingValueType.ShortText, DefaultValue = "Test" };
    private static readonly SettingDescriptor _apiKey = new() { Name = "Test.Card.ApiKey", ValueType = SettingValueType.ShortText, IsRequired = true };

    // The methods the store has saved; the search adds a new instance of every installed method the store has not saved.
    private readonly List<PaymentMethod> _saved = [];
    private readonly Mock<IPaymentMethodsService> _methodsService = new();

    [Fact]
    public async Task GetChoicesAsync_ListsTheInstalledMethods_WithTheirSettingsThatAreNotSecrets()
    {
        //Act
        var choices = await CreatePart().GetChoicesAsync();

        //Assert
        Assert.Equal(["DefaultManualPaymentMethod", CardCode], choices.Select(x => x.Value));
        Assert.All(choices, x => Assert.Equal("payment.methods.code", x.Field));
        Assert.Equal([_merchantName.Name, _mode.Name], choices.Single(x => x.Value == CardCode).Settings.Select(x => x.Name));
    }

    [Fact]
    public async Task CheckAsync_WithoutAnActiveMethod_IsBlocking()
    {
        //Act
        var findings = await CreatePart().CheckAsync(CreateContext());

        //Assert
        var finding = Assert.Single(findings);
        Assert.Equal("payment-no-active-method", finding.Id);
        Assert.Equal(StoreSetupFindingSeverity.Blocking, finding.Severity);
    }

    [Fact]
    public async Task CheckAsync_WithAnActiveMethodThatNeedsNoSettings_FindsNothing()
    {
        //Arrange
        _saved.Add(new DefaultManualPaymentMethod { StoreId = StoreId, IsActive = true });

        //Act
        var findings = await CreatePart().CheckAsync(CreateContext());

        //Assert
        Assert.Empty(findings);
    }

    [Fact]
    public async Task CheckAsync_ActiveMethodWithEmptyRequiredSettings_IsBlocking_AndNeverNamesTheSecret()
    {
        //Arrange
        _saved.Add(CreateCard(isActive: true));

        //Act
        var findings = await CreatePart().CheckAsync(CreateContext());

        //Assert
        var finding = Assert.Single(findings);
        Assert.Equal("payment-method-needs-settings", finding.Id);
        Assert.Equal(StoreSetupFindingSeverity.Blocking, finding.Severity);
        Assert.Contains(_merchantName.Name, finding.Evidence);
        Assert.Contains("1 secret setting is empty", finding.Evidence);
        Assert.DoesNotContain(_apiKey.Name, JsonSerializer.Serialize(finding));
    }

    [Fact]
    public async Task PlanAsync_TheListIsTheCompleteNewList_AndNothingIsSaved()
    {
        //Arrange
        _saved.Add(new DefaultManualPaymentMethod { StoreId = StoreId, IsActive = true });
        var section = CreateSection(new PaymentStoreSetupMethod { Code = CardCode, Priority = 2, Settings = new Dictionary<string, object> { [_merchantName.Name] = "EU shop" } });

        //Act
        var plan = await CreatePart().PlanAsync(CreateContext(), section);

        //Assert
        Assert.Empty(plan.Errors);
        Assert.Equal(
            ["methods.DefaultManualPaymentMethod.active", $"methods.{CardCode}.active", $"methods.{CardCode}.priority", $"methods.{CardCode}.{_merchantName.Name}"],
            plan.Changes.Select(x => x.Field));
        Assert.Contains(plan.Warnings, x => x.StartsWith("Switches off"));
        _methodsService.Verify(x => x.SaveChangesAsync(It.IsAny<IList<PaymentMethod>>()), Times.Never);
    }

    [Theory]
    [InlineData("unknown method", "'Unknown' is not installed")]
    [InlineData("secret setting", "is a secret")]
    [InlineData("unknown setting", "has no setting 'Test.Card.Colour'")]
    public async Task PlanAsync_RefusesWhatTheMethodsCannotTake(string gap, string expectedError)
    {
        //Arrange
        var method = gap switch
        {
            "unknown method" => new PaymentStoreSetupMethod { Code = "Unknown" },
            "secret setting" => new PaymentStoreSetupMethod { Code = CardCode, Settings = new Dictionary<string, object> { [_apiKey.Name] = "sk_live_1" } },
            _ => new PaymentStoreSetupMethod { Code = CardCode, Settings = new Dictionary<string, object> { ["Test.Card.Colour"] = "Red" } },
        };

        //Act
        var plan = await CreatePart().PlanAsync(CreateContext(), CreateSection(method));

        //Assert
        Assert.Contains(plan.Errors, x => x.Contains(expectedError));
        Assert.DoesNotContain(plan.Errors, x => x.Contains("sk_live_1"));
    }

    [Fact]
    public async Task ApplyAsync_SavesTheChangedMethodsForTheStore_AndARepeatChangesNothing()
    {
        //Arrange
        var part = CreatePart();
        var section = CreateSection(new PaymentStoreSetupMethod { Code = CardCode, Settings = new Dictionary<string, object> { [_merchantName.Name] = "EU shop" } });
        var context = CreateContext();
        var plan = await part.PlanAsync(context, section);

        //Act
        await part.ApplyAsync(context, plan);
        var repeat = await part.PlanAsync(CreateContext(), section);

        //Assert
        var saved = Assert.Single(_saved);
        Assert.Equal((CardCode, StoreId, true), (saved.Code, saved.StoreId, saved.IsActive));
        Assert.Equal("EU shop", saved.Settings.Single(x => x.Name == _merchantName.Name).Value);
        Assert.Empty(repeat.Changes);
    }

    private static StoreSetupContext CreateContext()
    {
        return new StoreSetupContext { Store = new Store { Id = StoreId, Name = "EU store" } };
    }

    private static PaymentStoreSetupSection CreateSection(params PaymentStoreSetupMethod[] methods)
    {
        return new PaymentStoreSetupSection { Methods = methods };
    }

    private static TestPaymentMethod CreateCard(bool isActive = false)
    {
        return new TestPaymentMethod(CardCode) { Name = "Test card", StoreId = isActive ? StoreId : null, IsActive = isActive };
    }

    private PaymentStoreSetupPart CreatePart()
    {
        var registrar = new Mock<IPaymentMethodsRegistrar>();
        registrar.Setup(x => x.GetRegisteredPaymentMethods()).ReturnsAsync(() => [new DefaultManualPaymentMethod(), CreateCard()]);

        var searchService = new Mock<IPaymentMethodsSearchService>();
        searchService
            .Setup(x => x.SearchAsync(It.IsAny<PaymentMethodsSearchCriteria>(), It.IsAny<bool>()))
            .ReturnsAsync((PaymentMethodsSearchCriteria criteria, bool _) =>
            {
                var saved = _saved.Where(x => x.StoreId == criteria.StoreId).Select(x => x.CloneTyped()).ToList();
                var installed = new PaymentMethod[] { new DefaultManualPaymentMethod(), CreateCard() };
                var all = saved.Concat(installed.Where(x => !saved.Any(s => s.Code == x.Code))).ToList();

                return new PaymentMethodsSearchResult { TotalCount = all.Count, Results = all.Skip(criteria.Skip).Take(criteria.Take).ToList() };
            });

        _methodsService
            .Setup(x => x.SaveChangesAsync(It.IsAny<IList<PaymentMethod>>()))
            .Callback((IList<PaymentMethod> methods) =>
            {
                foreach (var method in methods)
                {
                    _saved.RemoveAll(x => x.Code == method.Code && x.StoreId == method.StoreId);
                    _saved.Add(method.CloneTyped());
                }
            })
            .Returns(Task.CompletedTask);

        var settingsRegistrar = new Mock<ISettingsRegistrar>();
        settingsRegistrar.Setup(x => x.GetSettingsForType(nameof(TestPaymentMethod))).Returns([_merchantName, _mode, _apiKey]);
        settingsRegistrar.Setup(x => x.GetSettingsForType(nameof(DefaultManualPaymentMethod))).Returns([]);

        return new PaymentStoreSetupPart(registrar.Object, searchService.Object, _methodsService.Object, settingsRegistrar.Object, new TestStoreSetupSettings());
    }
}
