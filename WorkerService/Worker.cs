using System.Reactive;
using RxLibrary;

namespace WorkerService;

public class Worker : IHostedService
{
private readonly ILogger<Worker> _logger;
private readonly GrpcClient _client;
private readonly IApplicationLifetime _lifeTime;

public Worker(
    ILogger<Worker> logger, GrpcClient client, IApplicationLifetime lifeTime)
{
    _logger = logger;
    _client = client;
    _lifeTime = lifeTime;
}

public Task StartAsync(CancellationToken cancellationToken)
{
    _client.GetObservableEvents().Subscribe(
        e => _logger.LogInformation("worker processed event {id}", e.Id),
        ex =>
        {
            _logger.LogError(ex, "worker stream crashed");
            _lifeTime.StopApplication();
        },
        () => _logger.LogInformation("worker done"),
        cancellationToken
    );
    return Task.CompletedTask;
}

public Task StopAsync(CancellationToken cancellationToken)
{
    return Task.CompletedTask;
}
}