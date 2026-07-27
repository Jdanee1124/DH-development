using System.IO;
using CameraSDK;
using Xunit;

namespace DahuaGrab.Tests
{
    public class CameraLoggerTests : IDisposable
    {
        private readonly string _testLogDir;

        public CameraLoggerTests()
        {
            _testLogDir = Path.Combine(Path.GetTempPath(), $"CameraLoggerTest_{Guid.NewGuid():N}");
            CameraLogger.LogDirectory = _testLogDir;
            CameraLogger.MinLevel = LogLevel.Debug;
            CameraLogger.Clear();
        }

        public void Dispose()
        {
            CameraLogger.LogDirectory = string.Empty;
            if (Directory.Exists(_testLogDir))
                Directory.Delete(_testLogDir, true);
        }

        [Fact]
        public void Info_AddsToLogs()
        {
            CameraLogger.Info("Test", "hello");

            Assert.Single(CameraLogger.Logs);
            Assert.Equal(LogLevel.Info, CameraLogger.Logs[0].Level);
            Assert.Equal("Test", CameraLogger.Logs[0].Source);
            Assert.Equal("hello", CameraLogger.Logs[0].Message);
        }

        [Fact]
        public void Error_IncludesException()
        {
            var ex = new InvalidOperationException("boom");
            CameraLogger.Error("Test", "fail", ex);

            var log = CameraLogger.Logs[0];
            Assert.Equal(LogLevel.Error, log.Level);
            Assert.NotNull(log.Exception);
            Assert.Equal("boom", log.Exception!.Message);
        }

        [Fact]
        public void MinLevel_FiltersLogs()
        {
            CameraLogger.MinLevel = LogLevel.Warning;
            CameraLogger.Debug("T", "d");
            CameraLogger.Info("T", "i");
            CameraLogger.Warning("T", "w");

            Assert.Single(CameraLogger.Logs);
            Assert.Equal(LogLevel.Warning, CameraLogger.Logs[0].Level);
        }

        [Fact]
        public void MaxLogs_KeepsLatest()
        {
            for (int i = 0; i < 1100; i++)
                CameraLogger.Info("T", $"msg{i}");

            Assert.True(CameraLogger.Logs.Count <= 1000);
            Assert.Contains("msg1099", CameraLogger.Logs.Last().Message);
        }

        [Fact]
        public void GetLogs_FormatsCorrectly()
        {
            CameraLogger.Info("Src", "test msg");
            string result = CameraLogger.GetLogs();

            Assert.Contains("[Info]", result);
            Assert.Contains("[Src]", result);
            Assert.Contains("test msg", result);
        }

        [Fact]
        public void Clear_RemovesAllLogs()
        {
            CameraLogger.Info("T", "a");
            CameraLogger.Clear();
            Assert.Empty(CameraLogger.Logs);
        }

        [Fact]
        public void OnLogAdded_Fires()
        {
            LogEntry? received = null;
            CameraLogger.OnLogAdded += e => received = e;
            CameraLogger.Info("T", "event");

            Assert.NotNull(received);
            Assert.Equal("event", received!.Message);
        }

        [Fact]
        public void WriteToFile_CreatesLogFile()
        {
            CameraLogger.Info("T", "file test");

            Assert.True(Directory.Exists(_testLogDir));
            var files = Directory.GetFiles(_testLogDir, "camera_*.log");
            Assert.Single(files);

            string content = File.ReadAllText(files[0]);
            Assert.Contains("file test", content);
        }
    }
}
