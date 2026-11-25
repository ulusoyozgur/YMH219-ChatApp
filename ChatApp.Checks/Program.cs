using ChatApp;
using System.Collections.Concurrent;
using System.Net.Sockets;

int passed = 0;
void Check(bool value, string name)
{
    if (!value) throw new Exception("FAIL: " + name);
    Console.WriteLine("PASS: " + name);
    passed++;
}
async Task Until(Func<bool> condition)
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
    while (!condition()) await Task.Delay(20, timeout.Token);
}
var server = new ChatServer();
var first = new ChatClient();
var second = new ChatClient();
var messages1 = new ConcurrentQueue<ChatMessage>();
var messages2 = new ConcurrentQueue<ChatMessage>();
var received = new ConcurrentQueue<ChatMessage>();
first.MessageReceived += messages1.Enqueue;
second.MessageReceived += messages2.Enqueue;
server.MessageReceived += received.Enqueue;
try
{
    Check(!ChatServer.ValidName(" ") && !ChatServer.ValidName("a\nb") && ChatServer.ValidName("Özgür"), "Username validation");
    server.Start(0);
    int port = server.Port;
    Check(server.IsRunning && port > 0, "Server starts");
    var conflict = new ChatServer();
    bool rejected = false;
    try { conflict.Start(port); } catch (SocketException) { rejected = true; }
    finally { await conflict.StopAsync(); }
    Check(rejected, "Occupied port rejected");
    await first.ConnectAsync("127.0.0.1", port, "Özgür");
    await second.ConnectAsync("127.0.0.1", port, "Deneme");
    await Until(() => server.Clients.Length == 2);
    Check(first.IsConnected && second.IsConnected, "Two clients connected with names");
    await first.SendAsync("Merhaba çığ öşü 😀");
    await Until(() => messages1.Count == 1 && messages2.Count == 1 && received.Count == 1);
    Check(messages2.First().Text == "Merhaba çığ öşü 😀" && messages2.First().Name == "Özgür", "Unicode broadcast and sender identity");
    await second.SendAsync("Yanıt");
    await server.SendAsync("Sunucu yanıtı");
    await Until(() => messages1.Count == 3 && messages2.Count == 3);
    Check(messages1.Any(m => m.Name == "Deneme") && messages1.Any(m => m.Name == "Sunucu"), "Client and server replies");
    Check(messages1.All(m => m.Time > DateTimeOffset.Now.AddMinutes(-1)), "Timestamps transmitted");
    rejected = false;
    try { await first.SendAsync("  "); } catch (ArgumentException) { rejected = true; }
    Check(rejected, "Empty messages rejected");
    rejected = false;
    try { await first.SendAsync(new string('a', 2001)); } catch (ArgumentException) { rejected = true; }
    Check(rejected, "Long messages rejected");
    await Task.WhenAll(Enumerable.Range(0, 30).Select(i => (i % 2 == 0 ? first : second).SendAsync("Mesaj " + i)));
    await Until(() => messages1.Count == 33 && messages2.Count == 33);
    Check(messages1.Count == 33, "Concurrent sends preserve packet boundaries");
    await second.DisconnectAsync();
    await Until(() => server.Clients.Length == 1);
    Check(!second.IsConnected, "Disconnect removes client");
    await second.ConnectAsync("127.0.0.1", port, "Tekrar");
    await Until(() => server.Clients.Length == 2);
    Check(second.IsConnected, "Reconnect succeeds");
    using (var malformed = new TcpClient())
    {
        await malformed.ConnectAsync("127.0.0.1", port);
        await malformed.GetStream().WriteAsync(new byte[] { 0x7f, 0xff, 0xff, 0xff });
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        int length = await malformed.GetStream().ReadAsync(new byte[1], timeout.Token);
        Check(length == 0, "Oversized packet disconnected");
    }
    await server.StopAsync();
    await Until(() => !first.IsConnected && !second.IsConnected);
    Check(server.Clients.Length == 0, "Server shutdown disconnects all clients");
    await first.DisconnectAsync();
    await second.DisconnectAsync();
    rejected = false;
    try { await first.ConnectAsync("127.0.0.1", port, "Özgür"); }
    catch (SocketException) { rejected = true; }
    Check(rejected && !first.IsConnected, "Unavailable server handled");
    server.Start(port);
    await first.ConnectAsync("127.0.0.1", port, "Son");
    Check(first.IsConnected, "Server restart and reconnect");
}
finally
{
    await first.DisconnectAsync();
    await second.DisconnectAsync();
    await server.StopAsync();
}
Console.WriteLine($"All {passed} checks passed.");
