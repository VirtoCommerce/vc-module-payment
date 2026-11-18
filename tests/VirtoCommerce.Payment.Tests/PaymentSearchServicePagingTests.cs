using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MockQueryable.Moq;
using Moq;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.PaymentModule.Core.Model.Search;
using VirtoCommerce.PaymentModule.Core.Services;
using VirtoCommerce.PaymentModule.Data.Model;
using VirtoCommerce.PaymentModule.Data.Repositories;
using VirtoCommerce.PaymentModule.Data.Services;
using VirtoCommerce.Platform.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Core.Settings;
using Xunit;

namespace VirtoCommerce.Payment.Tests;

// The search lists a store's saved payment methods first, then a new instance of every registered method the store has
// not saved. These tests page through that list: they register R method types of their own, save the first S of them
// for one store, and make A of the saved ones active.
[Collection(PaymentMethodTypesCollection.Name)]
public class PaymentSearchServicePagingTests
{
    private const string StoreId = "store-1";
    private const int PageSize = 20;

    // A search that never ends fails the test instead of hanging it.
    private const int MaxPages = 100;

    // Distinct classes, because AbstractTypeFactory keys the types by name.
    private static readonly Type[] _methodTypes =
    [
        typeof(M01), typeof(M02), typeof(M03), typeof(M04), typeof(M05), typeof(M06), typeof(M07), typeof(M08), typeof(M09), typeof(M10),
        typeof(M11), typeof(M12), typeof(M13), typeof(M14), typeof(M15), typeof(M16), typeof(M17), typeof(M18), typeof(M19), typeof(M20),
        typeof(M21), typeof(M22), typeof(M23), typeof(M24), typeof(M25),
    ];

    // R registered, S saved, A active among the saved, and whether the criteria asks for IsActive = true.
    public static TheoryData<int, int, int, bool> SearchAllCases => new()
    {
        // All methods fit on the first page (S + R <= 20)
        { 12, 0, 0, false },
        { 12, 4, 4, false },
        { 12, 5, 5, false },
        { 12, 6, 6, false },
        { 12, 8, 8, false },
        { 10, 10, 10, false },
        // The second page used to start too early
        { 12, 9, 9, false },
        { 11, 10, 10, false },
        { 12, 10, 10, false },
        { 12, 11, 11, false },
        { 12, 12, 12, false },
        // More methods than one page holds
        { 25, 0, 0, false },
        { 25, 3, 3, false },
        { 25, 10, 10, false },
        { 25, 20, 20, false },
        { 25, 21, 21, false },
        // Active methods only: the methods a store has not saved are inactive
        { 12, 12, 9, true },
        { 12, 12, 10, true },
        { 12, 12, 11, true },
        { 12, 12, 12, true },
        { 25, 25, 20, true },
        { 25, 25, 21, true },
    };

    [Theory]
    [MemberData(nameof(SearchAllCases))]
    public async Task SearchAllAsync_ReturnsEachMethodOnce(int registered, int saved, int active, bool onlyActive)
    {
        // Arrange
        using var types = new RegisteredMethodTypes(_methodTypes.Take(registered));
        var rows = CreateSavedRows(saved, active);
        IPaymentMethodsSearchService searchService = new PageLimitedSearchService(CreateSearchService(rows), MaxPages);
        var criteria = new PaymentMethodsSearchCriteria { StoreId = StoreId, IsActive = onlyActive ? true : null };

        // Act
        var methods = await searchService.SearchAllAsync(criteria);

        // Assert
        var expected = onlyActive
            ? rows.Where(x => x.IsActive).Select(x => x.Code)
            : _methodTypes.Take(registered).Select(x => x.Name);

        Assert.Equal(expected.Order(), methods.Select(x => x.Code).Order());
    }

    // SearchBatchesAsync passes one criteria to every page, so the search must leave it as it was.
    [Theory]
    [InlineData(0, 20)]
    [InlineData(10, 10)]
    [InlineData(20, 20)]
    public async Task SearchAsync_LeavesTheCriteriaUnchanged(int skip, int take)
    {
        // Arrange
        using var types = new RegisteredMethodTypes(_methodTypes.Take(12));
        var searchService = CreateSearchService(CreateSavedRows(10, 10));
        var criteria = new PaymentMethodsSearchCriteria { StoreId = StoreId, Skip = skip, Take = take };

        // Act
        await searchService.SearchAsync(criteria);

        // Assert
        Assert.Equal((skip, take), (criteria.Skip, criteria.Take));
    }

    // Paging with a new criteria for each page, as the back office's list does.
    [Theory]
    [InlineData(25, 3)]
    [InlineData(25, 21)]
    public async Task SearchAsync_PagedWithANewCriteriaPerPage_ReturnsEachMethodOnce_AndTheSameTotalOnEveryPage(int registered, int saved)
    {
        // Arrange
        using var types = new RegisteredMethodTypes(_methodTypes.Take(registered));
        var searchService = CreateSearchService(CreateSavedRows(saved, saved));
        var codes = new List<string>();
        var totals = new List<int>();

        // Act
        for (var skip = 0; skip < registered; skip += PageSize)
        {
            var result = await searchService.SearchAsync(new PaymentMethodsSearchCriteria { StoreId = StoreId, Skip = skip, Take = PageSize });
            codes.AddRange(result.Results.Select(x => x.Code));
            totals.Add(result.TotalCount);
        }

        // Assert
        Assert.Equal(_methodTypes.Take(registered).Select(x => x.Name), codes.Order());
        Assert.All(totals, x => Assert.Equal(registered, x));
    }

