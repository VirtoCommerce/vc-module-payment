using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.PaymentModule.Core.Model.Search;
using VirtoCommerce.PaymentModule.Core.Services;
using VirtoCommerce.PaymentModule.Data.Model;
using VirtoCommerce.PaymentModule.Data.Repositories;
using VirtoCommerce.PaymentModule.Data.Services;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Core.Settings;
using Xunit;

namespace VirtoCommerce.Payment.Tests;

// The search lists a store's saved methods, then a new instance of each unsaved one; these tests page through that list.
[Collection(PaymentMethodTypesCollection.Name)]
public class PaymentSearchServicePagingTests
{
    private const string StoreId = "store-1";
    private const int MaxPages = 100;

    // The default Take, which SearchAllAsync callers keep.
    private static readonly int _pageSize = new PaymentMethodsSearchCriteria().Take;

    // Distinct classes, because AbstractTypeFactory keys the types by name.
    private static readonly Type[] _methodTypes =
    [
        typeof(M01), typeof(M02), typeof(M03), typeof(M04), typeof(M05), typeof(M06), typeof(M07), typeof(M08), typeof(M09), typeof(M10),
        typeof(M11), typeof(M12), typeof(M13), typeof(M14), typeof(M15), typeof(M16), typeof(M17), typeof(M18), typeof(M19), typeof(M20),
        typeof(M21), typeof(M22), typeof(M23), typeof(M24), typeof(M25),
    ];

    [Theory]
    // All methods fit on the first page (saved + registered <= 20)
    [InlineData(12, 0)]
    [InlineData(12, 4)]
    [InlineData(12, 5)]
    [InlineData(12, 6)]
    [InlineData(12, 8)]
    [InlineData(10, 10)]
    // The second page used to start too early
    [InlineData(12, 9)]
    [InlineData(11, 10)]
    [InlineData(12, 10)]
    [InlineData(12, 11)]
    [InlineData(12, 12)]
    // More methods than one page holds
    [InlineData(25, 0)]
    [InlineData(25, 3)]
    [InlineData(25, 10)]
    [InlineData(25, 20)]
    [InlineData(25, 21)]
    public async Task SearchAllAsync_ReturnsEachRegisteredMethodOnce(int registered, int saved)
    {
        // Arrange
        using var types = new RegisteredMethodTypes(_methodTypes.Take(registered));
        var rows = CreateSavedRows(saved, active: saved);
        var searchService = CreateSearchService(rows);

        // Act
        var methods = await searchService.SearchAllAsync(new PaymentMethodsSearchCriteria { StoreId = StoreId });

        // Assert
        Assert.Equal(_methodTypes.Take(registered).Select(x => x.Name), methods.Select(x => x.Code).Order());
        Assert.Equal(rows.Select(x => x.Id), methods.Where(x => x.Id != null).Select(x => x.Id).Order());
    }

    // All methods saved, the first ones active. Unsaved methods start inactive, so none is added.
    [Theory]
    [InlineData(12, 9)]
    [InlineData(12, 10)]
    [InlineData(12, 11)]
    [InlineData(12, 12)]
    [InlineData(25, 20)]
    [InlineData(25, 21)]
    public async Task SearchAllAsync_ActiveOnly_ReturnsEachActiveMethodOnce(int registered, int active)
    {
        // Arrange
        using var types = new RegisteredMethodTypes(_methodTypes.Take(registered));
        var rows = CreateSavedRows(registered, active);
        var searchService = CreateSearchService(rows);

        // Act
        var methods = await searchService.SearchAllAsync(new PaymentMethodsSearchCriteria { StoreId = StoreId, IsActive = true });

        // Assert
        Assert.Equal(_methodTypes.Take(active).Select(x => x.Name), methods.Select(x => x.Code).Order());
        Assert.Equal(rows.Where(x => x.IsActive).Select(x => x.Id), methods.Select(x => x.Id).Order());
    }

    [Theory]
    [InlineData(12, 11)]
    [InlineData(25, 21)]
    public async Task SearchAllAsync_WithoutTransient_ReturnsEachSavedMethodOnce(int registered, int saved)
    {
        // Arrange
        using var types = new RegisteredMethodTypes(_methodTypes.Take(registered));
        var rows = CreateSavedRows(saved, saved);
        var searchService = CreateSearchService(rows);

        // Act
        var methods = await searchService.SearchAllAsync(new PaymentMethodsSearchCriteria { StoreId = StoreId, WithoutTransient = true });

        // Assert
        Assert.Equal(rows.Select(x => x.Id), methods.Select(x => x.Id).Order());
    }

