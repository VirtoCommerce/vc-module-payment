using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.PaymentModule.Core;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.PaymentModule.Core.Model.Search;
using VirtoCommerce.PaymentModule.Core.Services;
using VirtoCommerce.PaymentModule.Data;
using VirtoCommerce.PaymentModule.Data.ExportImport;
using VirtoCommerce.PaymentModule.Data.Repositories;
using VirtoCommerce.PaymentModule.Data.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.ExportImport;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Core.JsonConverters;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Data.Extensions;

namespace VirtoCommerce.PaymentModule.Web
{
    public class Module : IModule, IExportSupport, IImportSupport
    {
        public ManifestModuleInfo ModuleInfo { get; set; }
        private IApplicationBuilder _appBuilder;

        public void Initialize(IServiceCollection serviceCollection)
        {
            var snapshot = serviceCollection.BuildServiceProvider();
            var configuration = snapshot.GetService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("VirtoCommerce.Payment") ?? configuration.GetConnectionString("VirtoCommerce");
            serviceCollection.AddDbContext<PaymentDbContext>(options => options.UseSqlServer(connectionString));
            serviceCollection.AddTransient<IPaymentRepository, PaymentRepository>();
            serviceCollection.AddTransient<Func<IPaymentRepository>>(provider => () => provider.CreateScope().ServiceProvider.GetService<IPaymentRepository>());
            serviceCollection.AddTransient<ISearchService<PaymentMethodsSearchCriteria, PaymentMethodsSearchResult, PaymentMethod>, PaymentMethodsSearchService>();
            serviceCollection.AddTransient(x => (IPaymentMethodsSearchService)x.GetRequiredService<ISearchService<PaymentMethodsSearchCriteria, PaymentMethodsSearchResult, PaymentMethod>>());
            serviceCollection.AddTransient<ICrudService<PaymentMethod>, PaymentMethodsService>();
            serviceCollection.AddTransient(x => (IPaymentMethodsService)x.GetRequiredService<ICrudService<PaymentMethod>>());
            serviceCollection.AddTransient<IPaymentMethodsRegistrar, PaymentMethodsService>();
            serviceCollection.AddTransient<PaymentExportImport>();
        }

        public void PostInitialize(IApplicationBuilder appBuilder)
        {
            _appBuilder = appBuilder;

            var settingsRegistrar = appBuilder.ApplicationServices.GetRequiredService<ISettingsRegistrar>();
            settingsRegistrar.RegisterSettings(ModuleConstants.Settings.AllSettings, ModuleInfo.Id);

            var paymentMethodsRegistrar = appBuilder.ApplicationServices.GetRequiredService<IPaymentMethodsRegistrar>();
            paymentMethodsRegistrar.RegisterPaymentMethod<DefaultManualPaymentMethod>();
            settingsRegistrar.RegisterSettingsForType(ModuleConstants.Settings.DefaultManualPaymentMethod.AllSettings, typeof(DefaultManualPaymentMethod).Name);

            PolymorphJsonConverter.RegisterTypeForDiscriminator(typeof(PaymentMethod), nameof(PaymentMethod.TypeName));

            using (var serviceScope = appBuilder.ApplicationServices.CreateScope())
            {
                var dbContext = serviceScope.ServiceProvider.GetRequiredService<PaymentDbContext>();
                dbContext.Database.MigrateIfNotApplied(MigrationName.GetUpdateV2MigrationName(ModuleInfo.Id));
                dbContext.Database.EnsureCreated();
                dbContext.Database.Migrate();
            }
        }

        public void Uninstall()
        {
        }

        public async Task ExportAsync(Stream outStream, ExportImportOptions options, Action<ExportImportProgressInfo> progressCallback,
            ICancellationToken cancellationToken)
        {
            await _appBuilder.ApplicationServices.GetRequiredService<PaymentExportImport>().DoExportAsync(outStream,
                progressCallback, cancellationToken);
        }

        public async Task ImportAsync(Stream inputStream, ExportImportOptions options, Action<ExportImportProgressInfo> progressCallback,
            ICancellationToken cancellationToken)
        {
            await _appBuilder.ApplicationServices.GetRequiredService<PaymentExportImport>().DoImportAsync(inputStream,
                progressCallback, cancellationToken);
        }
    }
}
