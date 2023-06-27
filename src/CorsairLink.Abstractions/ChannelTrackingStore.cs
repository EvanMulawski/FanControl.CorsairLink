namespace CorsairLink;

public sealed class ChannelTrackingStore
{
    private readonly Dictionary<int, byte> _store = new();
    private readonly Queue<(int Channel, byte Value)> _queue = new();

    private readonly object _lock = new();

    public byte this[int key]
    {
        get
        {
            return _store[key];
        }
        set
        {
            lock (_lock)
            {
                _queue.Enqueue((key, value));
            }
        }
    }

    public bool ApplyChanges()
    {
        lock (_lock)
        {
            var dirty = false;

            while (_queue.Count > 0)
            {
                var (channel, value) = _queue.Dequeue();
                if (!_store.TryGetValue(channel, out var currValue) || currValue != value)
                {
                    _store[channel] = value;
                    dirty = true;
                }
            }

            return dirty;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            while (_queue.Count > 0)
                _queue.Dequeue();
            _store.Clear();
        }
    }

    public IReadOnlyCollection<int> Channels => _store.Keys.ToList();

    internal int QueueLength => _queue.Count;
}
