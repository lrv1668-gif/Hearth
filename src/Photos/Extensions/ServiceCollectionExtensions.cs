using Microsoft.AspNetCore.Http.Features;
using ServiceDefaults;

namespace Photos.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServicesForPhotos(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<UploadStore>();
        services.AddHttpClient<PhotoFetcher>();

        services.AddKeyedSingleton<IPhotoSource, UnsplashPhotoSource>("unsplash");
        services.AddKeyedSingleton<IPhotoSource, LocalPhotoSource>("local");

        services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 200L * 1024 * 1024);

        services.AddHearthWebDefaults();
    }
}
