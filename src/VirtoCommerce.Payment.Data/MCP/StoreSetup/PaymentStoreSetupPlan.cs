using System.Collections.Generic;
using VirtoCommerce.MCP.Core.Models;
using VirtoCommerce.PaymentModule.Core.Model;

namespace VirtoCommerce.PaymentModule.Data.MCP.StoreSetup;

// The plan of the payment section, carrying the store's methods as they will be after the change: the check reads them
// for the preview ("what the store would miss afterwards"), and applying copies the fields of the plan's rows from them.
public class PaymentStoreSetupPlan : McpSetupPartPlan
{
    public IList<PaymentMethod> Methods { get; set; } = [];
}
