using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace LunaChat.Services;

/// <summary>
/// Stores provider API keys in the OS-native credential vault. Secrets never
/// touch luna-chat's JSON — only the vault holds them.
/// </summary>
public interface ISecretStore
{
    Task SetAsync(string account, string secret);
    Task<string?> GetAsync(string account);
    Task DeleteAsync(string account);
    /// <summary>Human label for the backing store (shown in Settings).</summary>
    string BackendName { get; }
}

public static class SecretStoreFactory
{
    private static ISecretStore? _instance;

    public static ISecretStore Instance => _instance ??= Create();

    private static ISecretStore Create()
    {
        if (OperatingSystem.IsMacOS()) return new KeychainSecretStore();
        if (OperatingSystem.IsWindows()) return new WindowsSecretStore();
        return new FileFallbackSecretStore();
    }
}

/// <summary>Shared service identifier under which all keys are filed.</summary>
internal static class SecretConst
{
    public const string Service = "LunaChat";
}

/// <summary>macOS: generic-password items in the user's login Keychain via /usr/bin/security.</summary>
[SupportedOSPlatform("macos")]
public sealed class KeychainSecretStore : ISecretStore
{
    public string BackendName => "macOS Keychain";

    public Task SetAsync(string account, string secret)
    {
        // -U updates the item if it already exists.
        Run(new[] { "add-generic-password", "-a", account, "-s", SecretConst.Service, "-w", secret, "-U" });
        return Task.CompletedTask;
    }

    public Task<string?> GetAsync(string account)
    {
        var (code, stdout, _) = Run(new[] { "find-generic-password", "-a", account, "-s", SecretConst.Service, "-w" });
        if (code != 0) return Task.FromResult<string?>(null);
        var value = stdout.TrimEnd('\n', '\r');
        return Task.FromResult<string?>(string.IsNullOrEmpty(value) ? null : value);
    }

    public Task DeleteAsync(string account)
    {
        Run(new[] { "delete-generic-password", "-a", account, "-s", SecretConst.Service });
        return Task.CompletedTask;
    }

    private static (int code, string stdout, string stderr) Run(string[] args)
    {
        var psi = new ProcessStartInfo("/usr/bin/security")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        try
        {
            using var p = Process.Start(psi)!;
            var so = p.StandardOutput.ReadToEnd();
            var se = p.StandardError.ReadToEnd();
            p.WaitForExit(10_000);
            return (p.ExitCode, so, se);
        }
        catch
        {
            return (-1, "", "security invocation failed");
        }
    }
}

/// <summary>Windows: generic credentials in Credential Manager via advapi32.</summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsSecretStore : ISecretStore
{
    public string BackendName => "Windows Credential Manager";

    private static string Target(string account) => $"{SecretConst.Service}:{account}";

    public Task SetAsync(string account, string secret)
    {
        var blob = Encoding.Unicode.GetBytes(secret);
        var handle = GCHandle.Alloc(blob, GCHandleType.Pinned);
        try
        {
            var cred = new CREDENTIAL
            {
                Type = CRED_TYPE_GENERIC,
                TargetName = Target(account),
                CredentialBlob = handle.AddrOfPinnedObject(),
                CredentialBlobSize = (uint)blob.Length,
                Persist = CRED_PERSIST_LOCAL_MACHINE,
                UserName = account
            };
            if (!CredWrite(ref cred, 0))
                throw new InvalidOperationException($"CredWrite failed ({Marshal.GetLastWin32Error()})");
        }
        finally { handle.Free(); }
        return Task.CompletedTask;
    }

    public Task<string?> GetAsync(string account)
    {
        if (!CredRead(Target(account), CRED_TYPE_GENERIC, 0, out var ptr))
            return Task.FromResult<string?>(null);
        try
        {
            var cred = Marshal.PtrToStructure<CREDENTIAL>(ptr);
            if (cred.CredentialBlobSize == 0 || cred.CredentialBlob == IntPtr.Zero)
                return Task.FromResult<string?>(null);
            var bytes = new byte[cred.CredentialBlobSize];
            Marshal.Copy(cred.CredentialBlob, bytes, 0, bytes.Length);
            return Task.FromResult<string?>(Encoding.Unicode.GetString(bytes));
        }
        finally { CredFree(ptr); }
    }

    public Task DeleteAsync(string account)
    {
        CredDelete(Target(account), CRED_TYPE_GENERIC, 0);
        return Task.CompletedTask;
    }

    private const int CRED_TYPE_GENERIC = 1;
    private const int CRED_PERSIST_LOCAL_MACHINE = 2;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public uint Flags;
        public uint Type;
        public string TargetName;
        public string? Comment;
        public long LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string? TargetAlias;
        public string UserName;
    }

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredWrite(ref CREDENTIAL credential, uint flags);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CredReadW")]
    private static extern bool CredRead(string target, int type, int reservedFlag, out IntPtr credentialPtr);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CredDeleteW")]
    private static extern bool CredDelete(string target, int type, int flags);

    [DllImport("advapi32.dll")]
    private static extern void CredFree(IntPtr buffer);
}

/// <summary>
/// Last-resort store for platforms without a supported native vault (e.g. Linux).
/// Keeps keys only for the running session; nothing persists to disk.
/// </summary>
public sealed class FileFallbackSecretStore : ISecretStore
{
    private readonly Dictionary<string, string> _mem = new();
    public string BackendName => "in-memory (session only)";

    public Task SetAsync(string account, string secret) { _mem[account] = secret; return Task.CompletedTask; }
    public Task<string?> GetAsync(string account) =>
        Task.FromResult(_mem.TryGetValue(account, out var v) ? v : null);
    public Task DeleteAsync(string account) { _mem.Remove(account); return Task.CompletedTask; }
}
