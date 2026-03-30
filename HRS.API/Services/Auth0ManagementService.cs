using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HRS.API.Services.Helpers;
using HRS.API.Services.Interfaces;
using HRS.Shared.Core.Enums;
using Microsoft.Extensions.Options;

namespace HRS.API.Services;

public class Auth0ManagementService : IAuth0ManagementService
{
    private readonly HttpClient _httpClient;
    private readonly Auth0ManagementOptions _options;
    private readonly ILogger<Auth0ManagementService> _logger;

    private string? _cachedAccessToken;
    private DateTimeOffset _tokenExpiresAt = DateTimeOffset.MinValue;

    public Auth0ManagementService(
        IHttpClientFactory httpClientFactory,
        IOptions<Auth0ManagementOptions> options,
        ILogger<Auth0ManagementService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("Auth0ManagementApi");
        _options = options.Value;
        _logger = logger;
    }

    public async Task SyncUserRoleAsync(string auth0UserId, UserRole role, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(auth0UserId))
            throw new ArgumentException("Auth0 user id is required", nameof(auth0UserId));

        ValidateOptions();

        var targetRoleId = GetRoleId(role);
        var roleIdsToRemove = GetConfiguredRoleIds().Where(roleId => roleId != targetRoleId).ToList();

        await AuthenticateClientAsync(cancellationToken);

        var encodedUserId = Uri.EscapeDataString(auth0UserId);

        if (roleIdsToRemove.Count > 0)
        {
            using var removeRequest = new HttpRequestMessage(HttpMethod.Delete, $"users/{encodedUserId}/roles")
            {
                Content = JsonContent.Create(new { roles = roleIdsToRemove })
            };

            var removeResponse = await _httpClient.SendAsync(
              removeRequest,
                cancellationToken);

            if (!removeResponse.IsSuccessStatusCode)
            {
                var body = await removeResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Failed removing Auth0 roles for user {Auth0UserId}. Status={StatusCode}. Body={Body}",
                    auth0UserId,
                    (int)removeResponse.StatusCode,
                    body);
                removeResponse.EnsureSuccessStatusCode();
            }
        }

        var assignResponse = await _httpClient.PostAsJsonAsync(
            $"users/{encodedUserId}/roles",
            new { roles = new[] { targetRoleId } },
            cancellationToken);

        if (!assignResponse.IsSuccessStatusCode)
        {
            var body = await assignResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Failed assigning Auth0 role for user {Auth0UserId}. Status={StatusCode}. Body={Body}",
                auth0UserId,
                (int)assignResponse.StatusCode,
                body);
            assignResponse.EnsureSuccessStatusCode();
        }
    }

    public async Task SyncUserMetadataAsync(string auth0UserId, int userId, int? storeId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(auth0UserId))
            throw new ArgumentException("Auth0 user id is required", nameof(auth0UserId));

        if (userId <= 0)
            throw new ArgumentOutOfRangeException(nameof(userId), "User id must be greater than zero");

        ValidateOptions();
        await AuthenticateClientAsync(cancellationToken);

        _logger.LogInformation(
          "Syncing Auth0 app_metadata for user {Auth0UserId} with userId={UserId} storeId={StoreId}",
          auth0UserId,
          userId,
          storeId);

        var encodedUserId = Uri.EscapeDataString(auth0UserId);
        var appMetadata = new Dictionary<string, object>
        {
            ["userId"] = userId
        };

        if (storeId.HasValue && storeId.Value > 0)
        {
            appMetadata["storeId"] = storeId.Value;
        }

        var response = await _httpClient.PatchAsJsonAsync(
            $"users/{encodedUserId}",
            new { app_metadata = appMetadata },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Failed syncing Auth0 app_metadata for user {Auth0UserId}. Status={StatusCode}. Body={Body}",
                auth0UserId,
                (int)response.StatusCode,
                body);
            response.EnsureSuccessStatusCode();
        }

        _logger.LogInformation(
            "Auth0 app_metadata synced for user {Auth0UserId} with userId={UserId} storeId={StoreId}",
            auth0UserId,
            userId,
            storeId);
    }

    private async Task AuthenticateClientAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_cachedAccessToken) && DateTimeOffset.UtcNow < _tokenExpiresAt)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _cachedAccessToken);
            return;
        }

        var tokenResponse = await _httpClient.PostAsJsonAsync($"https://{_options.Domain}/oauth/token",
            new
            {
                client_id = _options.ClientId,
                client_secret = _options.ClientSecret,
                audience = _options.Audience,
                grant_type = "client_credentials"
            },
            cancellationToken);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            var body = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Auth0 management token request failed. Status={StatusCode}. Body={Body}", (int)tokenResponse.StatusCode, body);
            tokenResponse.EnsureSuccessStatusCode();
        }

        await using var tokenStream = await tokenResponse.Content.ReadAsStreamAsync(cancellationToken);
        using var tokenJson = await JsonDocument.ParseAsync(tokenStream, cancellationToken: cancellationToken);

        if (!tokenJson.RootElement.TryGetProperty("access_token", out var accessTokenElement) ||
          !tokenJson.RootElement.TryGetProperty("expires_in", out var expiresInElement))
        {
            throw new InvalidOperationException("Auth0 token response missing required fields");
        }

        var accessToken = accessTokenElement.GetString();
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new InvalidOperationException("Auth0 token response has empty access_token");

        var expiresIn = expiresInElement.GetInt32();

        _cachedAccessToken = accessToken;
        _tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(expiresIn - 60, 30));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _cachedAccessToken);
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.Domain) ||
            string.IsNullOrWhiteSpace(_options.ClientId) ||
            string.IsNullOrWhiteSpace(_options.ClientSecret) ||
            string.IsNullOrWhiteSpace(_options.Audience) ||
            string.IsNullOrWhiteSpace(_options.CustomerRoleId) ||
          string.IsNullOrWhiteSpace(_options.OwnerRoleId) ||
            string.IsNullOrWhiteSpace(_options.EmployeeRoleId) ||
            string.IsNullOrWhiteSpace(_options.ManagerRoleId) ||
            string.IsNullOrWhiteSpace(_options.AdminRoleId))
        {
            throw new InvalidOperationException("Auth0Management configuration is incomplete");
        }
    }

    private string GetRoleId(UserRole role)
    {
        return role switch
        {
            UserRole.Customer => _options.CustomerRoleId,
            UserRole.Owner => _options.OwnerRoleId,
            UserRole.Employee => _options.EmployeeRoleId,
            UserRole.Manager => _options.ManagerRoleId,
            UserRole.Admin => _options.AdminRoleId,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported role")
        };
    }

    private List<string> GetConfiguredRoleIds()
    {
        return new[]
            {
                _options.CustomerRoleId,
          _options.OwnerRoleId,
                _options.EmployeeRoleId,
                _options.ManagerRoleId,
                _options.AdminRoleId
            }
            .Where(roleId => !string.IsNullOrWhiteSpace(roleId))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

}
