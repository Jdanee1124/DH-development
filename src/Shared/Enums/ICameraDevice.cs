using System;
using CameraSDK.Enums;

namespace CameraSDK
{
    public interface ICameraDevice : IDisposable
    {
        string Name { get; }
        string SerialNumber { get; }
        string VendorName { get; }
        DeviceState State { get; }

        bool Open();
        void Close();
        bool IsOpen { get; }

        double Exposure { get; set; }
        double ExposureMin { get; }
        double ExposureMax { get; }

        double Gain { get; set; }
        double GainMin { get; }
        double GainMax { get; }

        TriggerMode TriggerMode { get; set; }
        TriggerSource TriggerSource { get; set; }

        double PulseWidth { get; set; }
        double PulseWidthMin { get; }
        double PulseWidthMax { get; }

        int Width { get; }
        int Height { get; }

        bool StartGrabbing();
        bool StopGrabbing();
        bool SoftwareTrigger();

        bool GrabOne(IntPtr buffer, int bufferSize, int timeoutMs, out int width, out int height, out int pixelFormat);

        bool StartContinuousGrab(int intervalMs = 100, CancellationToken? cancellationToken = null);
        void StopContinuousGrab();

        bool SetAutoExposure(bool enable, double targetBrightness = 128.0);
        bool SetAutoGain(bool enable);
        bool SetBalanceRatioAuto(bool enable);

        Dictionary<string, string> GetCameraInfo();

        event Action<ImageData>? OnFrameCaptured;
        event Action<string, Exception>? OnError;
    }
}
