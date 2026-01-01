using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using ESPFlasher.Services;

namespace ESPFlasher
{
    internal static class Program
    {
        public static string LogFilePath { get; private set; } = string.Empty;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Initialize WinForms application
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Setup file logging
            var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ESPFlasher", "Logs");
            Directory.CreateDirectory(logDirectory);
            LogFilePath = Path.Combine(logDirectory, $"flasher_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddProvider(new FileLoggerProvider(LogFilePath));
                builder.SetMinimumLevel(LogLevel.Information);
            });
            
            var logger = loggerFactory.CreateLogger<MainForm>();
            logger.LogInformation($"=== ESP Flasher Started ===");
            logger.LogInformation($"Log file: {LogFilePath}");
            
            Application.Run(new MainForm(logger));
        }
    }
}
