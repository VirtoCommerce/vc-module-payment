using System;
using System.ComponentModel.DataAnnotations;
using VirtoCommerce.PaymentModule.Core.Model.StoredCredentials;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Domain;

namespace VirtoCommerce.PaymentModule.Data.Model
{
    public class PaymentConsentEntity : AuditableEntity, IDataEntity<PaymentConsentEntity, PaymentConsent>
    {
        [StringLength(128)]
        public string StoreId { get; set; }

        [Required]
        [StringLength(128)]
        public string CustomerId { get; set; }

        [StringLength(128)]
        public string StoredPaymentMethodId { get; set; }

        [StringLength(64)]
        public string Purpose { get; set; }

        [StringLength(64)]
        public string TermsVersion { get; set; }

        public string TermsText { get; set; }

        [StringLength(128)]
        public string TermsHash { get; set; }

        [StringLength(64)]
        public string Channel { get; set; }

        [StringLength(64)]
        public string IpAddress { get; set; }

        [StringLength(1024)]
        public string UserAgent { get; set; }

        public DateTime AcceptedDate { get; set; }
        public DateTime? RevokedDate { get; set; }

        [StringLength(128)]
        public string ObjectType { get; set; }

        [StringLength(128)]
        public string ObjectId { get; set; }

        public virtual PaymentConsent ToModel(PaymentConsent model)
        {
            ArgumentNullException.ThrowIfNull(model);

            model.Id = Id;
            model.CreatedBy = CreatedBy;
            model.CreatedDate = CreatedDate;
            model.ModifiedBy = ModifiedBy;
            model.ModifiedDate = ModifiedDate;

            model.StoreId = StoreId;
            model.CustomerId = CustomerId;
            model.StoredPaymentMethodId = StoredPaymentMethodId;
            model.Purpose = Purpose;
            model.TermsVersion = TermsVersion;
            model.TermsText = TermsText;
            model.TermsHash = TermsHash;
            model.Channel = Channel;
            model.IpAddress = IpAddress;
            model.UserAgent = UserAgent;
            model.AcceptedDate = AcceptedDate;
            model.RevokedDate = RevokedDate;
            model.ObjectType = ObjectType;
            model.ObjectId = ObjectId;

            return model;
        }

        public virtual PaymentConsentEntity FromModel(PaymentConsent model, PrimaryKeyResolvingMap pkMap)
        {
            ArgumentNullException.ThrowIfNull(model);

            pkMap.AddPair(model, this);

            Id = model.Id;
            CreatedBy = model.CreatedBy;
            CreatedDate = model.CreatedDate;
            ModifiedBy = model.ModifiedBy;
            ModifiedDate = model.ModifiedDate;

            StoreId = model.StoreId;
            CustomerId = model.CustomerId;
            StoredPaymentMethodId = model.StoredPaymentMethodId;
            Purpose = model.Purpose;
            TermsVersion = model.TermsVersion;
            TermsText = model.TermsText;
            TermsHash = model.TermsHash;
            Channel = model.Channel;
            IpAddress = model.IpAddress;
            UserAgent = model.UserAgent;
            AcceptedDate = model.AcceptedDate;
            RevokedDate = model.RevokedDate;
            ObjectType = model.ObjectType;
            ObjectId = model.ObjectId;

            return this;
        }

        public virtual void Patch(PaymentConsentEntity target)
        {
            ArgumentNullException.ThrowIfNull(target);

            // Consent evidence is immutable except for linking and revocation.
            target.StoredPaymentMethodId = StoredPaymentMethodId;
            target.RevokedDate = RevokedDate;
        }
    }
}
