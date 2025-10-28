namespace FamilyWall.Core.Abstractions;

/// <summary>
/// Manages authentication tokens for external services.
/// </summary>
public interface ITokenStore
{
    Task<string?> GetTokenAsync(string provider, string scope, CancellationToken cancellationToken = default);
    Task SetTokenAsync(string provider, string scope, string token, CancellationToken cancellationToken = default);
    Task RemoveTokenAsync(string provider, string scope, CancellationToken cancellationToken = default);
}
