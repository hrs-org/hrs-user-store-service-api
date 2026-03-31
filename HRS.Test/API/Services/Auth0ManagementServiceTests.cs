using System.Net;
using System.Text.Json;
using NSubstitute;
using HRS.API.Services;
using HRS.API.Services.Helpers;
using HRS.Shared.Core.Enums;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace HRS.Test.API.Services;

public class Auth0ManagementServiceTests
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<Auth0ManagementOptions> _options;
    private readonly ILogger<Auth0ManagementService> _logger;
    private readonly Auth0ManagementService _service;
    private readonly FakeAuth0HttpMessageHandler _handler;

    private const string Domain = "test.auth0.com";
    private const string Audience = "https://api.test.com";
    private const string ClientId = "test-client-id";
    private const string ClientSecret = "test-client-secret";
    private const string CustomerRoleId = "role_customer";
    private const string OwnerRoleId = "role_owner";
    private const string EmployeeRoleId = "role_employee";
    private const string ManagerRoleId = "role_manager";
    private const string AdminRoleId = "role_admin";

    public Auth0ManagementServiceTests()
    {
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _logger = Substitute.For<ILogger<Auth0ManagementService>>();

        var optionsValue = new Auth0ManagementOptions
        {
            Domain = Domain,
            Audience = Audience,
            ClientId = ClientId,
            ClientSecret = ClientSecret,
            CustomerRoleId = CustomerRoleId,
            OwnerRoleId = OwnerRoleId,
            EmployeeRoleId = EmployeeRoleId,
            ManagerRoleId = ManagerRoleId,
            AdminRoleId = AdminRoleId
        };

        _options = Options.Create(optionsValue);
        _handler = new FakeAuth0HttpMessageHandler();
        var httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri($"https://{Domain}/api/v2/")
        };

        _httpClientFactory.CreateClient("Auth0ManagementApi").Returns(httpClient);
        _service = new Auth0ManagementService(_httpClientFactory, _options, _logger);
    }

    [Fact]
    public async Task SyncUserRoleAsync_ThrowsArgumentException_WhenAuth0UserIdIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SyncUserRoleAsync(null!, UserRole.Customer));
    }

    [Fact]
    public async Task SyncUserRoleAsync_ThrowsArgumentException_WhenAuth0UserIdIsEmpty()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SyncUserRoleAsync("", UserRole.Customer));
    }

    [Fact]
    public async Task SyncUserRoleAsync_ThrowsArgumentException_WhenAuth0UserIdIsWhitespace()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SyncUserRoleAsync("   ", UserRole.Customer));
    }

    [Fact]
    public async Task SyncUserRoleAsync_RemovesAllExceptTargetRole_AndAssignsTargetRole_ForCustomer()
    {
        // Arrange
        const string auth0UserId = "auth0|user123";
        _handler.SetTokenResponse(Domain, "test-token", 3600);
        _handler.SetRoleRemovalResponse(auth0UserId, true);
        _handler.SetRoleAssignmentResponse(auth0UserId, true);

        // Act
        await _service.SyncUserRoleAsync(auth0UserId, UserRole.Customer);

        // Assert
        var deleteRequest = _handler.GetLastDeleteRequest();
        Assert.NotNull(deleteRequest);
        Assert.Contains(auth0UserId, deleteRequest.RequestUri!.ToString());

        var postRequest = _handler.GetLastPostRequest();
        Assert.NotNull(postRequest);
        Assert.Contains(auth0UserId, postRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task SyncUserRoleAsync_AssignsEmployeeRole_WhenRoleIsEmployee()
    {
        // Arrange
        const string auth0UserId = "auth0|user456";
        _handler.SetTokenResponse(Domain, "test-token", 3600);
        _handler.SetRoleRemovalResponse(auth0UserId, true);
        _handler.SetRoleAssignmentResponse(auth0UserId, true);

        // Act
        await _service.SyncUserRoleAsync(auth0UserId, UserRole.Employee);

        // Assert
        var postRequest = _handler.GetLastPostRequest();
        Assert.NotNull(postRequest);
    }

    [Fact]
    public async Task SyncUserRoleAsync_AssignsManagerRole_WhenRoleIsManager()
    {
        // Arrange
        const string auth0UserId = "auth0|user789";
        _handler.SetTokenResponse(Domain, "test-token", 3600);
        _handler.SetRoleRemovalResponse(auth0UserId, true);
        _handler.SetRoleAssignmentResponse(auth0UserId, true);

        // Act
        await _service.SyncUserRoleAsync(auth0UserId, UserRole.Manager);

        // Assert
        var postRequest = _handler.GetLastPostRequest();
        Assert.NotNull(postRequest);
    }

    [Fact]
    public async Task SyncUserRoleAsync_AssignsOwnerRole_WhenRoleIsOwner()
    {
        // Arrange
        const string auth0UserId = "auth0|owner123";
        _handler.SetTokenResponse(Domain, "test-token", 3600);
        _handler.SetRoleRemovalResponse(auth0UserId, true);
        _handler.SetRoleAssignmentResponse(auth0UserId, true);

        // Act
        await _service.SyncUserRoleAsync(auth0UserId, UserRole.Owner);

        // Assert
        var postRequest = _handler.GetLastPostRequest();
        Assert.NotNull(postRequest);
    }

    [Fact]
    public async Task SyncUserRoleAsync_AssignsAdminRole_WhenRoleIsAdmin()
    {
        // Arrange
        const string auth0UserId = "auth0|admin123";
        _handler.SetTokenResponse(Domain, "test-token", 3600);
        _handler.SetRoleRemovalResponse(auth0UserId, true);
        _handler.SetRoleAssignmentResponse(auth0UserId, true);

        // Act
        await _service.SyncUserRoleAsync(auth0UserId, UserRole.Admin);

        // Assert
        var postRequest = _handler.GetLastPostRequest();
        Assert.NotNull(postRequest);
    }

    [Fact]
    public async Task SyncUserRoleAsync_EnsuresSuccessStatusCode_WhenRemovalFails()
    {
        // Arrange
        const string auth0UserId = "auth0|user123";
        _handler.SetTokenResponse(Domain, "test-token", 3600);
        _handler.SetRoleRemovalResponse(auth0UserId, false);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _service.SyncUserRoleAsync(auth0UserId, UserRole.Customer));
    }

    [Fact]
    public async Task SyncUserRoleAsync_EnsuresSuccessStatusCode_WhenAssignmentFails()
    {
        // Arrange
        const string auth0UserId = "auth0|user123";
        _handler.SetTokenResponse(Domain, "test-token", 3600);
        _handler.SetRoleRemovalResponse(auth0UserId, true);
        _handler.SetRoleAssignmentResponse(auth0UserId, false);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _service.SyncUserRoleAsync(auth0UserId, UserRole.Customer));
    }

    [Fact]
    public async Task SyncUserMetadataAsync_ThrowsArgumentException_WhenAuth0UserIdIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SyncUserMetadataAsync(null!, 1, null));
    }

    [Fact]
    public async Task SyncUserMetadataAsync_ThrowsArgumentException_WhenAuth0UserIdIsEmpty()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SyncUserMetadataAsync("", 1, null));
    }

    [Fact]
    public async Task SyncUserMetadataAsync_ThrowsArgumentOutOfRangeException_WhenUserIdIsZero()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _service.SyncUserMetadataAsync("auth0|user123", 0, null));
    }

    [Fact]
    public async Task SyncUserMetadataAsync_ThrowsArgumentOutOfRangeException_WhenUserIdIsNegative()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _service.SyncUserMetadataAsync("auth0|user123", -1, null));
    }

    [Fact]
    public async Task SyncUserMetadataAsync_SendsUserIdAndStoreId_WhenBothProvided()
    {
        // Arrange
        const string auth0UserId = "auth0|user123";
        const int userId = 42;
        const int storeId = 10;
        _handler.SetTokenResponse(Domain, "test-token", 3600);
        _handler.SetMetadataSyncResponse(auth0UserId, true);

        // Act
        await _service.SyncUserMetadataAsync(auth0UserId, userId, storeId);

        // Assert
        var patchRequest = _handler.GetLastPatchRequest();
        Assert.NotNull(patchRequest);
        Assert.Contains(auth0UserId, patchRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task SyncUserMetadataAsync_SendsOnlyUserId_WhenStoreIdIsNull()
    {
        // Arrange
        const string auth0UserId = "auth0|user456";
        const int userId = 55;
        _handler.SetTokenResponse(Domain, "test-token", 3600);
        _handler.SetMetadataSyncResponse(auth0UserId, true);

        // Act
        await _service.SyncUserMetadataAsync(auth0UserId, userId, null);

        // Assert
        var patchRequest = _handler.GetLastPatchRequest();
        Assert.NotNull(patchRequest);
    }

    [Fact]
    public async Task SyncUserMetadataAsync_SendsOnlyUserId_WhenStoreIdIsZero()
    {
        // Arrange
        const string auth0UserId = "auth0|user789";
        const int userId = 77;
        _handler.SetTokenResponse(Domain, "test-token", 3600);
        _handler.SetMetadataSyncResponse(auth0UserId, true);

        // Act
        await _service.SyncUserMetadataAsync(auth0UserId, userId, 0);

        // Assert
        var patchRequest = _handler.GetLastPatchRequest();
        Assert.NotNull(patchRequest);
    }

    [Fact]
    public async Task SyncUserMetadataAsync_EnsuresSuccessStatusCode_WhenFails()
    {
        // Arrange
        const string auth0UserId = "auth0|user123";
        _handler.SetTokenResponse(Domain, "test-token", 3600);
        _handler.SetMetadataSyncResponse(auth0UserId, false);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _service.SyncUserMetadataAsync(auth0UserId, 1, null));
    }

    [Fact]
    public async Task Service_CachesAccessToken_AndReusesForSubsequentCalls()
    {
        // Arrange
        const string auth0UserId = "auth0|user123";
        var tokenCallCount = 0;
        _handler.SetTokenResponse(Domain, "cached-token", 3600, () => tokenCallCount++);
        _handler.SetRoleRemovalResponse(auth0UserId, true);
        _handler.SetRoleAssignmentResponse(auth0UserId, true);
        _handler.SetMetadataSyncResponse(auth0UserId, true);

        // Act
        await _service.SyncUserRoleAsync(auth0UserId, UserRole.Customer);
        await _service.SyncUserMetadataAsync(auth0UserId, 1, 10);

        // Assert
        // Token should only be fetched once
        Assert.Equal(1, tokenCallCount);
    }

    [Fact]
    public async Task Service_ThrowsInvalidOperationException_WhenConfigurationIsIncomplete()
    {
        // Arrange
        var incompleteOptions = Options.Create(new Auth0ManagementOptions
        {
            Domain = "", // Missing domain
            Audience = Audience,
            ClientId = ClientId,
            ClientSecret = ClientSecret,
            CustomerRoleId = CustomerRoleId,
            OwnerRoleId = OwnerRoleId,
            EmployeeRoleId = EmployeeRoleId,
            ManagerRoleId = ManagerRoleId,
            AdminRoleId = AdminRoleId
        });

        var handler = new FakeAuth0HttpMessageHandler();
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri($"https://invalid.auth0.com/api/v2/")
        };

        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("Auth0ManagementApi").Returns(httpClient);

        var service = new Auth0ManagementService(factory, incompleteOptions, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SyncUserRoleAsync("auth0|user123", UserRole.Customer));
    }
}

