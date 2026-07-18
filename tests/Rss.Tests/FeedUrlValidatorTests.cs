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
    public async Task IsAllowedAsync_NonHttpScheme_ReturnsFalse(string url)
    {
        Assert.False(await MakeValidator().IsAllowedAsync(url));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a url")]
    [InlineData("/relative/path")]
    [InlineData("example.com/feed")]
    [InlineData("http://")]
    public async Task IsAllowedAsync_RelativeOrMalformedUrl_ReturnsFalse(string url)
    {
        Assert.False(await MakeValidator().IsAllowedAsync(url));
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
    public async Task IsAllowedAsync_LiteralPrivateOrReservedIpv4_ReturnsFalse(string url)
    {
        Assert.False(await MakeValidator().IsAllowedAsync(url));
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
    public async Task IsAllowedAsync_LiteralNonPublicIpv6_ReturnsFalse(string url)
    {
        Assert.False(await MakeValidator().IsAllowedAsync(url));
    }

    [Theory]
    [InlineData("http://8.8.8.8/feed")]
    [InlineData("https://93.184.216.34/feed")]
    [InlineData("http://172.15.0.1/feed")]
    [InlineData("http://172.32.0.1/feed")]
    [InlineData("https://[2606:2800:220:1:248:1893:25c8:1946]/feed")]
    public async Task IsAllowedAsync_LiteralPublicIp_ReturnsTrue(string url)
    {
        Assert.True(await MakeValidator().IsAllowedAsync(url));
    }

    [Fact]
    public async Task IsAllowedAsync_HostnameResolvesToPublicIp_ReturnsTrue()
    {
        var validator = MakeValidator("93.184.216.34");

        Assert.True(await validator.IsAllowedAsync("https://example.com/feed"));
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.1.2.3")]
    [InlineData("169.254.169.254")]
    [InlineData("192.168.0.10")]
    [InlineData("::1")]
    [InlineData("fd00::1")]
    public async Task IsAllowedAsync_HostnameResolvesToNonPublicIp_ReturnsFalse(string resolvedIp)
    {
        var validator = MakeValidator(resolvedIp);

        Assert.False(await validator.IsAllowedAsync("https://evil.example.com/feed"));
    }

    [Fact]
    public async Task IsAllowedAsync_HostnameResolvesToMixedPublicAndPrivateIps_ReturnsFalse()
    {
        // DNS rebinding style: one public record plus one internal record must still be rejected.
        var validator = MakeValidator("93.184.216.34", "192.168.1.1");

        Assert.False(await validator.IsAllowedAsync("https://evil.example.com/feed"));
    }

    [Fact]
    public async Task IsAllowedAsync_DnsResolutionFails_ReturnsFalse()
    {
        Assert.False(await MakeValidator().IsAllowedAsync("https://does-not-resolve.example.com/feed"));
    }

    [Fact]
    public async Task IsAllowedAsync_DnsResolvesToNoAddresses_ReturnsFalse()
    {
        var validator = new FeedUrlValidator(_ => Task.FromResult(Array.Empty<IPAddress>()));

        Assert.False(await validator.IsAllowedAsync("https://empty.example.com/feed"));
    }
}
