using System;
using Newtonsoft.Json;
using VirtoCommerce.PaymentModule.Core.Events;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.JsonConverters;

namespace VirtoCommerce.PaymentModule.Web.JsonConverters
{
    public class PaymentMethodsJsonConverter : PolymorphJsonConverter
    {
        private readonly IEventPublisher _eventPublisher;

        public PaymentMethodsJsonConverter(IEventPublisher eventPublisher)
        {
            _eventPublisher = eventPublisher;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(PaymentMethod).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            _eventPublisher.Publish(new PaymentMethodInstancingEvent()).GetAwaiter().GetResult();

            return base.ReadJson(reader, objectType, existingValue, serializer);
        }
    }
}
