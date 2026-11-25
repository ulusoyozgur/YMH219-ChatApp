using System.Net.Sockets;
namespace ChatApp;

public sealed class ChatClient
{
    private TcpClient? socket;
    private CancellationTokenSource? cancellation;
    private Task? receiveTask;
    private readonly SemaphoreSlim sendLock = new(1, 1);
    public bool IsConnected { get; private set; }
    public event Action<ChatMessage>? MessageReceived;
    public event Action<string>? StatusChanged;
    public async Task ConnectAsync(string host, int port, string name)
    {
        if (!ChatServer.ValidName(name)) throw new ArgumentException("Kullanıcı adı 1–24 karakter olmalı.");
        await DisconnectAsync();
        var candidate = new TcpClient();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        try
        {
            await candidate.ConnectAsync(host, port, timeout.Token);
            await Wire.WriteAsync(candidate.GetStream(), new("hello", name.Trim(), "", DateTimeOffset.Now), timeout.Token);
            var welcome = await Wire.ReadAsync(candidate.GetStream(), timeout.Token);
            if (welcome?.Kind != "welcome") throw new IOException("Sunucu bağlantıyı kabul etmedi.");
            socket = candidate;
            cancellation = new CancellationTokenSource();
            IsConnected = true;
            StatusChanged?.Invoke("Bağlandı");
            receiveTask = ReceiveAsync(candidate, cancellation.Token);
        }
        catch { candidate.Dispose(); throw; }
    }
    private async Task ReceiveAsync(TcpClient connection, CancellationToken token)
    {
        string reason = "Sunucu bağlantıyı kapattı.";
        try
        {
            while (!token.IsCancellationRequested)
            {
                var message = await Wire.ReadAsync(connection.GetStream(), token).ConfigureAwait(false);
                if (message == null) break;
                if (message.Kind == "chat") MessageReceived?.Invoke(message);
            }
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or SocketException or OperationCanceledException or ObjectDisposedException or System.Text.Json.JsonException)
        { reason = token.IsCancellationRequested ? "Bağlantı kesildi." : "Bağlantı hatası: " + ex.Message; }
        finally
        {
            IsConnected = false;
            connection.Dispose();
            StatusChanged?.Invoke(reason);
        }
    }
    public async Task SendAsync(string text)
    {
        var connection = socket;
        if (!IsConnected || connection == null) throw new InvalidOperationException("Önce bağlanın.");
        if (string.IsNullOrWhiteSpace(text) || text.Length > 2000) throw new ArgumentException("Mesaj 1–2000 karakter olmalı.");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await sendLock.WaitAsync(timeout.Token);
        try { await Wire.WriteAsync(connection.GetStream(), new("chat", "", text.Trim(), DateTimeOffset.Now), timeout.Token); }
        catch { connection.Dispose(); throw; }
        finally { sendLock.Release(); }
    }
    public async Task DisconnectAsync()
    {
        cancellation?.Cancel();
        socket?.Dispose();
        if (receiveTask != null) await receiveTask;
        cancellation?.Dispose();
        cancellation = null;
        socket = null;
        receiveTask = null;
        IsConnected = false;
    }
}

