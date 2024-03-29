
using Application.Common.Interfaces;

namespace Application.Common.models;

public class ResponseWrapper<T> : IResponseWrapper<T>
{
    public virtual T? Data { get; set; }
    public virtual string Message { get; set; } = string.Empty;
    public virtual bool IsSuccess { get; set; }
    public virtual Exception? Error { get; set; }

    public ResponseWrapper() { }
}

public static class ResponseWrapper
{
    public static ResponseWrapper<T> Ok<T>(T data, string? msg= default)
    { 
        return new ResponseWrapper<T>()
        {
            Data = data,
            Error = null,
            Message = msg ?? string.Empty,
            IsSuccess = true
        };
    }

    public static ResponseWrapper<T> Error<T>(Exception error, string? msg= default, T? data=default)
    {
        return new ResponseWrapper<T>()
        {
            IsSuccess = false,
            Error = error,
            Message = msg ?? string.Empty,
            Data = data,
        };
    }

    public static ResponseWrapper<T> Error<T>(string error, string? msg= default, T? data = default)
    {
        return Error(new Exception(error), msg, data);
    }
}
