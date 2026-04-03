using HRS.Domain.Entities;
using HRS.Shared.Core.Dtos;

namespace HRS.API.Services.Interfaces;

public interface IUserContextService
{
    Task<User> GetUserAsync();

    Task<UserResponseDto> GetUserDtoAsync();

    Task<int> GetUserIdAsync();

    string GetAuth0Id();

    Task<User?> GetUserByAuth0IdAsync();
}
