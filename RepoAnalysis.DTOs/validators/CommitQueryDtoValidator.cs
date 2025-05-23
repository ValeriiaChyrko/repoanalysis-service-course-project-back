﻿using FluentValidation;

namespace RepoAnalysis.DTOs.validators;

public class CommitQueryDtoValidator : AbstractValidator<CommitQueryDto>
{
    public CommitQueryDtoValidator()
    {
        var baseValidator = new BaseQueryDtoValidator();
        RuleFor(x => x)
            .SetValidator(baseValidator);

        RuleFor(x => x.BranchTitle)
            .NotEmpty()
            .WithMessage("Branch title cannot be null or whitespace.");

        RuleFor(x => x.Since)
            .Must((dto, since) => !dto.Until.HasValue || !since.HasValue || since <= dto.Until)
            .WithMessage("The 'Since' date cannot be later than the 'Until' date.");

        RuleFor(x => x.Since)
            .Must(since => !since.HasValue || since <= DateTime.UtcNow)
            .WithMessage("The 'Since' date cannot be in the future.");

        RuleFor(x => x.Until)
            .Must(until => !until.HasValue || until <= DateTime.UtcNow)
            .WithMessage("The 'Until' date cannot be in the future.");
    }
}