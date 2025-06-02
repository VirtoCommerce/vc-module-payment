using System.ComponentModel.DataAnnotations;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.PaymentModule.Data.Model;

public class PaymentMethodLocalizedNameEntity : Entity
{
    [Required]
    [StringLength(16)]
    public string LanguageCode { get; set; } = string.Empty; // e.g., "en-US"

    [Required]
    public string Value { get; set; } = string.Empty;

    public string ParentEntityId { get; set; } // Foreign key to the parent entity
    public virtual StorePaymentMethodEntity ParentEntity { get; set; } = null!;
}
