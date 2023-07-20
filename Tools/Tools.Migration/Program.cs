using System.Data.Common;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using PEXC.Case.DataAccess.CosmosDB.Infrastructure;
using PEXC.Case.Tools.Migration;
using PEXC.Common.Taxonomy;
using PEXC.Document.Client.Options;

var environments = new[] { "Poc", "Development", "Uat", "Production" };
var location = Assembly.GetExecutingAssembly().Location;

if (args.Length < 2)
{
    PrintUsage(location);
    return;
}

var environment = args[0];
if (!IsValidEnvironment(environment))
{
    PrintUsage(location);
    return;
}

var filePath = args[1];

if (!File.Exists(filePath))
{
    Console.WriteLine($"File path: {filePath} does not exist!");
    PrintUsage(location);
    return;
}

var appSettingsDirectory = Path.GetDirectoryName(location)!;
var appSettingsPath = Path.Combine(appSettingsDirectory, "appsettings.json");
var appSettingsEnvPath = Path.Combine(appSettingsDirectory, $"appsettings.{environment}.json");

var configBuilder = new ConfigurationBuilder()
    .AddJsonFile(appSettingsPath, optional: true, reloadOnChange: false)
    .AddJsonFile(appSettingsEnvPath, optional: true, reloadOnChange: false)
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>();

var configuration = configBuilder.Build();

VerifyUserIntent();

var migrationManager = new MigrationManager();
await using var stream = new FileStream(filePath, FileMode.Open);

using var reader = new StreamReader(stream);
await migrationManager.StartMigration(reader, configuration);


bool IsValidEnvironment(string env)
    => environments.Any(e => string.Equals(e, env, StringComparison.OrdinalIgnoreCase));


void PrintUsage(string appLocation)
{
    Console.WriteLine("Missing required arguments. Usage:");
    Console.WriteLine($"{Path.GetFileName(appLocation)} environment migration_file");
    Console.WriteLine($"Environments: [{string.Join(", ", environments)}]");
    Console.WriteLine("Missing file path. Press any key to exit...");
    Console.ReadKey();
}

void VerifyUserIntent()
{
    var options = new MigrationOptions();
    configuration.GetSection(nameof(MigrationOptions)).Bind(options);
    var taxonomySettings = new TaxonomySettings();
    configuration.GetSection(taxonomySettings.SectionName).Bind(taxonomySettings);
    var documentOptions = new DocumentServiceOptions();
    configuration.GetSection(documentOptions.SectionName).Bind(documentOptions);
    var cosmosOptions = new CosmosOptions();
    configuration.GetSection(nameof(CosmosOptions)).Bind(cosmosOptions);
    DbConnectionStringBuilder builder = new DbConnectionStringBuilder { ConnectionString = cosmosOptions.ConnectionString };
    string? endpoint = builder["AccountEndpoint"]?.ToString();

    string documentMessage = options.CreateDirectories
        ? $"Creating document enabled, document endpoint: {documentOptions.BaseUrl}, \r\n"
        : "Creating document disabled\r\n";

    Console.WriteLine("Running application with:\r\n" +
                      $"environment {environment},\r\n" +
                      $"file: '{filePath}', \r\n" +
                      $"CosmosDb endpoint: {endpoint}, \r\n" +
                      $"Taxonomy endpoint: {taxonomySettings.BaseUrl}, \r\n" +
                      $"randomize data: {options.RandomizeData}, \r\n" +
                      documentMessage +
                      $"modify survey status: {options.RestoreSurveyOpeningToNew}\r\n"
                      );
    Console.Write("Are settings correct? [Y/n] ");
    var key = Console.ReadLine();

    if (key != null && !string.Equals(key.Trim(), "y", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Stopping app...");
        Environment.Exit(0);
    }
}