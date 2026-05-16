using Photos.Records;

namespace Photos;

public sealed class UnsplashCache
{
    private readonly Lock _lock = new();

    private List<PhotoResponse> _photos = [];
    private string _query = string.Empty;
    private string _orientation = string.Empty;
    private DateTime _fetchedAt = DateTime.MinValue;

    public bool TryGet(string query, string orientation, out List<PhotoResponse> photos)
    {
        lock (_lock)
        {
            if (_photos.Count > 0
                && string.Equals(_query, query, StringComparison.OrdinalIgnoreCase)
                && string.Equals(_orientation, orientation, StringComparison.OrdinalIgnoreCase)
                && (DateTime.UtcNow - _fetchedAt).TotalHours < 24)
            {
                photos = _photos;
                return true;
            }
            photos = [];
            return false;
        }
    }

    public void Set(List<PhotoResponse> photos, string query, string orientation)
    {
        lock (_lock)
        {
            _photos = photos;
            _query = query;
            _orientation = orientation;
            _fetchedAt = DateTime.UtcNow;
        }
    }
}
