using HRS.Domain.Entities;
using HRS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRS.Infrastructure.Repositories;

public class StoreRepository : CrudRepository<Store>, IStoreRepository
{
    public StoreRepository(AppDbContext db) : base(db)
    {
    }

    public async Task<Store?> GetByUserIdAsync(int userId)
    {
        var user = await _db.Users
            .Include(u => u.Store)
            .FirstOrDefaultAsync(u => u.Id == userId);

        return user?.Store;
    }

    public async Task<Store?> GetByNameAsync(string name)
    {
        var normalizedName = name.Trim();
        return await _db.Stores.FirstOrDefaultAsync(s => s.Name == normalizedName);
    }

    public async Task<bool> IsNameUniqueAsync(string name)
    {
        var normalizedName = name.Trim();
        return !await _db.Stores.AnyAsync(s => s.Name == normalizedName);
    }

    public async Task<bool> IsIdUniqueAsync(int id)
        => !await _db.Stores.AnyAsync(s => s.Id == id);
}
