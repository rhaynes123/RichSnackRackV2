namespace SnackRack.Tests.UI.Infrastructure;

/// <summary>
/// xUnit collection that shares a single <see cref="WebServerFixture"/> (and its
/// Testcontainers PostgreSQL instance) across all UI tests, keeping container
/// start-up cost low.
/// </summary>
[CollectionDefinition("UI")]
public class PlaywrightCollection : ICollectionFixture<WebServerFixture>;
