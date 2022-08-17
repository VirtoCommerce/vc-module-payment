using VirtoCommerce.Platform.Core.Events;

namespace VirtoCommerce.PaymentModule.Core.Events
{
    public class PaymentMethodInstancingEvent : DomainEvent
    {
        public string PaymentMethodCode { get; set; }
    }
}
