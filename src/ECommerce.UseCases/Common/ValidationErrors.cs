using FluentValidation.Results;

namespace ECommerce.UseCases.Common;

public static class ValidationErrors
{
    public static Result ToResult(this ValidationResult validationResult) =>
        Result.Failure(new Error(
            "Validation.Failed",
            string.Join("; ", validationResult.Errors.Select(error => error.ErrorMessage)),
            ErrorType.Validation));

    public static Result<T> ToResult<T>(this ValidationResult validationResult)
        where T : notnull =>
        Result<T>.Failure(new Error(
            "Validation.Failed",
            string.Join("; ", validationResult.Errors.Select(error => error.ErrorMessage)),
            ErrorType.Validation));
}
