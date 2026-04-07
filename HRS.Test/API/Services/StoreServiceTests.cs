using AutoMapper;
using NSubstitute;
using Xunit;
using HRS.API.Services;
using HRS.API.Services.Interfaces;
using HRS.Domain.Entities;
using HRS.Domain.Interfaces;
using HRS.API.Contracts.DTOs.Store;
using HRS.Shared.Core.Enums;

namespace HRS.Test.API.Services;

public class StoreServiceTests
{
    private readonly IStoreRepository _storeRepository;
    private readonly IAuth0ManagementService _auth0ManagementService;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly StoreService _storeService;
    private readonly IUserContextService _userContextService;

    public StoreServiceTests()
    {
        _storeRepository = Substitute.For<IStoreRepository>();
        _auth0ManagementService = Substitute.For<IAuth0ManagementService>();
        _userRepository = Substitute.For<IUserRepository>();
        _mapper = Substitute.For<IMapper>();
        _userContextService = Substitute.For<IUserContextService>();

        _storeService = new StoreService(
            _storeRepository,
            _auth0ManagementService,
            _userRepository,
            _mapper,
            _userContextService
        );
    }

    [Fact]
    public async Task GetStoreByIdAsync_ReturnsStoreDto_WhenStoreExists()
    {
        // Arrange
        var store = new Store { Id = 1, Name = "Test Store" };
        var storeDto = new StoreDto { Id = 1, Name = "Test Store" };
        _storeRepository.GetByIdAsync(1).Returns(store);
        _mapper.Map<StoreDto>(store).Returns(storeDto);

        // Act
        var result = await _storeService.GetStoreByIdAsync(1);

        // Assert
        Assert.Equal(storeDto, result);
        await _storeRepository.Received(1).GetByIdAsync(1);
    }

    [Fact]
    public async Task GetStoreByIdAsync_ThrowsKeyNotFoundException_WhenStoreNotFound()
    {
        // Arrange
        _storeRepository.GetByIdAsync(1).Returns((Store?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _storeService.GetStoreByIdAsync(1));
    }

    [Fact]
    public async Task GetAllStoresAsync_ReturnsMappedStoreDtos()
    {
        // Arrange
        var stores = new List<Store> { new() { Id = 1, Name = "Store1" }, new() { Id = 2, Name = "Store2" } };
        var storeDtos = new List<StoreDto> { new() { Id = 1, Name = "Store1" }, new() { Id = 2, Name = "Store2" } };
        _storeRepository.GetAllAsync().Returns(stores);
        _mapper.Map<IEnumerable<StoreDto>>(stores).Returns(storeDtos);

        // Act
        var result = await _storeService.GetAllStoresAsync();

        // Assert
        Assert.Equal(storeDtos, result);
    }

    [Fact]
    public async Task GetStoreByUserIdAsync_ReturnsStoreDto_WhenStoreExists()
    {
        // Arrange
        var store = new Store { Id = 1, Name = "User Store" };
        var storeDto = new StoreDto { Id = 1, Name = "User Store" };
        _storeRepository.GetByUserIdAsync(10).Returns(store);
        _mapper.Map<StoreDto>(store).Returns(storeDto);

        // Act
        var result = await _storeService.GetStoreByUserIdAsync(10);

        // Assert
        Assert.Equal(storeDto, result);
    }

    [Fact]
    public async Task GetStoreByUserIdAsync_ThrowsKeyNotFoundException_WhenStoreNotFound()
    {
        // Arrange
        _storeRepository.GetByUserIdAsync(10).Returns((Store?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _storeService.GetStoreByUserIdAsync(10));
    }

    [Fact]
    public async Task RegisterStoreAsync_ReturnsTrue_WhenAllValid()
    {
        // Arrange
        var dto = new RegisterStoreDto
        {
            Name = "New Store",
            Email = "admin@store.com",
            FirstName = "Admin",
            LastName = "User"
        };


        _userRepository.GetByEmailAsync(dto.Email).Returns((User?)null);
        _storeRepository.GetByNameAsync(dto.Name).Returns((Store?)null);
        _storeRepository.BeginTransactionAsync().Returns(Substitute.For<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>());
        _userContextService.GetAuth0Id().Returns("auth0|owner-1");

        // Act
        var result = await _storeService.RegisterStoreAsync(dto);

        // Assert
        Assert.True(result);
        await _storeRepository.Received(1).AddAsync(Arg.Any<Store>());
        await _storeRepository.Received(1).SaveChangesAsync();
        await _userRepository.Received(1).AddAsync(Arg.Is<User>(u =>
            u.Email == dto.Email &&
            u.FirstName == dto.FirstName &&
            u.Role == UserRole.Owner));
        await _userRepository.Received(1).SaveChangesAsync();
        await _auth0ManagementService.Received(1).SyncUserRoleAsync("auth0|owner-1", UserRole.Owner);
    }

    [Fact]
    public async Task RegisterStoreAsync_ThrowsInvalidOperationException_WhenUserEmailExists()
    {
        // Arrange
        var dto = new RegisterStoreDto
        {
            Name = "New Store",
            Email = "existing@store.com",
            FirstName = "Admin",
            LastName = "User"
        };

        var existingUser = new User { Id = 1, Email = dto.Email };
        _userRepository.GetByEmailAsync(dto.Email).Returns(existingUser);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _storeService.RegisterStoreAsync(dto));
        await _storeRepository.DidNotReceive().AddAsync(Arg.Any<Store>());
    }

    [Fact]
    public async Task RegisterStoreAsync_ThrowsInvalidOperationException_WhenStoreNameExists()
    {
        // Arrange
        var dto = new RegisterStoreDto
        {
            Name = "Existing Store",
            Email = "admin@store.com",
            FirstName = "Admin",
            LastName = "User"
        };

        var existingStore = new Store { Id = 1, Name = dto.Name };
        _userRepository.GetByEmailAsync(dto.Email).Returns((User?)null);
        _storeRepository.GetByNameAsync(dto.Name).Returns(existingStore);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _storeService.RegisterStoreAsync(dto));
        await _storeRepository.DidNotReceive().AddAsync(Arg.Any<Store>());
    }

