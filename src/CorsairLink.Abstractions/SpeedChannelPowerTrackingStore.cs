namespace CorsairLink
{
    public sealed class SpeedChannelPowerTrackingStore
    {
        private readonly Dictionary<int, byte> _store = new();
        public bool Dirty { get; private set; } = false;

        public byte this[int key]
        {
            get
            {
                return _store[key];
            }
            set
            {
                if (!_store.TryGetValue(key, out var val) || val != value)
                {
                    _store[key] = value;
                    Dirty = true;
                }
            }
        }

        public void ResetDirty()
        {
            Dirty = false;
        }

        public void Clear()
        {
            _store.Clear();
            ResetDirty();
        }

        public IReadOnlyCollection<int> Channels => _store.Keys.ToList();
    }
}
