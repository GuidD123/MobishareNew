namespace Mobishare.Infrastructure.IoT.EventArgs
{
    public class MqttOperationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public Exception? Exception { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public static MqttOperationResult Ok() => new() { Success = true };

        public static MqttOperationResult Error(string message, Exception? ex = null) =>
            new() { Success = false, ErrorMessage = message, Exception = ex };
    }
}
