using HRS.Shared.Core.Dtos;
using HRS.API.Contracts.DTOs.User;
using HRS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRS.API.Controllers;

[ApiController]
[Route("api/users")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "S6960", Justification = "User and employee operations share the same service layer and are intentionally co-located.")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IUserContextService _userContextService;

    public UsersController(IUserService userService, IUserContextService userContextService)
    {
        _userService = userService;
        _userContextService = userContextService;
    }

    [HttpGet("validate")]
    [Authorize]
    public async Task<ActionResult<bool>> ValidateUserExists()
    {
        var user = await _userContextService.GetUserByAuth0IdAsync();
        return Ok(ApiResponse<bool>.OkResponse(user != null));
    }

    [HttpGet("active-user")]
    [Authorize(Policy = "read:user")]
    public async Task<IActionResult> GetCurrentUserAsync()
    {
        var res = await _userContextService.GetUserDtoAsync();
        return Ok(ApiResponse<UserResponseDto>.OkResponse(res, "Get current user successful"));
    }

    [HttpGet]
    [Authorize(Policy = "read:user")]
    public async Task<ActionResult<List<UserResponseDto>>> GetUsersAsync()
    {
        var users = await _userService.GetUsers();
        return Ok(ApiResponse<List<UserResponseDto>>.OkResponse(users.ToList()));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "read:user")]
    public async Task<ActionResult<UserResponseDto>> GetUserAsync(int id)
    {
        var user = await _userService.GetUserById(id);
        return Ok(ApiResponse<UserResponseDto>.OkResponse(user));
    }

    [HttpPost("register/customer")]
    public async Task<ActionResult<bool>> Register([FromBody] RegisterDto dto)
    {
        var res = await _userService.Register(dto);
        return Ok(ApiResponse<bool>.OkResponse(res, "Registration successful"));
    }

    [HttpPost("sync-auth0-metadata")]
    [Authorize]
    public async Task<ActionResult<bool>> SyncAuth0Metadata()
    {
        var res = await _userService.SyncCurrentUserMetadataAsync();
        return Ok(ApiResponse<bool>.OkResponse(res, "Auth0 metadata synced successfully"));
    }

    [HttpGet("employees")]
    [Authorize(Policy = "read:employee")]
    public async Task<ActionResult<List<UserResponseDto>>> GetEmployees()
    {
        var employeeList = await _userService.GetEmployees();
        return Ok(ApiResponse<List<UserResponseDto>>.OkResponse(employeeList));
    }

    [HttpPut("employees")]
    [Authorize(Policy = "update:employee")]
    public async Task<ActionResult<UserResponseDto>> UpdateEmployee([FromBody] UpdateEmployeeDto dto)
    {
        var updatedEmployee = await _userService.UpdateEmployee(dto);
        if (updatedEmployee == null) return NotFound();
        return Ok(ApiResponse<UserResponseDto>.OkResponse(updatedEmployee));
    }

    [HttpDelete("employees/{id:int}")]
    [Authorize(Policy = "delete:employee")]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        var success = await _userService.DeleteEmployee(id);
        if (!success) return NotFound();
        return Ok(ApiResponse<bool>.OkResponse(true, "Employee deleted successfully"));
    }

    [HttpPost("employees/add")]
    [Authorize(Policy = "write:employee")]
    public async Task<ActionResult<UserResponseDto>> CreateNewEmployee([FromBody] RegisterEmployeeDetailDto dto)
    {
        var createdUser = await _userService.CreateNewEmployee(dto);
        return Ok(ApiResponse<UserResponseDto>.OkResponse(createdUser, "Employee Created successfully"));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "delete:user")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var success = await _userService.DeleteUser(id);
        if (!success) return NotFound();
        return NoContent();
    }
}
