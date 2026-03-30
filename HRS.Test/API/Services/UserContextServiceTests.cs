using System.Security.Claims;
using AutoMapper;
using HRS.API.Contracts.DTOs.User;
using HRS.API.Services;
using HRS.Domain.Entities;
using HRS.Shared.Core.Enums;
using HRS.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using HRS.Shared.Core.Dtos;

namespace HRS.Test.API.Services;

public class UserContextServiceTests
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMapper _mapper;
    private readonly UserContextService _mockContextService;
    private readonly IUserRepository _userRepository;

    public UserContextServiceTests()
    {
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _userRepository = Substitute.For<IUserRepository>();
        _mapper = Substitute.For<IMapper>();
        _mockContextService = new UserContextService(_httpContextAccessor, _userRepository, _mapper);
    }

    private void SetAuthenticatedUser(string auth0Id)
    {
        var identity = new ClaimsIdentity(new[] { new Claim("sub", auth0Id) }, authenticationType: "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var context = Substitute.For<HttpContext>();
        context.User.Returns(principal);
        _httpContextAccessor.HttpContext.Returns(context);
    }

    private void SetUnauthenticatedUser()
    {
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);
        var context = Substitute.For<HttpContext>();
        context.User.Returns(principal);
        _httpContextAccessor.HttpContext.Returns(context);
    }

    [Fact]
    public async Task GetUserAsync_ReturnsUser_WhenAuthenticated()
    {
        // Arrange
        var userId = 1;
        const string auth0Id = "auth0|user-1";
        SetAuthenticatedUser(auth0Id);

        var user = new User
        {
            Id = userId,
            FirstName = "Test",
            LastName = "User",
            Email = "test@hrs.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _userRepository.GetByAuth0IdAsync(auth0Id).Returns(user);

        // Act
        var result = await _mockContextService.GetUserAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("Test", result.FirstName);
        Assert.Equal("User", result.LastName);
    }

    [Fact]
    public async Task GetUserIdAsync_ReturnsUserId_WhenAuthenticated()
    {
        // Arrange
        var userId = 1;
        const string auth0Id = "auth0|user-1";
        SetAuthenticatedUser(auth0Id);

        var user = new User
        {
            Id = userId,
            FirstName = "Test",
            LastName = "User",
            Email = "test@hrs.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _userRepository.GetByAuth0IdAsync(auth0Id).Returns(user);

        // Act
        var result = await _mockContextService.GetUserIdAsync();

        // Assert
        Assert.Equal(userId, result);
    }

    [Fact]
    public async Task GetUserDtoAsync_ReturnsUserDto_WhenAuthenticated()
    {
        // Arrange
        var userId = 1;
        const string auth0Id = "auth0|user-1";
        SetAuthenticatedUser(auth0Id);

        var user = new User
        {
            Id = userId,
            FirstName = "Test",
            LastName = "User",
            Email = "test@hrs.com",
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var userDto = new UserResponseDto
        {
            Id = userId,
            FirstName = "Test",
            LastName = "User",
            Email = "test@hrs.com",
            Role = nameof(UserRole.Admin)
        };
        _userRepository.GetByAuth0IdAsync(auth0Id).Returns(user);
        _mapper.Map<UserResponseDto>(user).Returns(userDto);

        // Act
        var result = await _mockContextService.GetUserDtoAsync();

        // Assert
        Assert.Equal(result, userDto);
    }

    [Fact]
    public async Task GetUserAsync_Throws_WhenNotAuthenticated()
    {
        // Arrange
        SetUnauthenticatedUser();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _mockContextService.GetUserAsync());
    }

    [Fact]
    public async Task GetUserAsync_Throws_WhenUserIdClaimMissing()
    {
        // Arrange
        var identity = new ClaimsIdentity(authenticationType: "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var context = Substitute.For<HttpContext>();
        context.User.Returns(principal);
        _httpContextAccessor.HttpContext.Returns(context);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _mockContextService.GetUserAsync());
    }

    [Fact]
    public async Task GetUserAsync_Throws_WhenUserNotFound()
    {
        // Arrange
        const string auth0Id = "auth0|user-2";
        SetAuthenticatedUser(auth0Id);

        _userRepository.GetByAuth0IdAsync(auth0Id).Returns((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _mockContextService.GetUserAsync());
    }
}
