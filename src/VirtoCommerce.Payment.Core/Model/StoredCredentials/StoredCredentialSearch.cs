using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.PaymentModule.Core.Model.StoredCredentials
{
    public class StoredPaymentMethodSearchCriteria : SearchCriteriaBase
    {
        public string StoreId { get; set; }
        public string CustomerId { get; set; }
        public string OrganizationId { get; set; }
        public string PaymentMethodCode { get; set; }
        public IList<string> Statuses { get; set; }
        public string InitialOrderId { get; set; }
        public string InitialPaymentId { get; set; }
    }

    public class StoredPaymentMethodSearchResult : GenericSearchResult<StoredPaymentMethod>
    {
    }

    public class PaymentConsentSearchCriteria : SearchCriteriaBase
    {
        public string StoreId { get; set; }
        public string CustomerId { get; set; }
        public string StoredPaymentMethodId { get; set; }
        public string Purpose { get; set; }
        public string RelatedObjectType { get; set; }
        public string RelatedObjectId { get; set; }
    }

    public class PaymentConsentSearchResult : GenericSearchResult<PaymentConsent>
    {
    }
}
