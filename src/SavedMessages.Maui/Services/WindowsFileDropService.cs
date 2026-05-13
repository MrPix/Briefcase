#if WINDOWS
using SavedMessages.Components.Services;

namespace SavedMessages.Maui.Services;

public class WindowsFileDropService : IFileDropService
{
    public event Func<Stream, string, string, Task>? FileDropped;
    public bool IsSupported => true;

    public async Task HandleDroppedFilesAsync(IReadOnlyList<Windows.Storage.IStorageItem> items)
    {
        foreach (var item in items)
        {
            if (item is Windows.Storage.StorageFile file)
            {
                var stream = await file.OpenStreamForReadAsync();
                var contentType = file.ContentType ?? "application/octet-stream";
                if (FileDropped is not null)
                {
                    await FileDropped.Invoke(stream, file.Name, contentType);
                }
            }
        }
    }
}
#endif
