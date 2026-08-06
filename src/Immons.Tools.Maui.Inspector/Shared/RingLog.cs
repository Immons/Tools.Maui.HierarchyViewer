namespace Immons.Tools.Maui.Inspector.Shared;

/// <summary>Thread-safe, bounded, append-only buffer backing the in-memory logs.</summary>
internal sealed class RingLog<T>(int limit)
{
    readonly object _gate = new();
    readonly List<T> _entries = [];
    long _seq;

    public long LastSeq
    {
        get
        {
            lock (_gate)
            {
                return _seq;
            }
        }
    }

    /// <summary>Replaces the first entry matching the predicate with its transformed copy.</summary>
    public void Replace(Func<T, bool> match, Func<T, T> transform)
    {
        lock (_gate)
        {
            for (var i = 0; i < _entries.Count; i++)
            {
                if (match(_entries[i]))
                {
                    _entries[i] = transform(_entries[i]);
                    return;
                }
            }
        }
    }

    /// <summary>Adds the entry produced from the next sequence number, dropping the oldest at the limit.</summary>
    public void Add(Func<long, T> create)
    {
        lock (_gate)
        {
            _entries.Add(create(++_seq));
            if (_entries.Count > limit)
                _entries.RemoveAt(0);
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _entries.Clear();
        }
    }

    public T? Find(Func<T, bool> predicate)
    {
        lock (_gate)
        {
            return _entries.FirstOrDefault(predicate);
        }
    }

    /// <summary>Snapshot in newest-first order.</summary>
    public List<T> NewestFirst()
    {
        lock (_gate)
        {
            var copy = new List<T>(_entries);
            copy.Reverse();
            return copy;
        }
    }
}
