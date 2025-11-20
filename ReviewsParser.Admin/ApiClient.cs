using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Newtonsoft.Json;
public enum TaskStatus { Pending, Running, Paused, Completed, Failed }
public class ParsingTask
{
    public int Id { get; set; }
    public string TargetSite { get; set; }
    public TaskStatus Status { get; set; }
    public string? ProgressIdentifier { get; set; }
    public int ItemsProcessed { get; set; }
    public string? AssignedAgentId { get; set; }
    public string? ProxyAddress { get; set; }
    public string? ProxyUsername { get; set; }
    public string? ProxyPassword { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
public class ParsedReview
{
    public int Id { get; set; }
    public int ParsingTaskId { get; set; }
    public string AgentId { get; set; }
    public string Car { get; set; }
    public string Author { get; set; }
    public string Rating { get; set; }
    public string ReviewText { get; set; }
    public string Url { get; set; }
}
public class ApiClient
{
    private readonly HttpClient _client;
    private readonly string _baseUrl = "https://localhost:7182";

    public ApiClient()
    {
        var handler = new HttpClientHandler
        {
            UseProxy = false,
            Proxy = null
        };
        _client = new HttpClient(handler);
    }
    public async Task<List<string>> GetAvailableSitesAsync()
    {
        return await _client.GetFromJsonAsync<List<string>>($"{_baseUrl}/api/tasks/available-sites") ?? new List<string>();
    }

    public async Task<List<ParsingTask>> GetAllTasksAsync()
    {
        return await _client.GetFromJsonAsync<List<ParsingTask>>($"{_baseUrl}/api/tasks") ?? new List<ParsingTask>();
    }

    public async Task CreateTaskAsync(string targetSite, string? proxy, string? user, string? pass)
    {
        var dto = new
        {
            TargetSite = targetSite,
            ProxyAddress = proxy,
            ProxyUsername = user,
            ProxyPassword = pass
        };

        var content = new StringContent(JsonConvert.SerializeObject(dto), Encoding.UTF8, "application/json");
        var response = await _client.PostAsync($"{_baseUrl}/api/tasks", content);
        response.EnsureSuccessStatusCode();
    }

    public async Task PauseTaskAsync(int taskId)
    {
        var response = await _client.PutAsync($"{_baseUrl}/api/tasks/{taskId}/pause", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task ResumeTaskAsync(int taskId, string? proxy, string? user, string? pass)
    {
        var dto = new
        {
            ProxyAddress = proxy,
            ProxyUsername = user,
            ProxyPassword = pass
        };

        var content = new StringContent(JsonConvert.SerializeObject(dto), Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"{_baseUrl}/api/tasks/{taskId}/resume", content);
        response.EnsureSuccessStatusCode();
    }
    public async Task<List<ParsedReview>> GetTaskResultsAsync(int taskId)
    {
        return await _client.GetFromJsonAsync<List<ParsedReview>>($"{_baseUrl}/api/tasks/{taskId}/results") ?? new List<ParsedReview>();
    }
}