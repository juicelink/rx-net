using System.Reactive;
using RxLibrary;

namespace WorkerService;

public class Worker : IHostedService
{
private readonly ILogger<Worker> _logger;
private readonly GrpcClient _client;

public Worker(
    ILogger<Worker> logger, GrpcClient client)
{
    _logger = logger;
    _client = client;
}

public Task StartAsync(CancellationToken cancellationToken)
{
    return Task.CompletedTask;
}

public Task StopAsync(CancellationToken cancellationToken)
{
    return Task.CompletedTask;
}
}