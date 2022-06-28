using Divergic.Logging.Xunit;

namespace Tests.Core;

public class MyLogConfig : LoggingConfig
{
    public MyLogConfig()
    {
        Formatter = new MyLogFormatter();
    }

    public static MyLogConfig Current { get; } = new ();
}