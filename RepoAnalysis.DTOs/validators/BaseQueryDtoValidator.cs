using FluentValidation;

namespace RepoAnalysis.DTOs.validators;

public class BaseQueryDtoValidator : AbstractValidator<IBaseQueryDto>
{
    public BaseQueryDtoValidator()
    {
        RuleFor(x => x.RepoTitle)
            .NotEmpty()
            .WithMessage("Repository title cannot be null or whitespace.");

        RuleFor(x => x.OwnerGitHubUsername)
            .NotEmpty()
            .WithMessage("Owner GitHub username cannot be null or whitespace.");

        RuleFor(x => x.AuthorGitHubUsername)
            .NotEmpty()
            .WithMessage("Author GitHub username cannot be null or whitespace.");
    }
}