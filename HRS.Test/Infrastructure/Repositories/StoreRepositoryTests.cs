using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using HRS.Infrastructure.Repositories;
using HRS.Infrastructure;
using HRS.Domain.Entities;

namespace HRS.Test.Infrastructure.Repositories;

public class StoreRepositoryTests
{
    private AppDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetByNameAsync_ReturnsStore_WhenExists()
    {
        using var db = CreateInMemoryContext("GetByNameExists");
        var repo = new StoreRepository(db);

        var store = new Store { Name = "TestStore" };
        await repo.AddAsync(store);
        await repo.SaveChangesAsync();

        var found = await repo.GetByNameAsync("TestStore");

        Assert.NotNull(found);
        Assert.Equal("TestStore", found.Name);
    }

    [Fact]
    public async Task GetByNameAsync_ReturnsNull_WhenNotExists()
    {
        using var db = CreateInMemoryContext("GetByNameNotExists");
        var repo = new StoreRepository(db);

        var found = await repo.GetByNameAsync("NoSuch");

        Assert.Null(found);
    }

    [Fact]
    public async Task IsNameUniqueAsync_ReturnsFalse_WhenExists()
    {
        using var db = CreateInMemoryContext("IsNameUniqueFalse");
        var repo = new StoreRepository(db);

        var store = new Store { Name = "Dup" };
        await repo.AddAsync(store);
        await repo.SaveChangesAsync();

        var result = await repo.IsNameUniqueAsync("Dup");

        Assert.False(result);
    }

    [Fact]
    public async Task IsNameUniqueAsync_ReturnsTrue_WhenNotExists()
    {
        using var db = CreateInMemoryContext("IsNameUniqueTrue");
        var repo = new StoreRepository(db);

        var result = await repo.IsNameUniqueAsync("UniqueName");

        Assert.True(result);
    }

    [Fact]
    public async Task IsIdUniqueAsync_ReturnsFalse_WhenExists()
    {
        using var db = CreateInMemoryContext("IsIdUniqueFalse");
        var repo = new StoreRepository(db);

        var store = new Store { Name = "S1" };
        await repo.AddAsync(store);
        await repo.SaveChangesAsync();

        var result = await repo.IsIdUniqueAsync(store.Id);

        Assert.False(result);
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsStore_WhenUserHasStore()
    {
        using var db = CreateInMemoryContext("GetByUserId");
        var repo = new StoreRepository(db);

        var store = new Store { Name = "UserStore" };
        await repo.AddAsync(store);
        await repo.SaveChangesAsync();

        var user = new HRS.Domain.Entities.User { Auth0UserId = "auth0|user1", FirstName = "F", LastName = "L", Email = "u@e.com", StoreId = store.Id };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var found = await repo.GetByUserIdAsync(user.Id);

        Assert.NotNull(found);
        Assert.Equal(store.Id, found.Id);
    }
}
