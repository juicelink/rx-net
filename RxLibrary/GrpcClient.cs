using System.Reactive;
using System.Reactive.Linq;
using Grpc.Core;
using GrpcService.Protos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace RxLibrary;

public class GrpcClient
{
    private readonly MyService.MyServiceClient _client;
    private ILogger _logger;

    public GrpcClient(MyService.MyServiceClient client, ILogger<GrpcClient> logger = null)
    {
        _client = client;
        _logger = (ILogger)logger ?? NullLogger.Instance;
    }

    public async Task<Event> GetEvent(int id)
    {
        _logger.LogTrace("start reading event {id}", id);
        var result = await _client.ReadEventAsync(new GetEventRequest { Id = id }).ResponseAsync;
        _logger.LogTrace("return event {id}", id);
        return result;
    }

    public async IAsyncEnumerable<Event> GetAsyncEvents(int? maxNbEvents = null, int? delayMs = null)
    {
        _logger.LogTrace("start reading async events");
        var result = _client.ReadEvents(new GetEventsRequest { MaxNbEvents = maxNbEvents ?? 0, DelayMs = delayMs ?? 0});

        await foreach (var @event in result.ResponseStream.ReadAllAsync())
        {
            yield return @event;
        }
    }

    public IObservable<Event> GetObservableEvents(int? maxNbEvents = null, int? delayMs = null) => null;

    public IObservable<Event> GetObservableEvents(IObservable<int> nbEndedEvents, int? maxNbEvents = null,
        int? delayMs = null) => null;


    public IObservable<Unit> PushEvents(IObservable<Event> events) => null;

    public GrpcClient SetLogger(ILogger logger)
    {
        _logger = logger;
        return this;
    }
}