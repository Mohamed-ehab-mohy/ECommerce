namespace ECommerce.Domain;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; }

    protected Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);

    public static Result Failure(Error error) => new(false, error);

    public override string ToString() =>
        IsSuccess ? "Success" : $"Failure: {Error}";
}

public class Result<T> : Result
{
    public T? Data { get; }

    private Result(T data) : base(true, null)
    {
        Data = data;
    }

    private Result(Error error) : base(false, error)
    {
        Data = default;
    }

    public static Result<T> Success(T data) => new(data);

    public static new Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T data) => new(data);

    public static implicit operator Result<T>(Error error) => new(error);

    public override string ToString() =>
        IsSuccess ? $"Success: {Data}" : $"Failure: {Error}";
}
