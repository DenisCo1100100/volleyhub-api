using FluentValidation;

namespace VolleyHub.Application.GameTemplates.Commands.DeleteGameTemplate
{
    public sealed class DeleteGameTemplateCommandValidator : AbstractValidator<DeleteGameTemplateCommand>
    {
        public DeleteGameTemplateCommandValidator()
        {
            RuleFor(command => command.Id).NotEmpty();
        }
    }
}
