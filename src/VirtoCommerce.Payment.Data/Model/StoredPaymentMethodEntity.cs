using System;
using System.ComponentModel.DataAnnotations;
using VirtoCommerce.PaymentModule.Core.Model.StoredCredentials;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Domain;

namespace VirtoCommerce.PaymentModule.Data.Model
{
    public class StoredPaymentMethodEntity : AuditableEntity, IDataEntity<StoredPaymentMethodEntity, StoredPaymentMethod>
    {
        [StringLength(128)]
        public string StoreId { get; set; }

        [Required]
        [StringLength(128)]
        public string CustomerId { get; set; }

        [StringLength(128)]
        public string OrganizationId { get; set; }

        [Required]
        [StringLength(128)]
        public string PaymentMethodCode { get; set; }

        [StringLength(256)]
        public string ProviderCustomerRef { get; set; }

        [StringLength(512)]
        public string Token { get; set; }

        [StringLength(64)]
        public string Brand { get; set; }

        [StringLength(4)]
        public string Last4 { get; set; }

        public int? ExpirationMonth { get; set; }
        public int? ExpirationYear { get; set; }

        [StringLength(256)]
        public string CardholderName { get; set; }

        [StringLength(256)]
        public string Fingerprint { get; set; }

        [StringLength(256)]
        public string NetworkTransactionId { get; set; }

        [StringLength(256)]
        public string InitialTransactionRef { get; set; }

        [StringLength(128)]
        public string InitialOrderId { get; set; }

        [StringLength(128)]
        public string InitialPaymentId { get; set; }

        [StringLength(32)]
        public string Usage { get; set; }

        public bool IsAuthenticated { get; set; }

        [Required]
        [StringLength(32)]
        public string Status { get; set; }

        public bool IsDefault { get; set; }
        public DateTime? LastUsedDate { get; set; }
        public DateTime? UpdatedByProviderDate { get; set; }

        [StringLength(128)]
        public string ConsentId { get; set; }

        public virtual StoredPaymentMethod ToModel(StoredPaymentMethod model)
        {
            ArgumentNullException.ThrowIfNull(model);

            model.Id = Id;
            model.CreatedBy = CreatedBy;
            model.CreatedDate = CreatedDate;
            model.ModifiedBy = ModifiedBy;
            model.ModifiedDate = ModifiedDate;

            model.StoreId = StoreId;
            model.CustomerId = CustomerId;
            model.OrganizationId = OrganizationId;
            model.PaymentMethodCode = PaymentMethodCode;
            model.ProviderCustomerRef = ProviderCustomerRef;
            model.Token = Token;
            model.Brand = Brand;
            model.Last4 = Last4;
            model.ExpirationMonth = ExpirationMonth;
            model.ExpirationYear = ExpirationYear;
            model.CardholderName = CardholderName;
            model.Fingerprint = Fingerprint;
            model.NetworkTransactionId = NetworkTransactionId;
            model.InitialTransactionRef = InitialTransactionRef;
            model.InitialOrderId = InitialOrderId;
            model.InitialPaymentId = InitialPaymentId;
            model.Usage = EnumUtility.SafeParse(Usage, StoredCredentialUsage.CardOnFile);
            model.IsAuthenticated = IsAuthenticated;
            model.Status = Status;
            model.IsDefault = IsDefault;
            model.LastUsedDate = LastUsedDate;
            model.UpdatedByProviderDate = UpdatedByProviderDate;
            model.ConsentId = ConsentId;

            return model;
        }

        public virtual StoredPaymentMethodEntity FromModel(StoredPaymentMethod model, PrimaryKeyResolvingMap pkMap)
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
            OrganizationId = model.OrganizationId;
            PaymentMethodCode = model.PaymentMethodCode;
            ProviderCustomerRef = model.ProviderCustomerRef;
            Token = model.Token;
            Brand = model.Brand;
            Last4 = model.Last4;
            ExpirationMonth = model.ExpirationMonth;
            ExpirationYear = model.ExpirationYear;
            CardholderName = model.CardholderName;
            Fingerprint = model.Fingerprint;
            NetworkTransactionId = model.NetworkTransactionId;
            InitialTransactionRef = model.InitialTransactionRef;
            InitialOrderId = model.InitialOrderId;
            InitialPaymentId = model.InitialPaymentId;
            Usage = model.Usage.ToString();
            IsAuthenticated = model.IsAuthenticated;
            Status = model.Status ?? StoredPaymentMethodStatus.Active;
            IsDefault = model.IsDefault;
            LastUsedDate = model.LastUsedDate;
            UpdatedByProviderDate = model.UpdatedByProviderDate;
            ConsentId = model.ConsentId;

            return this;
        }

        public virtual void Patch(StoredPaymentMethodEntity target)
        {
            ArgumentNullException.ThrowIfNull(target);

            target.StoreId = StoreId;
            target.CustomerId = CustomerId;
            target.OrganizationId = OrganizationId;
            target.PaymentMethodCode = PaymentMethodCode;
            target.ProviderCustomerRef = ProviderCustomerRef;
            target.Token = Token;
            target.Brand = Brand;
            target.Last4 = Last4;
            target.ExpirationMonth = ExpirationMonth;
            target.ExpirationYear = ExpirationYear;
            target.CardholderName = CardholderName;
            target.Fingerprint = Fingerprint;
            target.NetworkTransactionId = NetworkTransactionId;
            target.InitialTransactionRef = InitialTransactionRef;
            target.InitialOrderId = InitialOrderId;
            target.InitialPaymentId = InitialPaymentId;
            target.Usage = Usage;
            target.IsAuthenticated = IsAuthenticated;
            target.Status = Status;
            target.IsDefault = IsDefault;
            target.LastUsedDate = LastUsedDate;
            target.UpdatedByProviderDate = UpdatedByProviderDate;
            target.ConsentId = ConsentId;
        }
    }
}