internal class FakeAuth0HttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, (string tokenResponse, int expiresIn, Action? onTokenCall)> _tokenResponses = new();
    private readonly Dictionary<string, bool> _roleRemovalResponses = new();
    private readonly Dictionary<string, bool> _roleAssignmentResponses = new();
    private readonly Dictionary<string, bool> _metadataSyncResponses = new();

    private HttpRequestMessage? _lastDeleteRequest;
    private HttpRequestMessage? _lastPostRequest;
    private HttpRequestMessage? _lastPatchRequest;

    public void SetTokenResponse(string domain, string accessToken, int expiresIn, Action? onTokenCall = null)
    {
        var jsonResponse = JsonSerializer.Serialize(new { access_token = accessToken, expires_in = expiresIn });
        _tokenResponses[domain] = (jsonResponse, expiresIn, onTokenCall);
    }

    public void SetRoleRemovalResponse(string auth0UserId, bool success)
    {
        _roleRemovalResponses[auth0UserId] = success;
    }

    public void SetRoleAssignmentResponse(string auth0UserId, bool success)
    {
        _roleAssignmentResponses[auth0UserId] = success;
    }

    public void SetMetadataSyncResponse(string auth0UserId, bool success)
    {
        _metadataSyncResponses[auth0UserId] = success;
    }

    public HttpRequestMessage? GetLastDeleteRequest() => _lastDeleteRequest;
    public HttpRequestMessage? GetLastPostRequest() => _lastPostRequest;
    public HttpRequestMessage? GetLastPatchRequest() => _lastPatchRequest;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Handle token requests
        if (request.RequestUri?.ToString().Contains("/oauth/token") == true)
        {
            foreach (var domainKey in _tokenResponses.Keys)
            {
                if (request.RequestUri.ToString().Contains(domainKey))
                {
                    var (tokenResponse, expiresIn, onTokenCall) = _tokenResponses[domainKey];
                    onTokenCall?.Invoke();

                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(tokenResponse, System.Text.Encoding.UTF8, "application/json")
                    });
                }
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        }

        // Extract user ID from URL
        var urlParts = request.RequestUri?.ToString().Split('/');
        var userIdIndex = Array.FindIndex(urlParts!, s => s == "users");
        var userIdPart = userIdIndex >= 0 && userIdIndex + 1 < urlParts!.Length
            ? urlParts[userIdIndex + 1]
            : null;

        if (userIdPart != null)
        {
            var decodedUserId = Uri.UnescapeDataString(userIdPart.Split('?')[0]);

            // Handle role removal (DELETE request)
            if (request.Method == HttpMethod.Delete && request.RequestUri?.ToString().Contains("/roles") == true)
            {
                _lastDeleteRequest = request;

                if (_roleRemovalResponses.TryGetValue(decodedUserId, out var removalSuccess))
                {
                    return Task.FromResult(new HttpResponseMessage(removalSuccess ? HttpStatusCode.OK : HttpStatusCode.BadRequest));
                }
            }

            // Handle role assignment (POST request to /roles)
            if (request.Method == HttpMethod.Post && request.RequestUri?.ToString().EndsWith("/roles") == true)
            {
                _lastPostRequest = request;

                if (_roleAssignmentResponses.TryGetValue(decodedUserId, out var assignSuccess))
                {
                    return Task.FromResult(new HttpResponseMessage(assignSuccess ? HttpStatusCode.OK : HttpStatusCode.BadRequest));
                }
            }

            // Handle metadata sync (PATCH request)
            if (request.Method == HttpMethod.Patch)
            {
                _lastPatchRequest = request;

                if (_metadataSyncResponses.TryGetValue(decodedUserId, out var syncSuccess))
                {
                    return Task.FromResult(new HttpResponseMessage(syncSuccess ? HttpStatusCode.OK : HttpStatusCode.BadRequest));
                }
            }
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
