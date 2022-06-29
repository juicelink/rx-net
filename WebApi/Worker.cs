using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using RxLibrary;

namespace WebApi;

public class Worker : IHostedService
{
private readonly ILogger<Worker> _logger;
private readonly TemperatureService _service;

public Worker(
    ILogger<Worker> logger, TemperatureService service)
{
    _logger = logger;
    _service = service;
}

public Task StartAsync(CancellationToken cancellationToken)
{
    var random = new Random();
    var tempStream = Observable.Return(Unit.Default)
        .Repeat()
        .Select(_ => Observable.Return(random.Next(-10, 40))
            .Delay(TimeSpan.FromMilliseconds(random.Next(100, 500))))
        .Concat();

    tempStream
        .SubscribeOn(NewThreadScheduler.Default)
        .Subscribe(v => _service.UpdateTemperature(v));

    return Task.CompletedTask;
}

public Task StopAsync(CancellationToken cancellationToken)
{
    return Task.CompletedTask;
}
}