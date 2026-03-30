using System;
using System.Net;
using System.Net.Http;
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
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly HttpClient _httpClient;
    private readonly StoreService _storeService;
    private readonly IUserContextService _userContextService;

    public StoreServiceTests()
    {
        _storeRepository = Substitute.For<IStoreRepository>();
        _auth0ManagementService = Substitute.For<IAuth0ManagementService>();
        _userRepository = Substitute.For<IUserRepository>();
        _mapper = Substitute.For<IMapper>();
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _userContextService = Substitute.For<IUserContextService>();

        var handler = new FakeHttpMessageHandler();
        _httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        _httpClientFactory.CreateClient("EmailService").Returns(_httpClient);

        _storeService = new StoreService(
            _storeRepository,
            _auth0ManagementService,
            _userRepository,
            _mapper,
            _httpClientFactory,
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
}
