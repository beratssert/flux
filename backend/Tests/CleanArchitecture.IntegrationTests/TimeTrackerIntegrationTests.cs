using Microsoft.Data.SqlClient;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace CleanArchitecture.IntegrationTests;

public class TimeTrackerIntegrationTests
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
    public async Task TeamEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var response = await client.GetAsync("/api/v1/TimeEntries/team?pageNumber=1&pageSize=10");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TeamEndpoint_EmployeeForbidden_ManagerAllowed()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };

        var employeeToken = await LoginAndGetTokenAsync(client, "employee@flux.local", "123Pa$$word!");
        var managerToken = await LoginAndGetTokenAsync(client, "manager@flux.local", "123Pa$$word!");

        using var employeeReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/TimeEntries/team?pageNumber=1&pageSize=10");
        employeeReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);
        var employeeResponse = await client.SendAsync(employeeReq);

        using var managerReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/TimeEntries/team?pageNumber=1&pageSize=10");
        managerReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var managerResponse = await client.SendAsync(managerReq);

        Assert.Equal(HttpStatusCode.Forbidden, employeeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, managerResponse.StatusCode);
    }

    [Fact]
    public async Task CreateTimeEntry_WritesAuditLogRow_InSqlServer()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var employeeToken = await LoginAndGetTokenAsync(client, "employee@flux.local", "123Pa$$word!");
        var employeeId = await GetCurrentUserIdAsync(client, employeeToken);
        var projectId = await GetAssignedProjectIdAsync(employeeId);

        var now = DateTime.UtcNow;

        using var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/TimeEntries")
        {
            Content = JsonContent.Create(new
            {
                projectId,
                entryDate = now.Date,
                durationMinutes = 17,
                description = "integration-audit-check",
                isBillable = true
            })
        };
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);

        var createResponse = await client.SendAsync(createReq);
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var entryIdRaw = await createResponse.Content.ReadAsStringAsync();
        Assert.True(int.TryParse(entryIdRaw, out var entryId));

        var auditCount = await GetAuditRowCountAsync(employeeId, entryId);
        Assert.True(auditCount > 0);
    }

    [Fact]
    public async Task ExpensesEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var response = await client.GetAsync("/api/v1/Expenses?pageNumber=1&pageSize=10");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RejectExpense_EmployeeForbidden()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var employeeToken = await LoginAndGetTokenAsync(client, "employee@flux.local", "123Pa$$word!");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/Expenses/1/reject")
        {
            Content = JsonContent.Create(new { id = 1, reason = "invalid-receipt" })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ExpenseCategories_Get_EmployeeAllowed_AdminWriteOnly()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var employeeToken = await LoginAndGetTokenAsync(client, "employee@flux.local", "123Pa$$word!");
        var adminToken = await LoginAndGetTokenAsync(client, "admin@flux.local", "123Pa$$word!");

        using var getReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/expense-categories");
        getReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);
        var getResp = await client.SendAsync(getReq);
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);

        using var employeePostReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/expense-categories")
        {
            Content = JsonContent.Create(new { name = "EmployeeShouldNotCreate" })
        };
        employeePostReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);
        var employeePostResp = await client.SendAsync(employeePostReq);
        Assert.Equal(HttpStatusCode.Forbidden, employeePostResp.StatusCode);

        using var adminPostReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/expense-categories")
        {
            Content = JsonContent.Create(new { name = $"AdminCanCreateCategory-{Guid.NewGuid():N}" })
        };
        adminPostReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var adminPostResp = await client.SendAsync(adminPostReq);
        Assert.Equal(HttpStatusCode.Created, adminPostResp.StatusCode);
    }

    [Fact]
    public async Task ExpenseReports_MeAndTeamEndpoints_WorkWithAuthorization()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var employeeToken = await LoginAndGetTokenAsync(client, "employee@flux.local", "123Pa$$word!");
        var managerToken = await LoginAndGetTokenAsync(client, "manager@flux.local", "123Pa$$word!");

        using var meReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/reports/me/expense-summary?groupBy=project");
        meReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);
        var meResp = await client.SendAsync(meReq);
        Assert.Equal(HttpStatusCode.OK, meResp.StatusCode);

        using var teamReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/reports/manager/team-expense-summary?groupBy=user");
        teamReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var teamResp = await client.SendAsync(teamReq);
        Assert.Equal(HttpStatusCode.OK, teamResp.StatusCode);

        using var employeeTeamReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/reports/manager/team-expense-summary?groupBy=user");
        employeeTeamReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);
        var employeeTeamResp = await client.SendAsync(employeeTeamReq);
        Assert.Equal(HttpStatusCode.Forbidden, employeeTeamResp.StatusCode);
    }

    private static async Task<string> LoginAndGetTokenAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password
        });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        Assert.False(string.IsNullOrWhiteSpace(payload?.AccessToken));
        return payload!.AccessToken;
    }

    private static async Task<string> GetCurrentUserIdAsync(HttpClient client, string token)
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

    private static async Task<int> GetAuditRowCountAsync(string actorUserId, int entryId)
    {
        await using var connection = new SqlConnection(SqlConnectionString);
        await connection.OpenAsync();

        const string sql = @"
SELECT COUNT(1)
FROM AuditLogs
WHERE ActorUserId = @actorUserId
  AND EntityName = 'TimeEntry'
  AND EntityId = @entityId
  AND ActionType = 'Create'";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@actorUserId", actorUserId);
        command.Parameters.AddWithValue("@entityId", entryId.ToString());

        var result = await command.ExecuteScalarAsync();
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