using FluentValidation.Results;

namespace ECommerceManagementSystem.Coffee.Application.Common.Exceptions;

public class ValidationException() : Exception("Một hoặc nhiều lỗi xác thực đã xảy ra.")
{
    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this()
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }

    public IDictionary<string, string[]> Errors { get; } = new Dictionary<string, string[]>();
}