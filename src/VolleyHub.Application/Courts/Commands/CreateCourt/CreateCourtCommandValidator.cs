using FluentValidation;
using VolleyHub.Domain.Courts;

namespace VolleyHub.Application.Courts.Commands.CreateCourt
{
    public sealed class CreateCourtCommandValidator : AbstractValidator<CreateCourtCommand>
    {
        public CreateCourtCommandValidator() 
        {
            RuleFor(command => command.Name)
                .NotEmpty()
                .MaximumLength(Court.MaxNameLength);

            RuleFor(command => command.Address)
                .NotEmpty()
                .MaximumLength(Court.MaxAddressLength);

            RuleFor(command => command.Latitude)
                .ExclusiveBetween(-90, 90);

            RuleFor(command => command.Longitude)
                .ExclusiveBetween(-180, 180);

            RuleFor(command => command.SurfaceType)
                .IsInEnum()
                .Must(surfaceType => surfaceType != CourtSurfaceType.Unknown)
                .WithMessage("Court surface type is required.");

            RuleFor(command => command.Description)
                .MaximumLength(Court.MaxDescriptionLength);
        }
    }
}
