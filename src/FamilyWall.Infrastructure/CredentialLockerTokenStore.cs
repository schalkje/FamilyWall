using FamilyWall.Core.Abstractions;
using System.Security.Cryptography;
using System.Text;

#if WINDOWS
using Windows.Security.Credentials;
#endif

namespace FamilyWall.Infrastructure;

/// <summary>
/// Token store using Windows Credential Locker (DPAPI) for secure token storage.
/// Stub implementation for cross-platform; Windows-specific code will go in the App layer.
/// </summary>
public class CredentialLockerTokenStore : ITokenStore
{
    private readonly string _storePath;

    public CredentialLockerTokenStore(string appDataPath)
    {
        _storePath = Path.Combine(appDataPath, "Tokens");
        if (!Directory.Exists(_storePath))
        {
            Directory.CreateDirectory(_storePath);
        }
    }

    public Task<string?> GetTokenAsync(string provider, string scope, CancellationToken cancellationToken = default)
    {
        var filePath = GetTokenFilePath(provider, scope);
        if (!File.Exists(filePath))
        {
            return Task.FromResult<string?>(null);
        }

        try
        {
            var encryptedBytes = File.ReadAllBytes(filePath);
#if WINDOWS
            // Use Windows DPAPI when available
            var decryptedBytes = System.Security.Cryptography.ProtectedData.Unprotect(
                encryptedBytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
            var token = Encoding.UTF8.GetString(decryptedBytes);
#else
            // Fallback: base64 encoding (not secure, for dev/testing only)
            var token = Encoding.UTF8.GetString(encryptedBytes);
#endif
            return Task.FromResult<string?>(token);
        }
        catch
        {
            return Task.FromResult<string?>(null);
        }
    }

    public Task SetTokenAsync(string provider, string scope, string token, CancellationToken cancellationToken = default)
    {
        var filePath = GetTokenFilePath(provider, scope);
        var tokenBytes = Encoding.UTF8.GetBytes(token);
#if WINDOWS
        // Use Windows DPAPI when available
        var encryptedBytes = System.Security.Cryptography.ProtectedData.Protect(
            tokenBytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
#else
        // Fallback: base64 encoding (not secure, for dev/testing only)
        var encryptedBytes = tokenBytes;
#endif
        File.WriteAllBytes(filePath, encryptedBytes);
        return Task.CompletedTask;
    }

    public Task RemoveTokenAsync(string provider, string scope, CancellationToken cancellationToken = default)
    {
        var filePath = GetTokenFilePath(provider, scope);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        return Task.CompletedTask;
    }

    private string GetTokenFilePath(string provider, string scope)
    {
        var key = $"{provider}_{scope}".Replace("/", "_").Replace(":", "_");
        return Path.Combine(_storePath, $"{key}.token");
    }
}
