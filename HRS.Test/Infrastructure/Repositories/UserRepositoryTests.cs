using HRS.Domain.Entities;
using HRS.Shared.Core.Enums;
using HRS.Infrastructure;
using HRS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HRS.Test.Infrastructure.Repositories;

public class UserRepositoryTests
{
    private static AppDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsUser_WhenExists()
    {
        // Arrange
        var dbName = $"UserRepoDb_{nameof(GetByEmailAsync_ReturnsUser_WhenExists)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repository = new UserRepository(dbContext);
        var user = new User { Id = 2, Auth0UserId = "auth0|user2", FirstName = "Evan", LastName = "Feri", Email = "test@mail.com", Role = UserRole.Manager };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await repository.GetByEmailAsync("test@mail.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test@mail.com", result!.Email);
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var dbName = $"UserRepoDb_{nameof(GetByEmailAsync_ReturnsNull_WhenNotFound)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repository = new UserRepository(dbContext);

        // Act
        var result = await repository.GetByEmailAsync("notfound@mail.com");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateUserAsync_UpdatesToken_WhenUserExists()
    {
        // Arrange
        var dbName = $"UserRepoDb_{nameof(UpdateUserAsync_UpdatesToken_WhenUserExists)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repository = new UserRepository(dbContext);
        var user = new User { Id = 1, Auth0UserId = "auth0|user1", FirstName = "Evan", LastName = "Feri", Email = "test@mail.com", Role = UserRole.Manager };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var update = new User
        {
            Id = 1
        };

        // Act
        await repository.UpdateUserAsync(update);

        // Assert
        var updated = await dbContext.Users.FindAsync(1);
        Assert.NotNull(updated);
    }

    [Fact]
    public async Task UpdateUserAsync_Throws_WhenUserNotFound()
    {
        // Arrange
        var dbName = $"UserRepoDb_{nameof(UpdateUserAsync_Throws_WhenUserNotFound)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repository = new UserRepository(dbContext);
        var update = new User { Id = 999 };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => repository.UpdateUserAsync(update));
    }

    [Fact]
    public async Task GetAllEmployee_ReturnsOnlyEmployees()
    {
        // Arrange
        var dbName = $"UserRepoDb_{nameof(GetAllEmployee_ReturnsOnlyEmployees)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repository = new UserRepository(dbContext);
        dbContext.Users.AddRange(
            new User { Id = 1, Auth0UserId = "auth0|user1", FirstName = "Evan", LastName = "Feri", Email = "test@mail.com", Role = UserRole.Employee, StoreId = 1 },
            new User { Id = 2, Auth0UserId = "auth0|user2", FirstName = "Jaseper", LastName = "Shen", Email = "test2@mail.com", Role = UserRole.Manager, StoreId = 1 }
        );
        await dbContext.SaveChangesAsync();

        // Act
        var employees = await repository.GetAllEmployee(1, false);

        // Assert
        Assert.Single(employees);
        Assert.Equal(UserRole.Employee, employees.First().Role);
    }

