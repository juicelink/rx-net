using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Core;

[Collection("tests")]
public abstract class BaseTests
{
    protected BaseTests(GlobalContext context, ITestOutputHelper output)
    {
        Provider = context.Provider;
        Logger = output.BuildLogger(MyLogConfig.Current);
    }

    protected IServiceProvider Provider { get; set; }
    protected ILogger Logger { get; set; }
}