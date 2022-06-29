using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace WebApi;

public class TemperatureService : IDisposable
{
    private readonly BehaviorSubject<int> _subject;

    public TemperatureService()
    {
        _subject = new BehaviorSubject<int>(0);
    }

    public void UpdateTemperature(int temp) => _subject.OnNext(temp);

    public IObservable<int> GetTemperature() => _subject.Synchronize();

    public void Dispose()
    {
        _subject.Dispose();
    }
}