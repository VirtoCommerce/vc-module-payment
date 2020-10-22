using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.PaymentModule.Core.Model.Search;
using VirtoCommerce.PaymentModule.Core.Services;

namespace VirtoCommerce.PaymentModule.Web.Controllers.Api
{
    [Route("api/payment")]
    [Authorize]
    public class PaymentModuleController : Controller
    {
        private readonly IPaymentMethodsSearchService _paymentMethodsSearchService;
        private readonly IPaymentMethodsService _paymentMethodsService;
        private readonly IPaymentMethodsRegistrar _paymentMethodsRegistrar;

        public PaymentModuleController(
            IPaymentMethodsSearchService paymentMethodsSearchService,
            IPaymentMethodsService paymentMethodsService,
            IPaymentMethodsRegistrar paymentMethodsRegistrar
            )
        {
            _paymentMethodsSearchService = paymentMethodsSearchService;
            _paymentMethodsService = paymentMethodsService;
            _paymentMethodsRegistrar = paymentMethodsRegistrar;
        }

        [HttpGet]
        [Route("")]
        public async Task<ActionResult<PaymentMethod>> GetRegisteredPaymentMethods()
        {
            var result = await _paymentMethodsRegistrar.GetRegisteredPaymentMethods();
            return Ok(result);
        }

        [HttpPost]
        [Route("search")]
        public async Task<ActionResult<PaymentMethodsSearchResult>> SearchPaymentMethods([FromBody] PaymentMethodsSearchCriteria criteria)
        {
            var result = await _paymentMethodsSearchService.SearchPaymentMethodsAsync(criteria);
            return Ok(result);
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<ActionResult<PaymentMethod>> GetPaymentMethodById(string id)
        {
            var result = await _paymentMethodsService.GetByIdAsync(id, null);
            return Ok(result);
        }

        [HttpPut]
        [Route("")]
        public async Task<ActionResult<PaymentMethod>> UpdatePaymentMethod([FromBody] PaymentMethod paymentMethod)
        {
            await _paymentMethodsService.SaveChangesAsync(new[] { paymentMethod });
            return Ok(paymentMethod);
        }
    }
}