    [Fact]
    public async Task IsEmailUniqueAsync_ReturnsFalse_WhenEmailExists()
    {
        // Arrange
        var dbName = $"UserRepoDb_{nameof(IsEmailUniqueAsync_ReturnsFalse_WhenEmailExists)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repository = new UserRepository(dbContext);
        dbContext.Users.Add(new User
        { Id = 1, Auth0UserId = "auth0|user1", FirstName = "Evan", LastName = "Feri", Email = "test@mail.com", Role = UserRole.Employee });
        await dbContext.SaveChangesAsync();

        // Act
        var result = await repository.IsEmailUniqueAsync("test@mail.com");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsEmailUniqueAsync_ReturnsTrue_WhenEmailNotExists()
    {
        // Arrange
        var dbName = $"UserRepoDb_{nameof(IsEmailUniqueAsync_ReturnsTrue_WhenEmailNotExists)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repository = new UserRepository(dbContext);

        // Act
        var result = await repository.IsEmailUniqueAsync("unique@mail.com");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsIdUniqueAsync_ReturnsFalse_WhenIdExists()
    {
        // Arrange
        var dbName = $"UserRepoDb_{nameof(IsIdUniqueAsync_ReturnsFalse_WhenIdExists)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repository = new UserRepository(dbContext);
        dbContext.Users.Add(new User
        { Id = 3, Auth0UserId = "auth0|user3", FirstName = "Evan", LastName = "Feri", Email = "test@mail.com", Role = UserRole.Employee });
        await dbContext.SaveChangesAsync();

        // Act
        var result = await repository.IsIdUniqueAsync(3);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsIdUniqueAsync_ReturnsTrue_WhenIdNotExists()
    {
        // Arrange
        var dbName = $"UserRepoDb_{nameof(IsIdUniqueAsync_ReturnsTrue_WhenIdNotExists)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repository = new UserRepository(dbContext);

        // Act
        var result = await repository.IsIdUniqueAsync(123);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetByAuth0IdAsync_ReturnsUser_WhenExists()
    {
        // Arrange
        var dbName = $"UserRepoDb_{nameof(GetByAuth0IdAsync_ReturnsUser_WhenExists)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repository = new UserRepository(dbContext);
        var auth0Id = "auth0|abc123xyz";
        var user = new User { Id = 1, Auth0UserId = auth0Id, FirstName = "John", LastName = "Doe", Email = "john@test.com", Role = UserRole.Employee };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await repository.GetByAuth0IdAsync(auth0Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(auth0Id, result!.Auth0UserId);
        Assert.Equal("John", result.FirstName);
    }

    [Fact]
    public async Task GetByAuth0IdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var dbName = $"UserRepoDb_{nameof(GetByAuth0IdAsync_ReturnsNull_WhenNotFound)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repository = new UserRepository(dbContext);

        // Act
        var result = await repository.GetByAuth0IdAsync("nonexistent|auth0id");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByAuth0IdAsync_IncludesStore_WhenRelated()
    {
        // Arrange
        var dbName = $"UserRepoDb_{nameof(GetByAuth0IdAsync_IncludesStore_WhenRelated)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repository = new UserRepository(dbContext);
        var store = new Store { Id = 1, Name = "Test Store" };
        var user = new User { Id = 1, Auth0UserId = "auth0|store-user", FirstName = "Store", LastName = "Owner", Email = "owner@store.com", Role = UserRole.Owner, Store = store, StoreId = 1 };
        dbContext.Stores.Add(store);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await repository.GetByAuth0IdAsync("auth0|store-user");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result!.Store);
        Assert.Equal("Test Store", result.Store.Name);
    }

    [Fact]
    public async Task GetByEmailAsync_NormalizesEmail_BeforeQuery()
    {
        // Arrange
        var dbName = $"UserRepoDb_{nameof(GetByEmailAsync_NormalizesEmail_BeforeQuery)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repository = new UserRepository(dbContext);
        var user = new User { Id = 1, Auth0UserId = "auth0|norm", FirstName = "Norm", LastName = "User", Email = "NORM@TEST.COM", Role = UserRole.Customer };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await repository.GetByEmailAsync("  NORM@TEST.COM  "); // Extra spaces should be trimmed

        // Assert
        Assert.NotNull(result);
        Assert.Equal("NORM@TEST.COM", result!.Email);
    }

    [Fact]
    public async Task UpdateUserAsync_UpdatesAllFields_WhenProvided()
    {
        // Arrange
        var dbName = $"UserRepoDb_{nameof(UpdateUserAsync_UpdatesAllFields_WhenProvided)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repository = new UserRepository(dbContext);
        var user = new User { Id = 1, Auth0UserId = "auth0|old", FirstName = "Old", LastName = "Name", Email = "old@test.com", Role = UserRole.Employee, StoreId = 1 };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var updateData = new User
        {
            Id = 1,
            Auth0UserId = "auth0|new",
            FirstName = "New",
            LastName = "Updated",
            Email = "new@test.com",
            Role = UserRole.Manager,
            StoreId = 2,
            UpdatedBy = 99
        };

        // Act
        await repository.UpdateUserAsync(updateData);

        // Assert
        var updated = await dbContext.Users.FindAsync(1);
        Assert.NotNull(updated);
        Assert.Equal("auth0|new", updated!.Auth0UserId);
        Assert.Equal("New", updated.FirstName);
        Assert.Equal("Updated", updated.LastName);
        Assert.Equal("new@test.com", updated.Email);
        Assert.Equal(UserRole.Manager, updated.Role);
        Assert.Equal(2, updated.StoreId);
        Assert.Equal(99, updated.UpdatedBy);
    }

    [Fact]
    public async Task GetAllEmployee_IncludesManagers_WhenFlagIsTrue()
    {
        // Arrange
        var dbName = $"UserRepoDb_{nameof(GetAllEmployee_IncludesManagers_WhenFlagIsTrue)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repository = new UserRepository(dbContext);
        dbContext.Users.AddRange(
            new User { Id = 1, Auth0UserId = "auth0|emp1", FirstName = "E1", LastName = "M1", Email = "e1@test.com", Role = UserRole.Employee, StoreId = 1 },
            new User { Id = 2, Auth0UserId = "auth0|mgr1", FirstName = "M1", LastName = "Name", Email = "m1@test.com", Role = UserRole.Manager, StoreId = 1 },
            new User { Id = 3, Auth0UserId = "auth0|cust1", FirstName = "C1", LastName = "Name", Email = "c1@test.com", Role = UserRole.Customer, StoreId = 1 }
        );
        await dbContext.SaveChangesAsync();

        // Act
        var result = await repository.GetAllEmployee(1, true);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, u => u.Role == UserRole.Manager);
        Assert.Contains(result, u => u.Role == UserRole.Employee);
    }

    [Fact]
    public async Task GetAllEmployee_FiltersOtherStores_Correctly()
    {
        // Arrange
        var dbName = $"UserRepoDb_{nameof(GetAllEmployee_FiltersOtherStores_Correctly)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repository = new UserRepository(dbContext);
        dbContext.Users.AddRange(
            new User { Id = 1, Auth0UserId = "auth0|s1e1", FirstName = "Store1", LastName = "Emp1", Email = "s1e1@test.com", Role = UserRole.Employee, StoreId = 1 },
            new User { Id = 2, Auth0UserId = "auth0|s2e1", FirstName = "Store2", LastName = "Emp1", Email = "s2e1@test.com", Role = UserRole.Employee, StoreId = 2 }
        );
        await dbContext.SaveChangesAsync();

        // Act
        var result = await repository.GetAllEmployee(1);

        // Assert
        Assert.Single(result);
        Assert.Equal(1, result.First().StoreId);
    }

    [Fact]
    public async Task GetByEmailAsync_IncludesStore_WhenUserHasStore()
    {
        // Arrange
        var dbName = $"UserRepoDb_{nameof(GetByEmailAsync_IncludesStore_WhenUserHasStore)}_{Guid.NewGuid()}";
        using var dbContext = CreateDbContext(dbName);
        var repository = new UserRepository(dbContext);
        var store = new Store { Id = 1, Name = "Shop" };
        var user = new User { Id = 1, Auth0UserId = "auth0|shopowner", FirstName = "Shop", LastName = "Owner", Email = "shop@test.com", Role = UserRole.Owner, Store = store, StoreId = 1 };
        dbContext.Stores.Add(store);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await repository.GetByEmailAsync("shop@test.com");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result!.Store);
        Assert.Equal("Shop", result.Store.Name);
    }
}
