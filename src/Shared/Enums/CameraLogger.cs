using System.Collections.Concurrent;
using System.IO;

namespace CameraSDK
{
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Fatal = 4
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Exception? Exception { get; set; }

        public override string ToString()
        {
            string exInfo = Exception != null ? $" | Exception: {Exception.GetType().Name}: {Exception.Message}" : "";
            return $"[{Timestamp:HH:mm:ss.fff}] [{Level}] [{Source}] {Message}{exInfo}";
        }
    }

    public static class CameraLogger
    {
        private static readonly ConcurrentQueue<LogEntry> _logs = new();
        private static readonly int _maxLogs = 1000;
        private static LogLevel _minLevel = LogLevel.Info;
        private static string _logDirectory = string.Empty;
        private static readonly object _fileLock = new();
        private static DateTime _currentLogFileDate = DateTime.MinValue;

        public static event Action<LogEntry>? OnLogAdded;

        public static LogLevel MinLevel
        {
            get => _minLevel;
            set => _minLevel = value;
        }

        public static string LogDirectory
        {
            get => _logDirectory;
            set
            {
                _logDirectory = value;
                if (!string.IsNullOrEmpty(_logDirectory) && !Directory.Exists(_logDirectory))
                    Directory.CreateDirectory(_logDirectory);
            }
        }

        public static IReadOnlyList<LogEntry> Logs => _logs.ToArray();

        public static void Log(LogLevel level, string source, string message, Exception? ex = null)
        {
            if (level < _minLevel) return;

            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Source = source,
                Message = message,
                Exception = ex
            };

            _logs.Enqueue(entry);

            while (_logs.Count > _maxLogs)
                _logs.TryDequeue(out _);

            OnLogAdded?.Invoke(entry);

            WriteToFile(entry);
        }

        public static void Debug(string source, string message) => Log(LogLevel.Debug, source, message);
        public static void Info(string source, string message) => Log(LogLevel.Info, source, message);
        public static void Warning(string source, string message) => Log(LogLevel.Warning, source, message);
        public static void Error(string source, string message, Exception? ex = null) => Log(LogLevel.Error, source, message, ex);
        public static void Fatal(string source, string message, Exception? ex = null) => Log(LogLevel.Fatal, source, message, ex);

        public static void Clear() => _logs.Clear();

        public static string GetLogs(LogLevel? level = null, int count = 100)
        {
            var logs = level.HasValue
                ? _logs.Where(l => l.Level >= level.Value).TakeLast(count)
                : _logs.TakeLast(count);

            return string.Join(Environment.NewLine, logs);
        }

        private static void WriteToFile(LogEntry entry)
        {
            if (string.IsNullOrEmpty(_logDirectory)) return;

            try
            {
                DateTime today = entry.Timestamp.Date;
                string fileName = $"camera_{today:yyyyMMdd}.log";
                string filePath = Path.Combine(_logDirectory, fileName);

                lock (_fileLock)
                {
                    if (!Directory.Exists(_logDirectory))
                        Directory.CreateDirectory(_logDirectory);

                    string exInfo = entry.Exception != null
                        ? $"\n  Exception: {entry.Exception.GetType().Name}: {entry.Exception.Message}\n  StackTrace: {entry.Exception.StackTrace}"
                        : "";

                    string line = $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{entry.Level}] [{entry.Source}] {entry.Message}{exInfo}\n";

                    File.AppendAllText(filePath, line);
                }
            }
            catch
            {
                // File write failed silently - avoid recursion from logging inside logger
            }
        }
    }

    public static class RetryHelper
    {
        private static readonly Random _random = new();

        public static async Task<T?> RetryAsync<T>(
            Func<Task<T>> operation,
            int maxRetries = 3,
            int baseDelayMs = 500,
            string source = "RetryHelper",
            Func<Exception, bool>? shouldRetry = null) where T : class
        {
            Exception? lastException = null;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var result = await operation();
                    if (attempt > 1)
                        CameraLogger.Info(source, $"操作成功（第{attempt}次尝试）");
                    return result;
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (shouldRetry != null && !shouldRetry(ex))
                    {
                        CameraLogger.Warning(source, $"操作失败，不重试: {ex.Message}");
                        break;
                    }

                    if (attempt < maxRetries)
                    {
                        int delay = baseDelayMs * attempt + _random.Next(0, 100);
                        CameraLogger.Warning(source, $"操作失败（第{attempt}次），{delay}ms后重试: {ex.Message}");
                        await Task.Delay(delay);
                    }
                    else
                    {
                        CameraLogger.Error(source, $"操作失败（已重试{maxRetries}次）: {ex.Message}", ex);
                    }
                }
            }

            return null;
        }

        public static bool RetryAction(
            Action operation,
            int maxRetries = 3,
            int baseDelayMs = 500,
            string source = "RetryHelper",
            Func<Exception, bool>? shouldRetry = null)
        {
            Exception? lastException = null;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    operation();
                    if (attempt > 1)
                        CameraLogger.Info(source, $"操作成功（第{attempt}次尝试）");
                    return true;
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (shouldRetry != null && !shouldRetry(ex))
                    {
                        CameraLogger.Warning(source, $"操作失败，不重试: {ex.Message}");
                        break;
                    }

                    if (attempt < maxRetries)
                    {
                        int delay = baseDelayMs * attempt + _random.Next(0, 100);
                        CameraLogger.Warning(source, $"操作失败（第{attempt}次），{delay}ms后重试: {ex.Message}");
                        Thread.Sleep(delay);
                    }
                    else
                    {
                        CameraLogger.Error(source, $"操作失败（已重试{maxRetries}次）: {ex.Message}", ex);
                    }
                }
            }

            return false;
        }
    }
}