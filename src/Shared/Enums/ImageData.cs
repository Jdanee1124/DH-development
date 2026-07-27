using System;
using System.Runtime.InteropServices;

namespace CameraSDK
{
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct ImageData
    {
        public IntPtr pData;
        public uint dataSize;
        public uint width;
        public uint height;
        public uint pixelFormat;
        public ulong timeStamp;
        public uint frameCount;
        public uint channelId;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] reserved;

        public IntPtr DataPtr => pData;
        public uint DataSize => dataSize;
        public uint Width => width;
        public uint Height => height;
        public uint PixelFormat => pixelFormat;
    }
}