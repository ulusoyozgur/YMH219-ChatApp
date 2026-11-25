using System.Net;
using System.Net.Sockets;

namespace ChatApp;

public sealed class ChatServer
{
    private TcpListener? listener;
    private CancellationTokenSource? cancellation;
    private Task? acceptTask;
    private readonly List<TcpClient> clients = [];
    public bool IsRunning => listener != null;
    public event Action<string>? Activity;

    public void Start(int port)
    {
        if (IsRunning) return;
        var candidate = new TcpListener(IPAddress.Loopback, port);
        candidate.Start();
        listener = candidate;
        cancellation = new CancellationTokenSource();
        Activity?.Invoke($"Sunucu 127.0.0.1:{port} adresinde başlatıldı.");
        acceptTask = AcceptClientsAsync(candidate, cancellation.Token);
    }

    private async Task AcceptClientsAsync(TcpListener server, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var client = await server.AcceptTcpClientAsync(token);
                clients.Add(client);
                Activity?.Invoke($"TCP bağlantısı kabul edildi: {client.Client.RemoteEndPoint}");
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (SocketException) when (token.IsCancellationRequested) { }
        catch (ObjectDisposedException) when (token.IsCancellationRequested) { }
        catch (Exception ex) when (ex is SocketException or IOException)
        {
            Activity?.Invoke($"Dinleme hatası: {ex.Message}");
        }
    }

    public async Task StopAsync()
    {
        if (listener == null) return;
        cancellation!.Cancel();
        listener.Stop();
        if (acceptTask != null) await acceptTask;
        foreach (var client in clients) client.Dispose();
        clients.Clear();
        cancellation.Dispose();
        cancellation = null;
        listener = null;
        acceptTask = null;
        Activity?.Invoke("Sunucu durduruldu; bağlantılar kapatıldı.");
    }
}
