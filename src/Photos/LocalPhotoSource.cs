using Photos.Records;

namespace Photos;

public sealed class LocalPhotoSource(UploadStore uploads) : IPhotoSource
{
    public string Key => "local";

    public Task<PhotoResponse?> GetRandomAsync(PhotoSourceContext ctx)
    {
        if (!uploads.TryGetRandom(out var photo))
            return Task.FromResult<PhotoResponse?>(null);

        return Task.FromResult<PhotoResponse?>(
            new PhotoResponse(photo!.Id, photo.Url, photo.ThumbUrl, photo.Caption, null, null, "local"));
    }
}
