using System.Runtime.InteropServices;
using CameraSDK;
using CameraSDK.Enums;
using CameraSDK.Exceptions;

namespace DahuaGrab
{
    public class DahuaCamera : ICameraDevice
    {
        private IntPtr _handle = IntPtr.Zero;
        private bool _isOpen;
        private bool _disposed;
        private DeviceState _state = DeviceState.Disconnected;
        private readonly object _lock = new();

        public string Name { get; }
        public string SerialNumber { get; }
        public string VendorName { get; }
        public DeviceState State => _state;
        public bool IsOpen => _isOpen;

        public double Exposure { get => GetDouble("ExposureTime"); set => SetDouble("ExposureTime", value); }
        public double ExposureMin => GetDoubleMin("ExposureTime");
        public double ExposureMax => GetDoubleMax("ExposureTime");

        public double Gain { get => GetDouble("Gain"); set => SetDouble("Gain", value); }
        public double GainMin => GetDoubleMin("Gain");
        public double GainMax => GetDoubleMax("Gain");

        public TriggerMode TriggerMode
        {
            get => (TriggerMode)GetEnum("TriggerMode");
            set => SetEnum("TriggerMode", (ulong)value);
        }

        public TriggerSource TriggerSource
        {
            get => (TriggerSource)GetEnum("TriggerSource");
            set => SetEnum("TriggerSource", (ulong)value);
        }

        public double PulseWidth { get => GetDouble("TriggerPulseWidth"); set => SetDouble("TriggerPulseWidth", value); }
        public double PulseWidthMin => GetDoubleMin("TriggerPulseWidth");
        public double PulseWidthMax => GetDoubleMax("TriggerPulseWidth");

        public int Width => (int)GetInt("Width");
        public int Height => (int)GetInt("Height");

        public DahuaCamera(string name, string serialNumber, string vendorName)
        {
            Name = name;
            SerialNumber = serialNumber;
            VendorName = vendorName;
        }

        internal void SetHandle(IntPtr handle) => _handle = handle;

