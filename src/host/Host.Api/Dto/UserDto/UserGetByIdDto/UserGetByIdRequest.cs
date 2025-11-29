using FastEndpoints;
using FluentValidation;

namespace Host.Api.Dto.UserDto.UserGetByIdDto;

public record UserGetByIdRequest(Guid Id)
{
    public class Validator : Validator<UserGetByIdRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .NotEmpty();
        }
    }
}