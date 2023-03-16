using Garath.Govee.SiteApp.Shared;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace Garath.SensorApi;

public class SensorDataNotification : INotification
{
    public IEnumerable<SensorData> Data { get; }

    public SensorDataNotification(IEnumerable<SensorData> data)
    {
        Data = data;
    }
}

public class SensorDatabaseHandler : INotificationHandler<SensorDataNotification>
{
    private readonly PgSensorDataProvider _dataProvider;

    public SensorDatabaseHandler(PgSensorDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task Handle(SensorDataNotification notification, CancellationToken cancellationToken)
    {
        await _dataProvider.AddRange(notification.Data, cancellationToken);
    }
}

public class SensorHubHandler : INotificationHandler<SensorDataNotification>
{
    private readonly IHubContext<SensorHub, ISensorHubClient> _dataHub;

    public SensorHubHandler(IHubContext<SensorHub, ISensorHubClient> dataHub)
    {
        _dataHub = dataHub;
    }

    public async Task Handle(SensorDataNotification notification, CancellationToken cancellationToken)
    {
        await _dataHub.Clients.All.ReceiveData(notification.Data, cancellationToken);
    }
}
