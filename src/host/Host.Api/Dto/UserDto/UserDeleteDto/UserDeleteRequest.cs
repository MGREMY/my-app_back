using Domain.Service.Contract.Dto.UserDto.UserDeleteDto;
using FastEndpoints;
using FluentValidation;

namespace Host.Api.Dto.UserDto.UserDeleteDto;

public record UserDeleteRequest(Guid Id)
{
    public class Validator : Validator<UserDeleteRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .NotEmpty();
        }
    }

    public UserDeleteServiceRequest ToServiceRequest()
    {
        return new UserDeleteServiceRequest(Id);
    }
}