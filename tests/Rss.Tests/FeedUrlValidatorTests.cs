using System.Net;
using System.Net.Sockets;
using Xunit;

namespace Rss.Tests;

public sealed class FeedUrlValidatorTests
{
    /// <summary>A validator whose DNS resolution always fails — literal-IP and scheme tests must never hit DNS.</summary>
    private static FeedUrlValidator MakeValidator() =>
        new(_ => throw new SocketException((int)SocketError.HostNotFound));

    private static FeedUrlValidator MakeValidator(params string[] resolvedIps) =>
        new(_ => Task.FromResult(resolvedIps.Select(IPAddress.Parse).ToArray()));

    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://example.com/feed")]
    [InlineData("gopher://example.com/")]
    [InlineData("javascript:alert(1)")]
    public async Task ResolvePinnedAddressAsync_NonHttpScheme_ReturnsNull(string url)
    {
        Assert.Null(await MakeValidator().ResolvePinnedAddressAsync(url));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a url")]
    [InlineData("/relative/path")]
    [InlineData("example.com/feed")]
    [InlineData("http://")]
    public async Task ResolvePinnedAddressAsync_RelativeOrMalformedUrl_ReturnsNull(string url)
    {
        Assert.Null(await MakeValidator().ResolvePinnedAddressAsync(url));
    }

    [Theory]
    [InlineData("http://127.0.0.1/feed")]
    [InlineData("http://127.8.9.10:8081/feed")]
    [InlineData("https://0.0.0.0/feed")]
    [InlineData("http://10.0.0.5/feed")]
    [InlineData("http://100.64.0.1/feed")]
    [InlineData("http://172.16.0.1/feed")]
    [InlineData("http://172.31.255.254/feed")]
    [InlineData("http://192.168.1.20/feed")]
    [InlineData("http://169.254.169.254/latest/meta-data/")]
    [InlineData("http://198.18.0.1/feed")]
    [InlineData("http://224.0.0.1/feed")]
    [InlineData("http://255.255.255.255/feed")]
    public async Task ResolvePinnedAddressAsync_LiteralPrivateOrReservedIpv4_ReturnsNull(string url)
    {
        Assert.Null(await MakeValidator().ResolvePinnedAddressAsync(url));
    }

    [Theory]
    [InlineData("http://[::1]/feed")]
    [InlineData("http://[::]/feed")]
    [InlineData("http://[fe80::1]/feed")]
    [InlineData("http://[fc00::1]/feed")]
    [InlineData("http://[fd12:3456::1]/feed")]
    [InlineData("http://[ff02::1]/feed")]
    [InlineData("http://[::ffff:127.0.0.1]/feed")]
    [InlineData("http://[::ffff:192.168.1.1]/feed")]
    [InlineData("http://[::ffff:169.254.169.254]/feed")]
    public async Task ResolvePinnedAddressAsync_LiteralNonPublicIpv6_ReturnsNull(string url)
    {
        Assert.Null(await MakeValidator().ResolvePinnedAddressAsync(url));
    }

    [Theory]
    [InlineData("http://8.8.8.8/feed")]
    [InlineData("https://93.184.216.34/feed")]
    [InlineData("http://172.15.0.1/feed")]
    [InlineData("http://172.32.0.1/feed")]
    [InlineData("https://[2606:2800:220:1:248:1893:25c8:1946]/feed")]
    public async Task ResolvePinnedAddressAsync_LiteralPublicIp_ReturnsAddress(string url)
    {
        Assert.NotNull(await MakeValidator().ResolvePinnedAddressAsync(url));
    }

    [Fact]
    public async Task ResolvePinnedAddressAsync_HostnameResolvesToPublicIp_ReturnsAddress()
    {
        var validator = MakeValidator("93.184.216.34");

        var result = await validator.ResolvePinnedAddressAsync("https://example.com/feed");

        Assert.Equal(IPAddress.Parse("93.184.216.34"), result);
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.1.2.3")]
    [InlineData("169.254.169.254")]
    [InlineData("192.168.0.10")]
    [InlineData("::1")]
    [InlineData("fd00::1")]
    public async Task ResolvePinnedAddressAsync_HostnameResolvesToNonPublicIp_ReturnsNull(string resolvedIp)
    {
        var validator = MakeValidator(resolvedIp);

        Assert.Null(await validator.ResolvePinnedAddressAsync("https://evil.example.com/feed"));
    }

    [Fact]
    public async Task ResolvePinnedAddressAsync_HostnameResolvesToMixedPublicAndPrivateIps_ReturnsNull()
    {
        // DNS rebinding style: one public record plus one internal record must still be rejected.
        var validator = MakeValidator("93.184.216.34", "192.168.1.1");

        Assert.Null(await validator.ResolvePinnedAddressAsync("https://evil.example.com/feed"));
    }

    [Fact]
    public async Task ResolvePinnedAddressAsync_DnsResolutionFails_ReturnsNull()
    {
        Assert.Null(await MakeValidator().ResolvePinnedAddressAsync("https://does-not-resolve.example.com/feed"));
    }

    [Fact]
    public async Task ResolvePinnedAddressAsync_DnsResolvesToNoAddresses_ReturnsNull()
    {
        var validator = new FeedUrlValidator(_ => Task.FromResult(Array.Empty<IPAddress>()));

        Assert.Null(await validator.ResolvePinnedAddressAsync("https://empty.example.com/feed"));
    }
}
