using System.Reactive.Linq;

namespace RxLibrary;

public static class ObservableExtensions
{
    public static IObservable<U> SelectManyOrdered<T, U>(
        this IObservable<T> that,
        Func<T, IObservable<U>> selector,
        int? maxConcurrent = null) =>
        that.Select((t, i) => selector(t).Select(u => (u, i)))
            .Merge(maxConcurrent ?? int.MaxValue)
            .Scan((currI: 0, cache: new Dictionary<int, U>(), result: Enumerable.Empty<U>()),
                (acc, current) =>
                {
                    if (current.i == acc.currI)
                    {
                        var result = new List<U> { current.u };
                        var i = current.i + 1;
                        while (acc.cache.TryGetValue(i, out var @event))
                        {
                            result.Add(@event);
                            acc.cache.Remove(i);
                            i++;
                        }

                        return (i, acc.cache, result);
                    }
                    acc.cache[current.i] = current.u;
                    return (acc.currI, acc.cache, Enumerable.Empty<U>());
                })
            .SelectMany(v => v.result);

    public static IObservable<T> RetryAfterDelay<T>(this IObservable<T> source, TimeSpan delay)
    {
        return RepeateInfinite(source, delay).Catch();
    }

    private static IEnumerable<IObservable<TSource>> RepeateInfinite<TSource>(IObservable<TSource> source, TimeSpan delay)
    {
        yield return source;

        while (true)
            yield return source.DelaySubscription(delay);
    }
}