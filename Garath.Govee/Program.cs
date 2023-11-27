using Garath.Govee;
using HashtagChris.DotNetBlueZ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Threading.Channels;

Console.WriteLine("Hello World!");

Activity.DefaultIdFormat = ActivityIdFormat.W3C;

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

        services.AddHttpClient<SensorApiClient>(client =>
        {
            string runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            string platform = System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier;
            System.Net.Http.Headers.ProductInfoHeaderValue runtimeHeader = new($"({runtime}; {platform})");

            string productName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "Garath.Govee";
            string? productVersion = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
            System.Net.Http.Headers.ProductInfoHeaderValue productHeader = new(productName, productVersion);

            client.DefaultRequestHeaders.UserAgent.Add(runtimeHeader);
            client.DefaultRequestHeaders.UserAgent.Add(productHeader);
            client.BaseAddress = new Uri(context.Configuration["SensorApiBaseAddress"]!);
        });

        OpenTelemetryBuilder oTelBuilder = services.AddOpenTelemetry()
            .WithTracing(builder => 
            {
                builder.AddHttpClientInstrumentation();
                builder.AddConsoleExporter();

                if (context.HostingEnvironment.IsDevelopment())
                {
                    builder.AddConsoleExporter();
                }
                else
                {
                    builder.AddOtlpExporter();
                }
            })
            .WithMetrics(builder => 
            {
                builder.AddHttpClientInstrumentation();
                
                if (context.HostingEnvironment.IsDevelopment())
                {
                    builder.AddConsoleExporter();
                }
                else
                {
                    builder.AddOtlpExporter();
                }
            })
            .ConfigureResource(resource => {
                resource.AddService(
                    serviceName: context.HostingEnvironment.ApplicationName, 
                    serviceVersion: System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString());
                resource.AddTelemetrySdk();
            });
    })
    .Build();

await host.RunAsync();
