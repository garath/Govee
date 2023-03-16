using Garath.Govee.SiteApp.Shared;
using MediatR;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Garath.SensorApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class GoveeController : ControllerBase
{
    private readonly PgSensorDataProvider _dataProvider;
    private readonly IMediator _mediator;

    public GoveeController(PgSensorDataProvider dataProvider, IMediator mediator)
    {
        _dataProvider = dataProvider;
        _mediator = mediator;
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
        await _mediator.Publish(new SensorDataNotification(data), cancellationToken);
    }
}
