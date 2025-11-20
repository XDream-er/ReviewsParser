using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text;
using System.Threading.Tasks;

public class ApiClient
{
    private readonly HttpClient _client;
    public readonly string _baseUrl = "https://localhost:7182";

    public ApiClient()
    {
        var handler = new HttpClientHandler
        {
            UseProxy = false,
            Proxy = null,
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        };
        _client = new HttpClient(handler);
    }

    public async Task<ParsingTask?> GetTaskAsync(string agentId)
    {
        var content = new StringContent($"\"{agentId}\"", Encoding.UTF8, "application/json");
        var response = await _client.PostAsync($"{_baseUrl}/api/agents/get-task", content);
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent) return null;
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<ParsingTask>(json);
    }

    public async Task SubmitResultsAsync(List<ParsedReview> reviews)
    {
        var content = new StringContent(JsonConvert.SerializeObject(reviews), Encoding.UTF8, "application/json");
        await _client.PostAsync($"{_baseUrl}/api/agents/submit-results", content);
    }

    public async Task UpdateProgressAsync(int taskId, string progress, int processedCount)
    {
        var update = new { TaskId = taskId, ProgressIdentifier = progress, ItemsProcessedCount = processedCount };
        var response = await _client.PostAsJsonAsync($"{_baseUrl}/api/agents/update-progress", update);
        response.EnsureSuccessStatusCode();
    }

    public async Task<TaskStatus> GetTaskStatusAsync(int taskId)
    {
        var response = await _client.GetAsync($"{_baseUrl}/api/agents/task-status/{taskId}");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<TaskStatus>(json);
    }

    public async Task CompleteTaskAsync(int taskId)
    {
        var response = await _client.PutAsync($"{_baseUrl}/api/tasks/{taskId}/complete", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task FailTaskAsync(int taskId)
    {
        var response = await _client.PutAsync($"{_baseUrl}/api/tasks/{taskId}/fail", null);
        response.EnsureSuccessStatusCode();
    }
}
public enum TaskStatus { Pending, Running, Paused, Completed, Failed }

public class ParsingTask
{
    public int Id { get; set; }
    public string TargetSite { get; set; }
    public string? ProgressIdentifier { get; set; }
    public string? ProxyAddress { get; set; }
    public string? ProxyUsername { get; set; }
    public string? ProxyPassword { get; set; }
}

public class ParsedReview
{
    public int ParsingTaskId { get; set; }
    public string AgentId { get; set; }
    public string Car { get; set; }
    public string Author { get; set; }
    public string Rating { get; set; }
    public string ReviewText { get; set; }
    public string Url { get; set; }
}