using HashtagChris.DotNetBlueZ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Garath.Govee
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            using IHost host = Host.CreateDefaultBuilder(args)
                .UseSystemd()
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton(services => BlueZManager.GetAdapterAsync("hci0").GetAwaiter().GetResult());
                    services.AddHostedService<GoveeMonitor>();
                    services.AddHostedService<PgSensorDataWriter>();

                    Channel<SensorData> channel = Channel.CreateUnbounded<SensorData>(
                        new UnboundedChannelOptions()
                        {
                            SingleReader = true,
                            SingleWriter = false
                        });
                    services.AddSingleton(channel.Reader);
                    services.AddSingleton(channel.Writer);

                    services.Configure<GoveeMonitorConfiguration>(context.Configuration.GetSection(nameof(GoveeMonitorConfiguration)));
                    services.Configure<PgSensorDataWriterConfiguration>(
                        config => config.ConnectionString = context.Configuration.GetConnectionString("SensorDataConnectionString"));
                })
                .Build();

            await host.RunAsync();
        }
    }
}
