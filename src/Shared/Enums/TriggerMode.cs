namespace CameraSDK.Enums
{
    public enum TriggerMode
    {
        Continuous = 0,
        Software = 1,
        Hardware = 2
    }

    public enum TriggerSource
    {
        Software = 0,
        Line0 = 1,
        Line1 = 2,
        Line2 = 3,
        Line3 = 4
    }

    public enum PixelFormat
    {
        Mono8 = 0,
        Mono10 = 1,
        Mono12 = 2,
        BayerRG8 = 3,
        BayerGB8 = 4,
        BayerGR8 = 5,
        BayerBG8 = 6,
        RGB8 = 7,
        BGR8 = 8
    }

    public enum DeviceState
    {
        Disconnected = 0,
        Connected = 1,
        Grabbing = 2,
        Error = 3
    }
}
