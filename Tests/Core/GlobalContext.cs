using Microsoft.Extensions.DependencyInjection;
using RxLibrary;

namespace Tests.Core;

public class GlobalContext
{
    public GlobalContext()
    {
        var collection = new ServiceCollection();
        collection.AddGrpcClient<GrpcService.Protos.MyService.MyServiceClient>(o =>
        {
            o.Address = new Uri("http://localhost:5000");
        });
        collection.AddTransient<GrpcClient>();
        Provider = collection.BuildServiceProvider();
    }
    
    public IServiceProvider Provider { get; }
}