using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Xunit;

namespace CleanArchitecture.IntegrationTests;

public class CalendarApiIntegrationTests
{
    private static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("INTEGRATION_API_BASE_URL") ?? "http://localhost:5001";

    private static readonly string SqlConnectionString =
        Environment.GetEnvironmentVariable("INTEGRATION_SQL_CONNECTION")
        ?? "Server=sqlserver,1433;Database=CleanArchitectureApplicationDb;User Id=sa;Password=Your_strong_password_123;TrustServerCertificate=True;Encrypt=False";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task Calendar_Post_WithoutToken_Unauthorized()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var resp = await client.PostAsJsonAsync("/api/v1/calendar-events", new { title = "x" });
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Calendar_Post_AsEmployee_Forbidden()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var token = await LoginAsync(client, "employee@flux.local", "123Pa$$word!");
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/calendar-events")
        {
            Content = JsonContent.Create(new
            {
                projectId = 1,
                title = "No",
                startAtUtc = DateTime.UtcNow.AddDays(1),
                endAtUtc = DateTime.UtcNow.AddDays(1).AddHours(1),
                visibilityType = "Project",
                isAllDay = false
            })
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Calendar_ProjectEvent_ManagerCreate_EmployeeSees_AdminLists_ThenManagerDeletes()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var managerToken = await LoginAsync(client, "manager@flux.local", "123Pa$$word!");
        var employeeToken = await LoginAsync(client, "employee@flux.local", "123Pa$$word!");
        var adminToken = await LoginAsync(client, "admin@flux.local", "123Pa$$word!");
        var employeeId = await GetUserIdAsync(client, employeeToken);
        var projectId = await GetAssignedProjectIdAsync(employeeId);

        var start = new DateTime(2031, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        var end = start.AddHours(1);
        var title = $"IT-Cal-{Guid.NewGuid():N}".Substring(0, 24);

        using (var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/calendar-events"))
        {
            createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
            createReq.Content = JsonContent.Create(new
            {
                projectId,
                title,
                description = "integration",
                startAtUtc = start,
                endAtUtc = end,
                visibilityType = "Project",
                isAllDay = false,
                participantUserIds = (string[]?)null
            });
            var createResp = await client.SendAsync(createReq);
            Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
            var created = await createResp.Content.ReadFromJsonAsync<CalendarEventResponse>(JsonOptions);
            Assert.NotNull(created?.Id);
            Assert.Equal(title, created.Title);

            using var empListReq = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/v1/calendar-events?from=2031-05-01&to=2031-07-01&projectId={projectId}");
            empListReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);
            var empListResp = await client.SendAsync(empListReq);
            Assert.Equal(HttpStatusCode.OK, empListResp.StatusCode);
            var empPage = await empListResp.Content.ReadFromJsonAsync<PagedCalendarResponse>(JsonOptions);
            Assert.Contains(empPage?.Items ?? Array.Empty<CalendarEventResponse>(), e => e.Id == created.Id);

            using var adminListReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/calendar-events?page=1&pageSize=50");
            adminListReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var adminListResp = await client.SendAsync(adminListReq);
            Assert.Equal(HttpStatusCode.OK, adminListResp.StatusCode);

            using var delReq = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/calendar-events/{created.Id}");
            delReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
            var delResp = await client.SendAsync(delReq);
            Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);

            using var getReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/calendar-events/{created.Id}");
            getReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);
            var getResp = await client.SendAsync(getReq);
            Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
        }
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        Assert.False(string.IsNullOrWhiteSpace(payload?.AccessToken));
        return payload!.AccessToken;
    }

    private static async Task<string> GetUserIdAsync(HttpClient client, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var profile = await response.Content.ReadFromJsonAsync<MyProfileResponse>(JsonOptions);
        Assert.False(string.IsNullOrWhiteSpace(profile?.Id));
        return profile!.Id;
    }

    private static async Task<int> GetAssignedProjectIdAsync(string userId)
    {
        await using var connection = new SqlConnection(SqlConnectionString);
        await connection.OpenAsync();
        const string sql = @"
SELECT TOP 1 ProjectId
FROM ProjectAssignments
WHERE UserId = @userId AND IsActive = 1
ORDER BY Id";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        var result = await command.ExecuteScalarAsync();
        Assert.NotNull(result);
        return Convert.ToInt32(result);
    }

    private sealed class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
    }

    private sealed class MyProfileResponse
    {
        public string Id { get; set; } = string.Empty;
    }

    private sealed class CalendarEventResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    private sealed class PagedCalendarResponse
    {
        public CalendarEventResponse[] Items { get; set; } = Array.Empty<CalendarEventResponse>();
    }
}
