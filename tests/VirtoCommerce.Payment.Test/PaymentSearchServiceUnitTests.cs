using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MockQueryable.Moq;
using Moq;
using VirtoCommerce.PaymentModule.Core.Model.Search;
using VirtoCommerce.PaymentModule.Core.Services;
using VirtoCommerce.PaymentModule.Data;
using VirtoCommerce.PaymentModule.Data.Model;
using VirtoCommerce.PaymentModule.Data.Repositories;
using VirtoCommerce.PaymentModule.Data.Services;
using VirtoCommerce.Platform.Caching;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Core.Settings;
using Xunit;

namespace VirtoCommerce.Payment.Test
{
    public class PaymentSearchServiceUnitTests
    {
        [Fact]
        public async Task SearchPaymentMethods_ReturnRegisteredPayments()
        {
            // Arrange
            var paymentMethods = new List<StorePaymentMethodEntity>
            {
                new() { Id = "1", Code = nameof(DefaultManualPaymentMethod), TypeName = nameof(DefaultManualPaymentMethod), },
                new() { Id = "2", Code = "not-a-class-name-1", TypeName = nameof(TestPaymentMethod), },
                new() { Id = "3", Code = "not-a-class-name-2", TypeName = nameof(TestPaymentMethod), },
            };

            var searchService = CreateService(paymentMethods);

            // Act
            var results = await searchService.SearchAsync(new PaymentMethodsSearchCriteria { WithoutTransient = true });

            // Assert
            Assert.Equal(results.Results.Count, results.TotalCount);
            Assert.Equal("not-a-class-name-1", results.Results[0].Code);
        }

        private static PaymentMethodsSearchService CreateService(List<StorePaymentMethodEntity> paymentsInDb)
        {
            var settingsManager = new Mock<ISettingsManager>();
            var eventPublisher = new Mock<IEventPublisher>();

            var paymentMethodsDbSet = paymentsInDb.BuildMockDbSet();

            var paymentRepository = new Mock<IPaymentRepository>();
            paymentRepository.Setup(x => x.PaymentMethods).Returns(paymentMethodsDbSet.Object);
            paymentRepository.Setup(x => x.GetByIdsAsync(new List<string> { "2" }, It.IsAny<string>()))
                .ReturnsAsync(paymentsInDb.Skip(1).Take(1).ToList());

            var memoryCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
            var platformMemoryCache = new PlatformMemoryCache(memoryCache, Options.Create(new CachingOptions()), new Mock<ILogger<PlatformMemoryCache>>().Object);
            var factory = () => paymentRepository.Object;
            var crudOptions = Options.Create(new CrudOptions());
            var crudService = new PaymentMethodsService(factory, platformMemoryCache, eventPublisher.Object, settingsManager.Object);
            var paymentMethodRegistrar = (IPaymentMethodsRegistrar)crudService;
            paymentMethodRegistrar.RegisterPaymentMethod(() => new TestPaymentMethod("not-a-class-name-1"));

            var searchService = new PaymentMethodsSearchService(factory,
                platformMemoryCache, crudService, crudOptions, settingsManager.Object, eventPublisher.Object);

            return searchService;
        }
    }
}
