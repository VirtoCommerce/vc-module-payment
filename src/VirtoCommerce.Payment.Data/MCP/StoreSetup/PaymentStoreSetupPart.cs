using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.PaymentModule.Core.MCP.StoreSetup;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.PaymentModule.Core.Model.Search;
using VirtoCommerce.PaymentModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.StoreModule.Core.MCP.StoreSetup;
using VirtoCommerce.StoreModule.Core.Model;

namespace VirtoCommerce.PaymentModule.Data.MCP.StoreSetup;

// Payment's part of a store's setup: the payment methods a store offers, with their settings that are not secrets.
// It covers all four duties. Payment has no permissions of its own yet (VCST-6919), so the part uses Store's.
// Store never references this class: Payment registers it in its Module.Initialize with AddStoreSetupPart.
public class PaymentStoreSetupPart(
    IPaymentMethodsRegistrar paymentMethodsRegistrar,
    IPaymentMethodsSearchService paymentMethodsSearchService,
    IPaymentMethodsService paymentMethodsService,
    ISettingsRegistrar settingsRegistrar,
    IStoreSetupSettings setupSettings)
    : IStoreSetupChoices, IStoreSetupState, IStoreSetupCheck, IStoreSetupSection
{
    public const string PartName = "payment";

    public virtual string Name => PartName;

    public virtual string Title => "Payment methods";

    public virtual StoreSetupPermissions Permissions => StoreSetupPermissions.ForStore();

    public virtual Type SectionType => typeof(PaymentStoreSetupSection);

    // A store can be created without payment; the readiness check then says it cannot go live (Q7).
    public virtual bool IsRequiredForNewStore => false;

    // 1. What can be chosen: every payment method installed on the platform, with the settings it takes.
    public virtual async Task<IList<StoreSetupChoice>> GetChoicesAsync()
    {
        var choices = new List<StoreSetupChoice>();

        foreach (var method in await paymentMethodsRegistrar.GetRegisteredPaymentMethods())
        {
            var choice = AbstractTypeFactory<StoreSetupChoice>.TryCreateInstance();
            choice.Field = $"{PartName}.methods.code";
            choice.Value = method.Code;
            choice.Title = method.Name;
            choice.Description = method.Description;

            // Secrets are not even listed, so the agent never proposes to send one.
            choice.Settings = setupSettings.DescribeAll(GetSettingDescriptors(method));

            choices.Add(choice);
        }

        return choices;
    }

    // 2. The store's current state, in the shape of the section: its active methods, by priority.
    public virtual async Task<object> GetStateAsync(Store store)
    {
        var methods = await GetStoreMethodsAsync(store.Id);

        var section = AbstractTypeFactory<PaymentStoreSetupSection>.TryCreateInstance();
        section.Methods = methods
            .Where(x => x.IsActive)
            .OrderBy(x => x.Priority)
            .Select(ToSectionMethod)
            .ToList();

        return section;
    }

    // 3. What is missing. When the request plans a payment section, the check reads the methods as they will be after
    // the change (the plan), so the preview says what the store would miss afterwards. Otherwise it reads the saved ones.
    public virtual async Task<IList<StoreSetupFinding>> CheckAsync(StoreSetupContext context)
    {
        var store = context.Store;
        var methods = context.Plans.TryGetValue(PartName, out var plan) && plan is PaymentStoreSetupPlan paymentPlan
            ? paymentPlan.Methods
            : await GetStoreMethodsAsync(store.Id);

        var activeMethods = methods.Where(x => x.IsActive).ToList();
        var findings = new List<StoreSetupFinding>();

        if (activeMethods.Count == 0)
        {
            findings.Add(CreateFinding(store, "payment-no-active-method",
                "No payment method is active",
                "Customers cannot pay for an order.",
                $"0 of {methods.Count} installed payment methods are active.",
                "Activate at least one payment method."));
        }

        foreach (var method in activeMethods)
        {
            var emptyRequired = GetSettingDescriptors(method).Where(x => x.IsRequired && IsEmpty(StoreSetupSettingsExtensions.GetValue(method, x))).ToList();

            if (emptyRequired.Count == 0)
            {
                continue;
            }

            // A secret is never named, not even when it is missing: a person enters it in the back office.
            var secretCount = emptyRequired.Count(setupSettings.IsSecret);
            var names = emptyRequired.Where(x => !setupSettings.IsSecret(x)).Select(x => x.Name).ToList();

            var finding = CreateFinding(store, "payment-method-needs-settings",
                $"{method.Name} needs its required settings",
                "The method cannot take payments until its required settings have values.",
                GetEmptySettingsText(names, secretCount),
                secretCount > 0
                    ? "Enter the empty settings in the back office: secrets are never sent through the agent."
                    : "Fill the settings in the payment section, or in the back office.");
            finding.ObjectType = nameof(PaymentMethod);
            finding.ObjectId = method.Code;
            finding.ObjectName = method.Name;

            findings.Add(finding);
        }

        return findings;
    }

    // 4a. Plans the section without side effects: the search returns copies, and nothing is saved here. The Store tool
    // plans for the dry run, again for the requester at approval, and again right before applying.
    public virtual async Task<StoreSetupPartPlan> PlanAsync(StoreSetupContext context, object section)
    {
        var requested = ((PaymentStoreSetupSection)section).Methods ?? [];

        var plan = AbstractTypeFactory<PaymentStoreSetupPlan>.TryCreateInstance();
        plan.Methods = await GetStoreMethodsAsync(context.Store.Id);

        foreach (var code in requested.GroupBy(x => x.Code, StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1).Select(x => x.Key))
        {
            plan.Errors.Add($"Payment method '{code}' is listed twice.");
        }

        foreach (var item in requested.Where(x => !plan.Methods.Any(method => method.Code.EqualsIgnoreCase(x.Code))))
        {
            plan.Errors.Add($"Payment method '{item.Code}' is not installed. get_store_setup_options lists the codes.");
        }

        // Any error refuses the whole request, in every part, before anything is applied.
        if (plan.Errors.Count > 0)
        {
            return plan;
        }

        // The list is the complete new list: a listed method is active, any other one is switched off.
        foreach (var method in plan.Methods)
        {
            var item = requested.FirstOrDefault(x => x.Code.EqualsIgnoreCase(method.Code));

            PlanActive(plan, method, isActive: item != null);

            if (item != null)
            {
                PlanPriority(plan, method, item.Priority);
                PlanSettings(plan, method, item.Settings);
            }
        }

        return plan;
    }

    // 4b. Saves the methods the plan changes. Because the plan was made from the saved rows a moment ago, a repeat after a
    // failure finds the applied methods unchanged and saves only the rest; the unique index (TypeName, StoreId) keeps
    // one row per method and store.
    public virtual async Task ApplyAsync(StoreSetupContext context, StoreSetupPartPlan plan)
    {
        var paymentPlan = (PaymentStoreSetupPlan)plan;
        var changed = paymentPlan.Methods.Where(x => paymentPlan.ChangedCodes.Contains(x.Code)).ToList();

        if (changed.Count == 0)
        {
            return;
        }

        // A method the store has never saved comes from the search without a store.
        foreach (var method in changed)
        {
            method.StoreId = context.Store.Id;
        }

        await paymentMethodsService.SaveChangesAsync(changed);
    }

    protected virtual void PlanActive(PaymentStoreSetupPlan plan, PaymentMethod method, bool isActive)
    {
        if (method.IsActive == isActive)
        {
            return;
        }

        AddChange(plan, method, "active", method.IsActive, isActive);

        if (!isActive)
        {
            plan.Warnings.Add($"Switches off {method.Name}: customers can no longer pay with it.");
        }

        method.IsActive = isActive;
    }

    protected virtual void PlanPriority(PaymentStoreSetupPlan plan, PaymentMethod method, int? priority)
    {
        if (priority == null || priority == method.Priority)
        {
            return;
        }

        AddChange(plan, method, "priority", method.Priority, priority.Value);
        method.Priority = priority.Value;
    }

    protected virtual void PlanSettings(PaymentStoreSetupPlan plan, PaymentMethod method, IDictionary<string, object> settings)
    {
        // Store's rules for a settings map, the same in every part: an unknown name, a secret or a wrong value is
        // refused, a null returns the default, and a value equal to the one in effect changes nothing.
        var changed = setupSettings.PlanSettings(plan, method, GetSettingDescriptors(method), settings,
            $"methods.{method.Code}.", $"Payment method '{method.Code}'", "get_store_setup_options");

        if (changed.Count > 0)
        {
            plan.ChangedCodes.Add(method.Code);
        }
    }

    // The store's saved methods, plus a new instance of every installed method the store has not saved yet.
    protected virtual async Task<IList<PaymentMethod>> GetStoreMethodsAsync(string storeId)
    {
        var criteria = AbstractTypeFactory<PaymentMethodsSearchCriteria>.TryCreateInstance();
        criteria.StoreId = storeId;

        return await paymentMethodsSearchService.SearchAllAsync(criteria);
    }

    // The settings a method takes: each payment module registers them for its method's type name.
    protected virtual IList<SettingDescriptor> GetSettingDescriptors(PaymentMethod method)
    {
        return settingsRegistrar.GetSettingsForType(method.TypeName)?.ToList() ?? [];
    }

    protected virtual PaymentStoreSetupMethod ToSectionMethod(PaymentMethod method)
    {
        var item = AbstractTypeFactory<PaymentStoreSetupMethod>.TryCreateInstance();
        item.Code = method.Code;
        item.Priority = method.Priority;
        item.Settings = setupSettings.GetValues(method, GetSettingDescriptors(method));

        return item;
    }

    protected virtual StoreSetupFinding CreateFinding(Store store, string id, string title, string why, string evidence, string fix)
    {
        var finding = StoreSetupFinding.ForStore(store, PartName, id, StoreSetupFindingSeverity.Blocking, "Payment methods");
        finding.Title = title;
        finding.Why = why;
        finding.Evidence = evidence;
        finding.Fix = fix;

        return finding;
    }

    private static void AddChange(PaymentStoreSetupPlan plan, PaymentMethod method, string field, object from, object to)
    {
        plan.AddChange($"methods.{method.Code}.{field}", from, to);
        plan.ChangedCodes.Add(method.Code);
    }

    private static bool IsEmpty(object value)
    {
        return value == null || value is string text && string.IsNullOrWhiteSpace(text);
    }

    private static string GetEmptySettingsText(IList<string> names, int secretCount)
    {
        var parts = new List<string>();

        if (names.Count > 0)
        {
            parts.Add($"Empty: {string.Join(", ", names)}.");
        }

        if (secretCount > 0)
        {
            parts.Add(secretCount == 1 ? "1 secret setting is empty." : $"{secretCount} secret settings are empty.");
        }

        return string.Join(" ", parts);
    }
}
