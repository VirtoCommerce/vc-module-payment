using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.PaymentModule.Core
{
    public class ModuleConstants
    {
        public static class Settings
        {
            public static class General
            {
                public static IEnumerable<SettingDescriptor> AllSettings => Enumerable.Empty<SettingDescriptor>();
            }

            public static class DefaultManualPaymentMethod
            {
                public static readonly SettingDescriptor ExampleSetting = new SettingDescriptor
                {
                    Name = "VirtoCommerce.Payment.DefaultManualPaymentMethod.ExampleSetting",
                    GroupName = "Payment|DefaultManualPaymentMethod",
                    ValueType = SettingValueType.ShortText
                };

                public static IEnumerable<SettingDescriptor> AllSettings
                {
                    get
                    {
                        yield return ExampleSetting;
                    }
                }
            }

            public static IEnumerable<SettingDescriptor> AllSettings => General.AllSettings.Concat(DefaultManualPaymentMethod.AllSettings);
        }
    }
}
