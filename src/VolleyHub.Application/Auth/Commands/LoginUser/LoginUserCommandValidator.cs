using FluentValidation;
using VolleyHub.Domain.Users;

namespace VolleyHub.Application.Auth.Commands.LoginUser
{
    public sealed class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
    {
        public LoginUserCommandValidator()
        {
            RuleFor(command => command.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(User.MaxEmailLength);

            RuleFor(command => command.Password)
                .NotEmpty();
        }
    }
}