        public bool Open()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DahuaCamera));
            if (_isOpen) return true;

            lock (_lock)
            {
                int ret = DahuaInterop.IMV_OpenDev(_handle);
                if (ret != (int)DahuaInterop.IMV_OK)
                    throw new CameraException($"打开相机失败: 0x{ret:X8}", ret);

                _isOpen = true;
                _state = DeviceState.Connected;
                return true;
            }
        }

        public void Close()
        {
            if (!_isOpen || _disposed) return;

            lock (_lock)
            {
                DahuaInterop.IMV_StopGrabbing(_handle);
                DahuaInterop.IMV_CloseDev(_handle);
                _isOpen = false;
                _state = DeviceState.Disconnected;
            }
        }

        public bool StartGrabbing()
        {
            if (!_isOpen) throw new CameraException("相机未打开");

            lock (_lock)
            {
                int ret = DahuaInterop.IMV_StartGrabbing(_handle);
                if (ret != (int)DahuaInterop.IMV_OK)
                    throw new CameraException($"开始采集失败: 0x{ret:X8}", ret);

                _state = DeviceState.Grabbing;
                return true;
            }
        }

        public bool StopGrabbing()
        {
            if (!_isOpen) return true;

            lock (_lock)
            {
                int ret = DahuaInterop.IMV_StopGrabbing(_handle);
                if (ret != (int)DahuaInterop.IMV_OK)
                    throw new CameraException($"停止采集失败: 0x{ret:X8}", ret);

                _state = DeviceState.Connected;
                return true;
            }
        }

        public bool SoftwareTrigger()
        {
            if (!_isOpen) throw new CameraException("相机未打开");

            int ret = DahuaInterop.IMV_TriggerSoftwareExecute(_handle);
            if (ret != (int)DahuaInterop.IMV_OK)
                throw new CameraException($"软触发失败: 0x{ret:X8}", ret);

            return true;
        }

        public bool GrabOne(IntPtr buffer, int bufferSize, int timeoutMs, out int width, out int height, out int pixelFormat)
        {
            width = 0;
            height = 0;
            pixelFormat = 0;

            if (!_isOpen) throw new CameraException("相机未打开");

            int ret = DahuaInterop.IMV_GetImage(_handle, out ImageData frame, (uint)timeoutMs);
            if (ret != (int)DahuaInterop.IMV_OK)
                return false;

            try
            {
                width = (int)frame.width;
                height = (int)frame.height;
                pixelFormat = (int)frame.pixelFormat;

                int copySize = Math.Min((int)frame.dataSize, bufferSize);
                if (frame.pData != IntPtr.Zero && copySize > 0)
                    unsafe { Buffer.MemoryCopy((void*)frame.pData, (void*)buffer, bufferSize, copySize); }

                return true;
            }
            finally
            {
                DahuaInterop.IMV_ReleaseImage(_handle, ref frame);
            }
        }

        private void SetDouble(string name, double value)
        {
            if (!_isOpen) return;
            int ret = DahuaInterop.IMV_SetDoubleFeatureValue(_handle, name, value);
            if (ret != (int)DahuaInterop.IMV_OK)
                throw new CameraException($"设置{name}失败: 0x{ret:X8}", ret);
        }

        private double GetDouble(string name)
        {
            if (!_isOpen) return 0;
            int ret = DahuaInterop.IMV_GetDoubleFeatureValue(_handle, name, out double value);
            return ret == (int)DahuaInterop.IMV_OK ? value : 0;
        }

        private double GetDoubleMin(string name)
        {
            if (!_isOpen) return 0;
            int ret = DahuaInterop.IMV_GetDoubleFeatureMin(_handle, name, out double value);
            return ret == (int)DahuaInterop.IMV_OK ? value : 0;
        }

        private double GetDoubleMax(string name)
        {
            if (!_isOpen) return 0;
            int ret = DahuaInterop.IMV_GetDoubleFeatureMax(_handle, name, out double value);
            return ret == (int)DahuaInterop.IMV_OK ? value : 0;
        }

        private void SetEnum(string name, ulong value)
        {
            if (!_isOpen) return;
            int ret = DahuaInterop.IMV_SetEnumFeatureValue(_handle, name, value);
            if (ret != (int)DahuaInterop.IMV_OK)
                throw new CameraException($"设置{name}失败: 0x{ret:X8}", ret);
        }

        private ulong GetEnum(string name)
        {
            if (!_isOpen) return 0;
            int ret = DahuaInterop.IMV_GetEnumFeatureValue(_handle, name, out ulong value);
            return ret == (int)DahuaInterop.IMV_OK ? value : 0;
        }

        private long GetInt(string name)
        {
            if (!_isOpen) return 0;
            int ret = DahuaInterop.IMV_GetIntFeatureValue(_handle, name, out long value);
            return ret == (int)DahuaInterop.IMV_OK ? value : 0;
        }

        public void Dispose()
        {
            if (_disposed) return;
            Close();
            if (_handle != IntPtr.Zero)
            {
                DahuaInterop.IMV_DestroyHandle(_handle);
                _handle = IntPtr.Zero;
            }
            _disposed = true;
        }
    }

    public static class DahuaCameraFinder
    {
        public static List<DahuaCamera> Scan()
        {
            var cameras = new List<DahuaCamera>();

            int ret = DahuaInterop.IMV_EnumDevices(out IntPtr pDeviceList, DahuaInterop.InterfaceType_All);
            if (ret != (int)DahuaInterop.IMV_OK || pDeviceList == IntPtr.Zero)
                return cameras;

            try
            {
                var list = Marshal.PtrToStructure<DeviceList>(pDeviceList);
                int deviceInfoSize = Marshal.SizeOf<DeviceInfo>();

                for (uint i = 0; i < list.deviceNum; i++)
                {
                    IntPtr pDeviceInfo = IntPtr.Add(list.pDeviceInfo, (int)(i * deviceInfoSize));
                    var info = Marshal.PtrToStructure<DeviceInfo>(pDeviceInfo);

                    var camera = new DahuaCamera(info.cameraName, info.serialNumber, info.vendorName);

                    ret = DahuaInterop.IMV_CreateHandle(out IntPtr pHandle, DahuaInterop.HandleType_Device, pDeviceInfo);
                    if (ret == (int)DahuaInterop.IMV_OK)
                    {
                        camera.SetHandle(pHandle);
                        cameras.Add(camera);
                    }
                }
            }
            finally
            {
                DahuaInterop.IMV_DestroyDeviceList(pDeviceList);
            }

            return cameras;
        }
    }
}
