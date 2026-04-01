using AutoMapper;
using HRS.API.Contracts.DTOs.User;
using HRS.API.Services.Interfaces;
using HRS.Domain.Entities;
using HRS.Shared.Core.Enums;
using HRS.Domain.Interfaces;
using HRS.Shared.Core.Dtos;

namespace HRS.API.Services;

public class UserService : IUserService
{
    private readonly IMapper _mapper;
    private readonly IAuth0ManagementService _auth0ManagementService;
    private readonly IUserContextService _userContextService;
    private readonly IUserRepository _userRepository;

    public UserService(
        IMapper mapper,
        IAuth0ManagementService auth0ManagementService,
        IUserRepository userRepository,
        IUserContextService userContextService)
    {
        _mapper = mapper;
        _auth0ManagementService = auth0ManagementService;
        _userRepository = userRepository;
        _userContextService = userContextService;
    }

    public async Task<IEnumerable<UserResponseDto>> GetUsers()
    {
        var users = await _userRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<UserResponseDto>>(users);
    }

    public async Task<UserResponseDto> GetUserById(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        return user == null ? throw new InvalidOperationException("User not found") : _mapper.Map<UserResponseDto>(user);
    }

    public async Task<bool> Register(RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            throw new ArgumentException("Email is required");

        var normalizedEmail = dto.Email.Trim();
        var existingUser = await _userRepository.GetByEmailAsync(normalizedEmail);
        if (existingUser != null)
            throw new InvalidOperationException("User with this email already exists");

        var user = new User
        {
            Auth0UserId = _userContextService.GetAuth0Id(),
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = normalizedEmail,
            Role = UserRole.Customer,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();
        await _auth0ManagementService.SyncUserRoleAsync(user.Auth0UserId, UserRole.Customer);
        await _auth0ManagementService.SyncUserMetadataAsync(user.Auth0UserId, user.Id, user.StoreId);

        return true;
    }

    public async Task<bool> SyncCurrentUserMetadataAsync()
    {
        var user = await _userContextService.GetUserAsync();
        await _auth0ManagementService.SyncUserMetadataAsync(user.Auth0UserId, user.Id, user.StoreId);
        return true;
    }

    public async Task<bool> DeleteUser(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) throw new KeyNotFoundException("User not found.");
        _userRepository.Remove(user);
        return true;
    }

    public async Task<List<UserResponseDto>> GetEmployees()
    {
        var user = await _userContextService.GetUserAsync();
        var employee = await _userRepository.GetAllEmployee(user.StoreId/*, user.Role == UserRole.Admin*/);
        return _mapper.Map<List<UserResponseDto>>(employee);
    }

    public async Task<UserResponseDto?> UpdateEmployee(UpdateEmployeeDto dto)
    {
        var editor = await _userContextService.GetUserAsync();
        var employee = await _userRepository.GetByIdAsync(dto.Id);
        if (employee == null) throw new KeyNotFoundException("User not found.");
        // if (employee.Role == UserRole.Customer) throw new InvalidOperationException("Cannot update a customer to an employee.");

        if (Enum.TryParse<UserRole>(dto.Role, true, out var parsedRole) &&
            (parsedRole == UserRole.Employee || parsedRole == UserRole.Manager || parsedRole == UserRole.Admin))
        {
            employee.FirstName = dto.FirstName;
            employee.LastName = dto.LastName;
            employee.Email = dto.Email;
            employee.Role = parsedRole;
            employee.UpdatedAt = DateTime.UtcNow;
            employee.UpdatedBy = editor.Id;

            await _auth0ManagementService.SyncUserRoleAsync(employee.Auth0UserId, parsedRole);
            await _auth0ManagementService.SyncUserMetadataAsync(employee.Auth0UserId, employee.Id, employee.StoreId);
        }
        else
        {
            throw new ArgumentException("Invalid role specified.");
        }

        await _userRepository.SaveChangesAsync();
        return _mapper.Map<UserResponseDto>(employee);
    }

    public async Task<bool> DeleteEmployee(int id)
    {
        var employee = await _userRepository.GetByIdAsync(id) ?? throw new KeyNotFoundException("User not found.");
        // if (employee.Role == UserRole.Customer) throw new InvalidOperationException("Cannot delete a customer as an employee.");

        _userRepository.Remove(employee);
        await _userRepository.SaveChangesAsync();
        return true;
    }

    public async Task<UserResponseDto> CreateNewEmployee(RegisterEmployeeDetailDto dto)
    {
        var editor = await _userContextService.GetUserAsync();

        var auth0UserId = await _auth0ManagementService.CreateUserAsync(
            dto.Email.Trim(),
            dto.FirstName.Trim(),
            dto.LastName.Trim());

        var role = Enum.Parse<UserRole>(dto.Role, ignoreCase: true);

        var user = new User
        {
            Auth0UserId = auth0UserId,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = dto.Email.Trim(),
            Role = role,
            StoreId = editor.StoreId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = editor.Id
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        await _auth0ManagementService.SyncUserRoleAsync(auth0UserId, role);
        await _auth0ManagementService.SyncUserMetadataAsync(auth0UserId, user.Id, user.StoreId);

        return _mapper.Map<UserResponseDto>(user);
    }
}
