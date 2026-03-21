using System;

namespace xinglin.Services.Data
{
    public class ConsoleLoggerService : ILoggerService
    {
        public void Information(string message)
        {
            Console.WriteLine($"[INFO] {DateTime.Now}: {message}");
        }

        public void Warning(string message)
        {
            Console.WriteLine($"[WARNING] {DateTime.Now}: {message}");
        }

        public void Error(string message)
        {
            Console.WriteLine($"[ERROR] {DateTime.Now}: {message}");
        }

        public void Error(string message, Exception exception)
        {
            Console.WriteLine($"[ERROR] {DateTime.Now}: {message}");
            Console.WriteLine($"[ERROR] Exception: {exception.Message}");
            Console.WriteLine($"[ERROR] Stack trace: {exception.StackTrace}");
        }
    }
}
