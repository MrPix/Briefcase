using System.Reflection;

namespace Briefcase.Components.Services;

public class AppVersionService : IAppVersionService
{
    public string Version { get; }
    public string BuildNumber { get; }
    public string FullVersion { get; }

    public AppVersionService()
    {
        var raw = Assembly.GetEntryAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "0.0.0+0";

        var plusIndex = raw.IndexOf('+');
        if (plusIndex >= 0)
        {
            Version = raw[..plusIndex];
            BuildNumber = raw[(plusIndex + 1)..];
        }
        else
        {
            Version = raw;
            BuildNumber = string.Empty;
        }

        FullVersion = raw;
    }
}
