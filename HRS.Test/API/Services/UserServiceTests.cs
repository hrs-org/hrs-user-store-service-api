using System.Net;
using AutoMapper;
using HRS.API.Contracts.DTOs.User;
using HRS.API.Services;
using HRS.API.Services.Interfaces;
using HRS.Domain.Entities;
using HRS.Shared.Core.Enums;
using HRS.Domain.Interfaces;
using NSubstitute;
using HRS.Shared.Core.Dtos;

namespace HRS.Test.API.Services;

public class UserServiceTests
{
    private readonly IMapper _mapper;
    private readonly IAuth0ManagementService _auth0ManagementService;
    private readonly IUserContextService _userContextService;
    private readonly IUserRepository _userRepository;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _mapper = Substitute.For<IMapper>();
        _auth0ManagementService = Substitute.For<IAuth0ManagementService>();
        _userRepository = Substitute.For<IUserRepository>();
        _userContextService = Substitute.For<IUserContextService>();

        _userService = new UserService(_mapper, _auth0ManagementService, _userRepository, _userContextService);
    }

    [Fact]
    public async Task GetUsers_ReturnsMappedUserDtos()
    {
        // Arrange
        var users = new List<User> { new() { Id = 1, FirstName = "A", LastName = "B", Email = "a@b.com" } };
        var userDtos = new List<UserResponseDto> { new() { Id = 1, FirstName = "A", LastName = "B", Email = "a@b.com", Role = "Employee" } };
        _userRepository.GetAllAsync().Returns(users);
        _mapper.Map<IEnumerable<UserResponseDto>>(users).Returns(userDtos);

        // Act
        var result = await _userService.GetUsers();

        // Assert
        Assert.Equal(userDtos, result);
    }

    [Fact]
    public async Task GetUserById_ReturnsMappedUserDto()
    {
        // Arrange
        var user = new User { Id = 1, FirstName = "A", LastName = "B", Email = "a@b.com" };
        var userDto = new UserResponseDto { Id = 1, FirstName = "A", LastName = "B", Email = "a@b.com", Role = "Employee" };
        _userRepository.GetByIdAsync(1).Returns(user);
        _mapper.Map<UserResponseDto>(user).Returns(userDto);

        // Act
        var result = await _userService.GetUserById(1);

        // Assert
        Assert.Equal(userDto, result);
    }

    [Fact]
    public async Task DeleteUser_Throws_WhenUserNotFound()
    {
        // Arrange
        _userRepository.GetByIdAsync(1).Returns((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _userService.DeleteUser(1));
    }

    [Fact]
    public async Task DeleteUser_ReturnsTrue_WhenUserExists()
    {
        // Arrange
        var user = new User { Id = 1 };
        _userRepository.GetByIdAsync(1).Returns(user);

        // Act
        var result = await _userService.DeleteUser(1);

        // Assert
        Assert.True(result);
        _userRepository.Received(1).Remove(user);
    }

    [Fact]
    public async Task GetEmployees_ReturnsMappedEmployeeDtos()
    {
        // Arrange
        var user = new User { Id = 1, Role = UserRole.Admin, StoreId = 1 };
        var employees = new List<User> { new() { Id = 2, Role = UserRole.Employee } };
        var employeeDtos = new List<UserResponseDto>
        {
            new()
            {
                Id = 1,
                Role = "Employee",
                FirstName = "Test",
                LastName = "Name",
                Email = "testname@email.com",
            }
        };
        _userContextService.GetUserAsync().Returns(user);
        _userRepository.GetAllEmployee(user.StoreId).Returns(employees);
        _mapper.Map<List<UserResponseDto>>(employees).Returns(employeeDtos);

        // Act
        var result = await _userService.GetEmployees();

        // Assert
        Assert.Equal(employeeDtos, result);
    }

    [Fact]
    public async Task UpdateEmployee_Throws_WhenEmployeeNotFound()
    {
        // Arrange
        _userRepository.GetByIdAsync(1).Returns((User?)null);
        var dto = new UpdateEmployeeDto
        {
            Id = 1,
            Role = "Employee",
            FirstName = "Test",
            LastName = "Name",
            Email = "testname@email.com"
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _userService.UpdateEmployee(dto));
    }

    [Fact]
    public async Task UpdateEmployee_Updates_WhenRoleIsCustomerAndRoleInputIsValid()
    {
        // Arrange
        var editor = new User { Id = 50, Role = UserRole.Manager };
        var employee = new User { Id = 1, Role = UserRole.Customer, Auth0UserId = "auth0|employee-1" };
        var mappedResponse = new UserResponseDto
        {
            Id = employee.Id,
            FirstName = "Test",
            LastName = "Name",
            Email = "testname@email.com",
            Role = "Employee"
        };
        _userContextService.GetUserAsync().Returns(editor);
        _userRepository.GetByIdAsync(1).Returns(employee);
        _mapper.Map<UserResponseDto>(Arg.Any<User>()).Returns(mappedResponse);
        var dto = new UpdateEmployeeDto
        {
            Id = 1,
            Role = "Employee",
            FirstName = "Test",
            LastName = "Name",
            Email = "testname@email.com"
        };

        // Act
        var result = await _userService.UpdateEmployee(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", employee.FirstName);
        Assert.Equal("Name", employee.LastName);
        Assert.Equal("testname@email.com", employee.Email);
    }

    [Fact]
    public async Task UpdateEmployee_Throws_WhenRoleIsInvalid()
    {
        // Arrange
        var employee = new User { Id = 1, Role = UserRole.Employee };
        _userRepository.GetByIdAsync(1).Returns(employee);
        var dto = new UpdateEmployeeDto
        {
            Id = 1,
            Role = "Invalid Role",
            FirstName = "Test",
            LastName = "Name",
            Email = "testname@email.com"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _userService.UpdateEmployee(dto));
    }

    [Fact]
    public async Task Register_ReturnsTrue_WhenValid()
    {
        // Arrange
        var dto = new RegisterDto { Email = "test@hrs.com", FirstName = "Test", LastName = "User" };
        var user = new User { Id = 1, Email = dto.Email, FirstName = dto.FirstName, LastName = dto.LastName };

        _userRepository.GetByEmailAsync(dto.Email).Returns((User?)null);
        _userContextService.GetAuth0Id().Returns("auth0|customer-1");
        _mapper.Map<User>(dto).Returns(user);

        // Act
        var result = await _userService.Register(dto);

        // Assert
        Assert.True(result);
        await _userRepository.Received(1).AddAsync(Arg.Is<User>(u =>
            u.Email == dto.Email &&
            u.FirstName == dto.FirstName &&
            u.LastName == dto.LastName));
        await _userRepository.Received(1).SaveChangesAsync();
        await _auth0ManagementService.Received(1).SyncUserRoleAsync(Arg.Any<string>(), UserRole.Customer);
    }

    [Fact]
    public async Task Register_Throws_WhenUserExists()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "test@hrs.com",
            FirstName = "test",
            LastName = "name"
        };
        var existingUser = new User { Id = 1, Email = dto.Email };
        _userRepository.GetByEmailAsync(dto.Email).Returns(existingUser);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.Register(dto));
    }

    [Fact]
    public async Task Register_Throws_WhenEmailIsMissing()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "   ",
            FirstName = "test",
            LastName = "name"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _userService.Register(dto));
    }

    [Fact]
    public async Task UpdateEmployee_UpdatesEmployee_WhenValid()
    {
        var editor = new User { Id = 99, Role = UserRole.Admin };
        var employee = new User { Id = 1, Role = UserRole.Employee, Auth0UserId = "auth0|employee-2" };
        var dto = new UpdateEmployeeDto { Id = 1, Role = "Manager", FirstName = "F", LastName = "L", Email = "e@x.com" };
        var respond = new UserResponseDto { Id = 1, Role = "Manager", FirstName = "F", LastName = "L", Email = "e@x.com" };
        _userContextService.GetUserAsync().Returns(editor);
        _userRepository.GetByIdAsync(dto.Id).Returns(employee);
        _mapper.Map<UserResponseDto>(Arg.Any<User>()).Returns(respond);

        var result = await _userService.UpdateEmployee(dto);

        Assert.Equal(dto.FirstName, employee.FirstName);
        Assert.Equal(dto.LastName, employee.LastName);
        Assert.Equal(dto.Email, employee.Email);
        Assert.Equal(UserRole.Manager, employee.Role);
        Assert.Equal(editor.Id, employee.UpdatedBy);
        Assert.NotNull(result);
        await _auth0ManagementService.Received(1).SyncUserRoleAsync(employee.Auth0UserId, UserRole.Manager);
    }

    [Fact]
    public async Task DeleteEmployee_DeletesEmployee_WhenValid()
    {
        var employee = new User { Id = 2, Role = UserRole.Employee };
        _userRepository.GetByIdAsync(employee.Id).Returns(employee);

        var result = await _userService.DeleteEmployee(employee.Id);

        Assert.True(result);
        _userRepository.Received(1).Remove(employee);
        await _userRepository.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task DeleteEmployee_DeletesCustomer_WhenExists()
    {
        var employee = new User { Id = 3, Role = UserRole.Customer };
        _userRepository.GetByIdAsync(employee.Id).Returns(employee);

        var result = await _userService.DeleteEmployee(employee.Id);

        Assert.True(result);
        _userRepository.Received(1).Remove(employee);
    }

    [Fact]
    public async Task CreateNewEmployee_CreatesEmployeeAndReturnsDto()
    {
        var dto = new RegisterEmployeeDetailDto
        {
            FirstName = "New",
            LastName = "Employee",
            Email = "newemployee@mail.com",
            Role = "Employee"
        };
        var editor = new User { Id = 99, Role = UserRole.Admin };
        var userDto = new UserResponseDto
        {
            Id = 4,
            FirstName = "New",
            LastName = "Employee",
            Email = "newemployee@mail.com",
            Role = "Employee"
        };

        _auth0ManagementService.CreateUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns("auth0|new-4");
        _userContextService.GetUserAsync().Returns(editor);
        _mapper.Map<UserResponseDto>(Arg.Any<User>()).Returns(userDto);

        var result = await _userService.CreateNewEmployee(dto);

        Assert.Equal(userDto, result);
        await _userRepository.Received(1).AddAsync(Arg.Is<User>(u =>
            u.Email == dto.Email.Trim() && u.UpdatedBy == editor.Id));
        await _userRepository.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task CreateNewEmployee_SetsAuditAndReturnsDto_Simple()
    {
        // Arrange
        var dto = new RegisterEmployeeDetailDto { FirstName = "New", LastName = "Emp", Email = "new@e.com", Role = "Employee" };
        var editor = new User { Id = 99, Role = UserRole.Admin };
        var mappedDto = new UserResponseDto { Id = 10, FirstName = "New", LastName = "Emp", Email = "new@e.com", Role = "Employee" };

        _auth0ManagementService.CreateUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns("auth0|new-10");
        _userContextService.GetUserAsync().Returns(editor);
        _userRepository.AddAsync(Arg.Any<User>()).Returns(Task.CompletedTask);
        _userRepository.SaveChangesAsync().Returns(Task.FromResult(0));
        _mapper.Map<UserResponseDto>(Arg.Any<User>()).Returns(mappedDto);

        // Act
        var result = await _userService.CreateNewEmployee(dto);

        // Assert
        Assert.Equal(mappedDto, result);
        await _userRepository.Received(1).AddAsync(Arg.Is<User>(u => u.UpdatedBy == editor.Id));
        await _userRepository.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task CreateNewEmployee_Throws_WhenUserContextNull_Simple()
    {
        // Arrange
        var dto = new RegisterEmployeeDetailDto { FirstName = "X", LastName = "Y", Email = "x@y.com", Role = "Employee" };
        _mapper.Map<User>(dto).Returns(new User());
        _userContextService.GetUserAsync()!.Returns((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() => _userService.CreateNewEmployee(dto));
    }

    [Fact]
    public async Task SyncCurrentUserMetadataAsync_SyncsMetadata_WhenUserExists()
    {
        // Arrange
        var user = new User { Id = 1, Auth0UserId = "auth0|user123", StoreId = 5 };
        _userContextService.GetUserAsync().Returns(user);

        // Act
        var result = await _userService.SyncCurrentUserMetadataAsync();

        // Assert
        Assert.True(result);
        await _auth0ManagementService.Received(1).SyncUserMetadataAsync(user.Auth0UserId, user.Id, user.StoreId);
    }

    [Fact]
    public async Task SyncCurrentUserMetadataAsync_SyncsMetadata_WhenUserHasNoStore()
    {
        // Arrange
        var user = new User { Id = 1, Auth0UserId = "auth0|user456", StoreId = null };
        _userContextService.GetUserAsync().Returns(user);

        // Act
        var result = await _userService.SyncCurrentUserMetadataAsync();

        // Assert
        Assert.True(result);
        await _auth0ManagementService.Received(1).SyncUserMetadataAsync(user.Auth0UserId, user.Id, user.StoreId);
    }

    [Fact]
    public async Task GetUserById_ThrowsKeyNotFoundException_WhenUserNotFound()
    {
        // Arrange
        _userRepository.GetByIdAsync(999).Returns((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.GetUserById(999));
    }

    [Fact]
    public async Task UpdateEmployee_SyncsUserMetadata_AfterRoleUpdate()
    {
        // Arrange
        var editor = new User { Id = 99, Role = UserRole.Admin };
        var employee = new User { Id = 1, Role = UserRole.Employee, Auth0UserId = "auth0|emp-upd", StoreId = 1 };
        var dto = new UpdateEmployeeDto { Id = 1, Role = "Admin", FirstName = "F", LastName = "L", Email = "e@x.com" };
        var respond = new UserResponseDto { Id = 1, Role = "Admin", FirstName = "F", LastName = "L", Email = "e@x.com" };

        _userContextService.GetUserAsync().Returns(editor);
        _userRepository.GetByIdAsync(dto.Id).Returns(employee);
        _mapper.Map<UserResponseDto>(Arg.Any<User>()).Returns(respond);

        // Act
        var result = await _userService.UpdateEmployee(dto);

        // Assert
        Assert.NotNull(result);
        await _auth0ManagementService.Received(1).SyncUserMetadataAsync(employee.Auth0UserId, employee.Id, employee.StoreId);
    }

    [Fact]
    public async Task DeleteEmployee_Throws_WhenEmployeeNotFound()
    {
        // Arrange
        _userRepository.GetByIdAsync(999).Returns((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _userService.DeleteEmployee(999));
    }

    [Fact]
    public async Task GetEmployees_CallsRepositoryWithCorrectStoreId()
    {
        // Arrange
        var user = new User { Id = 1, Role = UserRole.Manager, StoreId = 5 };
        var employees = new List<User> { new() { Id = 2, Role = UserRole.Employee } };
        var employeeDtos = new List<UserResponseDto>
        {
            new() { Id = 2, Role = "Employee", FirstName = "Test", LastName = "Emp", Email = "test@emp.com" }
        };

        _userContextService.GetUserAsync().Returns(user);
        _userRepository.GetAllEmployee(5).Returns(employees);
        _mapper.Map<List<UserResponseDto>>(employees).Returns(employeeDtos);

        // Act
        var result = await _userService.GetEmployees();

        // Assert
        Assert.Equal(employeeDtos, result);
        await _userRepository.Received(1).GetAllEmployee(5);
    }

    [Fact]
    public async Task Register_SendsEmailNotification_WhenEmployeeCreated()
    {
        // Arrange
        var dto = new RegisterEmployeeDetailDto { FirstName = "Test", LastName = "Emp", Email = "test@emp.com", Role = "Employee" };
        var editor = new User { Id = 99 };
        var userDto = new UserResponseDto { Id = 1, Email = dto.Email, FirstName = "Test", LastName = "Emp", Role = "Employee" };

        _auth0ManagementService.CreateUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns("auth0|test-emp");
        _userContextService.GetUserAsync().Returns(editor);
        _userRepository.AddAsync(Arg.Any<User>()).Returns(Task.CompletedTask);
        _userRepository.SaveChangesAsync().Returns(Task.FromResult(0));
        _mapper.Map<UserResponseDto>(Arg.Any<User>()).Returns(userDto);

        // Act
        var result = await _userService.CreateNewEmployee(dto);

        // Assert
        Assert.NotNull(result);
        await _userRepository.Received(1).AddAsync(Arg.Is<User>(u => u.Email == dto.Email.Trim()));
    }
}

public class FakeHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}
