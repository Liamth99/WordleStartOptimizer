using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Nodes;
using Spectre.Console;

namespace WordleStartOptimizer;

public static class VersionChecker
{
    [Conditional("DEBUG")]
    public static void CheckVersionAsync()
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("WordleStartOptimizer-VersionCheck");

            var json          = JsonNode.Parse(httpClient.GetStringAsync("https://api.github.com/repos/Liamth99/WordleStartOptimizer/releases/latest").Result);
            var latestVersion = json?["name"]?.ToString();

            var currentVersion = $"v{Assembly.GetAssembly(typeof(VersionChecker))!.GetName().Version!.ToString(3)}";

            if (!string.IsNullOrEmpty(latestVersion) && currentVersion != latestVersion)
            {
                AnsiConsole.MarkupLine($"New version available to download at {json!["html_url"]}");
            }
        }
        catch
        {
            // Non-critical
        }
    }
}