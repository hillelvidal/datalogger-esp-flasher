using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace ESPFlasher.Services
{
    public class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly string _logFilePath;
        private static readonly ConcurrentQueue<string> _logQueue = new();
        private static readonly object _fileLock = new();

        public FileLogger(string categoryName, string logFilePath)
        {
            _categoryName = categoryName;
            _logFilePath = logFilePath;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var level = logLevel.ToString().ToUpper();
            var message = formatter(state, exception);
            
            var logEntry = $"[{timestamp}] [{level}] {message}";
            
            if (exception != null)
            {
                logEntry += $"\n{exception}";
            }

            // Write to file immediately
            lock (_fileLock)
            {
                try
                {
                    File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                }
                catch
                {
                    // Ignore file write errors
                }
            }
        }
    }

    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _logFilePath;

        public FileLoggerProvider(string logFilePath)
        {
            _logFilePath = logFilePath;
            
            // Clear old log file
            try
            {
                if (File.Exists(_logFilePath))
                {
                    File.Delete(_logFilePath);
                }
                
                // Create directory if needed
                var directory = Path.GetDirectoryName(_logFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Write header
                File.WriteAllText(_logFilePath, $"=== ESP Flasher Log - {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==={Environment.NewLine}{Environment.NewLine}");
            }
            catch
            {
                // Ignore initialization errors
            }
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(categoryName, _logFilePath);
        }

        public void Dispose()
        {
        }
    }
}
