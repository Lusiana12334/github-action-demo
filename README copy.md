# tsg-peg-pexc-case
PEXC Case Microservice project

## Nuget Feed 
To attach private NuGet feed from GitHub
1. Close Visual Studio
2. Go to your GitHub Profile and generate Personal Access Token (PAT) with read:packages access right
https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/creating-a-personal-access-token
4. Create nuget.config file in folder above your repository with the following content:
```
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="github_bain" value="https://nuget.pkg.github.com/Bain/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <github_bain>
      <add key="Username" value="USERNAME" />
      <add key="ClearTextPassword" value="TOKEN" />
    </github_bain>
  </packageSourceCredentials>
</configuration>
```
where `USERNAME` is your e-mail address and `TOKEN` is the Personal Access Token generated in step 2

## Local Authentication and Authorization

For local devepolment we use "TSG-PEXC-local-Backend" (Azure AD -> app registration -> TSG-PEXC-local-Backend)
- This application is configured to authenticate through web based implicit flow
- To define more scopes go to "Expose an API" section in the app registration
- If needed more app roles to test go "App roles" section in the app registration
- To asign user or group to the role go to Azure AD -> Enterprise applications -> TSG-PEXC-local-Backend -> Users and Groups section

With following configuration, after running Swagger (https://localhost:5001/swagger/index.html) go to Authorization section and authorize in the context of banilab user 

```
  "AzureActiveDirectoryOptions": {
    "Instance": "https://login.microsoftonline.com",
    "ClientId": "eef83332-dcea-4306-b834-3c52b49f10b1",
    "TenantId": "f0488bf5-fdc1-419b-aecf-f8e9e04c82e7",
    "Scope": "Case.UserAccess"
  }
```


## Cosmos Emulator
To work locally with case sever you need to install azure cosmos db emmulator

1. open https://learn.microsoft.com/en-us/azure/cosmos-db/local-emulator?tabs=ssl-netstd21 and follow the instruction for SQL API
2. Run Emulator and open web dashboard 
2. Check if connection string in appsettings.Development.json is the same as in the emulator 

## Secrets 

Before running project locally please add following file to API project secrets 
(rclick on API project -> Manage User Secrets -> paste json)

```
{
  "CoveoApiOptions": {
    "CaseSearchApiKey" : "ask for it",
    "CaseManagementApiKey" : "ask for it" 
  },
  "AzureActiveDirectoryOptions": {
    "ClientId": "eef83332-dcea-4306-b834-3c52b49f10b1"
  },
  "HashiCorpVaultOptions": {
    "IsActive": false
  },
  "CosmosOptions": {
    "CreateDatabase": true,
    "SeedDatabase": true,
    "BlobConnectionString": "ask for it"
  }
}
```

## Azure Functions 

Before running project locally please add local.settings.json file and Copy content from local.settings.template.json file. 
Then enter the correct ClientId, TenantId, ClientSecret.
You also have to enter the correct connection strings for CosmosDb and ServiceBus.

```
"AzureActiveDirectoryOptions:ClientId": "ask for it",
"AzureActiveDirectoryOptions:TenantId": "ask for it",
"AzureActiveDirectoryOptions:ClientSecret": "ask for it",

"ServiceBusOptions:ConnectionString": "ask for it",
"CosmosOptions:ConnectionString": "ask for it",
```

## Package versions

You may ecounter problems with updating nuget package versions 
Some packages like
    System.Text.RegularExpressions
    System.Drawing.Common
    System.Text.Encodings.Web

Has been referenced directly on the project level, even when they are dependency of another library they are referenced with version where SNYK scan detected vulnerabilities.
Parent package does not have vesrion with upadte - so uptates has been done manually by team
https://bainco.atlassian.net/browse/P20-1193

