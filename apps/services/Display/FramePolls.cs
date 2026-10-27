using System.Text.Json.Serialization;

namespace Display;

/// <summary>
/// The frame client's last GET /display/frame: when, from where, who (its User-Agent) and
/// the ETag it was left holding — null when nothing was served (a 503).
/// </summary>
public sealed record FramePoll(
    DateTimeOffset At, string? Address, string? UserAgent,
    // "etag", not snake_case's "e_tag"
    [property: JsonPropertyName("etag")] string? ETag);

/// <summary>
/// Remembers the last poll of the panel's frame, so Settings → Frame can say whether the
/// frame is still checking in. In memory only: after a restart it's empty until the next poll.
/// </summary>
public sealed class FramePolls
{
    private FramePoll? _last;

    public FramePoll? Last => Volatile.Read(ref _last);

    public void Record(FramePoll poll) => Volatile.Write(ref _last, poll);
}
