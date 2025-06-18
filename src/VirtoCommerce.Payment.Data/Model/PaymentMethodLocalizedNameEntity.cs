using System.ComponentModel.DataAnnotations;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.PaymentModule.Data.Model;

public class PaymentMethodLocalizedNameEntity : Entity
{
    [Required]
    [StringLength(16)]
    public string LanguageCode { get; set; } = string.Empty;

    [Required]
    public string Value { get; set; } = string.Empty;

    public string ParentEntityId { get; set; }
    public virtual StorePaymentMethodEntity ParentEntity { get; set; } = null!;
}
