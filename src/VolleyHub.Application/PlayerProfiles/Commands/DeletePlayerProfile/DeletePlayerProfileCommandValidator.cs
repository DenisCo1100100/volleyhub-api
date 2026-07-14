using FluentValidation;

namespace VolleyHub.Application.PlayerProfiles.Commands.DeletePlayerProfile
{
    public sealed class DeletePlayerProfileCommandValidator : AbstractValidator<DeletePlayerProfileCommand>
    {
        public DeletePlayerProfileCommandValidator()
        {
            RuleFor(command => command.Id)
                .NotEmpty();
        }
    }
}