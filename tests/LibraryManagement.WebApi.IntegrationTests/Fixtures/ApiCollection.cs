namespace LibraryManagement.WebApi.IntegrationTests.Fixtures;

/// <summary>
/// Every integration test class joins this collection: they share one API
/// host and one SQL Server container, and run one at a time.
/// </summary>
[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<LibraryApiFactory>
{
    public const string Name = "Api";
}
