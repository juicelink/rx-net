using System.Reactive.Linq;

namespace RxLibrary;

public static class ObservableExtensions
{
    public static IObservable<U> SelectManyOrdered<T, U>(
        this IObservable<T> that,
        Func<T, IObservable<U>> selector,
        int? maxConcurrent= null) => null;

    public static IObservable<T> RetryAfterDelay<T>(this IObservable<T> source, TimeSpan delay) => null;
}