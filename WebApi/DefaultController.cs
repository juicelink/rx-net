using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using GrpcService.Protos;
using Microsoft.AspNetCore.Mvc;
using RxLibrary;

namespace WebApi;

public class DefaultController : ControllerBase
{
    private readonly GrpcClient _client;

    public DefaultController(GrpcClient client)
    {
        _client = client;
    }

    [HttpGet("events/take/{nb}")]
    public Task<Event[]> GetEvents(int nb) => Task.FromResult(Array.Empty<Event>());

    [HttpPost("events/push")]
    public Task PushEvents([FromBody] int[] ids)
    {
        return Task.CompletedTask;
    }
}