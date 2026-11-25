namespace ChatApp;

public abstract class ChatWindow : Form
{
    protected readonly FlowLayoutPanel Toolbar = new() { Dock = DockStyle.Fill, AutoScroll = true };
    protected readonly Label Status = new() { Dock = DockStyle.Fill, Text = "Bağlantı yok", AutoEllipsis = true };
    protected readonly ListBox History = new() { Dock = DockStyle.Fill, HorizontalScrollbar = true, IntegralHeight = false };
    protected readonly TextBox MessageInput = new() { Dock = DockStyle.Fill, MaxLength = 2000 };
    protected readonly Button SendButton = new() { Text = "Gönder", Dock = DockStyle.Fill, Enabled = false };
    protected readonly NumericUpDown PortInput = new() { Minimum = 1024, Maximum = 65535, Value = 5000, Width = 85 };
    protected readonly TableLayoutPanel Content = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 1 };
    protected bool Busy;
    protected bool IsClosing;

    protected ChatWindow(string title)
    {
        Text = title;
        Font = new Font("Segoe UI", 10);
        ClientSize = new Size(940, 580);
        MinimumSize = new Size(800, 500);
        StartPosition = FormStartPosition.CenterScreen;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 5 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.Controls.Add(new Label { Text = title, AutoSize = true, Font = new Font("Segoe UI", 20, FontStyle.Bold) }, 0, 0);
        layout.Controls.Add(Toolbar, 0, 1);
        layout.Controls.Add(Status, 0, 2);
        Content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Content.Controls.Add(History, 0, 0);
        layout.Controls.Add(Content, 0, 3);
        var composer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(0, 8, 0, 0) };
        composer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        composer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        composer.Controls.Add(MessageInput, 0, 0);
        composer.Controls.Add(SendButton, 1, 0);
        layout.Controls.Add(composer, 0, 4);
        Controls.Add(layout);
        AcceptButton = SendButton;
    }

    protected static Button MakeButton(string text) => new() { Text = text, AutoSize = true, Height = 32 };
    protected void Ui(Action action)
    {
        if (IsDisposed || Disposing || !IsHandleCreated) return;
        if (InvokeRequired)
        {
            try { BeginInvoke((Action)(() => { if (!IsDisposed && !Disposing) action(); })); }
            catch (InvalidOperationException) { }
        }
        else action();
    }
    protected void Log(string message)
    {
        if (History.Items.Count >= 1000) History.Items.RemoveAt(0);
        History.Items.Add(message);
        History.TopIndex = History.Items.Count - 1;
    }
    protected async Task RunAsync(Func<Task> action)
    {
        if (Busy || IsClosing) return;
        Busy = true;
        RefreshState();
        try { await action(); }
        catch (Exception ex) when (ex is System.Net.Sockets.SocketException or InvalidDataException or IOException or OperationCanceledException
            or ArgumentException or InvalidOperationException or System.Text.Json.JsonException)
        {
            Status.Text = "İşlem tamamlanamadı.";
            Log($"[{DateTime.Now:HH:mm:ss}] Hata: {ex.Message}");
            if (!IsClosing) MessageBox.Show(this, "Bağlantıyı, sunucu durumunu ve portu kontrol edin.\n" + ex.Message,
                "İşlem hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally { Busy = false; RefreshState(); }
    }
    protected abstract void RefreshState();
}

