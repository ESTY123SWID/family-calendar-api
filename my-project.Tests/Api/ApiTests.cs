using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using my_project.Core.DTOs.Auth;
using my_project.Core.DTOs.Common;

namespace my_project.Tests.Api;

// End to end over HTTP: status codes, ErrorResponse, JWT roles, family isolation and 409 on a stale Version
public class ApiTests : IClassFixture<ApiFactory>
{
    private const string DemoPassword = "Demo1234!";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ApiFactory _factory;

    public ApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> LoginAsync(string email, string password = DemoPassword)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = password });
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(Json);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }

    [Fact]
    public async Task Login_DemoUser_ReturnsToken()
    {
        var client = await LoginAsync("sara@family.demo");

        var response = await client.GetAsync("/api/families");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401WithErrorResponse()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "sara@family.demo", Password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(Json);
        Assert.Equal(401, error!.Status);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/families");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoint_AsRegularUser_Returns403()
    {
        var client = await LoginAsync("sara@family.demo");

        var response = await client.GetAsync("/api/admin/stats");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminFromConfiguration_IsCreatedAtStartup_AndCanUseAdminEndpoints()
    {
        var client = await LoginAsync(ApiFactory.AdminEmail, ApiFactory.AdminPassword);

        var response = await client.GetAsync("/api/admin/stats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task FamilyIsolation_MemberOfAnotherFamily_Gets403()
    {
        // Rivka is only in family כהן (2); family לוי is 1
        var client = await LoginAsync("rivka@family.demo");

        var family = await client.GetAsync("/api/families/1/members");
        var task = await client.GetAsync("/api/tasks/2");

        Assert.Equal(HttpStatusCode.Forbidden, family.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, task.StatusCode);
    }

    [Fact]
    public async Task UnknownTask_Returns404()
    {
        var client = await LoginAsync("sara@family.demo");

        var response = await client.GetAsync("/api/tasks/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task InvalidRequest_Returns400WithFieldErrors()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new RegisterRequest { FullName = "", Email = "not-an-email", Password = "1" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(Json);
        Assert.NotNull(error!.Errors);
        Assert.NotEmpty(error.Errors);
    }

    [Fact]
    public async Task UpdateTask_WithStaleVersion_Returns409()
    {
        // Sara manages family לוי; task 4 is from the seed
        var client = await LoginAsync("sara@family.demo");
        var body = new
        {
            title = "לסדר שולחנות",
            category = "Venue",
            priority = "High",
            dueDate = "2027-01-15T12:00:00Z",
            version = Guid.NewGuid() // not the Version in the database
        };

        var response = await client.PutAsJsonAsync("/api/tasks/4", body);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(Json);
        Assert.Equal(409, error!.Status);
    }

    [Fact]
    public async Task VolunteerForTaskTakenBySomeoneElse_Returns409()
    {
        // Task 5 is assigned to Noa (5); Moshe (3) tries to take it
        var client = await LoginAsync("moshe@family.demo");

        var response = await client.PutAsJsonAsync("/api/tasks/5/assignee", new { userId = 3 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task EveryResponse_HasCorrelationIdHeader()
    {
        var response = await _factory.CreateClient().GetAsync("/api/families");

        Assert.True(response.Headers.Contains("X-Correlation-ID"));
    }
}
