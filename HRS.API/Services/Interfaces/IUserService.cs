using HRS.API.Contracts.DTOs.User;
using HRS.Shared.Core.Dtos;
namespace HRS.API.Services.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserResponseDto>> GetUsers();
    Task<UserResponseDto> GetUserById(int id);
    Task<bool> Register(RegisterDto dto);
    Task<bool> SyncCurrentUserMetadataAsync();
    Task<bool> DeleteUser(int id);
    Task<List<UserResponseDto>> GetEmployees();
    Task<UserResponseDto?> UpdateEmployee(UpdateEmployeeDto dto);
    Task<bool> DeleteEmployee(int id);
    Task<UserResponseDto> CreateNewEmployee(RegisterEmployeeDetailDto dto);
}
