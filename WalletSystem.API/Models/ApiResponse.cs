namespace WalletSystem.API.Models
{


    public class ApiResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }

        public static ApiResponse Ok(string message)
            => new()
            { Success = true, Message = message };

        public static ApiResponse Fail(string message)
            => new()
            { Success = false, Message = message };
    }



    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Result { get; set; }

        public static ApiResponse<T> Ok(T result, string message)
            => new ApiResponse<T> { Success = true, Message = message, Result = result };

        public static ApiResponse<T> Fail(string message)
            => new ApiResponse<T> { Success = false, Message = message };
    }
}
