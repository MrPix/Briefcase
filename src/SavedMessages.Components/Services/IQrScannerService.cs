namespace SavedMessages.Components.Services;

public interface IQrScannerService
{
    Task<string?> ScanAsync();
    bool IsSupported { get; }
}
