using FluentValidation;
using VolleyHub.Domain.Users;

namespace VolleyHub.Application.Auth.Commands.RegisterUser
{
    public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
    {
        public RegisterUserCommandValidator()
        {
            RuleFor(command => command.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(User.MaxEmailLength);

            RuleFor(command => command.Password)
                .NotEmpty()
                .MinimumLength(8)
                .MaximumLength(100);
        }
    }
}