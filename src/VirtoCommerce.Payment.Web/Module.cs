using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VirtoCommerce.PaymentModule.Core;
using VirtoCommerce.PaymentModule.Core.Model;
using VirtoCommerce.PaymentModule.Core.Model.Search;
using VirtoCommerce.PaymentModule.Core.Services;
using VirtoCommerce.PaymentModule.Data;
using VirtoCommerce.PaymentModule.Data.ExportImport;
using VirtoCommerce.PaymentModule.Data.Repositories;
using VirtoCommerce.PaymentModule.Data.Services;
using VirtoCommerce.PaymentModule.Web.JsonConverters;
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
            serviceCollection.AddDbContext<PaymentDbContext>((provider, options) =>
            {
                var configuration = provider.GetRequiredService<IConfiguration>();
                options.UseSqlServer(configuration.GetConnectionString(ModuleInfo.Id) ?? configuration.GetConnectionString("VirtoCommerce"));
            });

            serviceCollection.AddTransient<IPaymentRepository, PaymentRepository>();
            serviceCollection.AddTransient<Func<IPaymentRepository>>(provider => () => provider.CreateScope().ServiceProvider.GetService<IPaymentRepository>());
            serviceCollection.AddTransient<ISearchService<PaymentMethodsSearchCriteria, PaymentMethodsSearchResult, PaymentMethod>, PaymentMethodsSearchService>();
            serviceCollection.AddTransient(x => (IPaymentMethodsSearchService)x.GetRequiredService<ISearchService<PaymentMethodsSearchCriteria, PaymentMethodsSearchResult, PaymentMethod>>());
            serviceCollection.AddTransient<ICrudService<PaymentMethod>, PaymentMethodsService>();
            serviceCollection.AddTransient(x => (IPaymentMethodsService)x.GetRequiredService<ICrudService<PaymentMethod>>());
            serviceCollection.AddTransient<IPaymentMethodsRegistrar, PaymentMethodsService>();
            serviceCollection.AddTransient<PaymentExportImport>();
            serviceCollection.AddTransient<PaymentMethodsJsonConverter>();
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

            var mvcJsonOptions = appBuilder.ApplicationServices.GetService<IOptions<MvcNewtonsoftJsonOptions>>();
            mvcJsonOptions.Value.SerializerSettings.Converters.Add(appBuilder.ApplicationServices.GetService<PaymentMethodsJsonConverter>());
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
