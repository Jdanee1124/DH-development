using Xunit;

namespace DahuaGrab.Tests
{
    public class InteropTests
    {
        [Fact]
        public void Structs_ImageData_HasExpectedSize()
        {
            // Verify ImageData struct can be marshaled
            int size = System.Runtime.InteropServices.Marshal.SizeOf<ImageData>();
            Assert.True(size > 0);
            Assert.True(size >= 20); // pData(8) + dataSize(4) + width(4) + height(4) = 20 minimum
        }

        [Fact]
        public void Structs_DeviceInfo_HasExpectedSize()
        {
            int size = System.Runtime.InteropServices.Marshal.SizeOf<DeviceInfo>();
            Assert.True(size > 0);
        }

        [Fact]
        public void Structs_DeviceList_HasExpectedSize()
        {
            int size = System.Runtime.InteropServices.Marshal.SizeOf<DeviceList>();
            Assert.Equal(16, size); // uint(4) + padding(4) + IntPtr(8) on x64
        }

        [Fact]
        public void Constants_AreDefined()
        {
            Assert.Equal(0u, DahuaInterop.IMV_OK);
            Assert.Equal(0u, DahuaInterop.HandleType_Device);
            Assert.Equal(0xFFFFFFFF, DahuaInterop.InterfaceType_All);
            Assert.Equal(0x1u, DahuaInterop.InterfaceType_GigE);
            Assert.Equal(0x2u, DahuaInterop.InterfaceType_USB);
        }

        [Fact]
        public void ImageData_Fields_AreAccessible()
        {
            var data = new ImageData();
            Assert.Equal(IntPtr.Zero, data.pData);
            Assert.Equal(0u, data.dataSize);
            Assert.Equal(0u, data.width);
            Assert.Equal(0u, data.height);
            Assert.Equal(0u, data.pixelFormat);
            Assert.Equal(0u, data.frameCount);
        }

        [Fact]
        public void DeviceInfo_Fields_AreAccessible()
        {
            var info = new DeviceInfo();
            Assert.Equal(0u, info.nInterfaceType);
        }
    }
}
