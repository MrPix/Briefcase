using Briefcase.Components.Services;

namespace Briefcase.Maui.Services;

public class MauiDeviceInfoProvider : IDeviceInfoProvider
{
    public string DeviceName => DeviceInfo.Current.Name;

    public string Platform => DeviceInfo.Current.Platform switch
    {
        var p when p == DevicePlatform.WinUI => "Windows",
        var p when p == DevicePlatform.Android => "Android",
        var p when p == DevicePlatform.iOS => "iOS",
        var p when p == DevicePlatform.macOS => "macOS",
        _ => "Web"
    };
}
