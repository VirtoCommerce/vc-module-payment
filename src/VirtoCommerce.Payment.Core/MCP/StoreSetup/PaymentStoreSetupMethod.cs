using System.Collections.Generic;
using System.ComponentModel;

namespace VirtoCommerce.PaymentModule.Core.MCP.StoreSetup;

public class PaymentStoreSetupMethod
{
    [Description("Method code, as get_store_setup_options lists it.")]
    public string Code { get; set; }

    [Description("Position at checkout: a lower number comes first. Left out: unchanged, which is 0 for a method the store " +
        "has never saved. Equal numbers come in no fixed order.")]
    public int? Priority { get; set; }

    [Description("Setting name to value for this method. Leaves the other settings as they are; null returns a setting " +
        "to its default, and a setting without a default takes a value instead. Secrets are entered in the back office.")]
    public IDictionary<string, object> Settings { get; set; }
}
