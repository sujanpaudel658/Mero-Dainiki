namespace Mero_Dainiki.Common
{
    /// <summary>
    /// Generic wrapper for service results with success/failure handling
    /// </summary>
    public class ServiceResult<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? ErrorMessage { get; set; }

        public static ServiceResult<T> Ok(T data) => new() { Success = true, Data = data };
        public static ServiceResult<T> Fail(string error) => new() { Success = false, ErrorMessage = error };
    }

    /// <summary>
    /// Non-generic service result for operations without return data
    /// </summary>
    public class ServiceResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        public static ServiceResult Ok() => new() { Success = true };
        public static ServiceResult Fail(string error) => new() { Success = false, ErrorMessage = error };
    }
}
