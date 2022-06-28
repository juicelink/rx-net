using RxLibrary;
using WorkerService;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
        services.AddGrpcClient<GrpcService.Protos.MyService.MyServiceClient>(o =>
        {
            o.Address = new Uri("http://localhost:5000");
        });
        services.AddTransient<GrpcClient>();
    })
    .Build();

await host.RunAsync();
