using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Rbac.AspNetCore.Tests;

public class SampleApiAuthorizationTests : IClassFixture<SampleApiFactory>
{
    private readonly SampleApiFactory _factory;

    public SampleApiAuthorizationTests(SampleApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_and_permission_gate_passengers()
    {
        var client = _factory.CreateClient();
        var viewer = await LoginAsync(client, "viewer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", viewer);

        var list = await client.GetAsync("/api/passengers");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);

        var create = await client.PostAsJsonAsync("/api/passengers", new
        {
            fullName = "Blocked User",
            documentNo = "X1",
            nationality = "MM"
        });
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
    }

    [Fact]
    public async Task Officer_can_create_but_cannot_export_reports()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "officer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/api/passengers", new
        {
            fullName = "New Passenger",
            documentNo = "AB123",
            nationality = "MM"
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var export = await client.GetAsync("/api/reports/export");
        Assert.Equal(HttpStatusCode.Forbidden, export.StatusCode);
    }

    [Fact]
    public async Task John_is_forbidden_from_report_export()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "john");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var export = await client.GetAsync("/api/reports/export");
        Assert.Equal(HttpStatusCode.Forbidden, export.StatusCode);

        var report = await client.GetAsync("/api/reports");
        Assert.Equal(HttpStatusCode.OK, report.StatusCode);
    }

    [Fact]
    public async Task Superadmin_can_read_roles_and_me()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "superadmin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var me = await client.GetAsync("/api/rbac/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);

        var roles = await client.GetAsync("/api/rbac/roles");
        Assert.Equal(HttpStatusCode.OK, roles.StatusCode);
    }

    [Fact]
    public async Task Viewer_cannot_administer_roles()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "viewer");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var roles = await client.GetAsync("/api/rbac/roles");
        Assert.Equal(HttpStatusCode.Forbidden, roles.StatusCode);
    }

    private static async Task<string> LoginAsync(HttpClient client, string username)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { username, password = "Passw0rd!" });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(payload?.AccessToken);
        return payload!.AccessToken;
    }

    private sealed class LoginResponse
    {
        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; } = string.Empty;
    }
}

public sealed class SampleApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"rbac-sample-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Sqlite", $"Data Source={_dbPath}");
        builder.UseSetting("ConnectionStrings:SqlServer", "");
        builder.UseEnvironment("Testing");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }
        catch
        {
            // best-effort cleanup
        }
    }
}
