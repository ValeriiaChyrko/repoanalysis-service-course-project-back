using FluentValidation;

namespace RepoAnalysis.DTOs.validators;

public class RepositoryWithBranchDtoValidator : AbstractValidator<RepositoryWithBranchQueryDto>
{
    public RepositoryWithBranchDtoValidator() 
    {
        var baseValidator = new BaseQueryDtoValidator();
        RuleFor(x => x)
            .SetValidator(baseValidator);

        RuleFor(x => x.BranchTitle)
            .NotEmpty()
            .WithMessage("Branch title cannot be null or whitespace.");
    }
}