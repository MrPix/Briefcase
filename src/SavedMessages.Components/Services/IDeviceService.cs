using SavedMessages.Domain.Entities;

namespace SavedMessages.Components.Services;

public interface IDeviceService
{
    Task<IReadOnlyList<Device>> GetDevicesAsync();
    Task RemoveDeviceAsync(Guid deviceId);
    Task<string> GeneratePairCodeAsync();
    Task ClaimDeviceAsync(string token);
}
