using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using GrpcService.Protos;
using Microsoft.AspNetCore.Mvc;
using RxLibrary;

namespace WebApi;

public class DefaultController : ControllerBase
{
    private readonly GrpcClient _client;
    private readonly TemperatureService _service;
    private readonly ILogger<DefaultController> _logger;

    public DefaultController(GrpcClient client, TemperatureService service, ILogger<DefaultController> logger)
    {
        _client = client;
        _service = service;
        _logger = logger;
    }

    [HttpGet("events/take/{nb}")]
    public Task<Event[]> GetEvents(int nb) => _client.GetObservableEvents(10).ToArray().ToTask();

    [HttpGet("events/takeWithTemp/{nb}")]
    public Task<EventWithTemp[]> GetEventsWithTemp(int nb) => _client.GetObservableEvents(nb, delayMs: 100)
        .WithLatestFrom(
            _service.GetTemperature(), (e, t) => new EventWithTemp(e, t))
        .ToArray().ToTask();

    [HttpPost("events/push")]
    public Task PushEvents([FromBody] int[] ids) =>
        _client.PushEvents(ids.ToObservable().Select(id => new Event
        {
            Id = id,
            Value = $"value {id}"
        }))
            .Do(_ => _logger.LogInformation("notify push done"))
            .ToTask();

    public record EventWithTemp(Event Event, int Temp)
    {
    }
}