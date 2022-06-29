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

    [Fact]
    public async Task TestScheduler()
    {
        var u = await Observable.Return(1, TaskPoolScheduler.Default).Repeat().Take(1);
    }

    [Fact]
    public async Task TestSelectMany()
    {
        var random = new Random();
        async Task<string> Transform(int id)
        {
            Logger.LogTrace("start task for {id}", id);
            await Task.Delay(random.Next(200, 1000));
            Logger.LogTrace("end task for {id}", id);
            return id.ToString();
        }

        IObservable<string> TransfromObservable (int id)
            => Observable.FromAsync(() => Transform(id));

        var end = await CreateSample(10).SelectMany(v => TransfromObservable(v))
            .Do(str => Logger.LogInformation(str));
    }

    [Fact]
    public async Task TestSelectManyOrdered()
    {
        var random = new Random();
        async Task<string> Transform(int id)
        {
            ///
            await Task.Delay(random.Next(200, 1000));
            return id.ToString();
        }

        IObservable<string> TransfromObservable(int id)
            => Observable.FromAsync(() => Transform(id));

        await Observable.Range(1, 10)
            .SelectManyOrdered(TransfromObservable, 50)
            .Do(e => Logger.LogWarning("event output {id}", e));
    }

    [Fact]
    public async Task TestLatestFrom()
    {
        var leftEvents =
            GenerateDelayedEvents(10)
                .Do(i => Logger.LogInformation("left {i}", i));

        var rightEvents =
            GenerateDelayedEvents(10)
                .Do(i => Logger.LogInformation("right {i}", i));

        await leftEvents.WithLatestFrom(rightEvents)
            .Do(v => Logger.LogWarning("left: {left} right : {right}", v.First, v.Second));
    }

    [Fact]
    public async Task TestCombineLatest()
    {
        var leftEvents =
            GenerateDelayedEvents(10)
                .Do(i => Logger.LogInformation("left {i}", i));

        var rightEvents =
            GenerateDelayedEvents(10)
                .Do(i => Logger.LogInformation("right {i}", i));

        await leftEvents.CombineLatest(rightEvents)
            .Do(v => Logger.LogWarning("left: {left} right : {right}", v.First, v.Second));
    }

    [Fact]
    public async Task TestJoin()
    {
        var left = CreateSample(10).Select(v => $"l{v}")
            .Publish().RefCount();

        var right = CreateSample(10).Select(v => $"r{v}")
            .Publish().RefCount();

        await left
            .Join(right,
                _ => left,
                _ => left,
                (l, r) => $"{l} {r}")
            .Do(v => Logger.LogInformation(v));
    }

    [Fact]
    public async Task TestExpand()
    {
        IObservable<(int nextPage, int[] data)> queryData(int page)
        {
            int[] getData(int p) =>
                page < 10 ? Enumerable.Range(1, 5).ToArray() : Array.Empty<int>();

            var data = getData(page);

            return data.Any()
                ? Observable.Return((page + 1, Enumerable.Range(1, 5).ToArray()))
                : Observable.Empty<(int, int[])>();
        }

        await queryData(1).Expand(v => queryData(v.nextPage))
            .SelectMany(d => d.data)
            .Do(v => Logger.LogInformation("{v}", v));
    }

    [Fact]
    public async Task TestBuffer()
    {
        var stream = CreateSample(50).Buffer(TimeSpan.FromSeconds(1), 5)
            .Do(l => Logger.LogInformation("{l}", l));
        await stream;
        await stream;
    }

    [Fact]
    public async Task TestConcat2()
    {
        var random = new Random();
        async Task<string> Transform(int id)
        {
            ///
            await Task.Delay(random.Next(200, 1000));
            return id.ToString();
        }

        IObservable<string> TransfromObservable(int id)
            => Observable.FromAsync(() => Transform(id));

        var end = await CreateSample(10).Select(v => TransfromObservable(v)).Concat()
            .Do(str => Logger.LogInformation(str));
    }

    [Fact]
    public async Task TestConcat()
    {
        var random = new Random();
        async Task<string> Transform(int id)
        {
            ///
            await Task.Delay(random.Next(200, 1000));
            return id.ToString();
        }

        IObservable<string> TransfromObservable(int id)
            => Observable.FromAsync(() => Transform(id));

        var end = await CreateSample(10).Select(v => TransfromObservable(v)).Concat()
            .Do(str => Logger.LogInformation(str));
    }

    [Fact]
    public async Task TestSubject()
    {

        var hot = GenerateDelayedEvents(10)
            .Do(v => Logger.LogInformation("{v}", v))
            .Publish().RefCount();

        await hot.Zip(hot, (left, right) => (left, right))
            .Do(v => Logger.LogInformation("{v}", v));
    }

    [Fact]
    public async Task TestSubject_X()
    {

        var connectable = GenerateDelayedEvents(10).Publish();

        await connectable
            .Do(_ => { });

        connectable.Connect();

    }

    [Fact]
    public async Task TestMultipleConsumers()
    {
        var source = GenerateDelayedEvents(5)
            .Do(e => Logger.LogTrace("received {id}", e))
            .Publish().RefCount();


        var firstStream = source.Select(e => $"fst {e}")
            .Do(data => Logger.LogWarning("output {data}", data));
        var secondStream = source.Select(e => $"snd {e}")
            .Do(data => Logger.LogWarning("output {data}", data));

        await Task.WhenAll(firstStream.ToTask(), secondStream.ToTask());

        await Task.WhenAll(firstStream.DefaultIfEmpty().ToTask(), secondStream.DefaultIfEmpty().ToTask());
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

    private static IObservable<int> GenerateDelayedEvents(int nb)
    {
        var random = new Random();
        return Observable.Range(1, nb)
            .Select(i => Observable.Return(i).Delay(TimeSpan.FromMilliseconds(random.Next(500, 2000)))).Concat();
    }
}