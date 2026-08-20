using System;
using System.IO;
using System.Text;
using System.Threading;
using InventoryPOS.Models;

namespace InventoryPOS.Services
{
    /// <summary>
    /// Simple file-based logger that writes daily log files to the application's
    /// LocalAppData folder. No external packages required. Thread-safe.
    /// </summary>
    public sealed class LoggerService
    {
        private static readonly Lazy<LoggerService> _instance = new(() => new LoggerService());
        public static LoggerService Instance => _instance.Value;

        /// <summary>
        /// Custom log folder path set via InitializeFromUiState, if any.
        /// Falls back to default %LOCALAPPDATA%\InventoryPOS\logs if not set.
        /// </summary>
        public static string? CustomLogFolderPath { get; set; }

        private readonly string _logFolder;
        private readonly object _lock = new();
        private StringBuilder? _buffer;
        private readonly System.Threading.Timer _flushTimer;
        private string _currentLogFilePath;
        private DateTime _currentLogDate;

        public enum LogLevel
        {
            Debug,
            Information,
            Warning,
            Error,
            Critical
        }

        /// <summary>
        /// Whether Debug-level messages are emitted (off by default).
        /// </summary>
        public bool IsDebugEnabled { get; set; } = false;

        /// <summary>
        /// Creates a new LoggerService instance. The log folder is determined
        /// by CustomLogFolderPath static property if set, otherwise falls back to
        /// the default %LOCALAPPDATA%\InventoryPOS\logs path.
        /// </summary>
        private LoggerService()
        {
            // Determine log folder: use custom path if set, otherwise default
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var defaultLogFolder = Path.Combine(appDataPath, "InventoryPOS", "logs");

            if (!string.IsNullOrWhiteSpace(CustomLogFolderPath))
            {
                _logFolder = CustomLogFolderPath;
                // Ensure the directory exists
                Directory.CreateDirectory(_logFolder);
            }
            else
            {
                _logFolder = defaultLogFolder;
                Directory.CreateDirectory(_logFolder);
            }

            _buffer = new StringBuilder();
            _currentLogDate = DateTime.Today;
            _currentLogFilePath = Path.Combine(_logFolder, $"InventoryPOS_{_currentLogDate:yyyyMMdd}.log");

            // Flush buffer every 30 seconds to disk
            _flushTimer = new System.Threading.Timer(FlushBuffer, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        private void FlushBuffer(object? state)
        {
            lock (_lock)
            {
                if (_buffer == null || _buffer.Length == 0) return;

                try
                {
                    RotateLogFileIfNeeded();
                    File.AppendAllText(_currentLogFilePath, _buffer.ToString());
                    _buffer.Clear();
                }
                catch
                {
                    // Swallow logging errors so logging never crashes the app
                }
            }
        }

        private void RotateLogFileIfNeeded()
        {
            var today = DateTime.Today;
            if (today != _currentLogDate)
            {
                _currentLogDate = today;
                _currentLogFilePath = Path.Combine(_logFolder, $"InventoryPOS_{_currentLogDate:yyyyMMdd}.log");
            }
        }

        private void Write(LogLevel level, string message, Exception? exception = null)
        {
            if (level == LogLevel.Debug && !IsDebugEnabled) return;

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var levelStr = level.ToString().ToUpperInvariant();
            var threadId = Thread.CurrentThread.ManagedThreadId;

            var line = $"[{timestamp}] [{levelStr}] [Thread-{threadId}] {message}";

            if (exception != null)
            {
                line += $" | Exception: {exception.GetType().FullName}: {exception.Message}";
                if (!string.IsNullOrEmpty(exception.StackTrace))
                    line += $" | StackTrace: {exception.StackTrace}";
                if (exception.InnerException != null)
                    line += $" | InnerException: {exception.InnerException.GetType().FullName}: {exception.InnerException.Message}";
            }

            line += Environment.NewLine;

            lock (_lock)
            {
                try
                {
                    RotateLogFileIfNeeded();
                    _buffer!.Append(line);

                    // Flush immediately for Warning, Error, and Critical to avoid losing crash data
                    if (level >= LogLevel.Warning)
                    {
                        FlushBufferNow();
                    }
                }
                catch
                {
                    // Swallow logging errors so logging never crashes the app
                }
            }
        }

        private void FlushBufferNow()
        {
            if (_buffer == null || _buffer.Length == 0) return;

            try
            {
                File.AppendAllText(_currentLogFilePath, _buffer.ToString());
                _buffer.Clear();
            }
            catch
            {
                // Swallow logging errors
            }
        }

        // Public log methods

        public void LogDebug(string message) => Write(LogLevel.Debug, message);

        public void LogInfo(string message) => Write(LogLevel.Information, message);

        public void LogWarning(string message) => Write(LogLevel.Warning, message);

        public void LogWarning(string message, Exception exception) => Write(LogLevel.Warning, message, exception);

        public void LogError(string message) => Write(LogLevel.Error, message);

        public void LogError(string message, Exception exception) => Write(LogLevel.Error, message, exception);

        public void LogCritical(string message) => Write(LogLevel.Critical, message);

        public void LogCritical(string message, Exception exception) => Write(LogLevel.Critical, message, exception);

        /// <summary>
        /// Returns the path to the current log file for this date.
        /// </summary>
        public string GetCurrentLogFilePath()
        {
            lock (_lock)
            {
                RotateLogFileIfNeeded();
                return _currentLogFilePath;
            }
        }

        /// <summary>
        /// Flushes any buffered log data to disk. Call before process exit.
        /// </summary>
        public void Flush()
        {
            lock (_lock)
            {
                FlushBufferNow();
            }
        }

        /// <summary>
        /// Initializes the logger with a custom log folder path from UiState.
        /// Call this after loading UiState in your application startup flow.
        /// </summary>
        /// <param name="uiState">UI state containing the optional LogFolderPath.</param>
        public static void InitializeFromUiState(UiState uiState)
        {
            if (uiState != null && !string.IsNullOrWhiteSpace(uiState.LogFolderPath))
            {
                CustomLogFolderPath = uiState.LogFolderPath;
            }
            else
            {
                CustomLogFolderPath = null;
            }
        }
    }
}