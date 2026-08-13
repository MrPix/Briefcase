using Briefcase.Components.Services;

namespace Briefcase.Maui.Services;

public class MauiDeviceInfoProvider : IDeviceInfoProvider
{
    private const string InstallationIdKey = "installation_id";

    public string DeviceName => DeviceInfo.Current.Name;

    public string? InstallationId
    {
        get
        {
            var id = Preferences.Default.Get<string?>(InstallationIdKey, null);
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString();
                Preferences.Default.Set(InstallationIdKey, id);
            }
            return id;
        }
    }

    public string Platform => DeviceInfo.Current.Platform switch
    {
        var p when p == DevicePlatform.WinUI => "Windows",
        var p when p == DevicePlatform.Android => "Android",
        var p when p == DevicePlatform.iOS => "iOS",
        var p when p == DevicePlatform.macOS => "macOS",
        _ => "Web"
    };
}
