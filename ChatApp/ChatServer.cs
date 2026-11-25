using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
namespace ChatApp;

public sealed class ChatServer
{
    private readonly ConcurrentDictionary<Guid, Peer> peers = new();
    private readonly ConcurrentBag<Task> sessions = [];
    private TcpListener? listener;
    private CancellationTokenSource? cancellation;
    private Task? acceptTask;
    public bool IsRunning => listener != null;
    public int Port => (listener?.LocalEndpoint as IPEndPoint)?.Port ?? 0;
    public event Action<string>? Activity;
    public event Action<ChatMessage>? MessageReceived;
    public event Action? ClientsChanged;
    public string[] Clients => peers.Values.Where(p => p.Name != null).Select(p => $"{p.Name} — {p.Address}").Order().ToArray();

    public void Start(int port)
    {
        if (IsRunning) return;
        var candidate = new TcpListener(IPAddress.Loopback, port);
        candidate.Start();
        listener = candidate;
        cancellation = new CancellationTokenSource();
        Activity?.Invoke($"Sunucu 127.0.0.1:{Port} adresinde başlatıldı.");
        acceptTask = AcceptAsync(candidate, cancellation.Token);
    }
    private async Task AcceptAsync(TcpListener server, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var socket = await server.AcceptTcpClientAsync(token).ConfigureAwait(false);
                var peer = new Peer(socket);
                peers[peer.Id] = peer;
                sessions.Add(HandleAsync(peer, token));
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or SocketException or ObjectDisposedException)
        {
            if (!token.IsCancellationRequested) Activity?.Invoke($"Dinleme hatası: {ex.Message}");
        }
    }
    private async Task HandleAsync(Peer peer, CancellationToken token)
    {
        try
        {
            using var handshake = CancellationTokenSource.CreateLinkedTokenSource(token);
            handshake.CancelAfter(TimeSpan.FromSeconds(10));
            var hello = await Wire.ReadAsync(peer.Socket.GetStream(), handshake.Token).ConfigureAwait(false);
            if (hello?.Kind != "hello" || !ValidName(hello.Name)) throw new InvalidDataException("Geçersiz kullanıcı adı.");
            peer.Name = hello.Name.Trim();
            await peer.SendAsync(new("welcome", "Sunucu", "Bağlantı kuruldu.", DateTimeOffset.Now), token).ConfigureAwait(false);
            ClientsChanged?.Invoke();
            Activity?.Invoke($"{peer.Name} bağlandı ({peer.Address}).");
            while (!token.IsCancellationRequested)
            {
                var incoming = await Wire.ReadAsync(peer.Socket.GetStream(), token).ConfigureAwait(false);
                if (incoming == null) break;
                if (incoming.Kind != "chat" || string.IsNullOrWhiteSpace(incoming.Text) || incoming.Text.Length > 2000)
                    throw new InvalidDataException("Geçersiz mesaj.");
                var message = new ChatMessage("chat", peer.Name, incoming.Text.Trim(), DateTimeOffset.Now);
                MessageReceived?.Invoke(message);
                await BroadcastAsync(message, token).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or SocketException or OperationCanceledException or ObjectDisposedException or System.Text.Json.JsonException)
        {
            if (!token.IsCancellationRequested) Activity?.Invoke($"{peer.Name ?? peer.Address}: bağlantı kapandı ({ex.Message}).");
        }
        finally
        {
            peers.TryRemove(peer.Id, out _);
            peer.Socket.Dispose();
            ClientsChanged?.Invoke();
            if (peer.Name != null) Activity?.Invoke($"{peer.Name} ayrıldı.");
        }
    }
    public static bool ValidName(string? name) => !string.IsNullOrWhiteSpace(name) && name.Trim().Length <= 24 && !name.Any(char.IsControl);
    public async Task SendAsync(string text)
    {
        if (!IsRunning || cancellation == null) throw new InvalidOperationException("Sunucu kapalı.");
        if (string.IsNullOrWhiteSpace(text) || text.Length > 2000) throw new ArgumentException("Mesaj 1–2000 karakter olmalı.");
        var message = new ChatMessage("chat", "Sunucu", text.Trim(), DateTimeOffset.Now);
        MessageReceived?.Invoke(message);
        await BroadcastAsync(message, cancellation.Token);
    }
    private Task BroadcastAsync(ChatMessage message, CancellationToken token) => Task.WhenAll(peers.Values.Where(p => p.Name != null).Select(async peer =>
    {
        try { await peer.SendAsync(message, token).ConfigureAwait(false); }
        catch (Exception ex) when (ex is InvalidDataException or IOException or SocketException or OperationCanceledException or ObjectDisposedException) { peer.Socket.Dispose(); }
    }));
    public async Task StopAsync()
    {
        if (listener == null) return;
        cancellation!.Cancel();
        listener.Stop();
        if (acceptTask != null) await acceptTask;
        foreach (var peer in peers.Values) peer.Socket.Dispose();
        await Task.WhenAll(sessions.ToArray());
        sessions.Clear();
        peers.Clear();
        cancellation.Dispose();
        cancellation = null;
        listener = null;
        acceptTask = null;
        ClientsChanged?.Invoke();
        Activity?.Invoke("Sunucu durduruldu.");
    }
    private sealed class Peer(TcpClient socket)
    {
        private readonly SemaphoreSlim sendLock = new(1, 1);
        public Guid Id { get; } = Guid.NewGuid();
        public TcpClient Socket { get; } = socket;
        public string Address { get; } = socket.Client.RemoteEndPoint?.ToString() ?? "Bilinmiyor";
        public string? Name { get; set; }
        public async Task SendAsync(ChatMessage message, CancellationToken token)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));
            await sendLock.WaitAsync(timeout.Token).ConfigureAwait(false);
            try { await Wire.WriteAsync(Socket.GetStream(), message, timeout.Token).ConfigureAwait(false); }
            finally { sendLock.Release(); }
        }
    }
}

