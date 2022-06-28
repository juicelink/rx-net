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

    }
}