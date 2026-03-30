using AutoMapper;
using HRS.API.Contracts.DTOs.User;
using HRS.Shared.Core.Dtos;
using HRS.Domain.Entities;
using HRS.Shared.Core.Enums;

namespace HRS.API.Mappings.Profiles;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserResponseDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));

        CreateMap<RegisterEmployeeDetailDto, User>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => Enum.Parse<UserRole>(src.Role)));
        CreateMap<User, RegisterEmployeeDetailDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));
    }
}
