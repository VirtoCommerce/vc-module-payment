using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MockQueryable.Moq;
using Moq;
using VirtoCommerce.PaymentModule.Data.Model;
using VirtoCommerce.PaymentModule.Data.Repositories;
using VirtoCommerce.PaymentModule.Data.Services;
using VirtoCommerce.Platform.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Domain;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.GenericCrud;

namespace VirtoCommerce.Payment.Tests.StoredCredentials;

/// <summary>
/// The stored payment method and consent services over an in-memory repository mock.
/// </summary>
internal sealed class StoredCredentialServicesFixture
{
    public List<StoredPaymentMethodEntity> StoredPaymentMethods { get; } = [];
    public List<PaymentConsentEntity> PaymentConsents { get; } = [];
    public List<IEntity> Added { get; } = [];

    public Mock<IPaymentRepository> Repository { get; } = new();
    public Mock<IUnitOfWork> UnitOfWork { get; } = new();
    public Mock<IEventPublisher> EventPublisher { get; } = new();

    public StoredPaymentMethodService StoredPaymentMethodService { get; }
    public StoredPaymentMethodSearchService StoredPaymentMethodSearchService { get; }
    public PaymentConsentService PaymentConsentService { get; }
    public PaymentConsentSearchService PaymentConsentSearchService { get; }

    public StoredCredentialServicesFixture(IEnumerable<StoredPaymentMethodEntity> storedPaymentMethods = null, IEnumerable<PaymentConsentEntity> paymentConsents = null)
    {
        StoredPaymentMethods.AddRange(storedPaymentMethods ?? []);
        PaymentConsents.AddRange(paymentConsents ?? []);

        Repository.Setup(x => x.StoredPaymentMethods).Returns(() => StoredPaymentMethods.BuildMockDbSet().Object);
        Repository.Setup(x => x.PaymentConsents).Returns(() => PaymentConsents.BuildMockDbSet().Object);
        Repository
            .Setup(x => x.GetStoredPaymentMethodsByIdsAsync(It.IsAny<IList<string>>(), It.IsAny<string>()))
            .ReturnsAsync((IList<string> ids, string _) => StoredPaymentMethods.Where(x => ids.Contains(x.Id)).ToList());
        Repository
            .Setup(x => x.GetPaymentConsentsByIdsAsync(It.IsAny<IList<string>>(), It.IsAny<string>()))
            .ReturnsAsync((IList<string> ids, string _) => PaymentConsents.Where(x => ids.Contains(x.Id)).ToList());
        Repository.Setup(x => x.UnitOfWork).Returns(UnitOfWork.Object);
        Repository.Setup(x => x.Add(It.IsAny<IEntity>())).Callback<IEntity>(Added.Add);

        var repositoryFactory = () => Repository.Object;
        var crudOptions = Options.Create(new CrudOptions());

        StoredPaymentMethodService = new StoredPaymentMethodService(repositoryFactory, CreateCache(), EventPublisher.Object);
        StoredPaymentMethodSearchService = new StoredPaymentMethodSearchService(repositoryFactory, CreateCache(), StoredPaymentMethodService, crudOptions);
        PaymentConsentService = new PaymentConsentService(repositoryFactory, CreateCache(), EventPublisher.Object);
        PaymentConsentSearchService = new PaymentConsentSearchService(repositoryFactory, CreateCache(), PaymentConsentService, crudOptions);
    }

    private static PlatformMemoryCache CreateCache()
    {
        var memoryCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        return new PlatformMemoryCache(memoryCache, Options.Create(new CachingOptions()), new Mock<ILogger<PlatformMemoryCache>>().Object);
    }
}
