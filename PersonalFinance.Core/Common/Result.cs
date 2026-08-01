namespace PersonalFinance.Core.Common;

/// <summary>
/// Lightweight result for operations that can fail without throwing.
/// No third-party dependency.
/// </summary>
public readonly struct Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }

    private Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Ok() => new(true, null);
    public static Result Fail(string error) => new(false, error);

    public static implicit operator bool(Result result) => result.IsSuccess;
}

public readonly struct Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }

    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Ok(T value) => new(true, value, null);
    public static Result<T> Fail(string error) => new(false, default, error);

    public static implicit operator bool(Result<T> result) => result.IsSuccess;
}