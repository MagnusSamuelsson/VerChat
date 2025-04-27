namespace VericateChat.Models
{
    public record SqlResult(
        bool success,
        string? jsonData = null,
        string? error = null)
    {
        public bool Success { get; } = success;
        public string? JsonData { get; } = jsonData;
        public string? Error { get; } = error;

        public static SqlResult Failure(string errorMessage)
            => new(success: false, error: errorMessage);
    }
}
