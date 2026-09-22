using FluentValidation;

namespace VolleyHub.Application.GameTemplates.Commands.CreateGameFromTemplate
{
    public sealed class CreateGameFromTemplateCommandValidator : AbstractValidator<CreateGameFromTemplateCommand>
    {
        public CreateGameFromTemplateCommandValidator()
        {
            RuleFor(command => command.Id).NotEmpty();
            RuleFor(command => command.StartsAt).NotEmpty();
        }
    }
}
