using FluentValidation;

namespace VolleyHub.Application.Courts.Commands.DeleteCourt
{
    public sealed class DeleteCourtCommandValidator : AbstractValidator<DeleteCourtCommand>
    {
        public DeleteCourtCommandValidator() 
        {
            RuleFor(command => command.Id)
                .NotEmpty();
        }
    }
}
