using HRS.Shared.Core.Dtos;
using HRS.API.Contracts.DTOs.Store;
using HRS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRS.API.Controllers;

[ApiController]
[Route("api/stores")]
public class StoreController : ControllerBase
{
    private readonly IStoreService _storeService;

    public StoreController(IStoreService storeService)
    {
        _storeService = storeService;
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "read:store")]
    public async Task<ActionResult<StoreDto>> GetStoreById(int id)
    {
        var store = await _storeService.GetStoreByIdAsync(id);
        return Ok(ApiResponse<StoreDto>.OkResponse(store));
    }

    [HttpGet("users/{userId:int}")]
    [Authorize(Policy = "read:store")]
    public async Task<ActionResult<StoreDto>> GetStoreByUserId(int userId)
    {
        var store = await _storeService.GetStoreByUserIdAsync(userId);
        return Ok(ApiResponse<StoreDto>.OkResponse(store));
    }

    [HttpGet]
    [Authorize(Policy = "read:store")]
    public async Task<ActionResult> GetAllStores([FromQuery] int? userId)
    {
        if (userId.HasValue)
        {
            var store = await _storeService.GetStoreByUserIdAsync(userId.Value);
            return Ok(ApiResponse<StoreDto>.OkResponse(store));
        }

        var stores = await _storeService.GetAllStoresAsync();
        return Ok(ApiResponse<List<StoreDto>>.OkResponse(stores.ToList()));
    }

    [HttpPost("register")]
    public async Task<ActionResult<bool>> RegisterStore([FromBody] RegisterStoreDto dto)
    {
        var res = await _storeService.RegisterStoreAsync(dto);
        return Ok(ApiResponse<bool>.OkResponse(res, "Store registration successful"));
    }
}
