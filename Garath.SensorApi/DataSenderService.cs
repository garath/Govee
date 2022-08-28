using Garath.Govee.SiteApp.Shared;
using Microsoft.AspNetCore.SignalR;

namespace Garath.SensorApi;

public sealed class DataSenderService : BackgroundService
{
    private readonly ILogger<DataSenderService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public DataSenderService(ILogger<DataSenderService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            using IServiceScope scope = _serviceProvider.CreateScope();

            PgSensorDataProvider sensorDataProvider = scope.ServiceProvider.GetRequiredService<PgSensorDataProvider>();
            IHubContext<SensorHub, ISensorHubClient> dataHub = scope.ServiceProvider.GetRequiredService<IHubContext<SensorHub, ISensorHubClient>>();

            IEnumerable<SensorData> data = await sensorDataProvider
                .Get(stoppingToken)
                .ToListAsync(stoppingToken);
            
            await dataHub.Clients.All.ReceiveData(data, stoppingToken);
        }
    }
}
