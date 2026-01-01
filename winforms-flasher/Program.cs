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
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Setup simple file logging
            var logFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ESPFlasher", "Firmware");
            Directory.CreateDirectory(logFolder);
            LogFilePath = Path.Combine(logFolder, "flasher.log");
            
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddProvider(new SimpleFileLoggerProvider(LogFilePath));
                builder.SetMinimumLevel(LogLevel.Information);
            });
            
            var logger = loggerFactory.CreateLogger<MainForm>();
            logger.LogInformation("=== ESP Flasher Started ===");
            
            Application.Run(new MainForm(logger));
        }
    }
}
