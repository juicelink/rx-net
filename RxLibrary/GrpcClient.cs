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

    public IObservable<Event> GetObservableEvents(int? maxNbEvents = null, int? delayMs = null)
    {
        return Observable.Create<Event>(async (observer, token) =>
        {
            _logger.LogTrace("start reading event stream");

            var stream = _client.ReadEvents(new GetEventsRequest
                { MaxNbEvents = maxNbEvents ?? 0, DelayMs = delayMs ?? 0 }, cancellationToken: token)
                .ResponseStream;
            
            if (token.IsCancellationRequested) return;
            var hasData = await stream.MoveNext();
            while (hasData)
            {
                observer.OnNext(stream.Current);
                _logger.LogTrace("pushed event {id} in stream", stream.Current.Id);
                if (token.IsCancellationRequested) return;
                hasData = await stream.MoveNext();
            }

            observer.OnCompleted();
        });
    }

    
    
    
    public IObservable<Event> GetObservableEvents(IObservable<int> nbEndedEvents, int? maxNbEvents = null,
        int? delayMs = null) => null;


    public IObservable<Unit> PushEvents(IObservable<Event> events) => Observable.Defer(() =>
    {
        var stream = _client.PushEvents().RequestStream;
        return events.Select(e => Observable.FromAsync(() => stream.WriteAsync(e)))
            .Concat()
            .LastOrDefaultAsync()
            .SelectMany(_ => Observable.FromAsync(stream.CompleteAsync));
    });

    public GrpcClient SetLogger(ILogger logger)
    {
        _logger = logger;
        return this;
    }
}