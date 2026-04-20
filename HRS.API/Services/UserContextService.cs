using System.Security.Claims;
using AutoMapper;
using HRS.API.Contracts.DTOs.User;
using HRS.API.Services.Interfaces;
using HRS.Domain.Entities;
using HRS.Domain.Interfaces;
using HRS.Shared.Core.Dtos;

namespace HRS.API.Services;

public class UserContextService : IUserContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMapper _mapper;
    private readonly IUserRepository _userRepository;

    public UserContextService(IHttpContextAccessor httpContextAccessor, IUserRepository userRepository, IMapper mapper)
    {
        _httpContextAccessor = httpContextAccessor;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<User> GetUserAsync()
    {
        var user = await GetUserByAuth0IdAsync();

        return user ?? throw new UnauthorizedAccessException("Authenticated user is not linked in the system");
    }

    public string GetAuth0Id()
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated is not true)
            throw new UnauthorizedAccessException("User is not authenticated");

        return principal.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("Auth0 ID (sub claim) not found in token");
    }

    public async Task<User?> GetUserByAuth0IdAsync()
    {
        var auth0Id = GetAuth0Id();
        return await _userRepository.GetByAuth0IdAsync(auth0Id);
    }

    public async Task<UserResponseDto> GetUserDtoAsync()
    {
        return _mapper.Map<UserResponseDto>(await GetUserAsync());
    }

    public async Task<int> GetUserIdAsync() => (await GetUserAsync()).Id;
}
