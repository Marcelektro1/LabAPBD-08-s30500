namespace Tutorial8.Utils;

public class ServiceResult
{
    public ServiceResult(bool success, string message)
    {
        Success = success;
        Message = message;
    }

    public bool Success { get; set; }
    public string Message { get; set; }
}
