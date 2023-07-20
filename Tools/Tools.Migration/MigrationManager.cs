using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog.Extensions.Logging;
using PEXC.Case.DataAccess.CosmosDB.Infrastructure;
using PEXC.Case.Services.Infrastructure;
using PEXC.Case.Tools.Migration.Ccm;
using PEXC.Case.Tools.Migration.Csv;
using PEXC.Case.Tools.Migration.Transformations;
using PEXC.Common.Authentication.Abstractions;
using PEXC.Common.BaseApi.Authentication;
using PEXC.Common.BaseApi.Options;
using PEXC.Common.Options;
using PEXC.Common.Taxonomy.Infrastructure;
using PEXC.Document.Client.Infrastructure;

namespace PEXC.Case.Tools.Migration;

public class MigrationManager
{
    public static IServiceProvider RegisterServices(IConfiguration configuration) 
        => RegisterServices(new ServiceCollection(), configuration);

    public static IServiceProvider RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.RegisterOptions<AzureActiveDirectoryOptions>(configuration);
        var migrationOptions = services.RegisterOptions<MigrationOptions>(configuration);
        var apiOptions = services.RegisterOptions<ClientCaseApiOptions>(configuration);
        services.AddMemoryCache();
        services.AddTaxonomy(configuration);
        services.AddScoped<IAppAuthorityTokenProvider, AppAuthorityTokenProvider>();

        services.AddScoped<TaxonomyDataMapper>();
        services.AddScoped<CsvRecordReader>();
        services.AddScoped<RecordProcessor>();  

        services.AddAutoMapper(typeof(MigrationProfile));

        services.AddHttpClient<CcmApi>(client =>
        {
            client.BaseAddress = new Uri(apiOptions.BaseAddress!);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiOptions.ApiKey);
        });

        services.AddSingleton<ICcmApi>(sp => new CmmCacheFileApi(sp.GetRequiredService<CcmApi>()));
        services.AddSingleton<MigrationDataPersister>();
        services.AddSingleton<MigrationContext>();
        services.AddSingleton<CcmLoader>();
        services.AddSingleton<MigrationReportPrinter>();
        services.AddSingleton<ExistingDataLoader>();

        var profileConnString = configuration.GetConnectionString("Profile");
        services.AddSingleton(_ => new ECodeLoader(profileConnString));
        services.AddSingleton<EcodesPropertiesProcessor>();

        if (migrationOptions.RandomizeData)
            services.AddSingleton<IRandomizer, DataRandomizer>();
        else
            services.AddSingleton<IRandomizer, EmptyRandomier>();

        services.AddCosmosDbServices(configuration);
        services.AddDocumentServiceClient(configuration);

        services.AddLogging(lb => lb.AddNLog());

        return services.BuildServiceProvider();
    }

    public async Task StartMigration(TextReader reader, IConfiguration configuration)
    {
        var sp = RegisterServices(configuration);

        var mapperConfiguration = sp.GetRequiredService<AutoMapper.IConfigurationProvider>();
        mapperConfiguration.AssertConfigurationIsValid();

        var recordProcessor = sp.GetRequiredService<RecordProcessor>();
        await recordProcessor.Process(reader);

        var reportPrinter = sp.GetRequiredService<MigrationReportPrinter>();
        reportPrinter.Print();
    }
}
