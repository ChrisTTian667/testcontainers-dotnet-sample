using System.Text;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace TestService.Tests;

[TestFixture]
public class RunIntegrationTestsAgainApiContainer
{
    private const int DbContainerInternalPort = 5432;
    private const int DbContainerExternalPort = 7432;

    private const int ApiContainerInternalPort =  80;
    private const int ApiContainerExternalPort = 7132;

    private async Task BuildContainerAsync()
    {
        var resourceGroup = Guid.NewGuid();

        var file = new FileInfo("../../../../TestService.Api/Dockerfile");
        var path = file.Directory!.FullName + "/";

        var apiContainerImageBuilder = new ImageFromDockerfileBuilder()
            .WithName("testservice")
            .WithDeleteIfExists(true)
            .WithDockerfileDirectory("../../../../TestService.Api")
            .WithCleanUp(true);

        var apiContainerImage = await apiContainerImageBuilder.Build();

        _network = new TestcontainersNetworkBuilder()
            .WithName($"IntegrationTestNetwork {Guid.NewGuid()}")
            .Build();

        await _network.CreateAsync();

        _dbContainer = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage("postgres:latest")
            .WithName("IntegrationTest-Postgres")
            .WithHostname("postgres")
            .WithResourceReaperSessionId(resourceGroup)
            .WithNetwork(_network)
            .WithEnvironment("POSTGRES_USER", "postgres")
            .WithEnvironment("POSTGRES_PASSWORD", "postgres")
            .WithEnvironment("POSTGRES_DB", "test")
            .WithPortBinding(DbContainerExternalPort, DbContainerInternalPort)
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilPortIsAvailable(DbContainerInternalPort))
            .Build();

        _apiContainer = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage(apiContainerImage)
            .WithName("IntegrationTest-TestService")
            .WithResourceReaperSessionId(resourceGroup)
            .WithNetwork(_network)
            .WithHostname("service")
            .WithEnvironment("ConnectionStrings__TestDb", $"server=postgres;port={DbContainerInternalPort};database=Test;User ID=postgres;password=postgres")
            .WithPortBinding(ApiContainerExternalPort, ApiContainerInternalPort)
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilPortIsAvailable(ApiContainerInternalPort))
            .Build();
    }

    [OneTimeSetUp]
    public async Task SetupAsync()
    {
        // spin up containers
        await BuildContainerAsync();
        await _dbContainer.StartAsync();
        await _apiContainer.StartAsync();

        _client = new HttpClient
        {
            BaseAddress = new Uri($"http://localhost:{ApiContainerExternalPort}"),
        };
    }

    [OneTimeTearDown]
    public async Task TearDownAsync()
    {
        await _apiContainer.DisposeAsync();
        await _dbContainer.DisposeAsync();
        await _network.DeleteAsync();
    }

    private HttpClient? _client;
    private TestcontainersContainer _apiContainer = default!;
    private TestcontainersContainer _dbContainer = default!;
    private IDockerNetwork _network;

    [Test]
    [Order(1)]
    public void EnvironmentTest() => Assert.IsNotNull(_client);

    [Test]
    [Order(2)]
    public async Task EnsureEmptyDatabase()
    {
        var response = await _client!.GetAsync("/messages");
        var content = await response.Content.ReadAsStringAsync();
        var messages = JsonSerializer.Deserialize<Message[]>(content);

        Assert.IsNotNull(messages);
        Assert.IsEmpty(messages!);
    }

    [Test]
    [Order(3)]
    public async Task PostMessages()
    {
        var message = new Message { Text = "Hello World!" };
        var messageContent = JsonSerializer.Serialize(message);
        var stringContent = new StringContent(messageContent, Encoding.UTF8, "application/json");

        var response = await _client!.PostAsync("/message", stringContent);

        Assert.IsTrue(response.IsSuccessStatusCode);
    }

    [Test]
    [Order(4)]
    public async Task GetMessages()
    {
        var response = await _client!.GetAsync("/messages");
        Assert.IsTrue(response.IsSuccessStatusCode);
    }
}