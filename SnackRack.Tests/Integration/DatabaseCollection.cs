namespace SnackRack.Tests.Integration;

/// <summary>
/// Shared collection so all integration test classes reuse one container
/// (start-up is expensive — about 5-10 s for the pgvector image).
/// </summary>
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<TestDatabaseFixture> { }
