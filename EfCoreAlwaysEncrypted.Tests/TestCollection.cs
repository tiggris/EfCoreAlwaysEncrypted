using EfCoreAlwaysEncrypted.Tests.Utils;
using Xunit;

namespace EfCoreAlwaysEncrypted.Tests
{
    [CollectionDefinition("Always encrypted collection")]
    public class TestCollection : ICollectionFixture<DatabaseFixture>
    {
    }
}
