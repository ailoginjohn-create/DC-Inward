using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using InwardDC.Infrastructure.Common;

namespace InwardDC.App.Services;

public sealed class UpdateService : IUpdateService
{
    private const string ManifestUrl =
        "https://raw.githubusercontent.com/ailoginjohn-create/DC-Inward/main/latest.json";

    private static readonly HttpClient _http = CreateClient();

    private readonly AppPaths _paths;

    public UpdateService(AppPaths paths)
    {
        _paths = paths;
    }

    public string CurrentVersion { get; } = ReadCurrentVersion();

    public async Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await _http.GetAsync(ManifestUrl, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var version = root.TryGetProperty("version", out var v) ? v.GetString() : null;
            var url = root.TryGetProperty("url", out var u) ? u.GetString() : null;
            if (string.IsNullOrWhiteSpace(version) || string.IsNullOrWhiteSpace(url))
                return null;

            var notes = root.TryGetProperty("notes", out var n) ? n.GetString() : null;

            var current = new Version(TrimVersion(CurrentVersion));
            var remote = new Version(TrimVersion(version));
            if (remote <= current)
                return null;

            return new UpdateInfo(version, url, string.IsNullOrWhiteSpace(notes) ? null : notes);
        }
        catch
        {
            return null;
        }
    }

    public async Task<string> DownloadUpdateAsync(UpdateInfo info, CancellationToken ct = default)
    {
        var updatesDir = Path.Combine(_paths.DataDirectory, "Updates");
        Directory.CreateDirectory(updatesDir);

        var fileName = $"InwardDC-{TrimVersion(info.Version)}.exe";
        var target = Path.Combine(updatesDir, fileName);

        using var response = await _http.GetAsync(info.Url, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        await using var file = File.Create(target);
        await stream.CopyToAsync(file, ct).ConfigureAwait(false);

        if (file.Length < 1_000_000)
            throw new InvalidOperationException("Downloaded file is unexpectedly small and was rejected.");

        return target;
    }

    public bool ApplyUpdate(string downloadedPath)
    {
        var currentExe = Environment.ProcessPath;
        if (string.IsNullOrEmpty(currentExe) || !File.Exists(currentExe))
            return false;

        var scriptDir = Path.Combine(_paths.DataDirectory, "Temp");
        Directory.CreateDirectory(scriptDir);
        var script = Path.Combine(scriptDir, "apply-update.cmd");

        // Give the exiting app a moment to release the file, then swap and restart.
        var lines = new[]
        {
            "@echo off",
            "ping -n 4 127.0.0.1 > nul",
            $"move /y \"{downloadedPath}\" \"{currentExe}\"",
            "if exist \"" + downloadedPath + "\" copy /y \"" + downloadedPath + "\" \"" + currentExe + "\"",
            $"start \"\" \"{currentExe}\"",
            "del \"%~f0\""
        };
        File.WriteAllLines(script, lines);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"\"{script}\"\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = scriptDir
            };
            Process.Start(psi);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("InwardDC-Updater/1.0");
        return client;
    }

    private static string ReadCurrentVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var info = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return TrimVersion(info ?? assembly.GetName().Version?.ToString() ?? "1.0.0");
    }

    private static string TrimVersion(string version)
    {
        var v = version.Split('+')[0].TrimStart('v', 'V');
        var end = v.IndexOfAny(new[] { '-', ' ' });
        return end > 0 ? v[..end] : v;
    }
}
