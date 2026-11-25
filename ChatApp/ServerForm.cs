namespace ChatApp;

public sealed class ServerForm : ChatWindow
{
    private readonly ChatServer server = new();
    private readonly Button start = MakeButton("Sunucuyu Başlat");
    private readonly Button stop = MakeButton("Durdur");
    private readonly Button openClient = MakeButton("İstemci Aç");
    private readonly ListBox clients = new() { Dock = DockStyle.Fill, HorizontalScrollbar = true };

    public ServerForm() : base("YMH219 Chat — Sunucu")
    {
        Toolbar.Controls.AddRange([new Label { Text = "Port", AutoSize = true }, PortInput, start, stop, openClient]);
        Content.ColumnCount = 2;
        Content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 0, 0) };
        panel.Controls.Add(clients);
        panel.Controls.Add(new Label { Text = "Bağlı kullanıcılar / adres", Dock = DockStyle.Top, Height = 28 });
        Content.Controls.Add(panel, 1, 0);
        server.Activity += message => Ui(() => Log($"[{DateTime.Now:HH:mm:ss}] {message}"));
        server.MessageReceived += message => Ui(() => Log(message.ToString()));
        server.ClientsChanged += () => Ui(() =>
        {
            clients.Items.Clear();
            clients.Items.AddRange(server.Clients);
        });
        start.Click += async (_, _) => await RunAsync(() =>
        {
            server.Start((int)PortInput.Value);
            Status.Text = $"Dinleniyor: 127.0.0.1:{server.Port}";
            return Task.CompletedTask;
        });
        stop.Click += async (_, _) => await RunAsync(async () => { await server.StopAsync(); Status.Text = "Sunucu kapalı"; });
        openClient.Click += (_, _) => new ClientForm((int)PortInput.Value).Show();
        SendButton.Click += async (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(MessageInput.Text)) return;
            await RunAsync(async () => { await server.SendAsync(MessageInput.Text); MessageInput.Clear(); });
        };
        FormClosing += async (_, e) =>
        {
            if (IsClosing) return;
            e.Cancel = true;
            if (Busy) return;
            IsClosing = true;
            Enabled = false;
            await server.StopAsync();
            Close();
        };
        RefreshState();
    }
    protected override void RefreshState()
    {
        start.Enabled = !Busy && !server.IsRunning;
        stop.Enabled = !Busy && server.IsRunning;
        PortInput.Enabled = !Busy && !server.IsRunning;
        SendButton.Enabled = !Busy && server.IsRunning;
    }
}

