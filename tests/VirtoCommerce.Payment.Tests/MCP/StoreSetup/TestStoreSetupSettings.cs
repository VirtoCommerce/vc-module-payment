using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.StoreModule.Core.MCP.StoreSetup;
using VirtoCommerce.StoreModule.Core.Model;

namespace VirtoCommerce.Payment.Tests.MCP.StoreSetup;

// Store's IStoreSetupSettings in short, for a part's tests: a SecureString or a name ending with Key is a secret, and a
// secret is refused.
public class TestStoreSetupSettings : IStoreSetupSettings
{
    public bool IsSecret(SettingDescriptor descriptor)
    {
        return descriptor.ValueType == SettingValueType.SecureString || descriptor.Name.EndsWith("Key", StringComparison.OrdinalIgnoreCase);
    }

    public StoreSetupSetting Describe(SettingDescriptor descriptor, object value)
    {
        return new StoreSetupSetting { Name = descriptor.Name, DefaultValue = descriptor.DefaultValue, Value = value ?? descriptor.DefaultValue, IsRequired = descriptor.IsRequired };
    }

    public bool TryConvert(SettingDescriptor descriptor, object raw, out object value, out string error)
    {
        value = null;
        error = null;

        if (IsSecret(descriptor))
        {
            error = $"Setting '{descriptor.Name}' is a secret. Secrets are entered in the back office only.";
            return false;
        }

        value = raw is JsonElement element ? element.ToString() : raw;
        return true;
    }

    public Task<IList<StoreSetupSetting>> GetStoreSettingsAsync(Store store)
    {
        return Task.FromResult<IList<StoreSetupSetting>>([]);
    }
}
