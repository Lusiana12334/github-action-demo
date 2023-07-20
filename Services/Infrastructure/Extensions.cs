using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PEXC.Case.DataAccess.CosmosDB.Infrastructure;
using PEXC.Case.Infrastructure;
using PEXC.Case.Services.CCM;
using PEXC.Case.Services.Coveo;
using PEXC.Case.Services.IRIS;
using PEXC.Case.Services.Mapping;
using PEXC.Case.Services.Staffing;
using PEXC.Case.Services.Workflow;
using PEXC.Common.Authentication.Abstractions;
using PEXC.Common.BaseApi.Authentication;
using PEXC.MailDistribution.Infrastructure;
using PEXC.Common.Options;
using PEXC.Common.Taxonomy.Infrastructure;
using Polly;

namespace PEXC.Case.Services.Infrastructure;

public static class Extensions
{
    public static void RegisterTaxonomy(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTaxonomy(configuration);
        services.AddScoped<IAppAuthorityTokenProvider, AppAuthorityTokenProvider>();
        services.AddScoped<ITaxonomyServiceFactory, TaxonomyServiceFactory>();
    }

    public static void RegisterCaseDataImportService(this IServiceCollection services, IConfiguration configuration)
    {
        var ccmApiOptions = services.RegisterOptions<ClientCaseApiOptions>(configuration);
        services
            .AddHttpClient<IClientCaseApiService, ClientCaseApiService>(
                httpClient =>
                {
                    httpClient.BaseAddress = new Uri(ccmApiOptions.BaseAddress!);
                    httpClient.SetBearer(ccmApiOptions.ApiKey!);
                })
            .AddPolicyHandler((serviceProvider, _) => CreateRetryPolicy<ClientCaseApiService>(serviceProvider, ccmApiOptions));
        services.RegisterOptions<CaseDataImportOptions>(configuration);
        services.AddAutoMapper(typeof(MainProfile));
        services.AddCosmosDbServices(configuration);
        services.AddTransient<ICaseDataImportService, CaseDataImportService>();
        services.AddTransient<IIrisDataImportService, IrisDataImportService>();

        var irisApiOptions = services.RegisterOptions<IrisApiOptions>(configuration);
        services
            .AddHttpClient<IIrisApiService, IrisApiService>(
                httpClient =>
                {
                    httpClient.BaseAddress = new Uri(irisApiOptions.BaseUrl);
                    httpClient.DefaultRequestHeaders.Add("authorization-caseintegration-key", irisApiOptions.ApiKey);
                })
            .AddPolicyHandler((serviceProvider, _) => CreateRetryPolicy<IrisApiService>(serviceProvider, irisApiOptions));
        services.AddScoped<IIrisIntegrationService, IrisIntegrationService>();
    }

    public static void RegisterWorkflowSurveyService(this IServiceCollection services, IConfiguration configuration)
    {
        services.RegisterOptions<CosmosChangeFeedOptions>(configuration);
        services.RegisterOptions<WorkflowSurveyOptions>(configuration);
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IWorkflowSurveyService, WorkflowSurveyService>();
        services.AddMailDistributionService(configuration, "ServiceBusOptions:ConnectionString");
    }

    public static void RegisterStaffingApiService(this IServiceCollection services, IConfiguration configuration)
    {
        var apiOptions = services.RegisterOptions<StaffingApiOptions>(configuration);
        services
            .AddHttpClient<IStaffingApiService, StaffingApiService>(
                (httpClient, sp) =>
                {
                    httpClient.BaseAddress = new Uri(apiOptions.BaseAddress!);
                    httpClient.SetBearer(apiOptions.ApiKey!);
                    httpClient.Timeout = apiOptions.HttpTimeout;
                    var logger = sp.GetRequiredService<ILogger<StaffingApiService>>();
                    return new StaffingApiService(httpClient, logger, apiOptions.ResourceAllocationRequestChunkSize);
                })
            .AddPolicyHandler((serviceProvider, _) => CreateRetryPolicy<StaffingApiService>(serviceProvider, apiOptions));
    }

    public static void RegisterCaseSearchabilityService(this IServiceCollection services, IConfiguration configuration)
    {
        services.RegisterOptions<CaseSearchabilityOptions>(configuration);
        services.AddScoped<ICaseSearchabilityService, CaseSearchabilityService>();
    }

    public static void RegisterDeletePerformanceTestCasesService(this IServiceCollection services)
    {
        services.AddScoped<IPerformanceTestCaseService, PerformanceTestCaseService>();
    }

    public static void RegisterCaseService(this IServiceCollection services)
    {
        services.AddScoped<ICaseService, CaseService>();
    }

    public static void RegisterCoveoRefreshService(this IServiceCollection services, IConfiguration configuration)
    {
        services.RegisterOptions<CoveoApiOptions>(configuration);
        services.AddHttpClient<ICoveoRefreshService, CoveoRefreshService>();
    }

    public static void RegisterCoveoAuthService(this IServiceCollection services, IConfiguration configuration)
    {
        services.RegisterOptions<CoveoApiOptions>(configuration);
        services.AddHttpClient<ICoveoAuthService, CoveoAuthService>();
    }

    public static string ToCamelCase(this string name) => JsonNamingPolicy.CamelCase.ConvertName(name);

    private static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy<TService>(
        IServiceProvider serviceProvider,
        IRetryPolicyOptions retryPolicyOptions)
        => Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                retryPolicyOptions.MaxRetryCount,
                r => TimeSpan.FromMilliseconds(Math.Pow(2, r) * retryPolicyOptions.RetryBaseBackoffMs),
                OnRetry<TService>(serviceProvider));

    private static Action<DelegateResult<HttpResponseMessage>, TimeSpan, int, Context> OnRetry<TService>(IServiceProvider services)
        => (dr, ts, rc, ctx) =>
        {
            var logger = services.GetService<ILogger<TService>>();
            logger?.LogInformation(
                "Retry calling: {serviceName}, attempt: {attemptNumber}, after: {attemptDelay}ms. Status: {statusCode}. Exception: {exception}",
                typeof(TService).Name,
                rc,
                ts,
                dr.Result.StatusCode,
                dr.Exception);
        };
}