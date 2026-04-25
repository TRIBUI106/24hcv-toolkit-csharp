namespace Toolkit.Domain.Models;

public sealed class OperationResult<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? ErrorMessage { get; }

    private OperationResult(bool isSuccess, T? value, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage;
    }

    public static OperationResult<T> Success(T value) =>
        new(true, value, null);

    public static OperationResult<T> Failure(string errorMessage) =>
        new(false, default, errorMessage);
}
