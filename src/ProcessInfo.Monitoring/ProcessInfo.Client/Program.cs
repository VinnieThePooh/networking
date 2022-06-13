using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProcessInfo.Client.Settings;

namespace ProcessInfo.Client;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var client = new ProcessInfoClient(GetNetworkingSettings());
        Console.CancelKeyPress += async (sender, eventArgs) => { await client.StopSendingInfo(); };

        try
        {
            await client.StartSendingInfo();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Environment.Exit(-1);
        }
    }

    private static NetworkingSettings GetNetworkingSettings()
    {
        var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        var services = new ServiceCollection();
        services.Configure<NetworkingSettings>(config.GetSection("Networking"));

        return services.BuildServiceProvider().GetRequiredService<IOptions<NetworkingSettings>>().Value;
    }
}