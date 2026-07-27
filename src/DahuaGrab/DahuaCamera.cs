using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        private CancellationTokenSource? _grabCts;
        private Task? _grabTask;

        private DeviceInfo _deviceInfo;

        public string Name { get; }
        public string SerialNumber { get; }
        public string VendorName { get; }
        public DeviceState State => _state;
        public bool IsOpen => _isOpen;

        public event Action<ImageData>? OnFrameCaptured;
        public event Action<string, Exception>? OnError;

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

        internal void SetDeviceInfo(DeviceInfo info) => _deviceInfo = info;

        private bool CreateHandle()
        {
            DestroyHandle();
            IntPtr pDeviceInfo = Marshal.AllocHGlobal(Marshal.SizeOf<DeviceInfo>());
            try
            {
                Marshal.StructureToPtr(_deviceInfo, pDeviceInfo, false);
                int ret = DahuaInterop.IMV_CreateHandle(out IntPtr pHandle, DahuaInterop.HandleType_Device, pDeviceInfo);
                if (ret != (int)DahuaInterop.IMV_OK)
                    return false;
                _handle = pHandle;
                return true;
            }
            finally
            {
                Marshal.FreeHGlobal(pDeviceInfo);
            }
        }

        private void DestroyHandle()
        {
            if (_handle != IntPtr.Zero)
            {
                try { DahuaInterop.IMV_DestroyHandle(_handle); } catch { }
                _handle = IntPtr.Zero;
            }
        }

        public bool Open()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DahuaCamera));
            if (_isOpen) return true;

            return RetryHelper.RetryAction(() =>
            {
                lock (_lock)
                {
                    CameraLogger.Info(nameof(DahuaCamera), $"正在打开相机 {SerialNumber}...");

                    if (!CreateHandle())
                        throw new CameraException("创建相机句柄失败");

                    int ret = DahuaInterop.IMV_OpenDev(_handle);
                    if (ret != (int)DahuaInterop.IMV_OK)
                    {
                        DestroyHandle();
                        string msg = (ret == unchecked((int)0x80000003) || ret == unchecked((int)0x80000004))
                            ? $"相机已被其他程序占用: 0x{ret:X8}"
                            : $"打开相机失败: 0x{ret:X8}";
                        throw new CameraException(msg, ret);
                    }

                    _isOpen = true;
                    _state = DeviceState.Connected;
                    CameraLogger.Info(nameof(DahuaCamera), $"相机 {SerialNumber} 已打开");
                }
            }, maxRetries: 3, baseDelayMs: 500, source: nameof(DahuaCamera));
        }

        public void Close()
        {
            if (!_isOpen || _disposed) return;

            try
            {
                StopContinuousGrab();
            }
            catch { }

            lock (_lock)
            {
                CameraLogger.Info(nameof(DahuaCamera), $"正在关闭相机 {SerialNumber}...");
                DahuaInterop.IMV_StopGrabbing(_handle);
                DahuaInterop.IMV_CloseDev(_handle);
                _isOpen = false;
                _state = DeviceState.Disconnected;
                CameraLogger.Info(nameof(DahuaCamera), $"相机 {SerialNumber} 已关闭");
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

        public bool StartContinuousGrab(int intervalMs = 100, CancellationToken? cancellationToken = null)
        {
            if (!_isOpen) throw new CameraException("相机未打开");

            StopContinuousGrab();

            _grabCts = cancellationToken != null
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken.Value)
                : new CancellationTokenSource();

            var token = _grabCts.Token;

            _grabTask = Task.Run(async () =>
            {
                CameraLogger.Info(nameof(DahuaCamera), $"开始连续采集，间隔 {intervalMs}ms");

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        if (GrabOneInternal(out ImageData frame, 5000))
                        {
                            try
                            {
                                OnFrameCaptured?.Invoke(frame);
                            }
                            finally
                            {
                                DahuaInterop.IMV_ReleaseImage(_handle, ref frame);
                            }
                        }

                        await Task.Delay(intervalMs, token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        CameraLogger.Error(nameof(DahuaCamera), "连续采集出错", ex);
                        OnError?.Invoke("连续采集出错", ex);
                        await Task.Delay(1000, token);
                    }
                }

                CameraLogger.Info(nameof(DahuaCamera), "连续采集已停止");
            }, token);

            _state = DeviceState.Grabbing;
            return true;
        }

        public void StopContinuousGrab()
        {
            if (_grabCts != null)
            {
                _grabCts.Cancel();
                _grabCts.Dispose();
                _grabCts = null;
            }

            if (_grabTask != null)
            {
                try
                {
                    _grabTask.Wait(2000);
                }
                catch { }

                _grabTask = null;
            }

            if (_state == DeviceState.Grabbing)
                _state = DeviceState.Connected;
        }

        public bool SetAutoExposure(bool enable, double targetBrightness = 128.0)
        {
            if (!_isOpen) return false;

            try
            {
                SetEnum("ExposureAuto", enable ? 1UL : 0UL);
                CameraLogger.Info(nameof(DahuaCamera), $"自动曝光 {(enable ? "启用" : "禁用")}");
                return true;
            }
            catch (Exception ex)
            {
                CameraLogger.Error(nameof(DahuaCamera), "设置自动曝光失败", ex);
                OnError?.Invoke("设置自动曝光失败", ex);
                return false;
            }
        }

        public bool SetAutoGain(bool enable)
        {
            if (!_isOpen) return false;

            try
            {
                SetEnum("GainAuto", enable ? 1UL : 0UL);
                CameraLogger.Info(nameof(DahuaCamera), $"自动增益 {(enable ? "启用" : "禁用")}");
                return true;
            }
            catch (Exception ex)
            {
                CameraLogger.Error(nameof(DahuaCamera), "设置自动增益失败", ex);
                OnError?.Invoke("设置自动增益失败", ex);
                return false;
            }
        }

        public bool SetBalanceRatioAuto(bool enable)
        {
            if (!_isOpen) return false;

            try
            {
                SetEnum("BalanceWhiteAuto", enable ? 1UL : 0UL);
                CameraLogger.Info(nameof(DahuaCamera), $"自动白平衡 {(enable ? "启用" : "禁用")}");
                return true;
            }
            catch (Exception ex)
            {
                CameraLogger.Error(nameof(DahuaCamera), "设置自动白平衡失败", ex);
                OnError?.Invoke("设置自动白平衡失败", ex);
                return false;
            }
        }

        public Dictionary<string, string> GetCameraInfo()
        {
            var info = new Dictionary<string, string>();

            if (!_isOpen) return info;

            try
            {
                info["ModelName"] = GetString("DeviceModelName") ?? "Unknown";
                info["SerialNumber"] = SerialNumber;
                info["VendorName"] = VendorName;
                info["FirmwareVersion"] = GetString("DeviceFirmwareVersion") ?? "Unknown";
                info["ManufacturerInfo"] = GetString("DeviceManufacturerInfo") ?? "Unknown";
                info["Width"] = Width.ToString();
                info["Height"] = Height.ToString();
            }
            catch (Exception ex)
            {
                CameraLogger.Warning(nameof(DahuaCamera), $"获取相机信息部分失败: {ex.Message}");
            }

            return info;
        }

        private string? GetString(string name)
        {
            if (!_isOpen) return null;

            try
            {
                var sb = new System.Text.StringBuilder(256);
                int ret = DahuaInterop.IMV_GetStringFeatureValue(_handle, name, sb, (uint)sb.Capacity);
                return ret == (int)DahuaInterop.IMV_OK ? sb.ToString() : null;
            }
            catch
            {
                return null;
            }
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

        private bool GrabOneInternal(out ImageData frame, int timeoutMs = 5000)
        {
            frame = default;

            if (!_isOpen) throw new CameraException("相机未打开");

            int ret = DahuaInterop.IMV_GetImage(_handle, out frame, (uint)timeoutMs);
            return ret == (int)DahuaInterop.IMV_OK;
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
            DestroyHandle();
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
                    camera.SetDeviceInfo(info);
                    cameras.Add(camera);
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
