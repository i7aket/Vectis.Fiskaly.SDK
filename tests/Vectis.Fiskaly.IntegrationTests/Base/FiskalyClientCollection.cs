namespace Vectis.Fiskaly.IntegrationTests.Base;

/// <summary>
/// xUnit collection definition for FiskalyClientFixture.
/// </summary>
/// <remarks>
/// This allows multiple test classes to share the same fixture instance.
/// All tests in the "FiskalyClient collection" will share the same FiskalyClientFixture.
/// </remarks>
[CollectionDefinition("FiskalyClient collection")]
public class FiskalyClientCollection : ICollectionFixture<FiskalyClientFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
