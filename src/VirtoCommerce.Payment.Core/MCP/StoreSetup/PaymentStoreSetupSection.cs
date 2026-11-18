using System.Collections.Generic;
using System.ComponentModel;

namespace VirtoCommerce.PaymentModule.Core.MCP.StoreSetup;

// Payment's section of a store's setup, as an agent fills it: "payment": { "methods": [ { "code": ... } ] }.
// The descriptions become the JSON schema that get_store_setup_options shows.
public class PaymentStoreSetupSection
{
    [Description("The payment methods the store offers: the complete list. A method that is not listed is switched off.")]
    public IList<PaymentStoreSetupMethod> Methods { get; set; }
}