    // SearchBatchesAsync reuses the criteria for every page.
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

    // A new criteria per page, as the back office's list does.
    [Theory]
    [InlineData(25, 3)]
    [InlineData(25, 21)]
    public async Task SearchAsync_PagedWithANewCriteriaPerPage_ReturnsEachMethodOnce_AndTheSameTotalOnEveryPage(int registered, int saved)
    {
        // Arrange
        using var types = new RegisteredMethodTypes(_methodTypes.Take(registered));
        var rows = CreateSavedRows(saved, saved);
        var searchService = CreateSearchService(rows);
        var codes = new List<string>();
        var ids = new List<string>();
        var totals = new List<int>();

        // Act
        for (var skip = 0; skip < registered; skip += _pageSize)
        {
            var result = await searchService.SearchAsync(new PaymentMethodsSearchCriteria { StoreId = StoreId, Skip = skip, Take = _pageSize });
            codes.AddRange(result.Results.Select(x => x.Code));
            ids.AddRange(result.Results.Where(x => x.Id != null).Select(x => x.Id));
            totals.Add(result.TotalCount);
        }

        // Assert
        Assert.Equal(_methodTypes.Take(registered).Select(x => x.Name), codes.Order());
        Assert.Equal(rows.Select(x => x.Id), ids.Order());
        Assert.All(totals, x => Assert.Equal(registered, x));
    }

    [Fact]
    public async Task SearchAsync_TakeZero_CountsTheUnsavedMethodsToo()
    {
        // Arrange
        using var types = new RegisteredMethodTypes(_methodTypes.Take(12));
        var searchService = CreateSearchService(CreateSavedRows(9, 9));

        // Act
        var result = await searchService.SearchAsync(new PaymentMethodsSearchCriteria { StoreId = StoreId, Take = 0 });

        // Assert
        Assert.Equal(12, result.TotalCount);
        Assert.Empty(result.Results);
    }

    [Fact]
    public async Task SearchAsync_WithoutTransient_ReturnsOnlyTheSavedMethods()
    {
        // Arrange
        using var types = new RegisteredMethodTypes(_methodTypes.Take(12));
        var rows = CreateSavedRows(9, 9);
        var searchService = CreateSearchService(rows);

        // Act
        var result = await searchService.SearchAsync(new PaymentMethodsSearchCriteria { StoreId = StoreId, WithoutTransient = true });

        // Assert
        Assert.Equal(rows.Count, result.TotalCount);
        Assert.Equal(rows.Select(x => x.Id), result.Results.Select(x => x.Id));
    }

