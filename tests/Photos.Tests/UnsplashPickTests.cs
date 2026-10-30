using Photos.Records;

namespace Photos.Tests;

/// <summary><see cref="UnsplashPhotoSource.Pick"/> — skipping the photos the frame has shown this pass.</summary>
public sealed class UnsplashPickTests
{
    private static readonly PhotoResponse[] Pool = [Photo("a"), Photo("b"), Photo("c")];

    private static PhotoResponse Photo(string id) => new(id, $"https://images.unsplash.com/{id}", null, null, "Ana", null, "unsplash");

    [Fact]
    public void Pick_SkipsTheExcluded()
    {
        for (var seed = 0; seed < 20; seed++)
            Assert.Equal("b", UnsplashPhotoSource.Pick(Pool, ["a", "c"], new Random(seed)).Id);
    }

    [Fact]
    public void Pick_AllExcluded_AvoidsTheMostRecent()
    {
        var picks = Enumerable.Range(0, 20).Select(seed => UnsplashPhotoSource.Pick(Pool, ["a", "b", "c"], new Random(seed)).Id);

        Assert.DoesNotContain("c", picks);
    }

    [Fact]
    public void Pick_OnePhoto_AlwaysReturnsIt()
    {
        Assert.Equal("a", UnsplashPhotoSource.Pick([Photo("a")], ["a"], new Random(1)).Id);
    }
}
