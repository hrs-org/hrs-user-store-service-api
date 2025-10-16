using AutoMapper;
using HRS.API.Contracts.DTOs.Store;
using HRS.API.Services.Interfaces;
using HRS.Domain.Entities;
using HRS.Domain.Enums;
using HRS.Domain.Interfaces;

namespace HRS.API.Services;

public class StoreService : IStoreService
{
    private readonly IStoreRepository _storeRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserVerificationService _userVerificationService;
    private readonly IMapper _mapper;
    private readonly HttpClient _httpClient;

    public StoreService(
        IStoreRepository storeRepository,
        IUserRepository userRepository,
        IUserVerificationService userVerificationService,
        IMapper mapper,
        IHttpClientFactory httpClientFactory)
    {
        _storeRepository = storeRepository;
        _userRepository = userRepository;
        _userVerificationService = userVerificationService;
        _mapper = mapper;
        _httpClient = httpClientFactory.CreateClient("EmailService");
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
        var existingUser = await _userRepository.GetByEmailAsync(dto.Email);
        if (existingUser != null)
            throw new InvalidOperationException("User with this email already exists");

        var existingStore = await _storeRepository.GetByNameAsync(dto.Name);
        if (existingStore != null)
            throw new InvalidOperationException("Store with this name already exists");

        if (dto.Password.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters long");

        await using var tx = await _storeRepository.BeginTransactionAsync();

        try
        {
            var store = new Store
            {
                Name = dto.Name,
                Description = dto.Description,
                Address = dto.Address,
                PhoneNumber = dto.PhoneNumber,
            };

            await _storeRepository.AddAsync(store);
            await _storeRepository.SaveChangesAsync();

            var user = new User
            {
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = UserRole.Admin,
                StoreId = store.Id,
                IsVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            var verification = await _userVerificationService.CreateAsync(
                user.Id,
                "Email",
                TimeSpan.FromHours(24)
            );

            var verificationRequest = new
            {
                Email = user.Email,
                VerificationToken = verification.Token,
                FirstName = user.FirstName
            };

            var response = await _httpClient.PostAsJsonAsync("/api/email/send-verification", verificationRequest);
            response.EnsureSuccessStatusCode();

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
    