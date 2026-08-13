namespace Briefcase.Components.Services;

public interface IDeviceInfoProvider
{
    string DeviceName { get; }
    string Platform { get; }

    /// <summary>Stable id for this install so the server can bind sessions to this device.</summary>
    string? InstallationId { get; }
}

/// <summary>
/// Default fallback for platforms where device info is not available (e.g. web).
/// </summary>
public class DefaultDeviceInfoProvider : IDeviceInfoProvider
{
    public string DeviceName => "Web Browser";
    public string Platform => "Web";
    public string? InstallationId => null;
}
