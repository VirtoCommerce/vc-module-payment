using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using VirtoCommerce.MCP.Core.Models;
using VirtoCommerce.MCP.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.StoreModule.Core.MCP.StoreSetup;
using VirtoCommerce.StoreModule.Core.Model;

namespace VirtoCommerce.Payment.Tests.MCP.StoreSetup;

// Store's IStoreSetupSettings in short, for a part's tests: a SecureString or a name ending with Key is a secret, a
// secret is refused, and a null returns the setting to its default. Store's rules for an object's settings come from
// StoreSetupSettingsBase, so the part's tests run them.
public class TestStoreSetupSettings() : StoreSetupSettingsBase(new McpSetupValues())
{
    public override bool IsSecret(SettingDescriptor descriptor)
    {
        return descriptor.ValueType == SettingValueType.SecureString || descriptor.Name.EndsWithIgnoreCase("Key");
    }

    public override McpSetupSetting Describe(SettingDescriptor descriptor, object value)
    {
        return new McpSetupSetting { Name = descriptor.Name, DefaultValue = descriptor.DefaultValue, Value = value ?? descriptor.DefaultValue, IsRequired = descriptor.IsRequired };
    }

    public override bool TryConvert(SettingDescriptor descriptor, object raw, out object value, out string error)
    {
        value = null;
        error = null;

        if (IsSecret(descriptor))
        {
            error = $"Setting '{descriptor.Name}' is a secret. Secrets are entered in the back office only.";
            return false;
        }

        value = raw switch
        {
            null or JsonElement { ValueKind: JsonValueKind.Null } => descriptor.DefaultValue,
            JsonElement element => element.ToString(),
            _ => raw,
        };
        return true;
    }

    public override Task<IList<McpSetupSetting>> GetStoreSettingsAsync(Store store)
    {
        return Task.FromResult<IList<McpSetupSetting>>([]);
    }
}
