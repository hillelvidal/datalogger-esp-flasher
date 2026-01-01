using System;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

namespace ESPFlasher
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Warning);
            });
            
            var logger = loggerFactory.CreateLogger<MainForm>();
            
            Application.Run(new MainForm(logger));
        }
    }
}
