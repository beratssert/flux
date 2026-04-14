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

/// <summary>HTTP coverage for /api/v1/reports (auth, CSV exports, groupBy errors, team vs self).</summary>
public class ReportsApiIntegrationTests
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
    public async Task Reports_MeTimeSummary_WithoutToken_Unauthorized()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var resp = await client.GetAsync("/api/v1/reports/me/time-summary?groupBy=day");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Reports_ManagerTeamTimeSummary_ManagerAndAdmin_ReturnOK_EmployeeForbidden()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var employeeToken = await LoginAsync(client, "employee@flux.local", "123Pa$$word!");
        var managerToken = await LoginAsync(client, "manager@flux.local", "123Pa$$word!");
        var adminToken = await LoginAsync(client, "admin@flux.local", "123Pa$$word!");

        using var empReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/reports/manager/team-time-summary?groupBy=week");
        empReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(empReq)).StatusCode);

        using var mgrReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/reports/manager/team-time-summary?groupBy=project");
        mgrReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(mgrReq)).StatusCode);

        using var admReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/reports/manager/team-time-summary?groupBy=user");
        admReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(admReq)).StatusCode);
    }

    [Fact]
    public async Task Reports_ExportManagerTeamTimeSummary_Manager_Csv_OK()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var token = await LoginAsync(client, "manager@flux.local", "123Pa$$word!");
        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/reports/manager/team-time-summary/export?format=csv&groupBy=user");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("text/csv", resp.Content.Headers.ContentType?.MediaType);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("key,minutes", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Reports_ExportManagerTeamTimeSummary_Employee_Forbidden()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var token = await LoginAsync(client, "employee@flux.local", "123Pa$$word!");
        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/reports/manager/team-time-summary/export?format=csv&groupBy=user");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Reports_ExportManagerTeamTimeSummary_InvalidFormat_BadRequest()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var token = await LoginAsync(client, "manager@flux.local", "123Pa$$word!");
        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/reports/manager/team-time-summary/export?format=pdf&groupBy=user");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Reports_ExportMyExpenseSummary_Csv_OK()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var token = await LoginAsync(client, "employee@flux.local", "123Pa$$word!");
        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/reports/me/expense-summary/export?format=csv&groupBy=month");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("text/csv", resp.Content.Headers.ContentType?.MediaType);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("key,amount", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Reports_ExportMyExpenseSummary_InvalidFormat_BadRequest()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var token = await LoginAsync(client, "employee@flux.local", "123Pa$$word!");
        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/reports/me/expense-summary/export?format=xml&groupBy=month");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Reports_ExportManagerTeamExpenseSummary_Admin_Csv_OK()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var token = await LoginAsync(client, "admin@flux.local", "123Pa$$word!");
        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/reports/manager/team-expense-summary/export?format=csv&groupBy=project");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("text/csv", resp.Content.Headers.ContentType?.MediaType);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("key,amount", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Reports_ManagerTeamTimeSummary_InvalidGroupBy_BadRequest()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var token = await LoginAsync(client, "manager@flux.local", "123Pa$$word!");
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/reports/manager/team-time-summary?groupBy=day");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Reports_ManagerTeamExpenseSummary_InvalidGroupBy_BadRequest()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var token = await LoginAsync(client, "manager@flux.local", "123Pa$$word!");
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/reports/manager/team-expense-summary?groupBy=category");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Reports_MyExpenseSummary_InvalidGroupBy_BadRequest()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var token = await LoginAsync(client, "employee@flux.local", "123Pa$$word!");
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/reports/me/expense-summary?groupBy=week");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task ProjectSummary_WithFromTo_ReturnsOK()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var managerToken = await LoginAsync(client, "manager@flux.local", "123Pa$$word!");
        var employeeToken = await LoginAsync(client, "employee@flux.local", "123Pa$$word!");
        var employeeId = await GetUserIdAsync(client, employeeToken);
        var projectId = await GetAssignedProjectIdAsync(employeeId);

        var url = $"/api/v1/reports/projects/{projectId}/summary?from=2020-01-01&to=2030-12-31";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task ProjectSummary_Admin_ReturnsOK()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var adminToken = await LoginAsync(client, "admin@flux.local", "123Pa$$word!");
        var employeeToken = await LoginAsync(client, "employee@flux.local", "123Pa$$word!");
        var employeeId = await GetUserIdAsync(client, employeeToken);
        var projectId = await GetAssignedProjectIdAsync(employeeId);

        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/reports/projects/{projectId}/summary");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task ProjectSummary_Export_InvalidFormat_BadRequest()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var managerToken = await LoginAsync(client, "manager@flux.local", "123Pa$$word!");
        var employeeToken = await LoginAsync(client, "employee@flux.local", "123Pa$$word!");
        var employeeId = await GetUserIdAsync(client, employeeToken);
        var projectId = await GetAssignedProjectIdAsync(employeeId);

        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/reports/projects/{projectId}/summary/export?format=json");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task ProjectSummary_ToBeforeFrom_BadRequest()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var managerToken = await LoginAsync(client, "manager@flux.local", "123Pa$$word!");
        var employeeToken = await LoginAsync(client, "employee@flux.local", "123Pa$$word!");
        var employeeId = await GetUserIdAsync(client, employeeToken);
        var projectId = await GetAssignedProjectIdAsync(employeeId);

        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/reports/projects/{projectId}/summary?from=2026-06-10&to=2026-01-01");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
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
}
