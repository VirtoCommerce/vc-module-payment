using System.Collections.Generic;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.Platform.Core.Events;
namespace VirtoCommerce.PaymentModule.Core.Events
{
    public class PaymentMethodChangeEvent : GenericChangedEntryEvent<PaymentMethod>
    {
        public PaymentMethodChangeEvent(IEnumerable<GenericChangedEntry<PaymentMethod>> changedEntries) : base(changedEntries)
        {
        }
    }
}
