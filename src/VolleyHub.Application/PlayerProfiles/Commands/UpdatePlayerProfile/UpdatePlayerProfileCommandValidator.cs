using FluentValidation;
using VolleyHub.Domain.PlayerProfiles;

namespace VolleyHub.Application.PlayerProfiles.Commands.UpdatePlayerProfile
{
    public sealed class UpdatePlayerProfileCommandValidator : AbstractValidator<UpdatePlayerProfileCommand>
    {
        public UpdatePlayerProfileCommandValidator()
        {
            RuleFor(command => command.Id)
                .NotEmpty();

            RuleFor(command => command.DisplayName)
                .NotEmpty()
                .MaximumLength(PlayerProfile.MaxDisplayNameLength);

            RuleFor(command => command.SkillLevel)
                .Must(skillLevel => skillLevel is not PlayerSkillLevel.Unknown && Enum.IsDefined(skillLevel))
                .WithMessage("Player skill level is invalid.");

            RuleFor(command => command.City)
                .MaximumLength(PlayerProfile.MaxCityLength);

            RuleFor(command => command.Bio)
                .MaximumLength(PlayerProfile.MaxBioLength);
        }
    }
}