    [Fact]
    public async Task SearchAsync_GetTransientMethodsAsyncOverride_ChangesTheUnsavedMethods()
    {
        // Arrange
        using var types = new RegisteredMethodTypes(_methodTypes.Take(12));
        var (_, searchService) = PaymentSearchServiceUnitTests.CreateService(
            PaymentSearchServiceUnitTests.CreateRepositoryMock(CreateSavedRows(3, 3)),
            (repositoryFactory, cache, crudService, crudOptions, settingsManager, eventPublisher) =>
                new CodesSearchService(repositoryFactory, cache, crudService, crudOptions, settingsManager, eventPublisher));

        // Act
        var result = await searchService.SearchAsync(new PaymentMethodsSearchCriteria { StoreId = StoreId, Codes = ["M02", "M05"] });

        // Assert
        Assert.Equal(["M02", "M05"], result.Results.Select(x => x.Code));
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task SearchAsync_GetSavedMethodTypesAsyncOverride_RunsOnAPageWithEverySavedRow()
    {
        // Arrange
        using var types = new RegisteredMethodTypes(_methodTypes.Take(12));
        var (_, searchService) = PaymentSearchServiceUnitTests.CreateService(
            PaymentSearchServiceUnitTests.CreateRepositoryMock(CreateSavedRows(3, 3)),
            (repositoryFactory, cache, crudService, crudOptions, settingsManager, eventPublisher) =>
                new M12SavedSearchService(repositoryFactory, cache, crudService, crudOptions, settingsManager, eventPublisher));

        // Act
        var result = await searchService.SearchAsync(new PaymentMethodsSearchCriteria { StoreId = StoreId });

        // Assert
        Assert.Equal(_methodTypes.Take(11).Select(x => x.Name), result.Results.Select(x => x.Code));
        Assert.Equal(11, result.TotalCount);
    }

    [Fact]
    public async Task SearchAsync_OverrideEditingTheSavedRowsLookup_LeavesTheCachedMethodsUnchanged()
    {
        // Arrange
        using var types = new RegisteredMethodTypes(_methodTypes.Take(12));
        var (_, searchService) = PaymentSearchServiceUnitTests.CreateService(
            PaymentSearchServiceUnitTests.CreateRepositoryMock(CreateSavedRows(3, 3)),
            (repositoryFactory, cache, crudService, crudOptions, settingsManager, eventPublisher) =>
                new SavedRowsEditingSearchService(repositoryFactory, cache, crudService, crudOptions, settingsManager, eventPublisher));

        // Act: a page past the saved rows looks them up, and the override edits what that lookup gets.
        await searchService.SearchAsync(new PaymentMethodsSearchCriteria { StoreId = StoreId, Skip = 3, Take = 2 });
        var result = await searchService.SearchAsync(new PaymentMethodsSearchCriteria { StoreId = StoreId });

        // Assert
        var savedMethods = result.Results.Where(x => x.Id != null).ToList();
        Assert.Equal(3, savedMethods.Count);
        Assert.All(savedMethods, x => Assert.NotEqual(SavedRowsEditingSearchService.Edited, x.Description));
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

    private static IPaymentMethodsSearchService CreateSearchService(List<StorePaymentMethodEntity> rows)
    {
        var (_, searchService) = PaymentSearchServiceUnitTests.CreateService(PaymentSearchServiceUnitTests.CreateRepositoryMock(rows));

        return new PageLimitedSearchService(searchService);
    }

    // Fails a search that never ends, instead of hanging the test.
    private sealed class PageLimitedSearchService(IPaymentMethodsSearchService searchService) : IPaymentMethodsSearchService
    {
        private int _pages;

        public Task<PaymentMethodsSearchResult> SearchAsync(PaymentMethodsSearchCriteria criteria, bool clone = true)
        {
            if (++_pages > MaxPages)
            {
                throw new InvalidOperationException($"The search did not stop after {MaxPages} pages.");
            }

            return searchService.SearchAsync(criteria, clone);
        }
    }

    // Registers only the given method types, and restores the previous registrations on dispose.
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

    // Lists only the unsaved methods whose codes the criteria asks for.
    private sealed class CodesSearchService(
        Func<IPaymentRepository> repositoryFactory,
        IPlatformMemoryCache platformMemoryCache,
        IPaymentMethodsService crudService,
        IOptions<CrudOptions> crudOptions,
        ISettingsManager settingsManager,
        IEventPublisher eventPublisher)
        : PaymentMethodsSearchService(repositoryFactory, platformMemoryCache, crudService, crudOptions, settingsManager, eventPublisher)
    {
        protected override async Task<IList<PaymentMethod>> GetTransientMethodsAsync(PaymentMethodsSearchCriteria criteria)
        {
            var methods = await base.GetTransientMethodsAsync(criteria);

            return methods.Where(x => criteria.Codes.Contains(x.Code)).ToList();
        }
    }

    // Counts M12 as saved.
    private sealed class M12SavedSearchService(
        Func<IPaymentRepository> repositoryFactory,
        IPlatformMemoryCache platformMemoryCache,
        IPaymentMethodsService crudService,
        IOptions<CrudOptions> crudOptions,
        ISettingsManager settingsManager,
        IEventPublisher eventPublisher)
        : PaymentMethodsSearchService(repositoryFactory, platformMemoryCache, crudService, crudOptions, settingsManager, eventPublisher)
    {
        protected override async Task<ISet<Type>> GetSavedMethodTypesAsync(PaymentMethodsSearchResult result, PaymentMethodsSearchCriteria criteria)
        {
            var types = await base.GetSavedMethodTypesAsync(result, criteria);
            types.Add(typeof(M12));

            return types;
        }
    }

    // Edits the models of every saved-rows-only search, as an override that changes what base returns may do.
    private sealed class SavedRowsEditingSearchService(
        Func<IPaymentRepository> repositoryFactory,
        IPlatformMemoryCache platformMemoryCache,
        IPaymentMethodsService crudService,
        IOptions<CrudOptions> crudOptions,
        ISettingsManager settingsManager,
        IEventPublisher eventPublisher)
        : PaymentMethodsSearchService(repositoryFactory, platformMemoryCache, crudService, crudOptions, settingsManager, eventPublisher)
    {
        public const string Edited = "edited by an override";

        protected override async Task<PaymentMethodsSearchResult> ProcessSearchResultAsync(PaymentMethodsSearchResult result, PaymentMethodsSearchCriteria criteria)
        {
            result = await base.ProcessSearchResultAsync(result, criteria);

            if (criteria.WithoutTransient)
            {
                foreach (var method in result.Results)
                {
                    method.Description = Edited;
                }
            }

            return result;
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
