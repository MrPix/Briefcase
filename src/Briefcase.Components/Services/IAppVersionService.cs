namespace Briefcase.Components.Services;

public interface IAppVersionService
{
    /// <summary>Semantic version, e.g. "1.2.3" or "0.0.0".</summary>
    string Version { get; }

    /// <summary>CI build number, e.g. "42". Empty string for local builds.</summary>
    string BuildNumber { get; }

    /// <summary>Full informational version, e.g. "1.2.3+42".</summary>
    string FullVersion { get; }
}
