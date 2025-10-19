using HRS.API.Contracts.DTOs.Store;

namespace HRS.API.Services.Interfaces;

public interface IStoreService
{
    Task<StoreDto> GetStoreByIdAsync(int id);
    Task<IEnumerable<StoreDto>> GetAllStoresAsync();
    Task<StoreDto> GetStoreByUserIdAsync(int userId);
    Task<bool> RegisterStoreAsync(RegisterStoreDto dto);
}
