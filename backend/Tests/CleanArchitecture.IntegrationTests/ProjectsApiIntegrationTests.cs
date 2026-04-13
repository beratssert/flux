using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace CleanArchitecture.IntegrationTests;

/// <summary>HTTP tests for /api/v1/projects. Uses manager-created projects to avoid clashing with parallel tests on shared seed rows.</summary>
public class ProjectsApiIntegrationTests
{
    private static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("INTEGRATION_API_BASE_URL") ?? "http://localhost:5001";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task Projects_Get_WithoutToken_ReturnsUnauthorized()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var response = await client.GetAsync("/api/v1/projects?pageNumber=1&pageSize=10");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Projects_Post_Employee_ReturnsForbidden()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var token = await LoginAndGetTokenAsync(client, "employee@flux.local", "123Pa$$word!");
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/projects")
        {
            Content = JsonContent.Create(new { name = "EmpShouldNotCreate", code = (string?)null })
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Projects_Manager_Create_Employee_Visibility_GetById_And_Assignments()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var managerToken = await LoginAndGetTokenAsync(client, "manager@flux.local", "123Pa$$word!");
        var employeeToken = await LoginAndGetTokenAsync(client, "employee@flux.local", "123Pa$$word!");
        var employeeId = await GetCurrentUserIdAsync(client, employeeToken);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var code = $"ITG-{suffix}";

        using var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/projects")
        {
            Content = JsonContent.Create(new { name = $"Integration Project {suffix}", code, description = "itg" })
        };
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var createResp = await client.SendAsync(createReq);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var created = await createResp.Content.ReadFromJsonAsync<ProjectIdResponse>(JsonOptions);
        Assert.NotNull(created);
        var projectId = created!.Id;

        using var empBefore = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/projects/{projectId}");
        empBefore.Headers.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);
        var notYet = await client.SendAsync(empBefore);
        Assert.Equal(HttpStatusCode.NotFound, notYet.StatusCode);

        using var assignReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/projects/{projectId}/assignments")
        {
            Content = JsonContent.Create(new { userId = employeeId })
        };
        assignReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var assignResp = await client.SendAsync(assignReq);
        Assert.Equal(HttpStatusCode.Created, assignResp.StatusCode);

