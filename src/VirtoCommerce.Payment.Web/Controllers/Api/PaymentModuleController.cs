using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.PaymentModule.Core.Model.Search;
using VirtoCommerce.PaymentModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.PaymentModule.Web.Controllers.Api
{
    [Route("api/payment")]
    [Authorize]
    public class PaymentModuleController : Controller
    {
        private readonly IPaymentMethodsRegistrar _paymentMethodsRegistrar;
        private readonly IPaymentMethodsService _paymentMethodCrudService;
        private readonly IPaymentMethodsSearchService _paymentMethodsSearchService;

        public PaymentModuleController(
            IPaymentMethodsRegistrar paymentMethodsRegistrar,
            IPaymentMethodsService paymentMethodsService,
            IPaymentMethodsSearchService paymentMethodsSearchService)
        {
            _paymentMethodsRegistrar = paymentMethodsRegistrar;
            _paymentMethodCrudService = paymentMethodsService;
            _paymentMethodsSearchService = paymentMethodsSearchService;
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
            var result = await _paymentMethodsSearchService.SearchNoCloneAsync(criteria);
            return Ok(result);
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<ActionResult<PaymentMethod>> GetPaymentMethodById(string id)
        {
            var result = await _paymentMethodCrudService.GetNoCloneAsync(id);
            return Ok(result);
        }

        [HttpPut]
        [Route("")]
        public async Task<ActionResult<PaymentMethod>> UpdatePaymentMethod([FromBody] PaymentMethod paymentMethod)
        {
            await _paymentMethodCrudService.SaveChangesAsync(new[] { paymentMethod });
            return Ok(paymentMethod);
        }
    }
}
