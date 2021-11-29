using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Garath.SensorApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class GoveeController : ControllerBase
{
    private readonly PgSensorDataProvider _dataProvider;

    public GoveeController(PgSensorDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    [HttpGet]
    [EnableCors("ApiRelaxed")]
    public IAsyncEnumerable<SensorData> Get(CancellationToken cancellationToken)
    {
        return _dataProvider.Get(cancellationToken);
    }

    [HttpPost]
    public async Task Post(IEnumerable<SensorData> data, CancellationToken cancellationToken)
    {
        await _dataProvider.AddRange(data, cancellationToken);
    }
}
