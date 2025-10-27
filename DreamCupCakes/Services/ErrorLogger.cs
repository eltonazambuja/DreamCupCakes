using System.Text;

namespace DreamCupCakes.Services
{
    public interface IErrorLogger
    {
        Task LogErrorAsync(string source, string message, Exception? ex = null);
    }

    public class ErrorLogger : IErrorLogger
    {
        private readonly IWebHostEnvironment _env;
        private readonly string _logFilePath;

        public ErrorLogger(IWebHostEnvironment env)
        {
            _env = env;

            // Define o caminho do arquivo de log dentro da pasta Logs
            string logDirectory = Path.Combine(_env.ContentRootPath, "Logs");
            Directory.CreateDirectory(logDirectory); // Cria a pasta se não existir
            _logFilePath = Path.Combine(logDirectory, "errorlog.txt");
        }

        public async Task LogErrorAsync(string source, string message, Exception? ex = null)
        {
            var logEntry = new StringBuilder();
            logEntry.AppendLine("--------------------------------------------------");
            logEntry.AppendLine($"[Timestamp]: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            logEntry.AppendLine($"[Source]: {source}");
            logEntry.AppendLine($"[Message]: {message}");

            if (ex != null)
            {
                logEntry.AppendLine($"[Exception Type]: {ex.GetType().Name}");
                logEntry.AppendLine($"[Exception Message]: {ex.Message}");
                logEntry.AppendLine($"[Stack Trace]: {ex.StackTrace?.Trim()}");
            }
            logEntry.AppendLine("--------------------------------------------------");

            await File.AppendAllTextAsync(_logFilePath, logEntry.ToString());
        }
    }
}