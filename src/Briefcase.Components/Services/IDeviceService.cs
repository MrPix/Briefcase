using Briefcase.Domain.Entities;

namespace Briefcase.Components.Services;

public interface IDeviceService
{
    Task<IReadOnlyList<Device>> GetDevicesAsync();
    Task RemoveDeviceAsync(Guid deviceId);
    Task<string> GeneratePairCodeAsync();
    Task ClaimDeviceAsync(string token);
}
