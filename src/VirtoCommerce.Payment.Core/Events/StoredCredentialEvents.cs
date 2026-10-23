using System.Collections.Generic;
using VirtoCommerce.PaymentModule.Core.Model.StoredCredentials;
using VirtoCommerce.Platform.Core.Events;

namespace VirtoCommerce.PaymentModule.Core.Events
{
    public class StoredPaymentMethodChangingEvent : GenericChangedEntryEvent<StoredPaymentMethod>
    {
        public StoredPaymentMethodChangingEvent(IEnumerable<GenericChangedEntry<StoredPaymentMethod>> changedEntries)
            : base(changedEntries)
        {
        }
    }

    public class StoredPaymentMethodChangedEvent : GenericChangedEntryEvent<StoredPaymentMethod>
    {
        public StoredPaymentMethodChangedEvent(IEnumerable<GenericChangedEntry<StoredPaymentMethod>> changedEntries)
            : base(changedEntries)
        {
        }
    }

    public class PaymentConsentChangingEvent : GenericChangedEntryEvent<PaymentConsent>
    {
        public PaymentConsentChangingEvent(IEnumerable<GenericChangedEntry<PaymentConsent>> changedEntries)
            : base(changedEntries)
        {
        }
    }

    public class PaymentConsentChangedEvent : GenericChangedEntryEvent<PaymentConsent>
    {
        public PaymentConsentChangedEvent(IEnumerable<GenericChangedEntry<PaymentConsent>> changedEntries)
            : base(changedEntries)
        {
        }
    }
}
