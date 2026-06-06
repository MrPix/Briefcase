namespace Briefcase.Components.Services;

public interface ITransferService
{
    Task<string> CreateSessionAsync();
    Task PushContentAsync(string sessionId, string content);
}