    private static List<StorePaymentMethodEntity> CreateSavedRows(int saved, int active)
    {
        return _methodTypes
            .Take(saved)
            .Select((type, index) => new StorePaymentMethodEntity
            {
                Id = $"saved-{type.Name}",
                Code = type.Name,
                TypeName = type.Name,
                StoreId = StoreId,
                IsActive = index < active,
            })
            .ToList();
    }

    private static PaymentMethodsSearchService CreateSearchService(List<StorePaymentMethodEntity> rows)
    {
        var paymentMethodsDbSet = rows.BuildMockDbSet();

        var repository = new Mock<IPaymentRepository>();
        repository.Setup(x => x.PaymentMethods).Returns(paymentMethodsDbSet.Object);
        repository
            .Setup(x => x.GetByIdsAsync(It.IsAny<IList<string>>(), It.IsAny<string>()))
            .ReturnsAsync((IList<string> ids, string _) => rows.Where(x => ids.Contains(x.Id)).ToList());

        IPaymentRepository RepositoryFactory() => repository.Object;

        var memoryCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        var platformMemoryCache = new PlatformMemoryCache(memoryCache, Options.Create(new CachingOptions()), new Mock<ILogger<PlatformMemoryCache>>().Object);

        var eventPublisher = new Mock<IEventPublisher>();
        var settingsManager = new Mock<ISettingsManager>();
        var crudService = new PaymentMethodsService(RepositoryFactory, platformMemoryCache, eventPublisher.Object, settingsManager.Object);

        return new PaymentMethodsSearchService(RepositoryFactory, platformMemoryCache, crudService, Options.Create(new CrudOptions()), settingsManager.Object, eventPublisher.Object);
    }

    // Stops a search that asks for more pages than any of these cases needs.
    private sealed class PageLimitedSearchService(IPaymentMethodsSearchService searchService, int maxPages) : IPaymentMethodsSearchService
    {
        private int _pages;

        public Task<PaymentMethodsSearchResult> SearchAsync(PaymentMethodsSearchCriteria criteria, bool clone = true)
        {
            if (++_pages > maxPages)
            {
                throw new InvalidOperationException($"The search did not stop after {maxPages} pages.");
            }

            return searchService.SearchAsync(criteria, clone);
        }
    }

    // Swaps the registered payment method types for the given ones, and puts the previous registrations back on dispose.
    private sealed class RegisteredMethodTypes : IDisposable
    {
        private readonly IList<TypeInfo<PaymentMethod>> _previous = AbstractTypeFactory<PaymentMethod>.AllTypeInfos.ToList();

        public RegisteredMethodTypes(IEnumerable<Type> types)
        {
            RemoveAll();

            foreach (var type in types)
            {
                AbstractTypeFactory<PaymentMethod>.RegisterType(type);
            }
        }

        public void Dispose()
        {
            RemoveAll();

            foreach (var previous in _previous)
            {
                var typeInfo = AbstractTypeFactory<PaymentMethod>.RegisterType(previous.Type);
                typeInfo.MappedType = previous.MappedType;
                typeInfo.Services = previous.Services;

                if (previous.Factory != null)
                {
                    typeInfo.WithFactory(previous.Factory);
                }

                if (previous.SetupAction != null)
                {
                    typeInfo.WithSetupAction(previous.SetupAction);
                }

                if (previous.TypeName != previous.Type.Name)
                {
                    typeInfo.WithTypeName(previous.TypeName);
                }
            }
        }

        private static void RemoveAll()
        {
            foreach (var typeInfo in AbstractTypeFactory<PaymentMethod>.AllTypeInfos.ToList())
            {
                AbstractTypeFactory<PaymentMethod>.RemoveType(typeInfo.Type);
            }
        }
    }

    public abstract class PagingTestMethod(string code) : PaymentMethod(code)
    {
        public override PaymentMethodType PaymentMethodType { get; }
        public override PaymentMethodGroupType PaymentMethodGroupType { get; }
    }

    public sealed class M01() : PagingTestMethod(nameof(M01));
    public sealed class M02() : PagingTestMethod(nameof(M02));
    public sealed class M03() : PagingTestMethod(nameof(M03));
    public sealed class M04() : PagingTestMethod(nameof(M04));
    public sealed class M05() : PagingTestMethod(nameof(M05));
    public sealed class M06() : PagingTestMethod(nameof(M06));
    public sealed class M07() : PagingTestMethod(nameof(M07));
    public sealed class M08() : PagingTestMethod(nameof(M08));
    public sealed class M09() : PagingTestMethod(nameof(M09));
    public sealed class M10() : PagingTestMethod(nameof(M10));
    public sealed class M11() : PagingTestMethod(nameof(M11));
    public sealed class M12() : PagingTestMethod(nameof(M12));
    public sealed class M13() : PagingTestMethod(nameof(M13));
    public sealed class M14() : PagingTestMethod(nameof(M14));
    public sealed class M15() : PagingTestMethod(nameof(M15));
    public sealed class M16() : PagingTestMethod(nameof(M16));
    public sealed class M17() : PagingTestMethod(nameof(M17));
    public sealed class M18() : PagingTestMethod(nameof(M18));
    public sealed class M19() : PagingTestMethod(nameof(M19));
    public sealed class M20() : PagingTestMethod(nameof(M20));
    public sealed class M21() : PagingTestMethod(nameof(M21));
    public sealed class M22() : PagingTestMethod(nameof(M22));
    public sealed class M23() : PagingTestMethod(nameof(M23));
    public sealed class M24() : PagingTestMethod(nameof(M24));
    public sealed class M25() : PagingTestMethod(nameof(M25));
}
