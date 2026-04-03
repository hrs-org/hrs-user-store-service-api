using HRS.Domain.Entities;
using HRS.Shared.Core.Enums;
using HRS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRS.Infrastructure.Repositories;

public class UserRepository : CrudRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext db) : base(db)
    {
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var normalizedEmail = email.Trim();
        return await _db.Users
            .Include(u => u.Store)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail);
    }

    public async Task<User?> GetByAuth0IdAsync(string auth0Id)
        => await _db.Users
            .Include(u => u.Store)
            .FirstOrDefaultAsync(u => u.Auth0UserId == auth0Id);

    public async Task UpdateUserAsync(User user)
    {
        var dbUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == user.Id);

        if (dbUser == null)
            throw new KeyNotFoundException($"User with Id {user.Id} not found");

        dbUser.Auth0UserId = user.Auth0UserId;
        dbUser.FirstName = user.FirstName;
        dbUser.LastName = user.LastName;
        dbUser.Email = user.Email;
        dbUser.Role = user.Role;
        dbUser.StoreId = user.StoreId;
        dbUser.UpdatedBy = user.UpdatedBy;
        dbUser.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task<List<User>> GetAllEmployee(int? storeId, bool includeManagers = false)
    {
        var query = _db.Users.Where(u => u.StoreId == storeId);

        if (includeManagers)
            query = query.Where(u => u.Role == UserRole.Manager || u.Role == UserRole.Employee);
        else
            query = query.Where(u => u.Role == UserRole.Employee);

        return await query.ToListAsync();
    }

    public async Task<bool> IsEmailUniqueAsync(string email)
    {
        var normalizedEmail = email.Trim();
        return !await _db.Users.AnyAsync(u => u.Email == normalizedEmail);
    }

    public async Task<bool> IsIdUniqueAsync(int id) => !await _db.Users.AnyAsync(u => u.Id == id);
}
