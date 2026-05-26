using System.Xml;
using CameraSDK.Enums;
using DIAVision.Core.CommonTypes;
using DIAVision.Core.FlowElements;
using DIAVision.Core.FlowElements.Attributes;

namespace DahuaGrab
{
    [Category("Camera")]
    [Description("大华相机采集算子 - 支持曝光、增益、触发模式、脉冲宽度控制")]
    public class DahuaGrab : FlowElement
    {
        private DahuaCamera? _camera;
        private Image? _imageBuffer;
        private bool _disposed;
        private readonly object _lock = new();
        private readonly List<DahuaCamera> _availableCameras = new();

        #region Inputs

        [Input]
        [DynamicName]
        [DynamicInitialValue]
        [Category("Camera", 1)]
        [Description("相机序列号")]
        public string CameraSerialNumber { get => (string)GetInputValue(nameof(CameraSerialNumber)); }

        [Input]
        [Category("Parameters", 2)]
        [InitialValue("10000")]
        [Range(1, 10000000, RangeMode.Inclusive)]
        [Description("曝光时间(us)")]
        public double Exposure { get => (double)GetInputValue(nameof(Exposure)); }

        [Input]
        [Category("Parameters", 2)]
        [InitialValue("1")]
        [Range(0, 32, RangeMode.Inclusive)]
        [Description("增益")]
        public double Gain { get => (double)GetInputValue(nameof(Gain)); }

        [Input]
        [Category("Trigger", 3)]
        [InitialValue("Software")]
        [Description("触发模式: Continuous/Software/Hardware")]
        public TriggerModeEnum TriggerMode { get => (TriggerModeEnum)GetInputValue(nameof(TriggerMode)); }

        [Input]
        [Category("Trigger", 3)]
        [InitialValue("Software")]
        [Description("触发源: Software/Line0/Line1")]
        public TriggerSourceEnum TriggerSource { get => (TriggerSourceEnum)GetInputValue(nameof(TriggerSource)); }

        [Input]
        [Category("Trigger", 3)]
        [InitialValue("10")]
        [Range(1, 100000, RangeMode.Inclusive)]
        [Description("触发脉冲宽度(us)")]
        public double PulseWidth { get => (double)GetInputValue(nameof(PulseWidth)); }

        [Input]
        [Category("Grab", 4)]
        [InitialValue("5000")]
        [Range(100, 60000, RangeMode.Inclusive)]
        [Description("采集超时(ms)")]
        public int Timeout { get => (int)GetInputValue(nameof(Timeout)); }

        #endregion

        #region Outputs

        [Output]
        [Category("Result", 1)]
        [Description("采集图像")]
        public Image OutputImage => _imageBuffer;

        [Output]
        [Category("Status", 2)]
        [Description("是否已连接")]
        public bool IsConnected { get; private set; }

        [Output]
        [Category("Status", 2)]
        [Description("采集状态")]
        [OutputCategory(CategoryTypes.Status)]
        public bool GrabSuccess { get; private set; }

        [Output]
        [Category("Info", 3)]
        [Description("图像宽度")]
        public int ImageWidth { get; private set; }

        [Output]
        [Category("Info", 3)]
        [Description("图像高度")]
        public int ImageHeight { get; private set; }

        #endregion

        #region Commands

        [Command]
        [Description("扫描可用相机")]
        public bool ScanCameras()
        {
            try
            {
                foreach (var cam in _availableCameras)
                    cam.Dispose();
                _availableCameras.Clear();

                _availableCameras.AddRange(DahuaCameraFinder.Scan());
                return _availableCameras.Count > 0;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"扫描相机失败: {ex.Message}";
                return false;
            }
        }

        [Command]
        [Description("连接相机")]
        public bool Connect()
        {
            try
            {
                Disconnect();

                string serial = CameraSerialNumber;
                if (string.IsNullOrEmpty(serial))
                {
                    if (_availableCameras.Count == 0) ScanCameras();
                    if (_availableCameras.Count == 0)
                    {
                        ErrorMessage = "未发现可用相机";
                        return false;
                    }
                    serial = _availableCameras[0].SerialNumber;
                }

                _camera = _availableCameras.FirstOrDefault(c => c.SerialNumber == serial);
                if (_camera == null)
                {
                    ScanCameras();
                    _camera = _availableCameras.FirstOrDefault(c => c.SerialNumber == serial);
                    if (_camera == null)
                    {
                        ErrorMessage = $"未找到相机: {serial}";
                        return false;
                    }
                }

                _camera.Open();
                ApplyParameters();
                IsConnected = true;
                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"连接相机失败: {ex.Message}";
                IsConnected = false;
                return false;
            }
        }

        [Command]
        [Description("断开相机")]
        public bool Disconnect()
        {
            try
            {
                if (_camera != null)
                {
                    if (_camera.State == DeviceState.Grabbing)
                        _camera.StopGrabbing();
                    _camera.Close();
                }
                IsConnected = false;
                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"断开相机失败: {ex.Message}";
                return false;
            }
        }

