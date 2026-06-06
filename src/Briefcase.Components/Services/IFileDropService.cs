namespace Briefcase.Components.Services;

public interface IFileDropService
{
    event Func<Stream, string, string, Task>? FileDropped;
    bool IsSupported { get; }
}
