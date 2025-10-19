using HRS.Domain.Entities;

namespace HRS.Domain.Interfaces;

public interface IStoreRepository : ICrudRepository<Store>
{
    Task<Store?> GetByUserIdAsync(int userId);
    Task<Store?> GetByNameAsync(string name);
    Task<bool> IsNameUniqueAsync(string name);
    Task<bool> IsIdUniqueAsync(int id);
}

