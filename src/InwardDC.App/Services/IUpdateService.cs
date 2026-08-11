namespace InwardDC.App.Services;

/// <summary>Result of an update check.</summary>
public sealed record UpdateInfo(string Version, string Url, string? Notes);

/// <summary>
/// Checks GitHub for a newer build, downloads the single-file exe and applies it.
/// The app is versioned via <c>latest.json</c> in the repository root; the manifest
/// carries the newest version number and the Release download URL.
/// </summary>
public interface IUpdateService
{
    string CurrentVersion { get; }

    /// <summary>Fetches the latest manifest. Returns <see langword="null"/> when the app
    /// is already current or the check fails (offline etc.).</summary>
    Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken ct = default);

    /// <summary>Downloads the update exe into the Updates folder. Returns its path.</summary>
    Task<string> DownloadUpdateAsync(UpdateInfo info, CancellationToken ct = default);

    /// <summary>Replaces the running exe with the downloaded one after a short delay and
    /// restarts the app. The caller should shut the app down immediately after a true return.</summary>
    bool ApplyUpdate(string downloadedPath);
}
