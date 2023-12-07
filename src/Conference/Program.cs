using Pulumi;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Storage.Inputs;
using System.Collections.Generic;
using System.Threading.Tasks;
using Conference;
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
    
    var appServicePlan = new AppServicePlan("appServicePlan", new AppServicePlanArgs
    {
        Kind = "Linux",
        Reserved = true,
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
    
    var appService = new WebApp(name: "appservice",
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
                },
                WebSocketsEnabled = true,
                LinuxFxVersion = "DOTNETCORE|8.0"
            },
            Reserved = true,
            HttpsOnly = true
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
    
    return new Dictionary<string, object?>
    {
        ["primaryStorageKey"] = primaryStorageKey
    };
});