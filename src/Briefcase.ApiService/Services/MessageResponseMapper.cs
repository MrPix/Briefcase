using Briefcase.ApiService.Models;
using Briefcase.Domain.Entities;

namespace Briefcase.ApiService.Services;

public class MessageResponseMapper(NavigationApplicationCatalog catalog)
{
    public MessageResponse Map(Message message, NavigationPreferences preferences)
    {
        var targets = preferences.Enabled
            && message.NavigationStatus == NavigationProcessingStatus.Completed
            && message.NavigationLatitude.HasValue
            && message.NavigationLongitude.HasValue
                ? catalog.BuildTargets(
                    message.NavigationLatitude.Value,
                    message.NavigationLongitude.Value,
                    preferences.ApplicationIds)
                    .Select(target => new NavigationTargetResponse(target.ApplicationId, target.DisplayName, target.Uri))
                    .ToList()
                : [];

        return new MessageResponse(
            message.Id,
            message.Kind,
            message.Content,
            message.FileId,
            message.FileName ?? message.FileAttachment?.OriginalName,
            message.FilePreviewUrl ?? (message.FileAttachment?.PreviewBlobPath is not null
                ? $"/api/files/{message.FileAttachment.Id}/preview"
                : null),
            message.IsPinned,
            message.PinnedAt,
            message.IsEncrypted,
            message.EncryptionIV,
            message.NavigationStatus,
            targets,
            message.CreatedAt,
            message.UpdatedAt);
    }
}