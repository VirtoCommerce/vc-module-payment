using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.MCP.Core.Models;
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
    // The settings manager's cached entries: every read of an unsaved test method gets a new list of these instances.
    private readonly List<ObjectSettingEntry> _cachedSettings = [];

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
        Assert.Equal(McpFindingSeverities.Critical, finding.Severity);
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
        Assert.Equal(McpFindingSeverities.Critical, finding.Severity);
        Assert.Contains(_merchantName.Name, finding.Evidence);
        Assert.Contains("1 secret setting is empty", finding.Evidence);
        Assert.DoesNotContain(_apiKey.Name, JsonSerializer.Serialize(finding));
        Assert.False(finding.ManualOnly);
    }

    // Only a person can enter a secret, so a finding about empty secrets alone names no tool.
    [Fact]
    public async Task CheckAsync_ActiveMethodWithOnlyAnEmptySecret_IsManualOnly()
    {
        //Arrange
        var card = CreateCard(isActive: true);
        card.Settings = [new ObjectSettingEntry(_merchantName) { Value = "EU shop" }];
        _saved.Add(card);

        //Act
        var findings = await CreatePart().CheckAsync(CreateContext());

        //Assert
        var finding = Assert.Single(findings);
        Assert.True(finding.ManualOnly);
        Assert.Equal("1 secret setting is empty.", finding.Evidence);
        Assert.StartsWith("A person enters the secret settings in the back office", finding.Fix);
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

    // A field that is left out changes nothing, as the agent is told; an empty list is what switches every method off.
    [Fact]
    public async Task PlanAsync_ASectionWithoutMethods_ChangesNothing()
    {
        //Arrange
        _saved.Add(new DefaultManualPaymentMethod { StoreId = StoreId, IsActive = true });

        //Act
        var withoutMethods = await CreatePart().PlanAsync(CreateContext(), new PaymentStoreSetupSection());
        var emptyList = await CreatePart().PlanAsync(CreateContext(), CreateSection());

        //Assert
        Assert.Empty(withoutMethods.Errors);
        Assert.Empty(withoutMethods.Changes);
        Assert.Equal(["methods.DefaultManualPaymentMethod.active"], emptyList.Changes.Select(x => x.Field));
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

    // A setting the method never saved has its default: sending that value changes nothing.
    [Fact]
    public async Task PlanAsync_ASettingSentWithTheValueInEffect_ChangesNothing()
    {
        //Arrange
        _saved.Add(CreateCard(isActive: true));
        var section = CreateSection(new PaymentStoreSetupMethod { Code = CardCode, Settings = new Dictionary<string, object> { [_mode.Name] = "Test" } });

        //Act
        var plan = await CreatePart().PlanAsync(CreateContext(), section);

        //Assert
        Assert.Empty(plan.Errors);
        Assert.Empty(plan.Changes);
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

    // Applying reads the methods again: a setting changed in the back office since the plan, which the plan does not
    // name, keeps its new value.
    [Fact]
    public async Task ApplyAsync_KeepsWhatChangedSinceThePlan()
    {
        //Arrange
        var card = CreateCard(isActive: true);
        card.Settings = [new ObjectSettingEntry(_merchantName) { Value = "Old shop" }];
        _saved.Add(card);
        var part = CreatePart();
        var context = CreateContext();
        var plan = await part.PlanAsync(context, CreateSection(new PaymentStoreSetupMethod { Code = CardCode, Settings = new Dictionary<string, object> { [_merchantName.Name] = "EU shop" } }));
        _saved.Single().Settings.Add(new ObjectSettingEntry(_mode) { Value = "Live" });

        //Act
        await part.ApplyAsync(context, plan);

        //Assert
        var saved = Assert.Single(_saved);
        Assert.Equal("EU shop", saved.Settings.Single(x => x.Name == _merchantName.Name).Value);
        Assert.Equal("Live", saved.Settings.Single(x => x.Name == _mode.Name).Value);
    }

    private static StoreSetupContext CreateContext()
    {
        return new StoreSetupContext { Store = new Store { Id = StoreId, Name = "EU store" } };
    }

    private static PaymentStoreSetupSection CreateSection(params PaymentStoreSetupMethod[] methods)
    {
        return new PaymentStoreSetupSection { Methods = methods };
    }

    // The state lists the active methods, each with its settings in effect, and never a secret's value.
    [Fact]
    public async Task GetStateAsync_ListsTheActiveMethods_WithTheirSettings_WithoutSecrets()
    {
        //Arrange
        var card = CreateCard(isActive: true);
        card.Priority = 2;
        card.Settings = [new ObjectSettingEntry(_merchantName) { Value = "EU shop" }, new ObjectSettingEntry(_apiKey) { Value = "sk_live_4242" }];
        _saved.Add(card);

        //Act
        var state = (PaymentStoreSetupSection)await CreatePart().GetStateAsync(CreateContext().Store);

        //Assert
        var method = Assert.Single(state.Methods);
        Assert.Equal((CardCode, 2), (method.Code, method.Priority));
        Assert.Equal(new Dictionary<string, object> { [_merchantName.Name] = "EU shop", [_mode.Name] = "Test" }, method.Settings);
        Assert.DoesNotContain("sk_live_4242", JsonSerializer.Serialize(state));
    }

    [Fact]
    public async Task PlanAsync_AnEntryWithoutACode_IsRefused_AndACodeIsReadWithoutSpaces()
    {
        //Act
        var withoutCode = await CreatePart().PlanAsync(CreateContext(), CreateSection(new PaymentStoreSetupMethod { Code = CardCode }, null));
        var spaced = await CreatePart().PlanAsync(CreateContext(), CreateSection(new PaymentStoreSetupMethod { Code = $" {CardCode} " }));

        //Assert
        Assert.Equal(["Each payment method needs its code. get_store_setup_options lists the codes."], withoutCode.Errors);
        Assert.Empty(spaced.Errors);
        Assert.Contains(spaced.Changes, x => x.Field == $"methods.{CardCode}.active");
    }

    // A priority left out keeps the method's own, which a method the store has never saved has as 0.
    [Fact]
    public void Priority_SaysWhatALeftOutPriorityIs()
    {
        //Act
        var description = typeof(PaymentStoreSetupMethod).GetProperty(nameof(PaymentStoreSetupMethod.Priority))!.GetCustomAttribute<DescriptionAttribute>()!.Description;

        //Assert
        Assert.EndsWith("Left out: unchanged, which is 0 for a method the store has never saved. Equal numbers come in no fixed order.", description);
    }

    // The settings map says what a null does, as Store's settings section does.
    [Fact]
    public void Settings_SaysWhatANullDoes()
    {
        //Act
        var description = typeof(PaymentStoreSetupMethod).GetProperty(nameof(PaymentStoreSetupMethod.Settings))!.GetCustomAttribute<DescriptionAttribute>()!.Description;

        //Assert
        Assert.Contains("null returns a setting to its default, and a setting without a default takes a value instead.", description);
    }

    // A dry run, the plan made again at submit, and the apply never write to the settings manager's cached entries.
    [Fact]
    public async Task PlanAndApply_NeverWriteToTheSettingsManagersCachedEntries()
    {
        //Arrange
        var cached = new ObjectSettingEntry(_mode) { Value = "Test" };
        _cachedSettings.Add(cached);
        var section = CreateSection(new PaymentStoreSetupMethod { Code = CardCode, Settings = new Dictionary<string, object> { [_mode.Name] = "Live" } });

        //Act
        var dryRun = await CreatePart().PlanAsync(CreateContext(), section);
        var atSubmit = await CreatePart().PlanAsync(CreateContext(), section);
        await CreatePart().ApplyAsync(CreateContext(), atSubmit);

        //Assert
        Assert.Equal("Test", cached.Value);
        Assert.Contains(dryRun.Changes, x => x.Field == $"methods.{CardCode}.{_mode.Name}");
        Assert.Contains(atSubmit.Changes, x => x.Field == $"methods.{CardCode}.{_mode.Name}");
        var savedMethods = (IList<PaymentMethod>)_methodsService.Invocations.Last(x => x.Method.Name == nameof(IPaymentMethodsService.SaveChangesAsync)).Arguments[0];
        Assert.DoesNotContain(savedMethods.SelectMany(x => x.Settings), x => ReferenceEquals(x, cached));
        Assert.Equal("Live", savedMethods.Single(x => x.Code == CardCode).Settings.Single(x => x.Name == _mode.Name).Value);
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
            .ReturnsAsync((PaymentMethodsSearchCriteria criteria, bool clone) =>
            {
                // As the search service: copies when asked, the saved instances themselves otherwise.
                var saved = _saved.Where(x => x.StoreId == criteria.StoreId).Select(x => clone ? x.CloneTyped() : x).ToList();
                var card = CreateCard();
                card.Settings = _cachedSettings.ToList();
                var installed = new PaymentMethod[] { new DefaultManualPaymentMethod(), card };
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
