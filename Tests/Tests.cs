using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RxLibrary;
using Tests.Core;
using Xunit;
using Xunit.Abstractions;

namespace Tests;

public class Tests : BaseTests
{
    private readonly GrpcClient _grpcClient;

    public Tests(GlobalContext context, ITestOutputHelper output) : base(context, output)
    {
        _grpcClient = Provider.GetRequiredService<GrpcClient>().SetLogger(Logger);
    }

    [Fact]
    public async Task Test()
    {
        await CreateSample(10)
            .Do(v => Logger.LogInformation(v.ToString()));
    }

    private IObservable<int> CreateSample(int? nb = null)
    {
        var random = new Random();
        return Observable
            .Range(1, nb ?? int.MaxValue)
            .Select(
                v => Observable.Return(v)
                    .Delay(TimeSpan.FromMilliseconds(random.Next(100, 500))))
            .Concat();
    }
}