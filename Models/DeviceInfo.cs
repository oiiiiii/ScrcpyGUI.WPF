namespace ScrcpyGUI.WPF.Models;

public class DeviceInfo
{
    public string SerialNumber { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ConnectionType { get; set; } = "USB";
    public string IpAddress { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}