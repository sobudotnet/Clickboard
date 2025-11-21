using System;
using System.IO;
using System.Windows.Forms;

namespace Clickboard
{
    public static class Logger
    {
        private static readonly string logPath = Path.Combine(Application.StartupPath, "Clickboard.log");

        public static void Log(string message, string level = "INFO")
        {
            try
            {
                string entry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
                File.AppendAllText(logPath, entry + Environment.NewLine);
            }
            catch { /*handles logging n stuff option*/ }
        }

        public static void LogException(Exception ex, string context = "")
        {
            Log($"{context} Exception: {ex}", "ERROR");
        }

        public static string GetLogFilePath()
        {
            return logPath;
        }
    }
}