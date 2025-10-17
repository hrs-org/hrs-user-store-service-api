using FluentValidation;
using HRS.Shared.Core.Dtos;
using HRS.Domain.Interfaces;
namespace HRS.API.Validators.User;


public class UserDtoValidators : AbstractValidator<UserResponseDto>
{
    public UserDtoValidators(IUserRepository userRepository)
    {
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MustAsync(async (email, cancellation) =>
            {
                return await userRepository.IsEmailUniqueAsync(email);
            })
            .WithMessage("Email is already in use.");
        RuleFor(x => x.LastName).NotEmpty();
        RuleFor(x => x.Role).NotEmpty().WithMessage("Role must be Assigned");


    }
}
