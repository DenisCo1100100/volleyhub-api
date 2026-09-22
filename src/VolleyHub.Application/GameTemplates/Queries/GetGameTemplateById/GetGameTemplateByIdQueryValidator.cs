using FluentValidation;

namespace VolleyHub.Application.GameTemplates.Queries.GetGameTemplateById
{
    public sealed class GetGameTemplateByIdQueryValidator : AbstractValidator<GetGameTemplateByIdQuery>
    {
        public GetGameTemplateByIdQueryValidator()
        {
            RuleFor(query => query.Id).NotEmpty();
        }
    }
}
