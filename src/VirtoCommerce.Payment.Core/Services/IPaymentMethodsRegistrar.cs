using System;
using System.Threading.Tasks;
using VirtoCommerce.PaymentModule.Core.Model;

namespace VirtoCommerce.PaymentModule.Core.Services
{
    public interface IPaymentMethodsRegistrar
    {
        void RegisterPaymentMethod<T>(Func<T> factory = null) where T : PaymentMethod;
        Task<PaymentMethod[]> GetRegisteredPaymentMethods();
    }
}
