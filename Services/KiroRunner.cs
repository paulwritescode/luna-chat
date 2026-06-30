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
/// Spawns and streams output from the kiro-cli binary.
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
    /// Runs `kiro --version`. Returns trimmed output, or null on failure.
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
    /// Spawns kiro with the given prompt file and output dir, streaming stdout
    /// lines as they arrive. Throws <see cref="KiroProcessException"/> on non-zero exit.
    /// </summary>
    public async IAsyncEnumerable<string> RunAsync(
        string promptFilePath,
        string outputDir,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDir);

        var psi = new ProcessStartInfo
        {
            FileName = _binary,
            Arguments = $"run --prompt \"{promptFilePath}\" --output \"{outputDir}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        };

        using var proc = new Process { StartInfo = psi };

        var stderr = new System.Text.StringBuilder();
        proc.Start();

        // Drain stderr in the background so the buffer never blocks.
        var errTask = Task.Run(async () =>
        {
            string? line;
            while ((line = await proc.StandardError.ReadLineAsync(ct)) != null)
                stderr.AppendLine(line);
        }, ct);

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

    /// <summary>
    /// Moves all files produced in <paramref name="tempOutputDir"/> into
    /// <paramref name="destFolder"/>, preserving relative structure.
    /// Returns the destination paths.
    /// </summary>
    public static List<string> CollectOutputs(string tempOutputDir, string destFolder)
    {
        var moved = new List<string>();
        if (!Directory.Exists(tempOutputDir))
            return moved;

        Directory.CreateDirectory(destFolder);

        foreach (var file in Directory.EnumerateFiles(tempOutputDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(tempOutputDir, file);
            var dest = Path.Combine(destFolder, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Move(file, dest, overwrite: true);
            moved.Add(dest);
        }

        return moved;
    }
}
