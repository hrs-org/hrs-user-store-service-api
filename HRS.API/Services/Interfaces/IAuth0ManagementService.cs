using HRS.Shared.Core.Enums;

namespace HRS.API.Services.Interfaces;

public interface IAuth0ManagementService
{
    Task SyncUserRoleAsync(string auth0UserId, UserRole role, CancellationToken cancellationToken = default);
    Task SyncUserMetadataAsync(string auth0UserId, int userId, int? storeId, CancellationToken cancellationToken = default);
    Task<string> CreateUserAsync(string email, string firstName, string lastName, CancellationToken cancellationToken = default);
}
