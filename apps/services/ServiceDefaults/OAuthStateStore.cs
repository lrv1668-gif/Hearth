using System.Collections.Concurrent;

namespace ServiceDefaults;

/// <summary>
/// The CSRF <c>state</c> values of OAuth sign-ins still in flight. Each lives ten minutes and
/// is good for one callback. Expired entries are swept whenever a new one is added, so
/// repeated unauthenticated hits on an <c>/auth</c> endpoint can't grow it without bound.
/// </summary>
public sealed class OAuthStateStore(TimeProvider? time = null)
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    private readonly TimeProvider _time = time ?? TimeProvider.System;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _pending = new();

    /// <summary>Creates a new random state and remembers it.</summary>
    public string Issue()
    {
        var state = Guid.NewGuid().ToString("N");
        Remember(state);
        return state;
    }

    /// <summary>Remembers <paramref name="state"/>; remembering it again restarts its lifetime.</summary>
    public void Remember(string state)
    {
        var now = _time.GetUtcNow();
        foreach (var (key, expiry) in _pending)
        {
            if (expiry < now) _pending.TryRemove(key, out _);
        }
        _pending[state] = now + Lifetime;
    }

    /// <summary>True once for a state that was remembered and hasn't expired; false after that.</summary>
    public bool TryConsume(string state) =>
        _pending.TryRemove(state, out var expiry) && expiry >= _time.GetUtcNow();

    /// <summary>States currently held, expired or not (for tests).</summary>
    public int Count => _pending.Count;
}
