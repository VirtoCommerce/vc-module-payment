using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.PaymentModule.Core.Model.Search;
using VirtoCommerce.PaymentModule.Core.Services;
using VirtoCommerce.Platform.Core.GenericCrud;

namespace VirtoCommerce.PaymentModule.Web.Controllers.Api
{
    [Route("api/payment")]
    [Authorize]
    public class PaymentModuleController : Controller
    {

        private readonly ISearchService<PaymentMethodsSearchCriteria, PaymentMethodsSearchResult, PaymentMethod> _paymentMethodsSearchService;
        private readonly IPaymentMethodsRegistrar _paymentMethodsRegistrar;
        private readonly ICrudService<PaymentMethod> _paymentMethodCrudService;

        public PaymentModuleController(IPaymentMethodsSearchService paymentMethodsSearchService, IPaymentMethodsService paymentMethodsService)
        {
            _paymentMethodsSearchService = (ISearchService<PaymentMethodsSearchCriteria, PaymentMethodsSearchResult, PaymentMethod>)paymentMethodsSearchService;
            _paymentMethodsRegistrar = (IPaymentMethodsRegistrar)paymentMethodsService;
            _paymentMethodCrudService = (ICrudService<PaymentMethod>)paymentMethodsService;
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
            var result = await _paymentMethodsSearchService.SearchAsync(criteria);
            return Ok(result);
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<ActionResult<PaymentMethod>> GetPaymentMethodById(string id)
        {
            var result = await _paymentMethodCrudService.GetByIdAsync(id, null);
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
