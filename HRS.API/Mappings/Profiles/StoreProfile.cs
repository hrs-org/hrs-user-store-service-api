using AutoMapper;
using HRS.API.Contracts.DTOs.Store;
using HRS.Domain.Entities;

namespace HRS.API.Mappings.Profiles;

public class StoreProfile : Profile
{
    public StoreProfile()
    {
        CreateMap<Store, StoreDto>();
        CreateMap<RegisterStoreDto, Store>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());
    }
}
