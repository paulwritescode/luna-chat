using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LunaChat.Services;

/// <summary>
/// Thrown when the kiro subprocess exits with a non-zero code.
/// </summary>
public class KiroProcessException : Exception
{
    public int ExitCode { get; }
    public string StdErr { get; }

    public KiroProcessException(int exitCode, string stderr)
        : base($"kiro exited with code {exitCode}: {stderr}")
    {
        ExitCode = exitCode;
        StdErr = stderr;
    }
}

/// <summary>
/// Spawns and streams output from the kiro-cli binary using headless mode
/// (<c>kiro-cli chat --no-interactive --trust-all-tools "&lt;prompt&gt;"</c>).
/// Additional context is piped via stdin.
/// </summary>
public class KiroRunner
{
    private readonly string _binary;

    public KiroRunner(string binary)
    {
        _binary = binary;
    }

    public string BinaryPath => _binary;

    /// <summary>
    /// Runs <c>kiro --version</c>. Returns trimmed output, or null on failure.
    /// </summary>
    public async Task<string?> GetVersionAsync(CancellationToken ct = default)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = _binary,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc == null) return null;

            var output = await proc.StandardOutput.ReadToEndAsync(ct);
            await proc.WaitForExitAsync(ct);
            return proc.ExitCode == 0 ? output.Trim() : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Runs a prompt headlessly, streaming stdout lines as they arrive.
    /// <paramref name="prompt"/> is passed as the chat argument; <paramref name="stdinContext"/>
    /// (skills + history + attachments) is piped through stdin.
    /// Throws <see cref="KiroProcessException"/> on a non-zero exit.
    /// </summary>
    public async IAsyncEnumerable<string> RunAsync(
        string prompt,
        string stdinContext,
        string workingDir,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(workingDir))
            Directory.CreateDirectory(workingDir);

        var psi = new ProcessStartInfo
        {
            FileName = _binary,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = string.IsNullOrWhiteSpace(workingDir)
                ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                : workingDir
        };
        psi.ArgumentList.Add("chat");
        psi.ArgumentList.Add("--no-interactive");
        psi.ArgumentList.Add("--trust-all-tools");
        psi.ArgumentList.Add(prompt);

        using var proc = new Process { StartInfo = psi };

        var stderr = new System.Text.StringBuilder();
        proc.Start();

        // Drain stderr in the background.
        var errTask = Task.Run(async () =>
        {
            string? line;
            while ((line = await proc.StandardError.ReadLineAsync(ct)) != null)
                stderr.AppendLine(line);
        }, ct);

        // Pipe context via stdin, then close it so kiro proceeds.
        try
        {
            if (!string.IsNullOrEmpty(stdinContext))
                await proc.StandardInput.WriteAsync(stdinContext.AsMemory(), ct);
            proc.StandardInput.Close();
        }
        catch
        {
            // If the process doesn't accept stdin, continue regardless.
        }

        var reader = proc.StandardOutput;
        string? outLine;
        while ((outLine = await reader.ReadLineAsync(ct)) != null)
        {
            yield return outLine;
        }

        await proc.WaitForExitAsync(ct);
        try { await errTask; } catch { /* best effort */ }

        if (proc.ExitCode != 0)
            throw new KiroProcessException(proc.ExitCode, stderr.ToString().Trim());
    }

    /// <summary>Snapshot the set of files currently in a directory (recursive).</summary>
    public static HashSet<string> SnapshotFiles(string dir)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) return set;
        foreach (var f in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
            set.Add(f);
        return set;
    }

    /// <summary>Return files present now that were not in the prior snapshot.</summary>
    public static List<string> NewFilesSince(string dir, HashSet<string> before)
    {
        var added = new List<string>();
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) return added;
        foreach (var f in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
            if (!before.Contains(f))
                added.Add(f);
        return added;
    }
}
