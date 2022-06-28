using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using GrpcService.Protos;

namespace GrpcService.Services
{
    public class MyService : Protos.MyService.MyServiceBase
    {
        private readonly ILogger<MyService> _logger;

        private static readonly Empty EmptyResponse = new ();
        private readonly Random _random;

        public MyService(ILogger<MyService> logger)
        {
            _logger = logger;
            _random = new Random();
        }

        public override async Task<Event> ReadEvent(GetEventRequest request, ServerCallContext context)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(_random.Next(1, 200)));
            var @event = new Event
            {
                Id = request.Id,
                Value = $"value {request.Id}"
            };
            _logger.LogInformation("sent event {id} on unary call", @event.Id);
            return @event;
        }

        public override Task<Empty> PushEvent(Event request, ServerCallContext context)
        {
            _logger.LogInformation("received event {id} on unary call", request.Id);
            return Task.FromResult(EmptyResponse);
        }

        public override async Task ReadEvents(GetEventsRequest request, IServerStreamWriter<Event> responseStream, ServerCallContext context)
        {
            var waitTime = TimeSpan.FromMilliseconds(request.DelayMs <= 0 ? 500  : request.DelayMs);
            var nb = 0;
            while (!context.CancellationToken.IsCancellationRequested && request.MaxNbEvents <= 0 || nb < request.MaxNbEvents)
            {
                await Task.Delay(waitTime);
                var id = (++nb);
                var @event = new Event
                {
                    Id = id,
                    Value = $"value {id}"
                };
                _logger.LogInformation("sent event {id} on streaming call", @event.Id);
                await responseStream.WriteAsync(@event);
            }
        }

        public override async Task<Empty> PushEvents(IAsyncStreamReader<Event> requestStream, ServerCallContext context)
        {
            await foreach (var @event in requestStream.ReadAllAsync())
            {
                _logger.LogInformation("received event {id} on streaming call", @event.Id);
            }

            return EmptyResponse;
        }
    }
}