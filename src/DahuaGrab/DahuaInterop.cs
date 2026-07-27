using System.Runtime.InteropServices;
using CameraSDK;

namespace DahuaGrab
{
    internal static class DahuaInterop
    {
        private const string DllName = "MVSDKmd";

        // 设备枚举
        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern int IMV_EnumDevices(out IntPtr pDeviceList, uint nInterfaceType);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern int IMV_DestroyDeviceList(IntPtr pDeviceList);

        // 句柄管理
        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern int IMV_CreateHandle(out IntPtr pHandle, uint handleType, IntPtr pInfo);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern int IMV_DestroyHandle(IntPtr pHandle);

        // 设备操作
        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern int IMV_OpenDev(IntPtr pHandle);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern int IMV_CloseDev(IntPtr pHandle);

        // 采集控制
        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern int IMV_StartGrabbing(IntPtr pHandle);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern int IMV_StopGrabbing(IntPtr pHandle);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern int IMV_TriggerSoftwareExecute(IntPtr pHandle);

        // 图像获取
        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern int IMV_GetImage(IntPtr pHandle, out ImageData pFrame, uint timeoutMs);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern int IMV_ReleaseImage(IntPtr pHandle, ref ImageData pFrame);

        // GenICam 参数 - Float
        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int IMV_SetDoubleFeatureValue(IntPtr pHandle, string pFeatureName, double value);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int IMV_GetDoubleFeatureValue(IntPtr pHandle, string pFeatureName, out double pValue);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int IMV_GetDoubleFeatureMin(IntPtr pHandle, string pFeatureName, out double pMin);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int IMV_GetDoubleFeatureMax(IntPtr pHandle, string pFeatureName, out double pMax);

        // GenICam 参数 - Enum
        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int IMV_SetEnumFeatureValue(IntPtr pHandle, string pFeatureName, ulong value);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int IMV_GetEnumFeatureValue(IntPtr pHandle, string pFeatureName, out ulong pValue);

        // GenICam 参数 - Int
        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int IMV_SetIntFeatureValue(IntPtr pHandle, string pFeatureName, long value);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int IMV_GetIntFeatureValue(IntPtr pHandle, string pFeatureName, out long pValue);

        // GenICam 参数 - String
        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int IMV_GetStringFeatureValue(IntPtr pHandle, string pFeatureName, System.Text.StringBuilder pValue, uint bufSize);

        // 设备信息
        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int IMV_GetDeviceInfo(IntPtr pHandle, out DeviceInfo pInfo);

        // 帧回调
        [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
        public static extern int IMV_AttachGrabbing(IntPtr pHandle, FrameCallback callback);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void FrameCallback(ref ImageData pFrame, IntPtr pUser);

        // 常量
        public const uint IMV_OK = 0;
        public const uint HandleType_Device = 0;
        public const uint InterfaceType_GigE = 0x1;
        public const uint InterfaceType_USB = 0x2;
        public const uint InterfaceType_All = 0xFFFFFFFF;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct DeviceInfo
    {
        public uint nInterfaceType;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string cameraName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string serialNumber;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string vendorName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string modelName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string ipAddress;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DeviceList
    {
        public uint deviceNum;
        public IntPtr pDeviceInfo;
    }
}
