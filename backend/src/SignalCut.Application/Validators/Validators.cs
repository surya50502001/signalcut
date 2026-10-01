using FluentValidation;
using SignalCut.Application.DTOs;
using SignalCut.Domain.Enums;

namespace SignalCut.Application.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).WithMessage("Password must be at least 8 characters long.");
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class SearchQueryRequestValidator : AbstractValidator<SearchQueryRequest>
{
    public SearchQueryRequestValidator()
    {
        RuleFor(x => x.Query).NotEmpty().MinimumLength(2).MaximumLength(500);
        RuleFor(x => x.Limit).InclusiveBetween(1, 100);
    }
}

public class RightsConfirmationRequestValidator : AbstractValidator<RightsConfirmationRequest>
{
    public RightsConfirmationRequestValidator()
    {
        RuleFor(x => x.SourceId).NotEmpty();
        RuleFor(x => x.ClaimedRightsStatus)
            .Must(status => status == RightsStatus.USER_OWNED ||
                            status == RightsStatus.USER_AUTHORIZED ||
                            status == RightsStatus.LICENSED ||
                            status == RightsStatus.PUBLIC_DOMAIN)
            .WithMessage("Rights confirmation requires a valid permissive status: USER_OWNED, USER_AUTHORIZED, LICENSED, or PUBLIC_DOMAIN.");

        RuleFor(x => x.ConfirmationStatement)
            .NotEmpty()
            .Equal("I confirm that I own or have permission to use this content.")
            .WithMessage("Exact confirmation statement 'I confirm that I own or have permission to use this content.' is mandatory.");
    }
}

public class UpdateClipRequestValidator : AbstractValidator<UpdateClipRequest>
{
    public UpdateClipRequestValidator()
    {
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime).WithMessage("EndTime must be greater than StartTime.");
        RuleFor(x => x.AspectRatio).Must(r => r == "9:16" || r == "1:1" || r == "16:9").WithMessage("Allowed aspect ratios are 9:16, 1:1, 16:9.");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
    }
}