    [Fact]
    public async Task RegisterStoreAsync_ThrowsArgumentException_WhenEmailMissing()
    {
        // Arrange
        var dto = new RegisterStoreDto
        {
            Name = "New Store",
            Email = "   ",
            FirstName = "Admin",
            LastName = "User"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _storeService.RegisterStoreAsync(dto));
        await _storeRepository.DidNotReceive().AddAsync(Arg.Any<Store>());
    }

    [Fact]
    public async Task RegisterStoreAsync_ThrowsArgumentException_WhenStoreName_IsMissing()
    {
        // Arrange
        var dto = new RegisterStoreDto
        {
            Name = "   ", // Empty store name
            Email = "admin@store.com",
            FirstName = "Admin",
            LastName = "User"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _storeService.RegisterStoreAsync(dto));
    }

    [Fact]
    public async Task RegisterStoreAsync_CreatesStoreWithOwnerUser_WhenAllValid()
    {
        // Arrange
        var dto = new RegisterStoreDto
        {
            Name = "Premium Store",
            Email = "owner@premium.com",
            FirstName = "Premium",
            LastName = "Owner",
            Description = "A premium store",
            Address = "123 Main St",
            PhoneNumber = "555-1234"
        };

        _userRepository.GetByEmailAsync(dto.Email).Returns((User?)null);
        _storeRepository.GetByNameAsync(dto.Name).Returns((Store?)null);
        var transaction = Substitute.For<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>();
        _storeRepository.BeginTransactionAsync().Returns(transaction);
        _userContextService.GetAuth0Id().Returns("auth0|premium-owner");

        // Act
        var result = await _storeService.RegisterStoreAsync(dto);

        // Assert
        Assert.True(result);
        await _storeRepository.Received(1).AddAsync(Arg.Is<Store>(s =>
            s.Name == dto.Name &&
            s.Description == dto.Description &&
            s.Address == dto.Address &&
            s.PhoneNumber == dto.PhoneNumber));
        await _auth0ManagementService.Received(1).SyncUserMetadataAsync(
            "auth0|premium-owner",
            Arg.Any<int>(),
            Arg.Any<int>());
        await transaction.Received(1).CommitAsync();
    }

