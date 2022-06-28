using Xunit;

namespace Tests.Core;

[CollectionDefinition("tests")]
public class TestCollection : ICollectionFixture<GlobalContext>
{
}