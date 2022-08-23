using CountryService.Web.Protos.model;
using FluentValidation;

namespace CountryService.Server.Validations;

public class CountryCreateRequestValidator : AbstractValidator<CountryCreationReply>
{
    public CountryCreateRequestValidator()
    {
        RuleFor(country => country.Name)
            .NotEmpty()
            .WithMessage("Name is Mandatory");

        RuleFor(country => country.Description)
            .NotEmpty()
            .WithMessage("Description is Mandatory")
            .MinimumLength(5)
            .WithMessage("Description is mandatory and be longer than 5 characters");
    }
}