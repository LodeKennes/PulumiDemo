﻿using Pulumi;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Storage.Inputs;
using System.Collections.Generic;
using Pulumi.AzureNative.Insights;
using Pulumi.AzureNative.Web;
using Pulumi.AzureNative.Web.Inputs;
using Pulumi.Random;
using Kind = Pulumi.AzureNative.Storage.Kind;
using Sql = Pulumi.AzureNative.Sql;

return await Pulumi.Deployment.RunAsync(() =>
{
    // Create an Azure Resource Group
    var resourceGroup = new ResourceGroup("resourceGroup");

    // Create an Azure resource (Storage Account)
    var storageAccount = new StorageAccount("sa", new StorageAccountArgs
    {
        ResourceGroupName = resourceGroup.Name,
        Sku = new SkuArgs
        {
            Name = SkuName.Standard_LRS
        },
        Kind = Kind.StorageV2
    });

    var storageAccountKeys = ListStorageAccountKeys.Invoke(new ListStorageAccountKeysInvokeArgs
    {
        ResourceGroupName = resourceGroup.Name,
        AccountName = storageAccount.Name
    });

    var primaryStorageKey = Output.Tuple(storageAccount.Name, storageAccountKeys).Apply(items =>
    {
        var accountName = items.Item1;
        var firstKey = items.Item2.Keys[0].Value;
        return
            $"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={firstKey};EndpointSuffix=core.windows.net";
    });

    var appServicePlan = new AppServicePlan("appServicePlan", new AppServicePlanArgs
    {
        Kind = "app",
        Name = "myexactname",
        ResourceGroupName = resourceGroup.Name,
        Sku = new SkuDescriptionArgs
        {
            Capacity = 1,
            Family = "B",
            Name = "B1",
            Size = "B1",
            Tier = "Basic"
        }
    });
    
    var appInsights = new Component("appinsights", new ComponentArgs
    {
        ApplicationType = ApplicationType.Web,
        Kind = "web",
        ResourceGroupName = resourceGroup.Name
    });

    var username = new Pulumi.Random.RandomString("db-username", new RandomStringArgs
    {
            Special = false,
            Length = 20
    });
    
    var password = new Pulumi.Random.RandomPassword("db-password", new RandomPasswordArgs
    {
        Special = false,
        Length = 20
    });

    var sql = new Sql.Server("sqlserver", new Sql.ServerArgs
    {
        AdministratorLogin = username.Result,
        AdministratorLoginPassword = password.Result,
        Location = resourceGroup.Location,
        ResourceGroupName = resourceGroup.Name,
        PublicNetworkAccess = Sql.ServerPublicNetworkAccess.Enabled
    });

    var database = new Sql.Database("database", new Sql.DatabaseArgs
    {
        DatabaseName = "database",
        ResourceGroupName = resourceGroup.Name,
        Location = sql.Location,
        ServerName = sql.Name,
        Sku = new Sql.Inputs.SkuArgs
        {
            Capacity = 10,
            Tier = "Standard",
            Name = "Standard"
        }
    });
    
    var sqlConnectionString = Output.Tuple(username.Result, password.Result, sql.Name)
        .Apply(result
            => $"Server=tcp:{result.Item3}.database.windows.net,1433;Initial Catalog=axxesapp;Persist Security Info=False;User ID={result.Item1};Password={result.Item2};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");
    
    var appService = new WebApp(name: "apeservice",
        new WebAppArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Kind = "App",
            ServerFarmId = appServicePlan.Id,
            SiteConfig = new SiteConfigArgs
            {
                AlwaysOn = true,
                AppSettings = new List<NameValuePairArgs>
                {
                    new()
                    {
                        Name = "storage",
                        Value = primaryStorageKey
                    },
                    new()
                    {
                        Name = "APPINSIGHTS_INSTRUMENTATIONKEY",
                        Value = appInsights.InstrumentationKey
                    },
                    new()
                    {
                        Name = "DATABASE_CONNECTION",
                        Value = sqlConnectionString
                    }
                },
                WebSocketsEnabled = true,
                NetFrameworkVersion = "v6.0"
            },
            Reserved = true,
            HttpsOnly = true
        });

    // Export the primary key of the Storage Account
    return new Dictionary<string, object?>
    {
        ["primaryStorageKey"] = primaryStorageKey
    };
});