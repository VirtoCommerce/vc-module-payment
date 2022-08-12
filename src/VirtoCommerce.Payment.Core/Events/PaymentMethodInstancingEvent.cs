using VirtoCommerce.Platform.Core.Events;

namespace VirtoCommerce.PaymentModule.Core.Events
{
    public class PaymentMethodInstancingEvent : DomainEvent
    {
        public string TypeName { get; set; }

        public PaymentMethodInstancingEvent(string typeName)
        {
            TypeName = typeName;
        }
    }
}
