using System.Net;
using System.Net.Http.Json;
using Hearth.TestUtilities;
using Microsoft.AspNetCore.Mvc.Testing;
using Tasks.Records;

namespace Tasks.Tests;

/// <summary>PUT /tasks/{id} replace semantics, the service's cross-origin stance, and its health endpoint.</summary>
public sealed class TaskEndpointTests : IDisposable
{
    private readonly TempDatabase _db = new();
    private readonly WebApplicationFactory<Program> _factory;

    public TaskEndpointTests()
    {
        _factory = HearthApp.Create<Program>("tasks", _db);
    }

    public void Dispose()
    {
        _factory.Dispose();
        _db.Dispose();
    }

    private async Task<TaskItem> CreateAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/tasks", new
        {
            title = "Water plants",
            due_date = DateTime.UtcNow.Date.AddDays(1),
            description = "Ferns too",
            assignee = "Sam",
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TaskItem>(ServiceDefaults.HearthJson.SnakeCaseLower))!;
    }

    [Fact]
    public async Task Put_NullFields_ClearsThem()
    {
        var client = _factory.CreateClient();
        var task = await CreateAsync(client);

        var response = await client.PutAsJsonAsync($"/tasks/{task.Id}", new
        {
            done = false,
            title = "Water plants",
            due_date = (DateTime?)null,
            description = (string?)null,
            assignee = (string?)null,
        });

        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<TaskItem>(ServiceDefaults.HearthJson.SnakeCaseLower);
        Assert.Null(updated!.DueDate);
        Assert.Null(updated.Description);
        Assert.Null(updated.Assignee);
    }

    [Fact]
    public async Task Put_BlankTitle_Returns400()
    {
        var client = _factory.CreateClient();
        var task = await CreateAsync(client);

        var response = await client.PutAsJsonAsync($"/tasks/{task.Id}", new { done = true, title = " " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    public static TheoryData<object> RejectedTasks => new()
    {
        new { title = " " },
        new { title = new string('a', 201) },
        new { title = "Bins", due_time = "9am" },
        new { title = "Bins", due_date = "2030-01-01", recurrence_unit = "fortnight" },
        new { title = "Bins", due_date = "2030-01-01", recurrence_unit = "day", recurrence_interval = 0 },
        new { title = "Bins", due_date = "2030-01-01", recurrence_unit = "day", recurrence_interval = -1 },
        new { title = "Bins", recurrence_unit = "day" },
        new { title = "Bins", due_date = "2030-01-01", recurrence_unit = "week", recurrence_days = "Mon,Funday" },
        new { title = "Bins", due_date = "2030-01-01", recurrence_unit = "day", recurrence_days = "Mon" },
        new { title = "Bins", due_date = "2030-01-01", recurrence_unit = "day", recurrence_end_date = "2029-12-01" },
        new { title = "Trip", due_date = "2030-01-01", recurrence_unit = "day", is_countdown = true },
    };

    [Theory]
    [MemberData(nameof(RejectedTasks))]
    public async Task Post_InvalidTask_Returns400AndCreatesNothing(object body)
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/tasks", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<List<TaskItem>>("/tasks", ServiceDefaults.HearthJson.SnakeCaseLower))!);
    }

    [Fact]
    public async Task Post_WeeklyOnChosenDays_CreatesTheSeries()
    {
        var client = _factory.CreateClient();
        var start = DateTime.UtcNow.Date.AddDays(1);

        var response = await client.PostAsJsonAsync("/tasks", new
        {
            title = "Bins",
            due_date = start,
            due_time = "07:30",
            recurrence_unit = "week",
            recurrence_interval = 1,
            recurrence_days = "Mon,Thu",
            recurrence_end_date = start.AddDays(14),
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var tasks = await client.GetFromJsonAsync<List<TaskItem>>("/tasks", ServiceDefaults.HearthJson.SnakeCaseLower);
        Assert.Equal(5, tasks!.Count);
    }

    [Fact]
    public async Task Put_MalformedDueTime_Returns400()
    {
        var client = _factory.CreateClient();
        var task = await CreateAsync(client);

        var response = await client.PutAsJsonAsync($"/tasks/{task.Id}", new { done = false, title = "Water plants", due_time = "25:00" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Preflight_FromAnotherOrigin_IsNotAllowed()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/tasks/1");
        request.Headers.Add("Origin", "https://evil.example");
        request.Headers.Add("Access-Control-Request-Method", "DELETE");

        var response = await _factory.CreateClient().SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Get_FromAnotherOrigin_HasNoAllowOriginHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/tasks");
        request.Headers.Add("Origin", "https://evil.example");

        var response = await _factory.CreateClient().SendAsync(request);

        response.EnsureSuccessStatusCode();
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task GetHealth_DatabaseReadable_ReturnsHealthyJson()
    {
        var response = await _factory.CreateClient().GetAsync("/tasks/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<ServiceDefaults.HealthReportJson>(ServiceDefaults.HearthJson.SnakeCaseLower);
        Assert.Equal("healthy", report!.Status);
        Assert.Contains(report.Checks, c => c.Name == "database" && c.Status == "healthy");
    }
}
