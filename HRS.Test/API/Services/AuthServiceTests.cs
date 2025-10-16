using System.Net;
using HRS.API.Contracts.DTOs.Auth;
using HRS.API.Services;
using HRS.API.Services.Interfaces;
using HRS.Domain.Entities;
using HRS.Domain.Interfaces;
using NSubstitute;

namespace HRS.Test.API.Services;

public class AuthServiceTests
{
    private readonly AuthService _service;
    private readonly IUserContextService _userContextService;
    private readonly IUserRepository _userRepository;
    private readonly IUserSessionService _userSessionService;
    private readonly IUserVerificationService _userVerificationService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly HttpClient _httpClient;

    public AuthServiceTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _userContextService = Substitute.For<IUserContextService>();
        _userSessionService = Substitute.For<IUserSessionService>();
        _userVerificationService = Substitute.For<IUserVerificationService>();
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        
        // 创建模拟的 HttpClient
        var handler = new FakeHttpMessageHandler();
        _httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        _httpClientFactory.CreateClient("EmailService").Returns(_httpClient);
        
        _service = new AuthService(_userRepository, _userContextService, _userSessionService, _userVerificationService, _httpClientFactory);
    }

    [Fact]
    public async Task LoginAsync_Throws_WhenUserNotFound()
    {
        // Arrange
        _userRepository.GetByEmailAsync(Arg.Any<string>()).Returns((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.LoginAsync(new LoginRequestDto()));
    }

    [Fact]
    public async Task LoginAsync_Throws_WhenPasswordIncorrect()
    {
        // Arrange
        var user = new User { Id = 1, Email = "test@hrs.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("right"), IsVerified = true };
        _userRepository.GetByEmailAsync(user.Email).Returns(user);
        var dto = new LoginRequestDto { Email = user.Email, Password = "wrong" };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.LoginAsync(dto));
    }

    [Fact]
    public async Task LoginAsync_Throws_WhenNotVerified()
    {
        // Arrange
        var user = new User { Id = 1, Email = "test@hrs.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass"), IsVerified = false };
        _userRepository.GetByEmailAsync(user.Email).Returns(user);
        var dto = new LoginRequestDto { Email = user.Email, Password = "pass" };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.LoginAsync(dto));
    }

    [Fact]
    public async Task LoginAsync_ReturnsResponse_WhenValid()
    {
        // Arrange
        var user = new User { Id = 1, Email = "test@hrs.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass"), IsVerified = true };
        _userRepository.GetByEmailAsync(user.Email).Returns(user);
        _userSessionService.CreateAsync(user.Id).Returns(("token", "refresh"));
        var dto = new LoginRequestDto { Email = user.Email, Password = "pass" };

        // Act
        var result = await _service.LoginAsync(dto);

        // Assert
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal("token", result.Token);
        Assert.Equal("refresh", result.RefreshToken);
    }

    [Fact]
    public async Task RefreshTokenAsync_ReturnsResponse()
    {
        // Arrange
        _userSessionService.RefreshAsync(Arg.Any<string>()).Returns(("token", "refresh"));
        _userContextService.GetUserAsync().Returns(new User { Id = 1 });

        // Act
        var result = await _service.RefreshTokenAsync("refresh");

        // Assert
        Assert.Equal(1, result.UserId);
        Assert.Equal("token", result.Token);
        Assert.Equal("refresh", result.RefreshToken);
    }

    [Fact]
    public async Task LogoutAsync_ReturnsLogoutResponse()
    {
        // Arrange
        _userContextService.GetUserAsync().Returns(new User { Id = 1 });
        _userSessionService.RevokeAllForUserAsync(1, Arg.Any<string>()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.LogoutAsync();

        // Assert
        Assert.Equal("Logout successful", result.Message);
    }

    [Fact]
    public async Task ChangePasswordAsync_Throws_WhenCurrentPasswordIncorrect()
    {
        // Arrange
        _userContextService.GetUserAsync().Returns(new User { PasswordHash = BCrypt.Net.BCrypt.HashPassword("right") });
        var dto = new ChangePasswordRequestDto { CurrentPassword = "wrong", NewPassword = "new" };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.ChangePasswordAsync(dto));
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsResponse_WhenValid()
    {
        // Arrange
        var user = new User { Id = 1, PasswordHash = BCrypt.Net.BCrypt.HashPassword("old") };
        var requestDto = new ChangePasswordRequestDto { CurrentPassword = "old", NewPassword = "new" };
        _userContextService.GetUserAsync().Returns(user);
        _userRepository.UpdateUserAsync(user).Returns(Task.CompletedTask);

        // Act
        var result = await _service.ChangePasswordAsync(requestDto);

        // Assert
        Assert.Equal(user.Id, result.UserId);
        Assert.True(result.PasswordChangedAtUtc <= DateTime.UtcNow);
    }

    #region ForgotPasswordAsync Tests

    [Fact]
    public async Task ForgotPasswordAsync_WithValidEmail_ShouldSendResetEmail()
    {
        // Arrange
        var request = new ForgotPasswordRequestDto { Email = "test@example.com" };
        var user = new User { Id = 1, Email = "test@example.com", FirstName = "John" };
        var verification = new UserVerification { Token = "reset-token", UserId = user.Id };

        _userRepository.GetByEmailAsync(request.Email).Returns(user);
        _userVerificationService.CreateAsync(user.Id, "PasswordReset", Arg.Any<TimeSpan>()).Returns(verification);
        // HttpClient 会被 FakeHttpMessageHandler 处理

        // Act
        await _service.ForgotPasswordAsync(request);

        // Assert
        await _userVerificationService.Received(1).CreateAsync(user.Id, "PasswordReset", Arg.Any<TimeSpan>());
    }

    [Fact]
    public async Task ForgotPasswordAsync_WithNonExistentEmail_ShouldThrowException()
    {
        // Arrange
        var request = new ForgotPasswordRequestDto { Email = "nonexistent@example.com" };
        _userRepository.GetByEmailAsync(request.Email).Returns((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ForgotPasswordAsync(request));
    }

    #endregion

    #region ResetPasswordAsync Tests

    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_ShouldResetPassword()
    {
        // Arrange
        var request = new ResetPasswordRequestDto
        {
            Email = "test@example.com",
            Token = "valid-token",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!"
        };
        var user = new User { Id = 1, Email = "test@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123!") };
        var verification = new UserVerification { UserId = user.Id, Token = request.Token };

        _userRepository.GetByEmailAsync(request.Email).Returns(user);
        _userVerificationService.ValidateAndConsumeAsync(request.Token, "PasswordReset").Returns(verification);

        // Act
        await _service.ResetPasswordAsync(request);

        // Assert
        await _userRepository.Received(1).UpdateUserAsync(Arg.Is<User>(u => u.Id == user.Id));
    }

    [Fact]
    public async Task ResetPasswordAsync_WithInvalidEmail_ShouldThrowException()
    {
        // Arrange
        var request = new ResetPasswordRequestDto { Email = "invalid@example.com", Token = "token", NewPassword = "Pass123!", ConfirmNewPassword = "Pass123!" };
        _userRepository.GetByEmailAsync(request.Email).Returns((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ResetPasswordAsync(request));
    }

    [Fact]
    public async Task ResetPasswordAsync_WithMismatchedPasswords_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var request = new ResetPasswordRequestDto
        {
            Email = "test@example.com",
            Token = "token",
            NewPassword = "Password123!",
            ConfirmNewPassword = "DifferentPassword123!"
        };
        var user = new User { Id = 1, Email = "test@example.com" };
        _userRepository.GetByEmailAsync(request.Email).Returns(user);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ResetPasswordAsync(request));
        await _userRepository.DidNotReceive().UpdateUserAsync(Arg.Any<User>());
    }

    [Fact]
    public async Task ResetPasswordAsync_WithSameAsCurrentPassword_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var currentPassword = "SamePassword123!";
        var request = new ResetPasswordRequestDto
        {
            Email = "test@example.com",
            Token = "token",
            NewPassword = currentPassword,
            ConfirmNewPassword = currentPassword
        };
        var user = new User { Id = 1, Email = "test@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword(currentPassword) };
        _userRepository.GetByEmailAsync(request.Email).Returns(user);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ResetPasswordAsync(request));
        await _userRepository.DidNotReceive().UpdateUserAsync(Arg.Any<User>());
    }

    [Fact]
    public async Task ResetPasswordAsync_WithInvalidToken_ShouldThrowException()
    {
        // Arrange
        var request = new ResetPasswordRequestDto
        {
            Email = "test@example.com",
            Token = "invalid-token",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!"
        };
        var user = new User { Id = 1, Email = "test@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123!") };

        _userRepository.GetByEmailAsync(request.Email).Returns(user);
        _userVerificationService.ValidateAndConsumeAsync(request.Token, "PasswordReset").Returns((UserVerification?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ResetPasswordAsync(request));
        await _userRepository.DidNotReceive().UpdateUserAsync(Arg.Any<User>());
    }

    [Fact]
    public async Task ResetPasswordAsync_WithTokenForDifferentUser_ShouldThrowException()
    {
        // Arrange
        var request = new ResetPasswordRequestDto
        {
            Email = "test@example.com",
            Token = "token",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!"
        };
        var user = new User { Id = 1, Email = "test@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123!") };
        var verification = new UserVerification { UserId = 999, Token = request.Token }; // Different user ID

        _userRepository.GetByEmailAsync(request.Email).Returns(user);
        _userVerificationService.ValidateAndConsumeAsync(request.Token, "PasswordReset").Returns(verification);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ResetPasswordAsync(request));
        await _userRepository.DidNotReceive().UpdateUserAsync(Arg.Any<User>());
    }

    #endregion
    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
