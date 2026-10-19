using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using my_project.Core.DTOs.Admin;
using my_project.Core.DTOs.Auth;
using my_project.Core.DTOs.Events;
using my_project.Core.Enums;
using my_project.Core.DTOs.Users;

namespace my_project.Tests.Api;

// Every AdminController action, as the React admin area calls it.
// (The search parameter uses PostgreSQL ILIKE, so it is not run on SQLite)
public class AdminApiTests : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ApiFactory _factory;

    public AdminApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpResponseMessage> LoginResponseAsync(string email, string password) =>
        await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = password });

    private async Task<HttpClient> LoginAsync(string email, string password = "Demo1234!")
    {
        var response = await LoginResponseAsync(email, password);
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(Json);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }

    private Task<HttpClient> LoginAsAdminAsync() => LoginAsync(ApiFactory.AdminEmail, ApiFactory.AdminPassword);

    private sealed record PageOf<T>(List<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);

    [Theory]
    [InlineData("GET", "/api/admin/stats")]
    [InlineData("GET", "/api/admin/users")]
    [InlineData("PATCH", "/api/admin/users/6")]
    [InlineData("GET", "/api/admin/families")]
    [InlineData("GET", "/api/admin/families/1")]
    [InlineData("GET", "/api/admin/families/1/events")]
    [InlineData("DELETE", "/api/admin/families/1")]
    public async Task EveryAdminEndpoint_RegularUser_Gets403(string method, string url)
    {
        var client = await LoginAsync("sara@family.demo");
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method == "PATCH")
        {
            request.Content = JsonContent.Create(new { isActive = false });
        }

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Stats_CountsTheSeedAndTheAdmin()
    {
        var client = await LoginAsAdminAsync();

        var stats = await client.GetFromJsonAsync<AdminStatsDto>("/api/admin/stats", Json);

        Assert.True(stats!.TotalUsers >= 7);
        Assert.True(stats.TotalEvents >= 4);
        Assert.True(stats.TotalTasks >= 9);
        Assert.True(stats.OpenTasks <= stats.TotalTasks);
    }

    [Fact]
    public async Task Users_ArePagedAndSortedInTheDatabase()
    {
        var client = await LoginAsAdminAsync();

        var page = await client.GetFromJsonAsync<PageOf<AdminUserDto>>("/api/admin/users?page=2&pageSize=3&sortBy=name", Json);

        Assert.Equal(2, page!.Page);
        Assert.Equal(3, page.Items.Count);
        Assert.True(page.TotalCount >= 7);
        Assert.Equal((int)Math.Ceiling(page.TotalCount / 3.0), page.TotalPages);

        var newest = await client.GetFromJsonAsync<PageOf<AdminUserDto>>("/api/admin/users?sortBy=createdAt&sortDirection=Desc&pageSize=1", Json);
        // The admin is created at startup, after the seed (2026-09-01)
        Assert.Equal(ApiFactory.AdminEmail, newest!.Items.Single().Email);
    }

    [Fact]
    public async Task BlockAndUnblockUser_BlockedUserCannotLogIn()
    {
        var client = await LoginAsAdminAsync();
        const int yael = 6;

        var block = await client.PatchAsJsonAsync($"/api/admin/users/{yael}", new { isActive = false });
        Assert.Equal(HttpStatusCode.NoContent, block.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginResponseAsync("yael@family.demo", "Demo1234!")).StatusCode);

        var unblock = await client.PatchAsJsonAsync($"/api/admin/users/{yael}", new { isActive = true });
        Assert.Equal(HttpStatusCode.NoContent, unblock.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginResponseAsync("yael@family.demo", "Demo1234!")).StatusCode);
    }

    [Fact]
    public async Task ChangeRole_ToSystemAdminAndBack()
    {
        var client = await LoginAsAdminAsync();
        const int moshe = 3;

        Assert.Equal(HttpStatusCode.NoContent, (await client.PatchAsJsonAsync($"/api/admin/users/{moshe}", new { role = "SystemAdmin" })).StatusCode);
        var asAdmin = await LoginAsync("moshe@family.demo");
        Assert.Equal(HttpStatusCode.OK, (await asAdmin.GetAsync("/api/admin/stats")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PatchAsJsonAsync($"/api/admin/users/{moshe}", new { role = "User" })).StatusCode);
        var asUser = await LoginAsync("moshe@family.demo");
        Assert.Equal(HttpStatusCode.Forbidden, (await asUser.GetAsync("/api/admin/stats")).StatusCode);
    }

    [Fact]
    public async Task AdminCannotBlockOrDemoteThemselves_Returns409()
    {
        var client = await LoginAsAdminAsync();
        var me = await client.GetFromJsonAsync<UserDto>("/api/users/me", Json);

        var block = await client.PatchAsJsonAsync($"/api/admin/users/{me!.Id}", new { isActive = false });
        var demote = await client.PatchAsJsonAsync($"/api/admin/users/{me.Id}", new { role = "User" });

        Assert.Equal(HttpStatusCode.Conflict, block.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, demote.StatusCode);
    }

    [Fact]
    public async Task UpdateUnknownUser_Returns404_InvalidRole_Returns400()
    {
        var client = await LoginAsAdminAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await client.PatchAsJsonAsync("/api/admin/users/99999", new { isActive = false })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PatchAsJsonAsync("/api/admin/users/2", new { role = "Owner" })).StatusCode);
    }

    [Fact]
    public async Task Families_ListAndDetailsWithMembers()
    {
        var client = await LoginAsAdminAsync();

        var families = await client.GetFromJsonAsync<PageOf<AdminFamilyDto>>("/api/admin/families?sortBy=name", Json);
        Assert.Contains(families!.Items, family => family.JoinCode == "LEVI-4821");

        var levi = await client.GetFromJsonAsync<AdminFamilyDetailsDto>("/api/admin/families/1", Json);
        Assert.Equal(5, levi!.Members.Count);
        Assert.Equal(levi.MembersCount, levi.Members.Count);
        Assert.Equal(3, levi.EventsCount);
        Assert.Equal("שרה לוי", levi.ManagerName);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/admin/families/99999")).StatusCode);
    }

    [Fact]
    public async Task FamilyEvents_AdminWhoIsNotAMember_SeesThePagedList()
    {
        // The admin is not a member of family לוי (1), which has 3 events, one of them cancelled
        var client = await LoginAsAdminAsync();

        var all = await client.GetFromJsonAsync<PageOf<EventSummaryDto>>("/api/admin/families/1/events", Json);
        Assert.Equal(3, all!.TotalCount);
        Assert.All(all.Items, evt => Assert.Equal(1, evt.FamilyId));
        Assert.Equal(all.Items.OrderBy(evt => evt.StartAt).Select(evt => evt.Id), all.Items.Select(evt => evt.Id));

        var cancelled = await client.GetFromJsonAsync<PageOf<EventSummaryDto>>("/api/admin/families/1/events?status=Cancelled", Json);
        Assert.Equal(EventStatus.Cancelled, Assert.Single(cancelled!.Items).Status);

        var firstPage = await client.GetFromJsonAsync<PageOf<EventSummaryDto>>("/api/admin/families/1/events?page=1&pageSize=2", Json);
        Assert.Equal(2, firstPage!.Items.Count);
        Assert.Equal(2, firstPage.TotalPages);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/admin/families/99999/events")).StatusCode);
    }

    [Fact]
    public async Task DeleteFamily_Returns204_ThenTheFamilyIsGone()
    {
        var client = await LoginAsAdminAsync();

        // Family כהן (2); the other tests of this class use family לוי (1)
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/admin/families/2")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/admin/families/2")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync("/api/admin/families/2")).StatusCode);
    }
}
