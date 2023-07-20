using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PEXC.Case.DataAccess.CosmosDB.Infrastructure;
using PEXC.Case.Functions;
using PEXC.Case.Infrastructure.Converter;
using PEXC.Case.Services;
using PEXC.Case.Services.Health;
using PEXC.Case.Services.Infrastructure;
using PEXC.Common.BaseApi.Options;
using PEXC.Common.BaseApi.Profile;
using PEXC.Common.Options;
using PEXC.Common.ServiceBus.Extensions;
using PEXC.Document.Client.Infrastructure;

[assembly: FunctionsStartup(typeof(Startup))]

namespace PEXC.Case.Functions;

public class Startup : FunctionsStartup
{
    public Startup()
    {
        TypeConverterExtension.AddTypeDescriptors();
    }

    public override void Configure(IFunctionsHostBuilder builder)
    {
        var configuration = builder.GetContext().Configuration;
        builder.Services.RegisterOptions<AzureActiveDirectoryOptions>(configuration);

        builder.Services.AddEventDistributionService(configuration, "ServiceBusOptions:ConnectionString");
        builder.Services.AddDocumentServiceClient(configuration);
        builder.Services.AddCosmosDbServices(configuration);
        builder.Services.AddCosmosDbPerformanceTestServices();
        builder.Services.RegisterCaseDataImportService(configuration);

        builder.Services.RegisterTaxonomy(configuration);
        builder.Services.RegisterProfileApi(configuration);
        builder.Services.AddScoped<IProfileMapper, ProfileMapper>();
        builder.Services.RegisterWorkflowSurveyService(configuration);
        builder.Services.RegisterStaffingApiService(configuration);
        builder.Services.RegisterCaseService();
        builder.Services.RegisterCaseSearchabilityService(configuration);
        builder.Services.RegisterDeletePerformanceTestCasesService();
        builder.Services.RegisterCoveoRefreshService(configuration);


        builder.Services
            .AddHealthChecks()
            .AddCheck<CosmosDbHealthCheck>(CosmosDbHealthCheck.HealthCheckName)
            .AddCheck<ServiceBusHealthCheck>(ServiceBusHealthCheck.HealthCheckName);
    }

    public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
    {
        var context = builder.GetContext();
        builder.ConfigurationBuilder
            .AddJsonFile(
                Path.Combine(context.ApplicationRootPath, "appsettings.json"),
                optional: true,
                reloadOnChange: false)
            .AddJsonFile(
                Path.Combine(context.ApplicationRootPath, $"appsettings.{context.EnvironmentName}.json"),
                optional: true,
                reloadOnChange: false)
            .AddEnvironmentVariables();
    }
}