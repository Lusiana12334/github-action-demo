using System.Net;
using System.Text;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PEXC.Common.Options;
using Polly;

namespace PEXC.Case.DataAccess.CosmosDB.Infrastructure;

public static class Extensions
{
    public static void UseCosmosDb(this IServiceProvider services)
    {
        services.GetRequiredService<IDbInitializer>().EnsureIsCreated().GetAwaiter().GetResult();
    }

    public static void AddCosmosDbServices(this IServiceCollection services, IConfiguration configuration)
    {
        var cosmosOptions = services.RegisterOptions<CosmosOptions>(configuration);

        services.AddSingleton(_ => new CosmosClient(cosmosOptions.ConnectionString,
            new CosmosClientOptions
            {
                MaxRetryAttemptsOnRateLimitedRequests = int.MaxValue,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromHours(24),
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
                    IgnoreNullValues = true
                }
            }
        ));

        services.AddSingleton<IAsyncPolicy>(_ =>
        {
            return Policy
                .Handle<CosmosException>(e => e.StatusCode != HttpStatusCode.NotFound)
                .WaitAndRetryAsync(
                    cosmosOptions.MaxRetryCount,
                    r => TimeSpan.FromMilliseconds(Math.Pow(2, r) * cosmosOptions.RetryBaseBackoffMs),
                    OnRetryHandler);
        });

        services.AddScoped<ICaseRepository, CosmosCaseRepository>();
        services.AddScoped<ISingleCaseRepository, CachedSingleCaseRepository>();
        services.AddScoped(typeof(IDataImportStateRepository<>), typeof(CosmosDataImportStateRepository<>));

        services.AddTransient<Func<string, ICosmosDbRepository>>((ctx) =>
        {
            var cosmosClient = ctx.GetRequiredService<CosmosClient>();
            var asyncPolicy = ctx.GetRequiredService<IAsyncPolicy>();
            var logger = ctx.GetRequiredService<ILogger<CosmosDbRepository>>();
            return container => new CosmosDbRepository(
                cosmosClient,
                asyncPolicy,
                cosmosOptions.Database,
                container,
                cosmosOptions.PopulateIndexMetrics,
                logger);
        });

        services.AddTransient(ctx =>
        {
            var cosmosDbRepositoryFactory = ctx.GetRequiredService<Func<string, ICosmosDbRepository>>();
            return cosmosDbRepositoryFactory(cosmosOptions.Container);
        });

        services.AddTransient<IDbInitializer, DbInitializer>();
    }

    public static void AddCosmosDbPerformanceTestServices(this IServiceCollection services)
    {
        services.AddScoped<IPerformanceTestCaseRepository, PerformanceTestCaseRepository>();
    }

    private static void OnRetryHandler(Exception exception, TimeSpan timeSpan, int retryCount, Context context)
    {
        var logger = (ILogger)context["logger"];
        logger.LogInformation("retry calling: {operationName}, attempt: {attemptNumber}, after: {attemptDelay}ms. Exception: {exception}",
            context.OperationKey, retryCount, timeSpan, exception);
    }

    public static string? ToBase64(this string? input)
        => input == null ? null : Convert.ToBase64String(Encoding.ASCII.GetBytes(input));

    public static string? FromBase64(this string? input)
        => input == null ? null : Encoding.ASCII.GetString(Convert.FromBase64String(input));
}