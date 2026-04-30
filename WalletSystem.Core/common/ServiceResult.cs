namespace WalletSystem.Core.common
{
    public class ServiceResult
    {
        public bool Success { get; }
        public string? Message { get; }

        protected ServiceResult(bool success, string? message)
        {
            Success = success;
            Message = message;
        }

        public static ServiceResult Ok(string? message = null)
            => new(true, message);

        public static ServiceResult Fail(string message)
            => new(false, message);
    }

    public class ServiceResult<T> : ServiceResult
    {
        public T? Result { get; }

        private ServiceResult(bool success, string? message, T? result)
            : base(success, message)
        {
            Result = result;
        }

        public static ServiceResult<T> Ok(T result, string? message = null)
            => new(true, message, result);

        public static new  ServiceResult<T> Fail(string message)
            => new(false, message, default);
    }
}