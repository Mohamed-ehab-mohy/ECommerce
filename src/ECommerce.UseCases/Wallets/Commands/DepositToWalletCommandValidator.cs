using FluentValidation;

namespace ECommerce.UseCases.Wallets.Commands;

public sealed class DepositToWalletCommandValidator : AbstractValidator<DepositToWalletCommand>
{
    public DepositToWalletCommandValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Deposit amount must be greater than zero.")
            .LessThanOrEqualTo(50000).WithMessage("Deposit amount exceeds maximum allowed per transaction.");
    }
}
