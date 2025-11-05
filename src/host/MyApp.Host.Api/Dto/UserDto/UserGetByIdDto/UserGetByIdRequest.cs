using FastEndpoints;
using FluentValidation;

namespace MyApp.Host.Api.Dto.UserDto.UserGetByIdDto;

public record UserGetByIdRequest(string Id)
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