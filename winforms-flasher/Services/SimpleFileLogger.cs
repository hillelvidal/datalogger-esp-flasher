using Microsoft.Extensions.Logging;

namespace ESPFlasher.Services
{
    public class SimpleFileLogger : ILogger
    {
        private readonly string _logFilePath;
        private static readonly object _lock = new();

        public SimpleFileLogger(string logFilePath)
        {
            _logFilePath = logFilePath;
            
            // Write header
            try
            {
                var dir = Path.GetDirectoryName(_logFilePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                
                File.WriteAllText(_logFilePath, $"=== ESP Flasher Log - {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n\n");
            }
            catch { }
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var level = logLevel.ToString().ToUpper();
            var message = formatter(state, exception);
            
            var logEntry = $"[{timestamp}] [{level}] {message}";
            
            if (exception != null)
            {
                logEntry += $"\n{exception}";
            }

            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_logFilePath, logEntry + "\n");
                }
                catch { }
            }
        }
    }

    public class SimpleFileLoggerProvider : ILoggerProvider
    {
        private readonly string _logFilePath;

        public SimpleFileLoggerProvider(string logFilePath)
        {
            _logFilePath = logFilePath;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new SimpleFileLogger(_logFilePath);
        }

        public void Dispose() { }
    }
}
