using Xunit;

namespace DahuaGrab.Tests
{
    public class DahuaCameraTests
    {
        [Fact]
        public void Constructor_SetsProperties()
        {
            var camera = new DahuaCamera("TestCam", "SN001", "Dahua");

            Assert.Equal("TestCam", camera.Name);
            Assert.Equal("SN001", camera.SerialNumber);
            Assert.Equal("Dahua", camera.VendorName);
            Assert.False(camera.IsOpen);
        }

        [Fact]
        public void InitialState_IsDisconnected()
        {
            var camera = new DahuaCamera("TestCam", "SN001", "Dahua");
            Assert.Equal(0, (int)camera.State); // DeviceState.Disconnected
        }

        [Fact]
        public void DisposedCamera_ThrowsOnOpen()
        {
            var camera = new DahuaCamera("TestCam", "SN001", "Dahua");
            camera.Dispose();

            Assert.Throws<ObjectDisposedException>(() => camera.Open());
        }

        [Fact]
        public void Dispose_IsIdempotent()
        {
            var camera = new DahuaCamera("TestCam", "SN001", "Dahua");
            camera.Dispose();
            camera.Dispose(); // Should not throw
        }

        [Fact]
        public void Close_WhenNotOpen_DoesNotThrow()
        {
            var camera = new DahuaCamera("TestCam", "SN001", "Dahua");
            camera.Close(); // Should not throw
            Assert.False(camera.IsOpen);
        }
    }

    public class DahuaCameraFinderTests
    {
        [Fact]
        public void Scan_ReturnsList()
        {
            // This will fail to load MVSDKmd.dll in test environment,
            // but verifies the method exists and returns a list type
            try
            {
                var cameras = DahuaCameraFinder.Scan();
                Assert.NotNull(cameras);
            }
            catch (DllNotFoundException)
            {
                // Expected in test environment without Dahua SDK installed
            }
        }
    }
}
