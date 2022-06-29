using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using GrpcService.Protos;
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

    [Fact]
    public async Task TestGetGrpcEvents()
    {

        try
        {
            var result = await _grpcClient.GetObservableEvents(50).Take(2)
                .Do(
                    _ => { },
                    ex => Logger.LogError(ex.Message)
                )
                .Catch<Event, Exception>(ex =>
                {
                    Logger.LogError(ex.Message);
                    return Observable.Empty<Event>();
                }).DefaultIfEmpty();
        }

        catch(Exception ex)
        {
            Logger.LogError( "global try catch {msg}", ex.Message);
        }
    }

    [Fact]
    public async Task TestGetGrpcEventsContinuousStream()
    {

        await _grpcClient.GetObservableEvents().RetryAfterDelay(TimeSpan.FromSeconds(5))
            .Do(_ =>
            {

            });
    }

    [Fact]
    public async Task TestCatch()
    {
        IObservable<int> createStream()
        {
            return Observable.Defer(() =>
            {
                throw new Exception("unhandled");
                var stream = Observable.Range(1, 3)
                    .Select(v => Observable.Return(v).Delay(TimeSpan.FromMilliseconds(10))).Concat()
                    .Concat(Observable.Throw<int>(new Exception("error")));
                return stream;
            });
        }

        

        var result = await createStream().Catch<int, Exception>(ex =>
        {
            Logger.LogError(ex.Message);
            return Observable.Return(-1);
        }).ToArray();
    }

    [Fact]
    public async Task TestOnErrorResumeNext()
    {
        var stream = Observable.Range(1, 3)
            .Select(v => Observable.Return(v).Delay(TimeSpan.FromMilliseconds(10))).Concat()
            .Concat(Observable.Throw<int>(new Exception("error")));

        var result = await stream.OnErrorResumeNext(Observable.Return(-1)).ToArray();
        Logger.LogInformation("{data}", result);
    }

    [Fact]
    public async Task HandleErrors()
    {
        IObservable<int> eventsStream()
        {
            var a = Array.Empty<int>();
            var i = a[0];
            return Observable.Return(a)
                .SelectMany(async a =>
                {
                    await Task.Delay(500);
                    return a;
                })
                .Select(array => array[0]);
        }

        await Observable.Defer(eventsStream).Catch<int, Exception>(ex =>
        {
            Logger.LogError(ex, "stream failed");
            return Observable.Empty<int>();
        }).Select(_ => Unit.Default).LastOrDefaultAsync();

        Logger.LogInformation("done");
    }


    [Fact]
    public async Task TestParallelization()
    {
        var random = new Random();
        var array = await Observable.Range(1, 10)
            .Select(v =>
                Observable.Defer(() =>
                Observable.Start(() =>
            {
                Logger.LogInformation("before sleep {v}, thread: {thread}", v, Thread.CurrentThread.ManagedThreadId);
                Thread.Sleep(TimeSpan.FromMilliseconds(random.Next(1000, 2000)));
                Logger.LogInformation("id: {id}, thread: {thread}", v, Thread.CurrentThread.ManagedThreadId);

                return $"{v}";
            }))).Merge(2);
    }

    [Fact]
    public async Task TestBackPressure()
    {
        var nbEventsProcessed = new BehaviorSubject<int>(0);

        var source = _grpcClient.GetObservableEvents(nbEventsProcessed, delayMs: 1, maxConcurrentNb: 10)
                .Do(e => Logger.LogTrace($"start processing {e.Id}"))
                .SelectMany(_ => Observable.Return(1).Delay(TimeSpan.FromMilliseconds(1000))) //do some stuff and return nb events processed
                .Scan(0, (acc, v) => acc + v)
                .StartWith(0) // not necessary here, initialize stream with seed value
            ;

        var done = false;

        source.Subscribe((v) =>
        {
            nbEventsProcessed.OnNext(v);
        }, () => done = true);

        while (!done)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
        nbEventsProcessed.OnCompleted();
    }

    [Fact]

    public async Task TestAsyncEmptyObservable()
    {
        var u = await Observable.Empty<int>().ToArray();
    }

    [Fact]

    public async Task TestAsyncObservable()
    {
        var v = await new[]{1, 1, 1, 2, 3, 3, 1, 2}.ToObservable().DistinctUntilChanged().ToArray();
        Logger.LogInformation("{v}", v);
    }

    [Fact]
    public async Task TestScanDistinct()
    {
        var random = new Random();
        var events =
            Observable.Generate(0,
                s => s < 10, 
                s => s + 1, _ => random.Next(1, 5));

        await events
            .Do(v => Logger.LogInformation("input {v}", v))
            .Scan((ids: new HashSet<int>(), result: Observable.Empty<int>()), (acc, current) =>
            {
                if (acc.ids.Contains(current)) return (acc.ids, Observable.Empty<int>());
                acc.ids.Add(current);
                return (acc.ids, Observable.Return(current));
            })
            .SelectMany(acc => acc.result)
            .Do(v => Logger.LogWarning("distinct {v}", v));
    }

    [Fact]
    public async Task TestCount()
    {
        var count = 0;
        var total = await new[]{7, 3, 5}.ToObservable()
            .Select(_ => count++)
            .ToArray();
        Logger.LogInformation("{r}", total);
    }

    [Fact]
    public async Task TestAddDataToStream()
    {
        var currentStream = Observable.Range(1, 5)
            .Select(_ => new Data { Count = _ })
            .WithLatestFrom(
                Observable.Return(new Event()), (data, @event) => (data, @event))
            .Select(d =>
            {
                d.data.Count = d.@event.Id;
                return d.data;
            });
    }

    public class Data
    {
        public int Count
        {
            get;set;

        }
    }

    [Fact]
    public async Task TestSwitch()
    {
        var subject = new Subject<IObservable<int>>();

        var completed = false;

        subject.Switch().Subscribe(
            v => Logger.LogInformation(v.ToString()),
            () => completed = true);

        subject.OnNext(Observable.Return(1));
        subject.OnNext(Observable.Return(2).Delay(TimeSpan.FromSeconds(5)));
        subject.OnNext(Observable.Return(3).Delay(TimeSpan.FromMilliseconds(500)));

        subject.OnCompleted();
        while (!completed)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }
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