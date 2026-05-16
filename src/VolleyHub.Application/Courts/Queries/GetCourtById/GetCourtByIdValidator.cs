using FluentValidation;

namespace VolleyHub.Application.Courts.Queries.GetCourtById
{
    public class GetCourtByIdValidator : AbstractValidator<GetCourtByIdQuery>
    {
        public GetCourtByIdValidator() 
        {
            RuleFor(query => query.Id)
                .NotEmpty();
        }
    }
}
