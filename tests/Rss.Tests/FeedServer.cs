using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Rss.Tests;

/// <summary>A loopback HTTP server answering every request with its feed, or a 500 while failing.</summary>
internal sealed class FeedServer : IDisposable
{
    private readonly string _feed;
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _stop = new();
    private int _requests;

    public FeedServer(string feed)
    {
        _feed = feed;
        _listener.Start();
        _ = Task.Run(ServeAsync);
    }

    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;
    public int Requests => Volatile.Read(ref _requests);
    public volatile bool Failing;

    private async Task ServeAsync()
    {
        while (!_stop.IsCancellationRequested)
        {
            TcpClient client;
            try { client = await _listener.AcceptTcpClientAsync(_stop.Token); }
            catch (OperationCanceledException) { return; }
            _ = Task.Run(async () =>
            {
                using (client)
                {
                    var stream = client.GetStream();
                    using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
                    while (!string.IsNullOrEmpty(await reader.ReadLineAsync())) { } // drain request headers
                    Interlocked.Increment(ref _requests);

                    var (status, body) = Failing ? ("500 Internal Server Error", "") : ("200 OK", _feed);
                    var bytes = Encoding.UTF8.GetBytes(body);
                    await stream.WriteAsync(Encoding.ASCII.GetBytes(
                        $"HTTP/1.1 {status}\r\nContent-Type: application/xml\r\nContent-Length: {bytes.Length}\r\nConnection: close\r\n\r\n"));
                    await stream.WriteAsync(bytes);
                }
            });
        }
    }

    public void Dispose()
    {
        _stop.Cancel();
        _listener.Stop();
        _stop.Dispose();
    }
}
