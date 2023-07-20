using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PEXC.Case.DataAccess.CosmosDB.Infrastructure;
using PEXC.Case.Services.Infrastructure;
using PEXC.Case.Tools.AdvisorsDbProcessor;

var location = Assembly.GetExecutingAssembly().Location;
var appSettingsDirectory = Path.GetDirectoryName(location)!;
var appSettingsPath = Path.Combine(appSettingsDirectory, "appsettings.json");


var configuration = new ConfigurationBuilder()
    .AddJsonFile(appSettingsPath, optional: true, reloadOnChange: false)
    .SetBasePath(Directory.GetCurrentDirectory())
    .Build();

var services = new ServiceCollection();

services.RegisterStaffingApiService(configuration);
services.AddCosmosDbServices(configuration);

services.AddSingleton<Runner>();
services.AddSingleton<DbFacade>();

var sp = services.BuildServiceProvider();

var runner = sp.GetRequiredService<Runner>();

await runner.Run(Console.WriteLine);