        [Command]
        [Description("软触发采集")]
        public bool Trigger()
        {
            try
            {
                if (_camera == null || !_camera.IsOpen)
                {
                    ErrorMessage = "相机未连接";
                    return false;
                }
                return _camera.SoftwareTrigger();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"软触发失败: {ex.Message}";
                return false;
            }
        }

        #endregion

        protected override IReadOnlyList<string> GetDynamicValueNames(string inputName)
        {
            if (inputName == nameof(CameraSerialNumber))
            {
                if (_availableCameras.Count == 0)
                    ScanCameras();

                return _availableCameras
                    .Select(c => $"\"{c.SerialNumber}\"")
                    .ToList()
                    .AsReadOnly();
            }
            return base.GetDynamicValueNames(inputName);
        }

        protected override string GetDynamicInitialValue(string inputName)
        {
            if (inputName == nameof(CameraSerialNumber))
            {
                if (_availableCameras.Count == 0)
                    ScanCameras();
                return _availableCameras.FirstOrDefault()?.SerialNumber ?? "";
            }
            return base.GetDynamicInitialValue(inputName);
        }

        public override bool CanExecute() => _camera != null && _camera.IsOpen;

        public override bool Execute()
        {
            GrabSuccess = false;

            if (_camera == null || !_camera.IsOpen)
            {
                ErrorMessage = "相机未连接";
                return false;
            }

            try
            {
                ApplyParameters();

                int w, h, fmt;
                int bufSize = _camera.Width * _camera.Height * 3;

                if (_imageBuffer == null || _imageBuffer.Width != _camera.Width || _imageBuffer.Height != _camera.Height)
                {
                    _imageBuffer?.Dispose();
                    _imageBuffer = new Image();
                    _imageBuffer.Allocate(_camera.Width, _camera.Height, 8);
                }

                lock (_lock)
                {
                    if (TriggerMode == TriggerModeEnum.Software)
                        _camera.SoftwareTrigger();

                    if (!_camera.GrabOne(_imageBuffer.Bits, bufSize, Timeout, out w, out h, out fmt))
                    {
                        ErrorMessage = "采集超时";
                        return false;
                    }
                }

                ImageWidth = w;
                ImageHeight = h;
                GrabSuccess = true;
                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"采集失败: {ex.Message}";
                return false;
            }
        }

        private void ApplyParameters()
        {
            if (_camera == null || !_camera.IsOpen) return;

            _camera.Exposure = Exposure;
            _camera.Gain = Gain;
            _camera.TriggerMode = (CameraSDK.Enums.TriggerMode)TriggerMode;
            _camera.TriggerSource = (CameraSDK.Enums.TriggerSource)TriggerSource;
            _camera.PulseWidth = PulseWidth;
        }

        public override void SaveCustomData(XmlWriter writer, string folderPath)
        {
            writer.WriteStartElement("DahuaGrab");
            writer.WriteAttributeString("CameraSerialNumber", CameraSerialNumber);
            writer.WriteAttributeString("Exposure", Exposure.ToString());
            writer.WriteAttributeString("Gain", Gain.ToString());
            writer.WriteAttributeString("TriggerMode", TriggerMode.ToString());
            writer.WriteAttributeString("TriggerSource", TriggerSource.ToString());
            writer.WriteAttributeString("PulseWidth", PulseWidth.ToString());
            writer.WriteEndElement();
        }

        public override bool LoadCustomData(XmlElement element)
        {
            var elem = element.ChildNodes.Cast<XmlNode>()
                .FirstOrDefault(n => n.Name == "DahuaGrab") as XmlElement;
            if (elem != null)
            {
                if (elem.HasAttribute("CameraSerialNumber"))
                    InputExpressions[nameof(CameraSerialNumber)].SetExpression($"\"{elem.GetAttribute("CameraSerialNumber")}\"");
                if (elem.HasAttribute("Exposure"))
                    InputExpressions[nameof(Exposure)].SetExpression(elem.GetAttribute("Exposure"));
                if (elem.HasAttribute("Gain"))
                    InputExpressions[nameof(Gain)].SetExpression(elem.GetAttribute("Gain"));
                if (elem.HasAttribute("PulseWidth"))
                    InputExpressions[nameof(PulseWidth)].SetExpression(elem.GetAttribute("PulseWidth"));
            }
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                Disconnect();
                _imageBuffer?.Dispose();
                foreach (var cam in _availableCameras)
                    cam.Dispose();
                _availableCameras.Clear();
            }
            _disposed = true;
            base.Dispose(disposing);
        }
    }

    public enum TriggerModeEnum
    {
        Continuous = 0,
        Software = 1,
        Hardware = 2
    }

    public enum TriggerSourceEnum
    {
        Software = 0,
        Line0 = 1,
        Line1 = 2,
        Line2 = 3,
        Line3 = 4
    }
}
