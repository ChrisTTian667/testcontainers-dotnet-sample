using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace TestService.Tests;

public class TestServiceApplicationFactory : WebApplicationFactory<Program>
{
    private readonly bool _testLocal;
    private const int DbContainerInternalPort = 5432;
    private const int DbContainerExternalPort = 7432;

    private static TestcontainersContainer DbContainer =>
        new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage("postgres:latest")
            .WithName("IntegrationTest-Postgres")
            .WithEnvironment("POSTGRES_USER", "postgres")
            .WithEnvironment("POSTGRES_PASSWORD", "postgres")
            .WithEnvironment("POSTGRES_DB", "test")
            .WithPortBinding(DbContainerExternalPort, DbContainerInternalPort)
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilPortIsAvailable(DbContainerInternalPort))
            .Build();

    public TestServiceApplicationFactory(bool testLocal = false) =>
        _testLocal = testLocal;

    /// <summary>
    ///     Since nUnit doesn't support an equivalent to IAsyncLifetime, we have to write our own methods *rolleyes*
    /// </summary>
    public async Task InitializeAsync() =>
        await DbContainer.StartAsync();

    public override async ValueTask DisposeAsync()
    {
        await DbContainer.DisposeAsync();
        await base.DisposeAsync();

        GC.SuppressFinalize(this);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (_testLocal)
            return;

        builder.ConfigureTestServices(services =>
        {
            // revert the original DbContextFactory registration.
            services.RevertDbContextFactoryRegistration<TestDbContext>();

            // but instead, register a new DbContextFactory that points to the database in the docker container
            services.AddDbContextFactory<TestDbContext>(opt =>
            {
                opt.UseNpgsql(
                    $"server=localhost;port={DbContainerExternalPort};database=Test;User ID=postgres;password=postgres");
            });
        });
    }
}