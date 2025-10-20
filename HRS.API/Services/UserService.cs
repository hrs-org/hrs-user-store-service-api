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
    private readonly IUserContextService _userContextService;
    private readonly IUserRepository _userRepository;
    private readonly IUserVerificationService _userVerificationService;
    private readonly HttpClient _httpClient;

    public UserService(
        IMapper mapper,
        IUserRepository userRepository,
        IUserContextService userContextService,
        IUserVerificationService userVerificationService,
        IHttpClientFactory httpClientFactory)
    {
        _mapper = mapper;
        _userRepository = userRepository;
        _userContextService = userContextService;
        _userVerificationService = userVerificationService;
        _httpClient = httpClientFactory.CreateClient("EmailService");
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
        var existingUser = await _userRepository.GetByEmailAsync(dto.Email);
        if (existingUser != null)
            throw new InvalidOperationException("User with this email already exists.");
        if (dto.Password.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters long.");

        var user = _mapper.Map<User>(dto);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        user.IsVerified = false;

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        var verification = await _userVerificationService.CreateAsync(
            user.Id,
            "Email",
            TimeSpan.FromHours(24)
        );

        var verificationRequest = new
        {
            Email = user.Email,
            VerificationToken = verification.Token,
            FirstName = user.FirstName
        };
        var response = await _httpClient.PostAsJsonAsync("/api/email/send-verification", verificationRequest);
        response.EnsureSuccessStatusCode();
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
        var employee = await _userRepository.GetAllEmployee(user.StoreId, user.Role == UserRole.Admin);
        return _mapper.Map<List<UserResponseDto>>(employee);
    }

    public async Task<UserResponseDto?> UpdateEmployee(UpdateEmployeeDto dto)
    {
        var editor = await _userContextService.GetUserAsync();
        var employee = await _userRepository.GetByIdAsync(dto.Id);
        if (employee == null) throw new KeyNotFoundException("User not found.");
        if (employee.Role == UserRole.Customer) throw new InvalidOperationException("Cannot update a customer to an employee.");

        if (dto.Role == "Employee" || dto.Role == "Manager")
        {
            var role = Enum.Parse<UserRole>(dto.Role);
            employee.FirstName = dto.FirstName;
            employee.LastName = dto.LastName;
            employee.Email = dto.Email;
            employee.Role = role;
            employee.UpdatedAt = DateTime.UtcNow;
            employee.UpdatedBy = editor.Id;
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
        if (employee.Role == UserRole.Customer) throw new InvalidOperationException("Cannot delete a customer as an employee.");

        _userRepository.Remove(employee);
        await _userRepository.SaveChangesAsync();
        return true;
    }

    public async Task<UserResponseDto> CreateNewEmployee(RegisterEmployeeDetailDto dto)
    {
        var user = _mapper.Map<User>(dto);
        var editor = await _userContextService.GetUserAsync();
        var OriginPassword = Guid.NewGuid().ToString("N")[..8];
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = editor.Id;
        user.StoreId = editor.StoreId;
        user.IsVerified = true;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(OriginPassword);

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        var SendEmployeeWelcomeEmailRequest = new
        {
            Email = user.Email,
            Password = OriginPassword,
            FirstName = user.FirstName
        };
        var response = await _httpClient.PostAsJsonAsync("/api/email/send-employee-welcome", SendEmployeeWelcomeEmailRequest);
        response.EnsureSuccessStatusCode();

        //Send email to user with password setup link
        return _mapper.Map<UserResponseDto>(user);
    }
}
