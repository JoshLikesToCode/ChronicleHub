namespace ChronicleHub.Api.IntegrationTests;

/// <summary>
/// Collection definition to prevent parallel execution of integration tests.
/// All tests in this collection will run sequentially to avoid database conflicts.
/// </summary>
[CollectionDefinition("IntegrationTests", DisableParallelization = true)]
public class IntegrationTestCollection
{
    // This class is never instantiated. It's just a marker for the collection.
}
