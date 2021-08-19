using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.PaymentModule.Core.Model
{/*
    public class PaymentMethod : Entity, IHasSettings, ICloneable
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public int Priority { get; set; }
        public string LogoUrl { get; set; }
        public bool IsActive { get; set; }
        public bool IsAvailableForPartial { get; set; }
        public string TypeName { get; set; }
        public string StoreId { get; set; }
        public ICollection<ObjectSettingEntry> Settings { get; set; }

        public PaymentMethod(string code)
        {
            Code = code;
            Settings = Array.Empty<ObjectSettingEntry>();
        }
              
        public virtual object Clone()
        {
            var result = MemberwiseClone() as PaymentMethod;

            result.Settings = Settings?.Select(x => x.Clone()).OfType<ObjectSettingEntry>().ToList();

            return result;
        }

    }
    */
}