        using var empAfter = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/projects/{projectId}");
        empAfter.Headers.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);
        var okVisible = await client.SendAsync(empAfter);
        Assert.Equal(HttpStatusCode.OK, okVisible.StatusCode);

        using var dupAssign = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/projects/{projectId}/assignments")
        {
            Content = JsonContent.Create(new { userId = employeeId })
        };
        dupAssign.Headers.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var conflict = await client.SendAsync(dupAssign);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);

        using var listReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/projects/{projectId}/assignments");
        listReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var listResp = await client.SendAsync(listReq);
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var assignmentUsers = await listResp.Content.ReadFromJsonAsync<AssignmentUserRow[]>(JsonOptions);
        Assert.NotNull(assignmentUsers);
        Assert.Contains(assignmentUsers!, r => r.UserId == employeeId);

        using var delReq = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/api/v1/projects/{projectId}/assignments/{Uri.EscapeDataString(employeeId)}");
        delReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var delResp = await client.SendAsync(delReq);
        Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);

        using var reassignReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/projects/{projectId}/assignments")
        {
            Content = JsonContent.Create(new { userId = employeeId })
        };
        reassignReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var reassignResp = await client.SendAsync(reassignReq);
        Assert.Equal(HttpStatusCode.Created, reassignResp.StatusCode);
    }

    [Fact]
    public async Task Projects_Patch_And_Status_Manager_And_Patch_Employee_Forbidden()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var managerToken = await LoginAndGetTokenAsync(client, "manager@flux.local", "123Pa$$word!");
        var employeeToken = await LoginAndGetTokenAsync(client, "employee@flux.local", "123Pa$$word!");

        var suffix = Guid.NewGuid().ToString("N")[..8];
        using var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/projects")
        {
            Content = JsonContent.Create(new { name = $"Patch Project {suffix}", code = $"PCH-{suffix}" })
        };
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var createResp = await client.SendAsync(createReq);
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<ProjectIdResponse>(JsonOptions);
        var projectId = created!.Id;

        using var patchReq = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/projects/{projectId}")
        {
            Content = JsonContent.Create(new { name = $"Patched {suffix}" })
        };
        patchReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var patchResp = await client.SendAsync(patchReq);
        Assert.Equal(HttpStatusCode.OK, patchResp.StatusCode);

        using var statusReq = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/projects/{projectId}/status")
        {
            Content = JsonContent.Create(new { status = "Archived" })
        };
        statusReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var statusResp = await client.SendAsync(statusReq);
        Assert.Equal(HttpStatusCode.OK, statusResp.StatusCode);

        using var statusBack = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/projects/{projectId}/status")
        {
            Content = JsonContent.Create(new { status = "Active" })
        };
        statusBack.Headers.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var statusBackResp = await client.SendAsync(statusBack);
        Assert.Equal(HttpStatusCode.OK, statusBackResp.StatusCode);

        using var empPatch = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/projects/{projectId}")
        {
            Content = JsonContent.Create(new { name = "Nope" })
        };
        empPatch.Headers.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);
        var empPatchResp = await client.SendAsync(empPatch);
        Assert.Equal(HttpStatusCode.Forbidden, empPatchResp.StatusCode);
    }

    [Fact]
    public async Task Projects_GetAssignments_Employee_Forbidden_Admin_OK()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var managerToken = await LoginAndGetTokenAsync(client, "manager@flux.local", "123Pa$$word!");
        var employeeToken = await LoginAndGetTokenAsync(client, "employee@flux.local", "123Pa$$word!");
        var adminToken = await LoginAndGetTokenAsync(client, "admin@flux.local", "123Pa$$word!");

        var suffix = Guid.NewGuid().ToString("N")[..8];
        using var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/projects")
        {
            Content = JsonContent.Create(new { name = $"List Assign {suffix}", code = $"LAS-{suffix}" })
        };
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var createResp = await client.SendAsync(createReq);
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<ProjectIdResponse>(JsonOptions);
        var projectId = created!.Id;

        using var empList = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/projects/{projectId}/assignments");
        empList.Headers.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);
        var empListResp = await client.SendAsync(empList);
        Assert.Equal(HttpStatusCode.Forbidden, empListResp.StatusCode);

        using var adminList = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/projects/{projectId}/assignments");
        adminList.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var adminListResp = await client.SendAsync(adminList);
        Assert.Equal(HttpStatusCode.OK, adminListResp.StatusCode);
    }

    [Fact]
    public async Task Projects_PatchManager_Admin_Reassigns_To_Manager()
    {
        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var adminToken = await LoginAndGetTokenAsync(client, "admin@flux.local", "123Pa$$word!");
        var managerToken = await LoginAndGetTokenAsync(client, "manager@flux.local", "123Pa$$word!");
        var managerUserId = await GetCurrentUserIdAsync(client, managerToken);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        using var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/projects")
        {
            Content = JsonContent.Create(new { name = $"Admin Reassign {suffix}", code = $"ADM-{suffix}" })
        };
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var createResp = await client.SendAsync(createReq);
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<ProjectIdResponse>(JsonOptions);
        var projectId = created!.Id;

        using var reassignReq = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/projects/{projectId}/manager")
        {
            Content = JsonContent.Create(new { managerUserId })
        };
        reassignReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var reassignResp = await client.SendAsync(reassignReq);
        Assert.Equal(HttpStatusCode.OK, reassignResp.StatusCode);
        var body = await reassignResp.Content.ReadFromJsonAsync<ProjectIdResponse>(JsonOptions);
        Assert.Equal(managerUserId, body!.ManagerUserId);
    }

    private static async Task<string> LoginAndGetTokenAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
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

    private sealed class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
    }

    private sealed class MyProfileResponse
    {
        public string Id { get; set; } = string.Empty;
    }

    private sealed class ProjectIdResponse
    {
        public int Id { get; set; }
        public string ManagerUserId { get; set; } = string.Empty;
    }

    private sealed class AssignmentUserRow
    {
        public string UserId { get; set; } = string.Empty;
    }
}
