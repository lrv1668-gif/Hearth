namespace Photos.Extensions;

public static class WebApplicationExtensions
{
    private static readonly string[] KnownSources = ["unsplash", "local"];

    public static void InitializeWebAppForPhotos(this WebApplication app)
    {
        app.AddPhotoEndpoints();
    }

    private static void AddPhotoEndpoints(this WebApplication app)
    {
        app.MapGet("/photos/sources", (IServiceProvider sp) =>
        {
            var available = KnownSources
                .Where(k => sp.GetKeyedService<IPhotoSource>(k) is not null)
                .ToArray();
            return Results.Ok(available);
        });

        app.MapGet("/photos/random", async (
            string? source,
            string? orientation,
            string? query,
            IServiceProvider sp) =>
        {
            var key = source ?? "unsplash";
            var provider = sp.GetKeyedService<IPhotoSource>(key);
            if (provider is null)
                return Results.BadRequest(new { error = $"unknown source: {key}" });

            var ctx = new PhotoSourceContext(orientation ?? "portrait", query ?? "nature");
            var photo = await provider.GetRandomAsync(ctx);
            return photo is null ? Results.NotFound() : Results.Ok(photo);
        });

        app.MapPost("/photos/uploads", async (HttpRequest req, UploadStore uploads) =>
        {
            if (!req.HasFormContentType)
                return Results.BadRequest(new { error = "multipart/form-data required" });

            var form = await req.ReadFormAsync();
            if (form.Files.Count == 0)
                return Results.BadRequest(new { error = "no files provided" });

            var tasks = form.Files.Select(async file =>
            {
                if (file.Length == 0)
                    return new BatchFileResult(file.FileName, "error", "file is empty", null);

                if (file.Length > 25 * 1024 * 1024)
                    return new BatchFileResult(file.FileName, "error", "file exceeds 25 MB limit", null);

                var ext = Path.GetExtension(file.FileName);
                if (!uploads.IsAllowedExtension(ext))
                    return new BatchFileResult(file.FileName, "error", "only JPEG, PNG, and WebP are accepted", null);

                try
                {
                    var photo = await uploads.SaveAsync(file);
                    return photo is null
                        ? new BatchFileResult(file.FileName, "duplicate", null, null)
                        : new BatchFileResult(file.FileName, "ok", null, photo);
                }
                catch (InvalidDataException ex)
                {
                    return new BatchFileResult(file.FileName, "error", ex.Message, null);
                }
            });

            var results = await Task.WhenAll(tasks);
            return Results.Ok(results);
        });

        app.MapGet("/photos/uploads", (UploadStore uploads) =>
        {
            var list = uploads.List().Select(p => new { p.Id, p.Url, p.ThumbUrl });
            return Results.Ok(list);
        });

        app.MapDelete("/photos/uploads/{id}", (string id, UploadStore uploads) =>
            uploads.Delete(id) ? Results.NoContent() : Results.NotFound());

        app.MapGet("/photos/files/{filename}", (string filename, UploadStore uploads, HttpContext ctx) =>
        {
            if (filename.Contains("..") || filename.Contains('/') || filename.Contains('\\'))
                return Results.BadRequest();

            var path = uploads.GetFilePath(filename);
            if (!File.Exists(path)) return Results.NotFound();

            var ext = Path.GetExtension(filename).ToLowerInvariant();
            var contentType = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png"            => "image/png",
                ".webp"           => "image/webp",
                _                 => "application/octet-stream",
            };

            ctx.Response.Headers.CacheControl = "public, max-age=86400";

            return Results.File(
                path,
                contentType,
                enableRangeProcessing: true,
                lastModified: File.GetLastWriteTimeUtc(path),
                entityTag: new Microsoft.Net.Http.Headers.EntityTagHeaderValue($"\"{Path.GetFileNameWithoutExtension(filename)}\""));
        });
    }
}
