using System.Threading.Channels;
using SignalCut.Application.Interfaces;

namespace SignalCut.Infrastructure.Services;

public class ChannelRenderJobQueue : IRenderJobQueue
{
    private readonly Channel<RenderJobQueueItem> _channel;

    public ChannelRenderJobQueue()
    {
        var options = new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        };
        _channel = Channel.CreateUnbounded<RenderJobQueueItem>(options);
    }

    public ValueTask EnqueueAsync(RenderJobQueueItem item, CancellationToken ct = default)
    {
        return _channel.Writer.WriteAsync(item, ct);
    }

    public IAsyncEnumerable<RenderJobQueueItem> DequeueAllAsync(CancellationToken ct)
    {
        return _channel.Reader.ReadAllAsync(ct);
    }
}