    [Fact]
    public async Task RegisterStoreAsync_RollsBackTransaction_WhenAddAsyncFails()
    {
        // Arrange
        var dto = new RegisterStoreDto
        {
            Name = "Failed Store",
            Email = "fail@store.com",
            FirstName = "Failed",
            LastName = "Owner"
        };

        _userRepository.GetByEmailAsync(dto.Email).Returns((User?)null);
        _storeRepository.GetByNameAsync(dto.Name).Returns((Store?)null);
        var transaction = Substitute.For<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>();
        _storeRepository.BeginTransactionAsync().Returns(transaction);
        _storeRepository.AddAsync(Arg.Any<Store>()).Returns(Task.FromException(new Exception("Database error")));
        _userContextService.GetAuth0Id().Returns("auth0|fail-owner");

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _storeService.RegisterStoreAsync(dto));
        await transaction.Received(1).RollbackAsync();
    }

    [Fact]
    public async Task RegisterStoreAsync_SyncsOwnerMetadataWithStoreId()
    {
        // Arrange
        var dto = new RegisterStoreDto
        {
            Name = "Sync Test Store",
            Email = "synctest@store.com",
            FirstName = "Sync",
            LastName = "Test"
        };

        Store? capturedStore = null;
        User? capturedUser = null;

        _userRepository.GetByEmailAsync(dto.Email).Returns((User?)null);
        _storeRepository.GetByNameAsync(dto.Name).Returns((Store?)null);
        var transaction = Substitute.For<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>();
        _storeRepository.BeginTransactionAsync().Returns(transaction);
        _userContextService.GetAuth0Id().Returns("auth0|sync-owner");

        // Capture the store and user that are added
        await _storeRepository.AddAsync(Arg.Do<Store>(s => capturedStore = s));
        await _userRepository.AddAsync(Arg.Do<User>(u => capturedUser = u));

        // Act
        await _storeService.RegisterStoreAsync(dto);

        // Assert
        Assert.NotNull(capturedStore);
        Assert.NotNull(capturedUser);
        Assert.Equal(capturedStore.Id, capturedUser.StoreId);
        await _auth0ManagementService.Received(1).SyncUserMetadataAsync(
            "auth0|sync-owner",
            capturedUser.Id,
            capturedUser.StoreId);
    }

    [Fact]
    public async Task RegisterStoreAsync_TrimsInputStrings()
    {
        // Arrange
        var dto = new RegisterStoreDto
        {
            Name = "  Trimmed Store  ",
            Email = "  trimmed@store.com  ",
            FirstName = "  Trimmed  ",
            LastName = "  Owner  "
        };

        _userRepository.GetByEmailAsync("trimmed@store.com").Returns((User?)null);
        _storeRepository.GetByNameAsync("Trimmed Store").Returns((Store?)null);
        var transaction = Substitute.For<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>();
        _storeRepository.BeginTransactionAsync().Returns(transaction);
        _userContextService.GetAuth0Id().Returns("auth0|trimmed-owner");

        // Act
        await _storeService.RegisterStoreAsync(dto);

        // Assert
        await _storeRepository.Received(1).AddAsync(Arg.Is<Store>(s => s.Name == "Trimmed Store"));
        await _userRepository.Received(1).AddAsync(Arg.Is<User>(u =>
            u.Email == "trimmed@store.com" &&
            u.FirstName == "Trimmed" &&
            u.LastName == "Owner"));
    }

    [Fact]
    public async Task RegisterStoreAsync_SetsTimestamps_OnStoreAndUser()
    {
        // Arrange
        var dto = new RegisterStoreDto
        {
            Name = "Timestamp Store",
            Email = "timestamp@store.com",
            FirstName = "Timestamp",
            LastName = "User"
        };

        var beforeTime = DateTime.UtcNow;
        _userRepository.GetByEmailAsync(dto.Email).Returns((User?)null);
        _storeRepository.GetByNameAsync(dto.Name).Returns((Store?)null);
        var transaction = Substitute.For<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>();
        _storeRepository.BeginTransactionAsync().Returns(transaction);
        _userContextService.GetAuth0Id().Returns("auth0|timestamp-owner");

        Store? addedStore = null;
        User? addedUser = null;
        await _storeRepository.AddAsync(Arg.Do<Store>(s => addedStore = s));
        await _userRepository.AddAsync(Arg.Do<User>(u => addedUser = u));

        // Act
        await _storeService.RegisterStoreAsync(dto);
        var afterTime = DateTime.UtcNow;

        // Assert
        Assert.NotNull(addedStore);
        Assert.NotNull(addedUser);
        Assert.True(addedStore.CreatedAt >= beforeTime && addedStore.CreatedAt <= afterTime);
        Assert.True(addedStore.UpdatedAt >= beforeTime && addedStore.UpdatedAt <= afterTime);
        Assert.True(addedUser.CreatedAt >= beforeTime && addedUser.CreatedAt <= afterTime);
        Assert.True(addedUser.UpdatedAt >= beforeTime && addedUser.UpdatedAt <= afterTime);
    }

    [Fact]
    public async Task GetAllStoresAsync_ReturnsEmptyList_WhenNoStores()
    {
        // Arrange
        _storeRepository.GetAllAsync().Returns(new List<Store>());

        // Act
        var result = await _storeService.GetAllStoresAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetStoreByIdAsync_MapsStoreToDto_Correctly()
    {
        // Arrange
        var store = new Store { Id = 1, Name = "Store 1", Description = "Desc 1" };
        var storeDto = new StoreDto { Id = 1, Name = "Store 1", Description = "Desc 1" };
        _storeRepository.GetByIdAsync(1).Returns(store);
        _mapper.Map<StoreDto>(store).Returns(storeDto);

        // Act
        var result = await _storeService.GetStoreByIdAsync(1);

        // Assert
        Assert.Equal(storeDto.Name, result.Name);
        _mapper.Received(1).Map<StoreDto>(store);
    }
}
