using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Briefcase.ApiService.Hubs;
using Briefcase.Domain.Entities;
using Briefcase.Infrastructure.Persistence;

namespace Briefcase.ApiService.Services;

public class GoogleMapsProcessingWorker(
    IServiceScopeFactory scopeFactory,
    IHubContext<MessageHub> hub,
    ILogger<GoogleMapsProcessingWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan ProcessingTimeout = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await ProcessBatchAsync(stoppingToken);
                if (!processed)
                    await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Google Maps processing batch failed.");
                await Task.Delay(PollInterval, stoppingToken);
            }
        }
    }

    private async Task<bool> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var resolver = scope.ServiceProvider.GetRequiredService<IGoogleMapsResolver>();
        var settingsService = scope.ServiceProvider.GetRequiredService<NavigationSettingsService>();
        var mapper = scope.ServiceProvider.GetRequiredService<MessageResponseMapper>();
        var staleBefore = DateTime.UtcNow - ProcessingTimeout;

        await db.Messages
            .Where(message => message.NavigationStatus == NavigationProcessingStatus.Processing
                && message.NavigationProcessingStartedAt < staleBefore)
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(message => message.NavigationStatus, NavigationProcessingStatus.Pending)
                .SetProperty(message => message.NavigationProcessingStartedAt, (DateTime?)null), cancellationToken);

        var candidateIds = await db.Messages.AsNoTracking()
            .Where(message => message.NavigationStatus == NavigationProcessingStatus.Pending
                && !message.IsDeleted
                && !message.IsPermanentlyDeleted)
            .OrderBy(message => message.CreatedAt)
            .Select(message => message.Id)
            .Take(10)
            .ToListAsync(cancellationToken);

        foreach (var id in candidateIds)
        {
            var now = DateTime.UtcNow;
            var claimed = await db.Messages
                .Where(message => message.Id == id && message.NavigationStatus == NavigationProcessingStatus.Pending)
                .ExecuteUpdateAsync(updates => updates
                    .SetProperty(message => message.NavigationStatus, NavigationProcessingStatus.Processing)
                    .SetProperty(message => message.NavigationProcessingStartedAt, now)
                    .SetProperty(message => message.NavigationProcessingAttempts, message => message.NavigationProcessingAttempts + 1), cancellationToken);
            if (claimed == 0) continue;

            var message = await db.Messages.Include(item => item.FileAttachment)
                .FirstAsync(item => item.Id == id, cancellationToken);
            var result = await resolver.ResolveAsync(message.Content!, cancellationToken);
            await db.Entry(message).ReloadAsync(cancellationToken);
            var preferences = await settingsService.GetAsync(message.UserId, cancellationToken);
            if (!preferences.Enabled || message.NavigationStatus != NavigationProcessingStatus.Processing)
                continue;

            if (result.Outcome == MapResolutionOutcome.Success)
            {
                message.NavigationStatus = NavigationProcessingStatus.Completed;
                message.NavigationLatitude = result.Latitude;
                message.NavigationLongitude = result.Longitude;
                message.NavigationProcessingError = null;
            }
            else
            {
                message.NavigationStatus = NavigationProcessingStatus.Failed;
                message.NavigationLatitude = null;
                message.NavigationLongitude = null;
                message.NavigationProcessingError = result.Error?[..Math.Min(result.Error.Length, 500)];
            }

            message.NavigationProcessingStartedAt = null;
            message.NavigationProcessedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            await hub.Clients.Group(message.UserId.ToString())
                .SendAsync(MessageHub.MessageUpdated, mapper.Map(message, preferences), cancellationToken);
        }

        return candidateIds.Count > 0;
    }
}