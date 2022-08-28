using Garath.Govee.SiteApp.Shared;
using Microsoft.AspNetCore.SignalR;

namespace Garath.SensorApi;

public class SensorHub : Hub<ISensorHubClient>, ISensorHubServer
{
    private readonly PgSensorDataProvider _dataProvider;

    public SensorHub(PgSensorDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task SendData(IEnumerable<SensorData> data, CancellationToken cancellationToken = default)
    {
        await Clients.All.ReceiveData(data, cancellationToken);
    }

    public async Task SendFirstData()
    {
        List<SensorData> data = await _dataProvider.Get(CancellationToken.None).ToListAsync();

        await Clients.Caller.ReceiveData(data);
    }
}
