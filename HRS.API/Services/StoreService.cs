using AutoMapper;
using HRS.API.Contracts.DTOs.Store;
using HRS.API.Services.Interfaces;
using HRS.Domain.Entities;
using HRS.Domain.Interfaces;
using HRS.Shared.Core.Enums;

namespace HRS.API.Services;

public class StoreService : IStoreService
{
    private readonly IStoreRepository _storeRepository;
    private readonly IAuth0ManagementService _auth0ManagementService;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly IUserContextService _userContextService;

    public StoreService(
        IStoreRepository storeRepository,
        IAuth0ManagementService auth0ManagementService,
        IUserRepository userRepository,
        IMapper mapper,
        IUserContextService userContextService)
    {
        _storeRepository = storeRepository;
        _auth0ManagementService = auth0ManagementService;
        _userRepository = userRepository;
        _mapper = mapper;
        _userContextService = userContextService;
    }

    public async Task<StoreDto> GetStoreByIdAsync(int id)
    {
        var store = await _storeRepository.GetByIdAsync(id);
        if (store == null)
            throw new KeyNotFoundException("Store not found");

        return _mapper.Map<StoreDto>(store);
    }

    public async Task<IEnumerable<StoreDto>> GetAllStoresAsync()
    {
        var stores = await _storeRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<StoreDto>>(stores);
    }

    public async Task<StoreDto> GetStoreByUserIdAsync(int userId)
    {
        var store = await _storeRepository.GetByUserIdAsync(userId);
        if (store == null)
            throw new KeyNotFoundException("Store not found for this user");

        return _mapper.Map<StoreDto>(store);
    }

    public async Task<bool> RegisterStoreAsync(RegisterStoreDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            throw new ArgumentException("Email is required");

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Store name is required");

        var existingUser = await _userRepository.GetByEmailAsync(dto.Email);
        if (existingUser != null)
            throw new InvalidOperationException("User with this email already exists");

        var existingStore = await _storeRepository.GetByNameAsync(dto.Name);
        if (existingStore != null)
            throw new InvalidOperationException("Store with this name already exists");

        await using var tx = await _storeRepository.BeginTransactionAsync();

        try
        {
            var now = DateTime.UtcNow;

            var store = new Store
            {
                Name = dto.Name.Trim(),
                Description = dto.Description,
                Address = dto.Address,
                PhoneNumber = dto.PhoneNumber,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _storeRepository.AddAsync(store);
            await _storeRepository.SaveChangesAsync();

            var user = new User
            {
                Auth0UserId = _userContextService.GetAuth0Id(),
                Email = dto.Email.Trim(),
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Role = UserRole.Owner,
                StoreId = store.Id,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();
            await _auth0ManagementService.SyncUserRoleAsync(user.Auth0UserId, UserRole.Owner);
            await _auth0ManagementService.SyncUserMetadataAsync(user.Auth0UserId, user.Id, user.StoreId);

            await tx.CommitAsync();

            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
