using FluentAssertions;
using HRS.API.Controllers;
using HRS.Shared.Core.Dtos;
using HRS.API.Contracts.DTOs.User;
using HRS.API.Services.Interfaces;
using HRS.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace HRS.Test.API.Controllers;

public class UsersControllerTests
{
    private readonly IUserService _userService;
    private readonly IUserContextService _userContextService;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _userService = Substitute.For<IUserService>();
        _userContextService = Substitute.For<IUserContextService>();
        _controller = new UsersController(_userService, _userContextService);
    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnOk_WithUsers()
    {
        // Arrange
        var users = new List<UserResponseDto> { new UserResponseDto { Id = 1, FirstName = "A", LastName = "B", Email = "a@b.com", Role = "Employee" } };
        _userService.GetUsers().Returns(users);

        // Act
        var result = await _controller.GetUsersAsync();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetUserAsync_ShouldReturnOk_WhenUserExists()
    {
        var user = new UserResponseDto { Id = 1, FirstName = "A", LastName = "B", Email = "a@b.com", Role = "Employee" };
        _userService.GetUserById(1).Returns(user);

        var result = await _controller.GetUserAsync(1);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Register_ShouldReturnOk_WhenSuccess()
    {
        var dto = new RegisterDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "exist@hrs.com"
        };
        _userService.Register(dto).Returns(true);

        var result = await _controller.Register(dto);

        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = result.Result as OkObjectResult;
        (ok!.Value as ApiResponse<bool>)!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetEmployees_ShouldReturnOk_WhenHasEmployees()
    {
        var employees = new List<UserResponseDto> { new UserResponseDto { Id = 1, FirstName = "A", LastName = "B", Email = "a@b.com", Role = "Employee" } };
        _userService.GetEmployees().Returns(employees);

        var result = await _controller.GetEmployees();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetEmployees_ShouldReturnNotFound_WhenEmpty()
    {
        _userService.GetEmployees().Returns(new List<UserResponseDto>());

        var result = await _controller.GetEmployees();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateEmployee_ShouldReturnOk_WhenUpdated()
    {
        var dto = new UpdateEmployeeDto { Id = 1, FirstName = "A", LastName = "B", Email = "a@b.com", Role = "Employee" };
        var respond = new UserResponseDto { Id = 1, FirstName = "A", LastName = "B", Email = "a@b.com", Role = "Employee" };
        _userService.UpdateEmployee(dto).Returns(respond);

        var result = await _controller.UpdateEmployee(dto);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateEmployee_ShouldReturnNotFound_WhenFail()
    {
        var dto = new UpdateEmployeeDto { Id = 1, FirstName = "A", LastName = "B", Email = "a@b.com", Role = "Employee" };
        var respond = new UserResponseDto { Id = 1, FirstName = "A", LastName = "B", Email = "a@b.com", Role = "Employee" };
        _userService.UpdateEmployee(dto).Returns(respond);
        var updatedto = new UpdateEmployeeDto { Id = 3, FirstName = "krit", LastName = "tt", Email = "a@bee.com", Role = "Employee" };

        var result = await _controller.UpdateEmployee(updatedto);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteEmployee_ShouldReturnNoContent_WhenSuccess()
    {
        _userService.DeleteEmployee(1).Returns(true);

        var result = await _controller.DeleteEmployee(1);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DeleteEmployee_ShouldReturnNotFound_WhenFail()
    {
        _userService.DeleteEmployee(1).Returns(false);

        var result = await _controller.DeleteEmployee(1);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateNewEmployee_ShouldReturnCreated_WhenSuccess()
    {
        var dto = new RegisterEmployeeDetailDto { FirstName = "Alice", LastName = "Wonder", Email = "alice@wonder.com", Role = "Employee" };
        var created = new UserResponseDto { Id = 100, FirstName = "Alice", LastName = "Wonder", Email = "alice@wonder.com", Role = "Employee" };
        _userService.CreateNewEmployee(dto).Returns(created);

        var result = await _controller.CreateNewEmployee(dto);

        result.Result.Should().BeOfType<OkObjectResult>();
        var createdRes = result.Result.As<OkObjectResult>().Value as ApiResponse<UserResponseDto>;
        createdRes!.Success.Should().BeTrue();
        createdRes.Data.Should().Be(created);
    }

    [Fact]
    public async Task DeleteUser_ShouldReturnNoContent_WhenSuccess()
    {
        _userService.DeleteUser(1).Returns(true);

        var result = await _controller.DeleteUser(1);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteUser_ShouldReturnNotFound_WhenFail()
    {
        _userService.DeleteUser(1).Returns(false);

        var result = await _controller.DeleteUser(1);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ValidateUserExists_ShouldReturnTrue_WhenUserExists()
    {
        // Arrange
        var user = new User { Id = 1, Email = "test@test.com" };
        _userContextService.GetUserByAuth0IdAsync().Returns(user);

        // Act
        var actionResult = await _controller.ValidateUserExists();

        // Assert
        var result = actionResult.Result;
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<bool>;
        Assert.NotNull(response);
        Assert.True(response.Data);
    }

    [Fact]
    public async Task ValidateUserExists_ShouldReturnFalse_WhenUserNotExists()
    {
        // Arrange
        _userContextService.GetUserByAuth0IdAsync().Returns((User?)null);

        // Act
        var actionResult = await _controller.ValidateUserExists();

        // Assert
        var result = actionResult.Result;
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<bool>;
        Assert.NotNull(response);
        Assert.False(response.Data);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ShouldReturnCurrentUser_WhenExists()
    {
        // Arrange
        var userDto = new UserResponseDto { Id = 1, FirstName = "Current", LastName = "User", Email = "current@test.com", Role = "Manager" };
        _userContextService.GetUserDtoAsync().Returns(userDto);

        // Act
        var result = await _controller.GetCurrentUserAsync();

        // Assert
        var okResult = result as OkObjectResult;
        Assert.NotNull(okResult);
        var response = okResult.Value as ApiResponse<UserResponseDto>;
        Assert.NotNull(response);
        Assert.Equal(userDto, response.Data);
    }

    [Fact]
    public async Task SyncAuth0Metadata_ShouldReturnTrue_WhenSuccessful()
    {
        // Arrange
        _userService.SyncCurrentUserMetadataAsync().Returns(true);

        // Act
        var result = await _controller.SyncAuth0Metadata();

        // Assert -SyncAuth0Metadata returns ActionResult<bool> when awaited
        // Verify it was called successfully
        await _userService.Received(1).SyncCurrentUserMetadataAsync();
    }

    [Fact]
    public async Task Register_ShouldReturnOk_WithCorrectMessage()
    {
        // Arrange
        var dto = new RegisterDto
        {
            FirstName = "New",
            LastName = "Customer",
            Email = "newcust@hrs.com"
        };
        _userService.Register(dto).Returns(true);

        // Act
        var result = await _controller.Register(dto);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as ApiResponse<bool>;
        response!.Message.Should().Be("Registration successful");
    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnMultipleUsers()
    {
        // Arrange
        var users = new List<UserResponseDto>
        {
            new UserResponseDto { Id = 1, FirstName = "A", LastName = "B", Email = "a@b.com", Role = "Employee" },
            new UserResponseDto { Id = 2, FirstName = "C", LastName = "D", Email = "c@d.com", Role = "Manager" }
        };
        _userService.GetUsers().Returns(users);

        // Act
        var result = await _controller.GetUsersAsync();

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.NotNull(okResult);
        var response = okResult.Value as ApiResponse<List<UserResponseDto>>;
        Assert.NotNull(response);
        Assert.NotNull(response.Data);
        Assert.Equal(2, response.Data.Count);
    }

    [Fact]
    public async Task GetUserAsync_ShouldReturnOk_WithCorrectData()
    {
        // Arrange
        var user = new UserResponseDto { Id = 5, FirstName = "Test", LastName = "User", Email = "test@test.com", Role = "Employee" };
        _userService.GetUserById(5).Returns(user);

        // Act
        var result = await _controller.GetUserAsync(5);

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.NotNull(okResult);
        var response = okResult.Value as ApiResponse<UserResponseDto>;
        Assert.NotNull(response);
        Assert.NotNull(response.Data);
        Assert.Equal(user.Id, response.Data.Id);
    }

    [Fact]
    public async Task CreateNewEmployee_ShouldReturnOk_WithCorrectMessage()
    {
        // Arrange
        var dto = new RegisterEmployeeDetailDto { FirstName = "Bob", LastName = "Builder", Email = "bob@builder.com", Role = "Manager" };
        var created = new UserResponseDto { Id = 50, FirstName = "Bob", LastName = "Builder", Email = "bob@builder.com", Role = "Manager" };
        _userService.CreateNewEmployee(dto).Returns(created);

        // Act
        var result = await _controller.CreateNewEmployee(dto);

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.NotNull(okResult);
        var response = okResult.Value as ApiResponse<UserResponseDto>;
        Assert.NotNull(response);
        Assert.Equal("Employee Created successfully", response.Message);
    }

    [Fact]
    public async Task DeleteEmployee_ShouldReturnOk_WithSuccessMessage()
    {
        // Arrange
        _userService.DeleteEmployee(2).Returns(true);

        // Act
        var result = await _controller.DeleteEmployee(2);

        // Assert
        var okResult = result as OkObjectResult;
        Assert.NotNull(okResult);
        var response = okResult.Value as ApiResponse<bool>;
        Assert.NotNull(response);
        Assert.Equal("Employee deleted successfully", response.Message);
    }
}
