using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace KanbanBoard.Services;

/// <summary>更新信息（来自更新源 JSON）。</summary>
public class UpdateInfo
{
    public string Version { get; set; } = "";
    public string Url { get; set; } = "";
    public string Notes { get; set; } = "";
}

/// <summary>更新检查：比对版本号，返回是否有新版本。</summary>
public static class UpdateService
{
    public static string CurrentVersion { get; } =
        typeof(UpdateService).Assembly.GetName().Version?.ToString(3) ?? "0.1.0";

    public static async Task<(bool HasUpdate, UpdateInfo? Info, string? Error)> CheckAsync(string feedUrl)
    {
        if (string.IsNullOrWhiteSpace(feedUrl))
            return (false, null, "no-feed");
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("KanbanBoard");
            var json = await http.GetStringAsync(feedUrl);
            var info = JsonSerializer.Deserialize<UpdateInfo>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (info == null) return (false, null, null);
            var has = CompareVersions(info.Version, CurrentVersion) > 0;
            return (has, info, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    private static int CompareVersions(string a, string b)
    {
        if (Version.TryParse(a, out var va) && Version.TryParse(b, out var vb))
            return va.CompareTo(vb);
        return string.CompareOrdinal(a, b);
    }
}
