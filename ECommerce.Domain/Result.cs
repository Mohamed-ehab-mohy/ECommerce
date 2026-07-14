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

    public static implicit operator Result(Error error) => new(false, error);

    public Result Then(Func<Result> next)
    {
        return IsFailure ? this : next();
    }

    public Result<TNext> Then<TNext>(Func<Result<TNext>> next)
    {
        return IsFailure ? Result<TNext>.Failure(Error!) : next();
    }

    public Result Ensure(Func<bool> predicate, Error error)
    {
        return IsFailure ? this : predicate() ? this : Failure(error);
    }

    public Result OnSuccess(Action action)
    {
        if (IsSuccess) action();
        return this;
    }

    public Result OnFailure(Action<Error> action)
    {
        if (IsFailure && Error is not null) action(Error);
        return this;
    }

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

    public Result<TNext> Map<TNext>(Func<T, TNext> map)
    {
        return IsFailure ? Result<TNext>.Failure(Error!) : Result<TNext>.Success(map(Data!));
    }

    public Result<TNext> Then<TNext>(Func<T, Result<TNext>> next)
    {
        return IsFailure ? Result<TNext>.Failure(Error!) : next(Data!);
    }

    public Result<TNext> Bind<TNext>(Func<T, Result<TNext>> next)
    {
        return IsFailure ? Result<TNext>.Failure(Error!) : next(Data!);
    }

    public Result<T> Ensure(Func<T, bool> predicate, Error error)
    {
        return IsFailure || predicate(Data!) ? this : Failure(error);
    }

    public Result<T> Tap(Action<T> action)
    {
        if (IsSuccess) action(Data!);
        return this;
    }

    public Result<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess) action(Data!);
        return this;
    }

    public new Result<T> OnFailure(Action<Error> action)
    {
        if (IsFailure && Error is not null) action(Error);
        return this;
    }

    public TNext Match<TNext>(Func<T, TNext> onSuccess, Func<Error, TNext> onFailure)
    {
        return IsSuccess ? onSuccess(Data!) : onFailure(Error!);
    }

    public static implicit operator Result<T>(T data) => new(data);

    public static implicit operator Result<T>(Error error) => new(error);

    public override string ToString() =>
        IsSuccess ? $"Success: {Data}" : $"Failure: {Error}";
}
