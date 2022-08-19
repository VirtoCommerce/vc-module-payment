using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Events;

namespace VirtoCommerce.PaymentModule.Core.Events
{
    public class PaymentMethodInstancingEvent : DomainEvent
    {
        public IEnumerable<string> PaymentMethodCodes { get; set; }
    }
}
