using FluentValidation;

namespace VolleyHub.Application.PlayerProfiles.Queries.GetPlayerProfileById
{
    public sealed class GetPlayerProfileByIdQueryValidator : AbstractValidator<GetPlayerProfileByIdQuery>
    {
        public GetPlayerProfileByIdQueryValidator()
        {
            RuleFor(query => query.Id)
                .NotEmpty();
        }
    }
}