using System;
using System.Collections.Generic;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.StoreModule.Core.MCP.StoreSetup;

namespace VirtoCommerce.PaymentModule.Data.MCP.StoreSetup;

// The plan of the payment section, carrying the store's methods as they will be after the change: the check reads them
// for the preview ("what the store would miss afterwards"), and applying saves the changed ones.
public class PaymentStoreSetupPlan : StoreSetupPartPlan
{
    public IList<PaymentMethod> Methods { get; set; } = [];

    public ISet<string> ChangedCodes { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}
