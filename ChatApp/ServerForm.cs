using System.Net.Sockets;

namespace ChatApp;

public sealed class ServerForm : Form
{
    private readonly ChatServer server = new();
    private readonly NumericUpDown port = new() { Minimum = 1024, Maximum = 65535, Value = 5000, Width = 95 };
    private readonly Button start = new() { Text = "Sunucuyu Başlat", AutoSize = true };
    private readonly Button stop = new() { Text = "Durdur", AutoSize = true, Enabled = false };
    private readonly Label status = new() { Text = "Sunucu kapalı", AutoSize = true };
    private readonly ListBox events = new() { Dock = DockStyle.Fill, HorizontalScrollbar = true };
    private bool closing;

    public ServerForm()
    {
        Text = "YMH219 Chat — Sunucu";
        ClientSize = new Size(800, 520);
        MinimumSize = new Size(700, 450);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 11);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), RowCount = 4, ColumnCount = 1 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label { Text = "Chat Sunucusu", Font = new Font("Segoe UI", 22, FontStyle.Bold), AutoSize = true }, 0, 0);
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill };
        toolbar.Controls.AddRange([new Label { Text = "Port", AutoSize = true }, port, start, stop]);
        layout.Controls.Add(toolbar, 0, 1);
        layout.Controls.Add(status, 0, 2);
        layout.Controls.Add(events, 0, 3);
        Controls.Add(layout);
        server.Activity += message =>
        {
            if (events.Items.Count >= 1000) events.Items.RemoveAt(0);
            events.Items.Add($"{DateTime.Now:HH:mm:ss}  {message}");
            events.TopIndex = events.Items.Count - 1;
        };
        start.Click += (_, _) =>
        {
            try { server.Start((int)port.Value); RefreshState(); }
            catch (SocketException)
            {
                MessageBox.Show(this, "Sunucu başlatılamadı. Port kullanımda olabilir; başka bir port deneyin.", "Bağlantı hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
        stop.Click += async (_, _) =>
        {
            stop.Enabled = false;
            await server.StopAsync();
            RefreshState();
        };
        FormClosing += async (_, e) =>
        {
            if (closing) return;
            e.Cancel = true;
            Enabled = false;
            await server.StopAsync();
            closing = true;
            Close();
        };
    }

    private void RefreshState()
    {
        start.Enabled = !server.IsRunning;
        stop.Enabled = server.IsRunning;
        port.Enabled = !server.IsRunning;
        status.Text = server.IsRunning ? $"Dinleniyor: 127.0.0.1:{port.Value}" : "Sunucu kapalı";
    }
}
