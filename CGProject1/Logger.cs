using System;
using System.IO;

namespace CGProject1 {
    public class Logger : IDisposable {

        public static Logger Instance;
        public const string LogFilename = "Last.log";

        public string Path { get; private set; }

        public static void StartLogger() {
            if (!isStarted) {
                isStarted = true;
                Instance = new Logger();
            }
        }

        public void Log(string message, LogType type = LogType.Info) {
            string prefix = type switch
            {
                LogType.Info => "[I]",
                LogType.Warning => "[W]",
                LogType.Error => "[E]",
                _ => "[?]"
            };

            writer.WriteLine($"{prefix}[{DateTime.Now}] {message}");
        }

        public void Dispose() {
            writer.WriteLine($"===== Logging ended. Current time is {DateTime.Now} =====");
            writer.Close();
        }

        public enum LogType {
            Info,
            Warning,
            Error
        }

        private StreamWriter writer;
        private static bool isStarted = false;

        private Logger() {
            Path = Directory.GetCurrentDirectory();
            writer = new StreamWriter(LogFilename);
            writer.WriteLine($"===== Logging started. Current time is {DateTime.Now} =====");
        }
    }
}
