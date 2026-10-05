namespace SignalCut.Application.Interfaces;

public record RenderJobQueueItem(
    Guid OrganizationId,
    Guid JobId,
    Guid ClipId,
    decimal RequiredCredits
);

public interface IRenderJobQueue
{
    ValueTask EnqueueAsync(RenderJobQueueItem item, CancellationToken ct = default);
    IAsyncEnumerable<RenderJobQueueItem> DequeueAllAsync(CancellationToken ct);
}
