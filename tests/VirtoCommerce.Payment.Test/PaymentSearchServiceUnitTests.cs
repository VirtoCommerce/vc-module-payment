using System;
using System.Collections.Generic;
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
            var paymentRepository = new Mock<IPaymentRepository>();
            var settingsManager = new Mock<ISettingsManager>();
            var eventPublisher = new Mock<IEventPublisher>();

            var paymentMethods = new List<StorePaymentMethodEntity>
            {
                new() { Id = "1", Code = "Default", TypeName = nameof(DefaultManualPaymentMethod), },
                new() { Id = "2", Code = "not-a-class-name", TypeName = nameof(TestPaymentMethod), },
            }.BuildMockDbSet();
            paymentRepository.Setup(x => x.PaymentMethods).Returns(paymentMethods.Object);

            var memoryCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
            var platformMemoryCache = new PlatformMemoryCache(memoryCache, Options.Create(new CachingOptions()), new Mock<ILogger<PlatformMemoryCache>>().Object);
            var factory = () => paymentRepository.Object;
            var crudOptions = Options.Create(new CrudOptions());
            var crudService = new PaymentMethodsService(factory, platformMemoryCache, eventPublisher.Object, settingsManager.Object);
            var paymentMethodRegistrar = (IPaymentMethodsRegistrar)crudService;
            paymentMethodRegistrar.RegisterPaymentMethod<TestPaymentMethod>();

            var searchService = new PaymentMethodsSearchService(factory,
                platformMemoryCache, crudService, crudOptions, settingsManager.Object, eventPublisher.Object);

            try
            {
                // Act
                var results = await searchService.SearchAsync(new PaymentMethodsSearchCriteria { WithoutTransient = true });

                // Assert
                Assert.Equal(results.Results.Count, results.TotalCount);
                Assert.Equal("not-a-class-name", results.Results[0].Code);
            }
            catch (Exception ex)
            {
                // Handle exception
                throw new Exception("An error occurred while executing the test.", ex);
            }
            finally
            {
                // Cleanup
                memoryCache.Dispose();
            }
        }
    }
}
