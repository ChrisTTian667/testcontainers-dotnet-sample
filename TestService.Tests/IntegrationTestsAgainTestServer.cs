using System.Text;
using System.Text.Json;
using NUnit.Framework;

namespace TestService.Tests;

/// <summary>
///     Creates a TestServer using WebApplicationFactory and spins up a fresh database in the local Docker-Environment
/// </summary>
[TestFixture]
public class RunIntegrationTestsAgainstTestServer
{
    [OneTimeSetUp]
    public async Task SetupAsync()
    {
        _webApplicationFactory = new TestServiceApplicationFactory(false);  // <-- here is the TestLocal switch

        // initialize the docker container
        await _webApplicationFactory.InitializeAsync();

        _client = _webApplicationFactory.CreateClient();
    }

    [OneTimeTearDown]
    public async Task TearDownAsync() =>
        await _webApplicationFactory.DisposeAsync();

    private HttpClient? _client;
    private TestServiceApplicationFactory _webApplicationFactory = default!;

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