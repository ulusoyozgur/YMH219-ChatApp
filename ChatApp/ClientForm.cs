namespace ChatApp;

public sealed class ClientForm : ChatWindow
{
    private readonly ChatClient client = new();
    private readonly TextBox host = new() { Text = "127.0.0.1", Width = 105 };
    private readonly TextBox name = new() { PlaceholderText = "Kullanıcı adı", MaxLength = 24, Width = 140 };
    private readonly Button connect = MakeButton("Bağlan");
    private readonly Button disconnect = MakeButton("Bağlantıyı Kes");

    public ClientForm(int port = 5000) : base("YMH219 Chat — İstemci")
    {
        PortInput.Value = port;
        Toolbar.Controls.AddRange([host, PortInput, name, connect, disconnect]);
        client.MessageReceived += message => Ui(() => Log(message.ToString()));
        client.StatusChanged += message => Ui(() =>
        {
            Status.Text = message;
            Log($"[{DateTime.Now:HH:mm:ss}] {message}");
            RefreshState();
        });
        connect.Click += async (_, _) => await RunAsync(async () =>
        {
            Status.Text = "Bağlanıyor…";
            await client.ConnectAsync(host.Text.Trim(), (int)PortInput.Value, name.Text);
            Text = $"YMH219 Chat — {name.Text.Trim()}";
        });
        disconnect.Click += async (_, _) => await RunAsync(async () =>
        {
            await client.DisconnectAsync();
            Status.Text = "Bağlantı kesildi";
        });
        SendButton.Click += async (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(MessageInput.Text)) return;
            await RunAsync(async () => { await client.SendAsync(MessageInput.Text); MessageInput.Clear(); });
        };
        FormClosing += async (_, e) =>
        {
            if (IsClosing) return;
            e.Cancel = true;
            if (Busy) return;
            IsClosing = true;
            Enabled = false;
            await client.DisconnectAsync();
            Close();
        };
        RefreshState();
    }
    protected override void RefreshState()
    {
        connect.Enabled = !Busy && !client.IsConnected;
        disconnect.Enabled = !Busy && client.IsConnected;
        SendButton.Enabled = !Busy && client.IsConnected;
        host.Enabled = name.Enabled = PortInput.Enabled = !Busy && !client.IsConnected;
    }
}

