using System.Net.Http.Headers;
using System.Text.Json;

namespace GodotHub.Lib;

public static class GodotApi
{
    private static readonly WeakReference<HttpClient?> Client = new(null);
    private const string GitHubReleasesUrl = "https://api.github.com/repos/godotengine/godot-builds/releases";
    private const string Token = "TOKEN_HERE";

    public static async Task<List<GodotRelease>> GetGitHubReleasesAsync()
    {
        var releases = new List<GodotRelease>();
        var page = 1;
        while (true)
        {
            var url = GitHubReleasesUrl + "?page=" + page;
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            var response = await GetHttpClient().SendAsync(request);
            response.EnsureSuccessStatusCode();
            
            await using var responseStream = await response.Content.ReadAsStreamAsync();
            var pageReleases = await JsonSerializer.DeserializeAsync<List<GodotRelease>>(responseStream);
            if (pageReleases == null || pageReleases.Count == 0)
                break;
            
            releases.AddRange(pageReleases);
            page++;
        }
        return releases;
    }

    private static HttpClient GetHttpClient()
    {
        if (Client.TryGetTarget(out var client))
            return client;

        client = new HttpClient(new HttpClientHandler { MaxConnectionsPerServer = 5 })
        {
            Timeout = TimeSpan.FromSeconds(30),
            DefaultRequestHeaders = { UserAgent = { new ProductInfoHeaderValue(new ProductHeaderValue("GodotHub")) }}
        };
        
        Client.SetTarget(client);
        return client;
    }
    
    public static void ResetHttpClient()
    {
        if (!Client.TryGetTarget(out var client)) return;
        
        client.Dispose();
        Client.SetTarget(null);
    }
}