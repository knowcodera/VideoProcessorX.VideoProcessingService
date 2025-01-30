namespace VideoProcessingService.Application.Common
{
    public class Result
    {
        public bool Succeeded { get; set; }
        public string Error { get; set; }

        public static Result Success() => new Result { Succeeded = true };
        public static Result Failure(string error) => new Result { Succeeded = false, Error = error };
    }

    public class Result<T>
    {
        public bool Succeeded { get; set; }
        public T Value { get; set; }
        public string Error { get; set; }

        public static Result<T> Success(T value)
            => new Result<T> { Succeeded = true, Value = value };
        public static Result<T> Failure(string error)
            => new Result<T> { Succeeded = false, Error = error };
    }
}
